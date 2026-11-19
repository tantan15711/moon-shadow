using System.Collections.Generic;
using UnityEngine;

namespace MoonShadow
{
    public enum HitResponse { Ignored, Damaged, Parried }

    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(KyleCombat2D))]
    public sealed partial class KyleController2D : MonoBehaviour
    {
        [SerializeField] private LayerMask terrain;
        [SerializeField] private SpriteRenderer view;
        [SerializeField] private float speed = 7.1f;
        [SerializeField] private float jumpSpeed = 12.4f;
        private Rigidbody2D body;
        private BoxCollider2D hull;
        private KyleInput input;
        private KyleCombat2D combat;
        private RuinsDemo demo;
        private float move;
        private float jumpBuffer;
        private float coyote;
        private float dashTime;
        private float dashCooldown;
        private float dashBuffer;
        private float hurtTime;
        private float trailTime;
        private bool grounded;
        private bool airDashUsed;
        private bool jumpCut;
        private bool airSliceUsed;
        private bool airJumpUsed;
        private float shotCooldown;
        private float shotPose;
        private float reloadTime;
        private Sidearm reloadWeapon;
        private CastRig rig;
        private readonly HashSet<EnemyBase2D> hitByDash = new();
        private Vector2 dashFrom;
        private bool dashSweepPending;
        private float stamina = 100f;
        private float staminaDelay;
        private SpriteRenderer equippedArt;
        private Sidearm displayedWeapon;
        private Camera aimCamera;
        private Vector2 shotDirection = Vector2.right;
        private Vector2 WeaponPivot => Position + new Vector2(Facing * 0.22f, 0.40f);

        public const int MaxHealth = 6;
        public float MaxStamina => 100f + CampaignProgress.Active.stamina * 20f;
        public float Stamina => stamina;
        public int Health { get; private set; } = MaxHealth;
        public int CurrentMaxHealth => MaxHealth + (demo ? demo.BonusHealth : 0);
        public int Facing { get; private set; } = 1;
        public bool AirDashUnlocked { get; private set; }
        public bool DoubleJumpUnlocked { get; private set; }
        public bool Dashing => dashTime > 0f;
        public bool Grounded => grounded;
        public bool Reloading => reloadTime > 0f;
        internal bool ShowingAim => !Dashing && !Reloading && shotPose > 0f && !combat.Attacking;
        internal Vector2 AimMarker => WeaponPivot + shotDirection * 2.6f;
        public float ReloadProgress => Reloading ? 1f - reloadTime / SidearmCatalog.Get(reloadWeapon).ReloadSeconds : 0f;
        public Vector2 Position => body.position;
        internal Vector2 Velocity => body.linearVelocity;
        public Bounds Hurtbox => hull.bounds;
        public bool HasDirection => Mathf.Abs(move) > 0.18f;
        public int AttackDirection => HasDirection ? (move > 0f ? 1 : -1) : Facing;
        internal KyleInput Input => input;

        public void Configure(LayerMask mask, SpriteRenderer visual)
        {
            terrain = mask;
            view = visual;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hull = GetComponent<BoxCollider2D>();
            combat = GetComponent<KyleCombat2D>();
            input = new KyleInput();
            rig = view.GetComponent<CastRig>();
        }

        private void Start()
        {
            demo = FindAnyObjectByType<RuinsDemo>();
            aimCamera = Camera.main;
            FullHeal();
        }
        private void OnDestroy() => input?.Dispose();

        private void Update()
        {
            if (!demo.Playing || demo.ResumedThisFrame)
                return;

            float delta = Time.deltaTime;
            move = input.Move.ReadValue<float>();
            if (Mathf.Abs(move) > 0.1f && !Dashing && !combat.Attacking && hurtTime <= 0)
                Facing = move > 0f ? 1 : -1;

            jumpBuffer = input.Jump.WasPressedThisFrame() && !demo.ResumedThisFrame ? 0.13f : jumpBuffer - delta;
            if (input.Jump.WasReleasedThisFrame())
                jumpCut = true;

            dashCooldown -= delta;
            shotCooldown -= delta;
            shotPose -= delta;
            hurtTime -= delta;
            staminaDelay -= delta;
            if (staminaDelay <= 0f)
                stamina = Mathf.MoveTowards(stamina, MaxStamina, delta * 37f);
            dashBuffer = input.Dash.WasPressedThisFrame() ? 0.17f : dashBuffer - delta;
            if (dashBuffer > 0f && !Dashing && dashCooldown <= 0f && combat.CanEvade &&
                hurtTime <= 0f && stamina >= 23f && (grounded || (AirDashUnlocked && !airDashUsed)))
            {
                stamina -= 23f;
                staminaDelay = 0.36f;
                dashBuffer = 0f;
                combat.CancelAttack();
                CancelReload();
                Facing = AttackDirection;
                airDashUsed |= !grounded;
                dashTime = 0.20f;
                hitByDash.Clear();
                dashSweepPending = false;
                dashCooldown = 0.34f;
                trailTime = 0f;
                demo.Effects.DashSparks(Position - Vector2.right * Facing * 0.42f, Facing, 7);
                demo.Effects.Sound(2);
            }

            combat.ReadFrame(hurtTime <= 0f && !Dashing);
            if (combat.Attacking || combat.Parrying || reloadWeapon != demo.Weapon)
                CancelReload();
            if (Reloading)
            {
                reloadTime = Mathf.Max(0f, reloadTime - delta);
                if (!Reloading)
                {
                    demo.Reload(reloadWeapon);
                    demo.Effects.ReloadSound(true);
                }
            }
            if (!demo.ResumedThisFrame && input.Reload.WasPressedThisFrame() && hurtTime <= 0f &&
                !Dashing && !combat.Attacking && !combat.Parrying)
                BeginReload();
            bool trigger = input.Fire.IsPressed();
            if (ShowingAim && trigger)
                UpdateAim();
            if (trigger && !demo.ResumedThisFrame && hurtTime <= 0f && !Dashing && !Reloading && shotCooldown <= 0f &&
                combat.CanCancel && !combat.Parrying)
                Fire();
        }

        private void FixedUpdate()
        {
            if (!demo || !demo.Playing)
                return;

            if (dashSweepPending)
            {
                StrikeAlongDash(dashFrom, Position);
                dashSweepPending = false;
            }

            var bounds = hull.bounds;
            Vector2 feet = new(bounds.center.x, bounds.min.y + 0.04f);
            Vector2 test = new(bounds.size.x * 0.83f, 0.06f);
            var support = Physics2D.BoxCast(feet, test, 0f, Vector2.down, 0.15f, terrain).collider;
            var lift = support ? support.GetComponent<MovingLedge>() : null;
            float liftSpeed = lift ? lift.VerticalSpeed : 0f;
            bool onFloor = support && body.linearVelocity.y <= liftSpeed + (lift ? 0.48f : 0.1f);
            if (onFloor)
            {
                coyote = 0.095f;
                airDashUsed = false;
                airSliceUsed = false;
                airJumpUsed = false;
            }
            else
                coyote -= Time.fixedDeltaTime;
            grounded = onFloor;

            Vector2 velocity = body.linearVelocity;
            if (onFloor && lift && hurtTime <= 0f)
                velocity.y = liftSpeed;
            if (Dashing)
            {
                dashFrom = Position;
                dashSweepPending = true;
                dashTime -= Time.fixedDeltaTime;
                trailTime -= Time.fixedDeltaTime;
                if (trailTime <= 0f)
                {
                    demo.Effects.Ghost(rig);
                    demo.Effects.DashSparks(Position - Vector2.right * Facing * 0.45f, Facing, 3);
                    trailTime = 0.045f;
                }
                body.gravityScale = 0f;
                body.linearVelocity = new Vector2(Facing * 21f, 0f);
                return;
            }

            body.gravityScale = (velocity.y < 0f ? 3.65f : 3.0f) * demo.Gravity;
            if (hurtTime <= 0f)
            {
                float walk = input.Walk.IsPressed() ? 0.44f : 1f;
                float pace = (grounded ? speed * 0.79f : speed) + CampaignProgress.Active.speed * 0.38f;
                float targetSpeed = move * pace * combat.MoveFactor * walk;
                if (shotPose > 0f && displayedWeapon == Sidearm.Incinerator)
                    targetSpeed *= 0.62f;
                if (Reloading)
                    targetSpeed *= 0.78f;
                float acceleration = grounded ? (Mathf.Abs(move) < 0.05f ? 54f : 49f) : 43f;
                velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
                bool canAirJump = DoubleJumpUnlocked && !airJumpUsed;
                if (jumpBuffer > 0f && combat.CanCancel && (coyote > 0f || canAirJump))
                {
                    combat.CancelAttack();
                    bool airJump = coyote <= 0f;
                    airJumpUsed |= airJump;
                    velocity.y = airJump ? jumpSpeed * 0.89f : jumpSpeed;
                    if (!airJump && combat.TryFollowLaunch(out float approach))
                    {
                        velocity.x = approach;
                        velocity.y = jumpSpeed * 0.89f;
                    }
                    grounded = false;
                    coyote = 0f;
                    jumpBuffer = 0f;
                    jumpCut = !input.Jump.IsPressed();
                    demo.Effects.Burst(feet, airJump ? new Color(0.59f, 0.83f, 1f) :
                        new Color(0.41f, 0.51f, 0.62f), airJump ? 13 : 7, 2.5f);
                }
            }

            if (jumpCut && velocity.y > 6.2f)
                velocity.y = 6.2f;
            jumpCut = false;
            body.linearVelocity = velocity;
        }

        private void StrikeAlongDash(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.0001f)
                return;
            var ray = new Ray(from, delta.normalized);
            float length = delta.magnitude;
            foreach (var enemy in demo.Enemies)
            {
                if (!enemy.Alive || hitByDash.Contains(enemy) || TerrainBetween(to, enemy.Position))
                    continue;
                Bounds hullArea = enemy.Hurtbox;
                hullArea.Expand(new Vector3(0.55f, 0.40f, 0.20f));
                if (!hullArea.Contains(from) && (!hullArea.IntersectRay(ray, out float distance) || distance > length))
                    continue;
                hitByDash.Add(enemy);
                if (enemy.Hit(1, Facing, false, false, fromDash: true))
                    demo.RecordHits(1);
            }
        }

        public void Face(int direction) => Facing = direction;
        public bool TerrainBetween(Vector2 a, Vector2 b) => Physics2D.Linecast(a, b, terrain).collider;

        public void Lunge(float velocity)
        {
            if (velocity > 0f)
                body.linearVelocity = new Vector2(Facing * velocity, body.linearVelocity.y);
        }

        internal bool TryCutFromDash(bool bladeWake)
        {
            if (dashTime <= 0f || dashTime > 0.14f || hurtTime > 0f)
                return false;
            dashTime = 0f;
            if (bladeWake)
                demo.Effects.KatanaWake(Position, Facing);
            return true;
        }

        internal bool InterceptProjectile(EnemyBase2D source, Ray path, float travel, out float distance) =>
            combat.TryIntercept(source, path, travel, out distance) || combat.TryCutProjectile(path, travel, out distance);

        internal bool TryCounterMelee(EnemyBase2D source) =>
            hurtTime <= 0f && !Dashing && combat.TryParry(source, source.Position, false);

        public void HangForAirSlice()
        {
            if (airSliceUsed)
                return;
            airSliceUsed = true;
            body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Max(1.15f, body.linearVelocity.y));
        }

        public void StartDive() =>
            body.linearVelocity = new Vector2(body.linearVelocity.x * 0.45f, -21f);

        private void Fire()
        {
            if (!demo.SpendShot())
            {
                if (input.Fire.WasPressedThisFrame())
                    demo.Message(demo.ReserveAmmo > 0 ? $"Cargador vacío  ·  {input.ReloadKey} para recargar" : "Sin munición en reserva", 1.5f);
                shotCooldown = 0.16f;
                return;
            }
            combat.CancelAttack();
            UpdateAim();
            var sidearm = demo.Weapon;
            ShowWeapon(sidearm);
            var profile = SidearmCatalog.Get(sidearm);
            shotCooldown = profile.Cooldown;
            shotPose = sidearm == Sidearm.Incinerator ? 0.24f : 0.19f;
            shotPending = true;
            shotKick = 1f;
            if (profile.UsesAmmo && demo.Ammo == 0)
                demo.Message(demo.ReserveAmmo > 0 ? $"{input.ReloadKey} para recargar" : "Sin munición en reserva", 1.4f);
        }

        private void UpdateAim()
        {
            shotDirection = input.ShotDirection(aimCamera, Position + Vector2.up * 0.40f, AttackDirection);
            if (Mathf.Abs(shotDirection.x) > 0.04f)
                Facing = shotDirection.x > 0f ? 1 : -1;
        }

        private void ShowWeapon(Sidearm sidearm)
        {
            displayedWeapon = sidearm;
            var weaponArt = demo.Effects.WeaponArt(sidearm);
            if (weaponArt && !equippedArt)
            {
                equippedArt = new GameObject("Arma equipada").AddComponent<SpriteRenderer>();
                equippedArt.transform.SetParent(transform, false);
                equippedArt.sortingOrder = view.sortingOrder + 1;
            }
            if (equippedArt)
            {
                equippedArt.sprite = weaponArt;
                if (weaponArt)
                {
                    float scale = SidearmCatalog.Get(sidearm).Length / weaponArt.bounds.size.x;
                    equippedArt.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private void BeginReload()
        {
            if (Reloading)
                return;
            var weapon = demo.Weapon;
            var data = SidearmCatalog.Get(weapon);
            if (!data.UsesAmmo || demo.Ammo >= data.Magazine)
                return;
            if (demo.ReserveAmmo == 0)
            {
                demo.Message("Sin munición en reserva", 1.4f);
                return;
            }
            reloadWeapon = weapon;
            reloadTime = data.ReloadSeconds;
            shotPose = 0f;
            ShowWeapon(weapon);
            demo.Effects.ReloadSound(false);
        }

        internal void CancelReload() => reloadTime = 0f;

        public HitResponse TakeHit(EnemyBase2D attacker, Vector2 origin, int damage, bool projectile = false,
            bool parryable = true)
        {
            if (Dashing || hurtTime > 0f || !demo.Playing)
                return HitResponse.Ignored;
            if (parryable && combat.TryParry(attacker, origin, projectile))
                return HitResponse.Parried;

            CancelReload();
            combat.ResetCombat();
            demo.ResetHits();
            Health -= damage;
            hurtTime = 0.58f;
            float away = Position.x >= origin.x ? 1f : -1f;
            body.linearVelocity = new Vector2(away * (damage > 1 ? 9.2f : 6.9f), 5.1f);
            demo.Effects.Burst(Position, new Color(1f, 0.26f, 0.38f), 13, 6f);
            demo.Effects.Sound(4);
            demo.Impact(0.045f, 0.23f);
            if (Health <= 0)
                demo.Respawn();
            return HitResponse.Damaged;
        }

        public void Heal() => Health = Mathf.Min(CurrentMaxHealth, Health + 1);
        public void FullHeal() => Health = CurrentMaxHealth;
        public void FallIntoHazard() => demo.Respawn();

        public void RestoreAbilities(bool airDash, bool doubleJump)
        {
            AirDashUnlocked = airDash;
            DoubleJumpUnlocked = doubleJump;
        }

        public void ResetAt(Vector2 position)
        {
            WarpTo(position);
            body.gravityScale = 3f;
            FullHeal();
            stamina = MaxStamina;
            staminaDelay = 0f;
            hurtTime = 0.6f;
        }

        internal void HoldForScene()
        {
            CancelReload();
            ResetPresentation();
            body.linearVelocity = Vector2.zero;
            dashTime = jumpBuffer = dashBuffer = 0f;
            dashSweepPending = false;
            combat.ResetCombat();
        }

        internal void SetSceneSpriteVisible(bool visible)
        {
            if (!visible)
            {
                rig?.Hide();
                view.enabled = false;
                if (equippedArt)
                    equippedArt.enabled = false;
            }
            else
            {
                rig?.ResetContacts();
            }
        }

        internal void PerformScenePose(float delta, CastRig.Motion motion, float progress, int direction, float speed = 0f)
        {
            Face(direction);
            if (equippedArt)
                equippedArt.enabled = false;
            rig?.Drive(delta, new Vector2(speed * direction, 0f), direction, motion, progress, Color.white,
                transform.position.y - hull.size.y * 0.5f, terrain);
        }

        internal void DialogueGesture(int direction, float delta, float progress, bool speaking) =>
            PerformScenePose(delta, speaking ? CastRig.Motion.Gesture : CastRig.Motion.Listen, progress, direction);

        internal void EndDialogueGesture() => rig?.ResetContacts();

        internal void WarpTo(Vector2 position)
        {
            CancelReload();
            transform.position = position;
            body.position = position;
            Physics2D.SyncTransforms();
            body.linearVelocity = Vector2.zero;
            dashTime = dashCooldown = dashBuffer = jumpBuffer = coyote = 0f;
            shotCooldown = shotPose = 0f;
            ResetPresentation();
            dashSweepPending = false;
            hitByDash.Clear();
            airDashUsed = airSliceUsed = airJumpUsed = grounded = jumpCut = false;
            Facing = 1;
            combat.ResetCombat();
        }
    }
}

