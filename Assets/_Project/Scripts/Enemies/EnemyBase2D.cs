using UnityEngine;

namespace MoonShadow
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed partial class EnemyBase2D : MonoBehaviour
    {
        public enum Kind { Walker, Runner, Brute, Sentry }
        public enum Boss { None, Vane, Vance, Vulkano, Orbital, Ojo }
        private enum State { Patrol, Chase, Windup, Strike, Recovery, Stunned, Dead }

        private readonly struct Pattern
        {
            public readonly int Health, Damage;
            public readonly float MoveSpeed, Trigger, Windup, Active, Recovery, Reach, Height, Lunge;

            public Pattern(int health, int damage, float moveSpeed, float trigger, float windup,
                float active, float recovery, float reach, float height, float lunge)
            {
                Health = health;
                Damage = damage;
                MoveSpeed = moveSpeed;
                Trigger = trigger;
                Windup = windup;
                Active = active;
                Recovery = recovery;
                Reach = reach;
                Height = height;
                Lunge = lunge;
            }
        }

        private static readonly Pattern[] Patterns =
        {
            new(4, 1, 2.18f, 1.55f, 0.57f, 0.13f, 0.69f, 1.53f, 0.98f, 1.7f),
            new(3, 1, 4.25f, 3.33f, 0.37f, 0.24f, 0.60f, 1.09f, 1.01f, 11.4f),
            new(7, 2, 1.70f, 2.31f, 0.79f, 0.21f, 1.02f, 2.45f, 1.08f, 1.8f),
            new(4, 1, 1.25f, 9.20f, 0.69f, 0.09f, 1.22f, 0f, 0f, 0f)
        };

        private static readonly Pattern[] BossPatterns =
        {
            new(28, 2, 2.64f, 2.70f, 0.72f, 0.20f, 0.91f, 2.63f, 1.22f, 3.2f),
            new(35, 2, 3.88f, 3.80f, 0.44f, 0.24f, 0.75f, 1.55f, 1.20f, 13.8f),
            new(42, 3, 2.36f, 3.00f, 0.76f, 0.22f, 0.93f, 3.05f, 1.33f, 3.2f),
            new(49, 3, 2.85f, 2.80f, 0.56f, 0.18f, 0.83f, 2.65f, 1.29f, 5.5f),
            new(65, 3, 3.96f, 3.35f, 0.53f, 0.22f, 0.76f, 1.58f, 1.16f, 13.9f)
        };
        private static readonly Pattern OjoMid = new(65, 3, 3.3f, 3.05f, 0.63f, 0.19f, 0.69f, 2.8f, 1.19f, 4.4f);
        private static readonly Pattern OjoFinal = new(65, 3, 5.0f, 3.95f, 0.41f, 0.24f, 0.62f, 1.50f, 1.25f, 16.6f);

        [SerializeField] private Kind kind;
        [SerializeField] private Boss boss;
        [SerializeField, Range(0, 4)] private int actTier;
        [SerializeField] private SpriteRenderer view;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Sprite characterArt;
        [SerializeField] private LayerMask terrain;
        [SerializeField] private bool guardsRelay;
        [SerializeField] private SpriteRenderer warning;

        private Rigidbody2D body;
        private BoxCollider2D hull;
        private RuinsDemo demo;
        private Vector2 spawn;
        private Vector2 aim;
        private State state;
        private float timer;
        private int facing = -1;
        private bool resolved;
        private bool rangedWindup;
        private int attackCount;
        private CastRig rig;
        public int AttackRevision { get; private set; }
        private Pattern Moves => boss switch
        {
            Boss.None => Patterns[(int)kind],
            Boss.Ojo when Phase == 2 => OjoMid,
            Boss.Ojo when Phase == 3 => OjoFinal,
            _ => BossPatterns[(int)boss - 1]
        };

        public Kind Archetype => kind;
        public Boss Profile => boss;
        public Sprite CharacterArt => characterArt;
        public bool IsBoss => boss != Boss.None;
        [SerializeField] private bool veteran;
        [SerializeField] private bool reinforcement;
        private int posture;
        public int MaximumHealth => IsBoss ? BossPatterns[(int)boss - 1].Health :
            Patterns[(int)kind].Health + actTier + (actTier >= 2 ? 1 : 0) + (veteran ? 7 : 0);
        public int RangedDamage => !IsBoss && actTier >= 2 ? 2 : 1;
        public int Phase => boss != Boss.Ojo || Health * 3 > MaximumHealth * 2 ? 1 :
            Health * 3 > MaximumHealth ? 2 : 3;
        public bool IsReinforcement => reinforcement;
        public int Health { get; private set; }
        public bool Alive => gameObject.activeInHierarchy && state != State.Dead;
        public bool GuardsRelay => guardsRelay;
        internal bool ReadyToTalk => Alive && Grounded();
        public bool CommittedMelee => !rangedWindup && state is State.Windup or State.Strike;
        public bool CommittedRanged => rangedWindup && state is State.Windup or State.Strike;
        public Vector2 Position => body.position;
        public Vector2 SpawnPosition => spawn;
        public Bounds Hurtbox => hull.bounds;

        public void Configure(Kind type, LayerMask mask, SpriteRenderer visual, Sprite[] sprites, bool guard)
        {
            kind = type;
            terrain = mask;
            view = visual;
            frames = sprites;
            guardsRelay = guard;
        }

        public void SetWarning(SpriteRenderer indicator) => warning = indicator;
        public void SetBoss(Boss profile) => boss = profile;
        public void SetActTier(int tier) => actTier = tier;
        public void SetVeteran(bool elite) => veteran = elite;
        public void MarkReinforcement() => reinforcement = true;

        public void SetCharacterArt(Sprite art, float worldHeight)
        {
            characterArt = art;
            view.sprite = art;
            float scale = worldHeight / art.bounds.size.y;
            view.transform.localScale = new Vector3(scale, scale, 1f);
            view.transform.localPosition = new Vector3(0f, -GetComponent<BoxCollider2D>().size.y * 0.5f, 0f);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hull = GetComponent<BoxCollider2D>();
            spawn = body.position;
            Health = MaximumHealth;
            rig = view.GetComponent<CastRig>();
        }

        private void Start() => demo = FindAnyObjectByType<RuinsDemo>();

        private void FixedUpdate()
        {
            if (!demo || !demo.Playing || !Alive || (IsBoss && !demo.BossReady))
                return;
            if (Position.y < -4.4f)
            {
                Die();
                return;
            }

            timer -= Time.fixedDeltaTime;
            reactionTime -= Time.fixedDeltaTime;
            vaultTime -= Time.fixedDeltaTime;
            Vector2 distance = demo.Player.Position - Position;
            float speed = 0f;
            switch (state)
            {
                case State.Windup:
                    if (!rangedWindup && timer > CurrentWindup * 0.56f && distance.x * facing < -0.48f)
                    {
                        facing = distance.x < 0f ? -1 : 1;
                        aim = demo.Player.Position;
                    }
                    if (timer <= 0f)
                        CommitStrike();
                    break;
                case State.Strike:
                    if (timer <= 0f)
                    {
                        EnterRecovery(distance);
                    }
                    else if (technique != Technique.None)
                    {
                        speed = AdvanceTechnique(distance);
                        if (state != State.Strike)
                            return;
                    }
                    else if (!rangedWindup)
                    {
                        speed = Moves.Lunge * facing;
                        ResolveMelee(distance);
                        if (state != State.Strike)
                            return;
                    }
                    break;
                case State.Recovery:
                    if (timer <= 0f)
                        state = State.Chase;
                    else if (IsBoss)
                        speed = recoveryStep * Mathf.Clamp01(timer / (RecoveryDuration * 0.68f));
                    break;
                case State.Stunned:
                    if (timer > 0f || !Grounded())
                        return;
                    state = State.Chase;
                    break;
                default:
                    speed = vaultTime > 0f ? vaultDirection * 6.0f : Think(distance);
                    break;
            }

            if (state is State.Patrol or State.Chase)
                speed = Mathf.MoveTowards(body.linearVelocity.x, speed, Time.fixedDeltaTime * (IsBoss ? 36f : kind == Kind.Brute ? 19f : 25f));
            if (Mathf.Abs(speed) > 0.01f && vaultTime <= 0f && !HasFooting(speed) && Grounded())
            {
                if (state == State.Patrol)
                    facing *= -1;
                speed = 0f;
            }
            body.linearVelocity = new Vector2(speed, body.linearVelocity.y);
        }

        private float Think(Vector2 offset)
        {
            if (IsBoss && (!demo.RelayActive || demo.BossShielded))
                return 0f;
            var special = BossResponse(Mathf.Abs(offset.x), ChooseTechnique(Mathf.Abs(offset.x)));
            bool ranged = special == Technique.None ? IsRangedMove(offset) : Specials[(int)special].Remote;
            float awareness = demo.Map && demo.Map.OnArenaGround(Position.x) ? 23f : ranged ? 10.4f : 7.5f;
            if (Mathf.Abs(offset.x) <= awareness && Mathf.Abs(offset.y) <= (IsBoss ? 4.0f : 2.7f))
            {
                state = State.Chase;
                facing = offset.x < 0f ? -1 : 1;
                if (IsBoss && attackCount > 0 && TryVault(offset))
                    return vaultDirection * (boss == Boss.Vulkano ? 5.3f : 7.1f);
                float reach = special == Technique.None ? (ranged && IsBoss ? 7.3f : Moves.Trigger) :
                    Specials[(int)special].Range;
                bool inRange = Mathf.Abs(offset.x) <= reach &&
                    Mathf.Abs(offset.y) < (ranged ? (IsBoss ? 3.45f : 2.05f) : 0.97f) &&
                    !Physics2D.Linecast(Position, demo.Player.Position, terrain);

                if (inRange && ShouldMakeRoom(offset))
                    return -facing * (veteran ? 3.0f : 2.15f);
                if (inRange && Grounded() && (ranged ? demo.CanCommitRanged(this) : demo.CanCommitMelee(this)))
                {
                    state = State.Windup;
                    technique = special;
                    timer = CurrentWindup;
                    rangedWindup = ranged;
                    aim = LeadTarget(technique);
                    AttackRevision++;
                    PrepareTechnique();
                    return 0f;
                }

                return MoveTactically(offset, ranged, inRange);
            }

            state = State.Patrol;
            if (Mathf.Abs(Position.x - spawn.x) > (ranged ? 0.9f : 1.6f))
                facing = Position.x < spawn.x ? 1 : -1;
            return (ranged ? 0.55f : kind == Kind.Brute ? 1.06f : 0.84f) * facing;
        }

        private void CommitStrike()
        {
            state = State.Strike;
            timer = ActiveDuration;
            previousMove = technique;
            resolved = false;
            attackCount++;
            if (technique != Technique.None)
            {
                StartTechnique();
                return;
            }
            if (rangedWindup)
            {
                Vector2 muzzle = Position + new Vector2(facing * 0.55f, 0.13f);
                FirePattern(muzzle, (aim - muzzle).normalized);
                return;
            }

            Kind strikeBody = IsBoss && Moves.Lunge < 5f ? Kind.Brute : kind;
            Technique strikeArt = boss switch
            {
                Boss.Vane => Technique.VaneRush,
                Boss.Vance => Technique.Feint,
                Boss.Vulkano => Technique.TailSweep,
                Boss.Orbital => Technique.GuardianRake,
                Boss.Ojo => Technique.CrossCut,
                _ => strikeBody == Kind.Runner ? Technique.Pounce :
                    strikeBody == Kind.Brute ? Technique.GuardianRake : Technique.DoubleClaw
            };
            demo.Effects.EnemySlash(Position, facing, strikeBody, strikeArt);
            ResolveMelee(demo.Player.Position - Position);
        }

        private void ResolveMelee(Vector2 offset, float reach = 0f, float height = 0f)
        {
            float forward = offset.x * facing;
            float arc = reach > 0f ? reach : Moves.Reach;
            float elevation = height > 0f ? height : Moves.Height;
            if (resolved || forward < -0.44f || forward > arc + KyleCombat2D.GuardMeleeMargin ||
                Mathf.Abs(offset.y) > elevation + 0.42f ||
                Physics2D.Linecast(Position, demo.Player.Position, terrain))
                return;

            if (demo.Player.TryCounterMelee(this))
            {
                resolved = true;
                return;
            }
            if (forward > arc || Mathf.Abs(offset.y) > elevation)
                return;
            resolved = true;
            int damage = !IsBoss && (actTier >= 3 || veteran) ? Moves.Damage + 1 : Moves.Damage;
            HitResponse result = demo.Player.TakeHit(this, Position, damage);
            if (IsZombie && result == HitResponse.Damaged)
                demo.Effects.HandImpact(demo.Player.Position - Vector2.right * facing * 0.18f, facing);
        }

        private bool IsRangedMove(Vector2 offset)
        {
            if (!IsBoss)
                return kind == Kind.Sentry && !IsZombie;
            if (Mathf.Abs(offset.x) < 2.65f && boss != Boss.Orbital && (boss != Boss.Ojo || Phase != 2))
                return false;
            return boss switch
            {
                Boss.Vane => attackCount % 3 == 2,
                Boss.Vance => attackCount % 3 == 1,
                Boss.Vulkano => attackCount % 2 == 0,
                Boss.Orbital => attackCount % 3 != 2,
                Boss.Ojo => Phase == 2 || (Phase == 1 && attackCount % 2 == 0) ||
                    (Phase == 3 && attackCount % 3 == 2),
                _ => false
            };
        }

        private void FirePattern(Vector2 muzzle, Vector2 heading)
        {
            demo.Effects.FireBolt(this, muzzle, heading);
            if (!IsBoss)
                return;

            float spread = boss == Boss.Vulkano ? 18f : boss == Boss.Vance ? 12f : 9f;
            if (boss == Boss.Ojo && Phase == 2)
                spread = 22f;
            demo.Effects.FireBolt(this, muzzle, Quaternion.Euler(0f, 0f, spread) * heading);
            demo.Effects.FireBolt(this, muzzle, Quaternion.Euler(0f, 0f, -spread) * heading);
        }

        private bool Grounded()
        {
            Vector2 feet = new(Position.x, hull.bounds.min.y + 0.12f);
            return body.linearVelocity.y <= 0.1f && Physics2D.Raycast(feet, Vector2.down, 0.27f, terrain);
        }

        private bool HasFooting(float speed)
        {
            float direction = Mathf.Sign(speed);
            float step = hull.bounds.extents.x + Mathf.Max(0.23f, Mathf.Abs(speed) * Time.fixedDeltaTime);
            Vector2 feet = new(Position.x + direction * step, hull.bounds.min.y + 0.24f);
            return Physics2D.Raycast(feet, Vector2.down, 0.69f, terrain);
        }

        private void LateUpdate()
        {
            if (!Alive || !demo || !demo.Playing)
                return;
            if (!characterArt)
            {
                int frame = state is State.Windup or State.Strike ? 3 :
                    (Mathf.Abs(body.linearVelocity.x) > 0.55f ? 1 + (int)(Time.time * 10f) % 2 : 0);
                view.sprite = frames[frame];
            }
            view.flipX = facing < 0;
            Color cue = kind switch
            {
                Kind.Brute => new Color(1f, 0.36f, 0.25f),
                Kind.Sentry => new Color(1f, 0.45f, 0.75f),
                _ => new Color(1f, 0.67f, 0.32f)
            };
            view.color = state switch
            {
                State.Windup => Color.Lerp(BaseTint(), cue, 0.22f + Mathf.PingPong(timer * 1.55f, 0.2f)),
                State.Stunned => new Color(0.62f, 0.86f, 1f),
                _ => BaseTint()
            };
            if (rig)
                AnimateEnemy();
            if (warning)
            {
                warning.enabled = state == State.Windup;
                if (warning.enabled)
                {
                    float progress = 1f - Mathf.Clamp01(timer / CurrentWindup);
                    warning.color = technique == Technique.None ? cue : TechniqueColor();
                    warning.transform.localScale = new Vector3(32f * Mathf.Lerp(0.38f, 0.88f, progress), 2.6f, 1f);
                }
            }
        }

        private Color BaseTint()
        {
            if (boss == Boss.Ojo)
                return Phase == 3 ? new Color(1f, 0.73f, 0.86f) : Color.white;
            if (IsBoss)
                return Color.white;
            return actTier switch
            {
                1 => new Color(0.97f, 0.89f, 1f),
                2 => new Color(1f, 0.85f, 0.75f),
                3 or 4 => new Color(0.83f, 0.95f, 1f),
                _ => Color.white
            };
        }

        private void AnimateEnemy()
        {
            bool grounded = Grounded();
            bool airborne = jumped && !grounded;
            if (jumped && !airborne && vaultTime <= 0f)
                jumped = false;
            CastRig.Motion motion = state switch
            {
                State.Windup => CastRig.Motion.Windup,
                State.Strike when IsBoss && technique is Technique.VaneRush or Technique.Feint or Technique.CrossCut =>
                    CastRig.Motion.Surge,
                State.Strike when technique == Technique.Pounce && Mathf.Abs(body.linearVelocity.y) > 1.1f => CastRig.Motion.Air,
                State.Strike => CastRig.Motion.Strike,
                State.Recovery when IsBoss && Mathf.Abs(body.linearVelocity.x) > 1.8f => CastRig.Motion.Surge,
                State.Recovery or State.Stunned => CastRig.Motion.Recover,
                _ when airborne => CastRig.Motion.Vault,
                _ => CastRig.Motion.Travel
            };
            float duration = state switch
            {
                State.Windup => CurrentWindup,
                State.Recovery => RecoveryDuration,
                State.Stunned => 0.45f,
                _ => ActiveDuration
            };
            float progress = motion == CastRig.Motion.Vault ? Mathf.InverseLerp(6f, -5f, body.linearVelocity.y) :
                1f - Mathf.Clamp01(timer / duration);
            if (state is State.Windup or State.Strike)
            {
                switch (technique)
                {
                    case Technique.DoubleClaw:
                    case Technique.ZombieRake:
                    case Technique.ZombieClutch:
                    case Technique.ZombieLunge:
                        motion = technique is Technique.DoubleClaw or Technique.ZombieRake ? CastRig.Motion.Claw : CastRig.Motion.Grab;
                        progress = state == State.Windup ? progress * 0.23f : Mathf.Lerp(0.23f, 1f, progress);
                        break;
                    case Technique.ZombieHammer:
                    case Technique.GroundSmash:
                    case Technique.Rupture:
                    case Technique.OrbitalShock:
                        motion = CastRig.Motion.Pound;
                        progress = state == State.Windup ? progress * 0.15f : Mathf.Lerp(0.15f, 1f, progress);
                        break;
                    case Technique.FireBreath:
                        motion = CastRig.Motion.Breath;
                        progress = state == State.Windup ? progress * 0.31f : Mathf.Lerp(0.31f, 1f, progress);
                        break;
                    case Technique.TailSweep:
                    case Technique.CrossCut:
                        motion = technique == Technique.TailSweep ? CastRig.Motion.TailSweep : CastRig.Motion.CrossCut;
                        progress = state == State.Windup ? progress * 0.28f : Mathf.Lerp(0.28f, 1f, progress);
                        break;
                    default:
                        if (rangedWindup && !IsZombie && (kind == Kind.Sentry || boss is Boss.Orbital or Boss.Ojo))
                            motion = CastRig.Motion.Aim;
                        break;
                }
            }
            rig.Drive(Time.deltaTime, body.linearVelocity, facing, motion, progress, view.color,
                grounded ? hull.bounds.min.y : float.NaN, terrain, aim: aim - Position);
        }

        public void Stagger(float duration)
        {
            if (state == State.Windup)
                AttackRevision++;
            state = State.Stunned;
            timer = duration;
            if (warning)
                warning.enabled = false;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        public bool Hit(int damage, int direction, bool breaksArmor, bool launch, bool fromDash = false)
        {
            if (!Alive || !demo.Playing || (IsBoss && !demo.BossReady))
                return false;

            if (boss == Boss.Orbital && Health > Mathf.CeilToInt(MaximumHealth * 0.50f) &&
                !breaksArmor && direction * facing < 0)
            {
                demo.Effects.Guard(Position + Vector2.right * facing * 0.45f, facing, true);
                return false;
            }
            if (boss == Boss.None && !IsZombie && kind == Kind.Brute && !breaksArmor && state is not (State.Stunned or State.Recovery) &&
                direction * facing < 0)
            {
                if (fromDash)
                    demo.Effects.DashSparks(Position, direction, 6);
                else
                    demo.Effects.Burst(Position - Vector2.right * direction * 0.4f,
                        new Color(1f, 0.81f, 0.5f), 9, 3.5f);
                return false;
            }

            int previousPhase = Phase;
            Health -= damage;
            Color particles = kind switch
            {
                Kind.Sentry => new Color(1f, 0.68f, 0.38f),
                Kind.Walker => new Color(0.63f, 0.87f, 0.43f),
                _ => new Color(0.94f, 0.46f, 0.34f)
            };
            if (fromDash)
                demo.Effects.DashSparks(Position, direction, Health > 0 ? 8 : 16);
            else
                demo.Effects.Burst(Position, particles, Health > 0 ? 8 : 18, 6f);
            if (Health <= 0)
            {
                Die();
                return true;
            }


            if (previousPhase != Phase)
            {
                attackCount = 0;
                Stagger(0.22f);
                demo.Message(Phase == 2 ? "El Ojo activa el protocolo de supresión." :
                    "El Ojo abandona el núcleo. Ataque directo.", 3.4f);
                demo.Effects.Burst(Position, new Color(1f, 0.43f, 0.66f), 26, 6f);
            }

            if (IsBoss)
            {
                if (breaksArmor && state is not (State.Windup or State.Strike))
                    Stagger(0.16f);
                return true;
            }

            int protection = (actTier >= 2 ? actTier - 1 : 0) + (veteran ? 2 : 0);
            if (!breaksArmor && !launch && state is not (State.Recovery or State.Stunned) &&
                ++posture <= protection)
                return true;
            posture = 0;
            Stagger(launch ? 0.48f : breaksArmor ? 0.32f : 0.21f);
            float push = launch ? 1.15f : 0.85f;
            float vertical = launch ? (kind == Kind.Brute ? 7.5f : 9.0f) :
                !Grounded() ? Mathf.Max(2.2f, body.linearVelocity.y) : body.linearVelocity.y;
            body.linearVelocity = new Vector2(direction * push, vertical);
            return true;
        }

        internal void HoldForDialogue()
        {
            state = State.Patrol;
            body.linearVelocity = Vector2.zero;
            if (warning)
                warning.enabled = false;
        }

        internal void DialogueGesture(int direction, float delta, float progress, bool speaking)
        {
            facing = direction;
            if (rig)
                rig.Drive(delta, Vector2.zero, facing, speaking ? CastRig.Motion.Gesture : CastRig.Motion.Listen,
                    progress, BaseTint(), hull.bounds.min.y, terrain);
        }

        internal void FaceAfterDialogue()
        {
            state = State.Recovery;
            timer = 0.32f;
        }

        private void Die()
        {
            HideEnemy();
            demo.EnemyDefeated(this);
        }

        internal void DescendForEntrance()
        {
            body.position += Vector2.up * 2.1f;
            body.linearVelocity = Vector2.down * 4.2f;
        }

        public void ResetEnemy()
        {
            body.position = spawn;
            body.linearVelocity = Vector2.zero;
            state = State.Patrol;
            timer = 0f;
            attackCount = 0;
            reactionTime = vaultTime = recoveryStep = 0f;
            rig?.ResetContacts();
            previousMove = Technique.None;
            jumped = false;
            posture = 0;
            technique = Technique.None;
            AttackRevision++;
            rangedWindup = false;
            facing = -1;
            Health = MaximumHealth;
            if (warning)
                warning.enabled = false;
            gameObject.SetActive(true);
        }

        public void HideEnemy()
        {
            state = State.Dead;
            gameObject.SetActive(false);
        }
    }
}
