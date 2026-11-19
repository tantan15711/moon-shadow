using System.Collections.Generic;
using UnityEngine;

namespace MoonShadow
{
    public sealed class KyleCombat2D : MonoBehaviour
    {
        private enum Attack { Light1, Light2, Sweep, Cross, Launcher, AirSlice, Dive, Counter }
        private enum Button { Light, Heavy }

        private readonly struct StrikeData
        {
            public readonly float Windup, Recovery, ChainAt, Reach, Height, Lunge, Arc;
            public readonly int Damage;
            public readonly bool BreaksArmor, Launches;

            public StrikeData(float windup, float recovery, float chainAt, float reach, float height,
                float lunge, float arc, int damage, bool breaksArmor = false, bool launches = false)
            {
                Windup = windup;
                Recovery = recovery;
                ChainAt = chainAt;
                Reach = reach;
                Height = height;
                Lunge = lunge;
                Arc = arc;
                Damage = damage;
                BreaksArmor = breaksArmor;
                Launches = launches;
            }
        }

        private static readonly StrikeData[] Strikes =
        {
            new(0.022f, 0.152f, 0.066f, 2.63f, 1.28f, 5.8f, -22f, 2),
            new(0.026f, 0.157f, 0.071f, 2.76f, 1.58f, 6.2f, 26f, 2),
            new(0.032f, 0.173f, 0.085f, 3.15f, 1.29f, 7.1f, -6f, 3, true),
            new(0.042f, 0.184f, 0.091f, 3.37f, 1.62f, 7.5f, 0f, 4, true),
            new(0.078f, 0.207f, 0.110f, 2.78f, 1.65f, 6.2f, 64f, 3, true, true),
            new(0.026f, 0.147f, 0.070f, 2.74f, 1.48f, 7.2f, -22f, 2),
            new(0.065f, 0.226f, 0.113f, 3.14f, 1.45f, 0f, -68f, 3, true),
            new(0.030f, 0.194f, 0.091f, 3.48f, 1.55f, 8.4f, 26f, 4, true)
        };

        internal const float GuardTime = 0.34f;
        internal const float GuardRadius = 1.98f;
        internal const float GuardHeight = 1.39f;
        internal const float GuardMeleeMargin = 0.95f;
        private KyleController2D player;
        private RuinsDemo demo;
        private Attack attack;
        private Button? queued;
        private float bufferTime;
        private float elapsed;
        private float parryWindow;
        private float parryBuffer;
        private float parryRecovery;
        private float counterWindow;
        private float chainMemory;
        private int lightStep;
        private bool struck;
        private bool diving;
        private int attackFacing;
        private bool madeContact;
        private readonly HashSet<EnemyBase2D> touched = new();
        private EnemyBase2D launched;
        private float followWindow;

        private float ActiveTime => attack switch
        {
            Attack.Light1 or Attack.Light2 => 0.067f,
            Attack.Sweep or Attack.Cross => 0.078f,
            Attack.AirSlice => 0.075f,
            _ => 0.105f
        };

        private float SwingTime => elapsed - Strikes[(int)attack].Windup;

        public bool Attacking { get; private set; }
        public bool Parrying => parryWindow > 0f;
        public bool CanCancel => !Attacking || (!diving && elapsed >= Strikes[(int)attack].Windup + ActiveTime);
        public bool CanEvade => !diving && (!Attacking || struck || WindupProgress >= 0.45f);
        private bool BladeActive => Attacking && struck && !diving && attack != Attack.Dive && !CanCancel;
        public bool Diving => diving;
        public bool AnimatingWindup => Attacking && !struck && !diving;
        public float WindupProgress => Mathf.Clamp01(elapsed / Strikes[(int)attack].Windup);
        internal CastRig.Motion VisualMotion => diving ? CastRig.Motion.Dive :
            AnimatingWindup ? CastRig.Motion.Windup : SwingTime < ActiveTime ? CastRig.Motion.Strike : CastRig.Motion.FollowThrough;
        internal float VisualProgress => AnimatingWindup ? WindupProgress :
            SwingTime < ActiveTime ? Mathf.Clamp01(SwingTime / ActiveTime) :
                Mathf.InverseLerp(ActiveTime, Strikes[(int)attack].Recovery, SwingTime);
        internal float VisualBladeAngle
        {
            get
            {
                if (diving)
                    return -76f;
                var (start, end) = BladeArc;
                if (AnimatingWindup)
                    return Mathf.Lerp(start - Mathf.Sign(end - start) * 33f, start, VisualProgress);
                if (SwingTime < ActiveTime)
                    return Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, VisualProgress));
                return Mathf.LerpAngle(end, -128f, Mathf.SmoothStep(0f, 1f, VisualProgress));
            }
        }
        public float MoveFactor => diving ? 0.34f : !Attacking ? 1f : struck ? 0.80f : 0.57f;

        private void Awake() => player = GetComponent<KyleController2D>();
        private void Start() => demo = FindAnyObjectByType<RuinsDemo>();

        public void ReadFrame(bool canAct)
        {
            float delta = Time.deltaTime;
            parryWindow -= delta;
            parryRecovery -= delta;
            parryBuffer = player.Input.Parry.WasPressedThisFrame() ? 0.19f : parryBuffer - delta;
            counterWindow -= delta;
            chainMemory -= delta;
            bufferTime -= delta;
            followWindow -= delta;

            if (player.Input.Heavy.WasPressedThisFrame())
                Queue(Button.Heavy);
            else if (player.Input.Attack.WasPressedThisFrame())
                Queue(Button.Light);
            else if (player.Input.Attack.IsPressed() && Attacking && SwingTime >= ActiveTime && !queued.HasValue)
                Queue(Button.Light);
            bool guardQueued = parryBuffer > 0f && parryRecovery <= 0f;
            if (player.Dashing && (guardQueued || (queued.HasValue && bufferTime > 0f)) && player.TryCutFromDash(!guardQueued))
                canAct = true;

            if (!canAct)
                return;

            if (guardQueued && !diving)
            {
                CancelAttack();
                lightStep = 0;
                chainMemory = 0f;
                parryBuffer = 0f;
                parryWindow = GuardTime;
                parryRecovery = 0.45f;
                demo.Effects.Guard(player.Position, player.Facing, false);
                return;
            }

            if (Attacking)
                Advance(delta);

            if (Parrying || queued is not Button button || bufferTime <= 0f)
                return;

            var strike = Strikes[(int)attack];
            if (!Attacking || (!diving && elapsed >= strike.Windup + Mathf.Max(strike.ChainAt, ActiveTime)))
            {
                queued = null;
                Begin(button);
            }
        }

        private void Queue(Button button)
        {
            queued = button;
            bufferTime = 0.28f;
        }

        private void Begin(Button button)
        {
            if (counterWindow > 0f)
                attack = Attack.Counter;
            else if (button == Button.Heavy)
                attack = player.Grounded ? Attack.Launcher : Attack.Dive;
            else if (!player.Grounded)
                attack = Attack.AirSlice;
            else
            {
                if (chainMemory <= 0f)
                    lightStep = 0;
                attack = (Attack)lightStep;
                lightStep = (lightStep + 1) % 4;
                chainMemory = 0.44f;
            }

            if (attack is not (Attack.Light1 or Attack.Light2 or Attack.Sweep or Attack.Cross))
                chainMemory = 0f;

            elapsed = 0f;
            Attacking = true;
            struck = diving = false;
            madeContact = false;
            touched.Clear();
            FaceTarget(Strikes[(int)attack].Reach + demo.BladeReach);
            attackFacing = player.Facing;
            if (attack == Attack.AirSlice)
                player.HangForAirSlice();
        }

        private void FaceTarget(float reach)
        {
            float nearest = reach + 0.25f;
            int chosen = player.AttackDirection;
            foreach (var enemy in demo.Enemies)
            {
                if (!enemy.Alive)
                    continue;
                Vector2 offset = enemy.Position - player.Position;
                float width = Mathf.Abs(offset.x);
                if (width <= 0.12f || width >= nearest || Mathf.Abs(offset.y) > 1.45f ||
                    (player.HasDirection && offset.x * chosen < 0f) ||
                    player.TerrainBetween(player.Position, enemy.Position))
                    continue;

                nearest = width;
                chosen = offset.x > 0 ? 1 : -1;
            }
            player.Face(chosen);
        }

        private void Advance(float delta)
        {
            var strike = Strikes[(int)attack];
            float previous = elapsed;
            elapsed += delta;

            if (diving)
            {
                if (player.Grounded)
                    LandDive();
                else if (elapsed > 0.66f)
                    CancelAttack();
                return;
            }

            if (!struck && elapsed >= strike.Windup)
            {
                if (attack == Attack.Dive && !player.Grounded)
                {
                    diving = true;
                    player.StartDive();
                    demo.Effects.Sound(2);
                    return;
                }
                if (attack == Attack.Dive)
                {
                    LandDive();
                    return;
                }
                StartStrike(strike, false);
            }

            if (struck && attack != Attack.Dive && previous < strike.Windup + ActiveTime)
                SweepStrike(strike, false, (previous - strike.Windup) / ActiveTime, (elapsed - strike.Windup) / ActiveTime);
            if (elapsed >= strike.Windup + strike.Recovery)
                Attacking = false;
        }

        private void LandDive()
        {
            diving = false;
            elapsed = Strikes[(int)Attack.Dive].Windup;
            var strike = Strikes[(int)Attack.Dive];
            StartStrike(strike, true);
            SweepStrike(strike, true, 0f, 1f);
        }

        private Color StrikeColor => attack switch
        {
            Attack.Counter => new Color(1f, 0.83f, 0.42f),
            Attack.Launcher or Attack.Dive => new Color(0.77f, 0.63f, 1f),
            _ => new Color(0.37f, 0.93f, 1f)
        };

        private void StartStrike(StrikeData strike, bool radial)
        {
            struck = true;
            if (!radial)
                player.Lunge(strike.Lunge);
            bool rising = attack is Attack.Light2 or Attack.Launcher;
            int art = attack switch
            {
                Attack.Light2 => 1,
                Attack.Sweep or Attack.Launcher => 2,
                Attack.Cross or Attack.Counter => 3,
                _ => 0
            };
            demo.Effects.PlayerStrike(player.Position, attackFacing, strike.Arc, radial, strike.BreaksArmor,
                rising, StrikeColor, art, strike.Reach + demo.BladeReach);
            demo.Effects.SwordSound(false, strike.BreaksArmor);
            if (attack == Attack.Counter)
                counterWindow = 0f;
        }

        private (float Start, float End) BladeArc => attack switch
            {
                Attack.Light2 or Attack.Launcher => (-63f, 72f),
                Attack.AirSlice => (32f, -72f),
                Attack.Sweep => (24f, -38f),
                Attack.Cross => (56f, -61f),
                _ => (59f, -55f)
            };

        private void SweepStrike(StrikeData strike, bool radial, float from, float to)
        {
            var (start, end) = BladeArc;
            float first = Mathf.Lerp(start, end, from);
            float last = Mathf.Lerp(start, end, to);
            float low = Mathf.Min(first, last) - 24f;
            float high = Mathf.Max(first, last) + 24f;
            Vector2 origin = player.Position + Vector2.up * 0.16f;
            int landed = 0;
            bool blocked = false;
            foreach (var enemy in demo.Enemies)
            {
                if (!enemy.Alive || touched.Contains(enemy))
                    continue;

                Vector2 offset = enemy.Position - origin;
                Vector2 contact = enemy.Hurtbox.ClosestPoint(origin);
                Vector2 gap = contact - origin;
                float angle = Mathf.Atan2(gap.y, Mathf.Max(0.01f, Mathf.Abs(gap.x))) * Mathf.Rad2Deg;
                if ((!radial && (offset.x * attackFacing < -0.42f || angle < low || angle > high)) ||
                    Mathf.Abs(gap.x) > strike.Reach + demo.BladeReach || Mathf.Abs(gap.y) > strike.Height ||
                    player.TerrainBetween(origin, contact))
                    continue;

                touched.Add(enemy);
                int direction = radial && offset.x < 0f ? -1 : radial ? 1 : attackFacing;
                if (enemy.Hit(strike.Damage + demo.BonusDamage, direction, strike.BreaksArmor, strike.Launches))
                {
                    demo.Effects.KatanaContact(contact, direction, strike.BreaksArmor, StrikeColor);
                    landed++;
                    if (strike.Launches && enemy.Alive && !enemy.IsBoss)
                    {
                        launched = enemy;
                        followWindow = 0.42f;
                    }
                }
                else
                    blocked = true;
                if (!demo.Playing)
                    break;
            }

            if (landed > 0)
            {
                demo.RecordHits(landed);
                if (!madeContact)
                {
                    madeContact = true;
                    demo.Effects.SwordSound(true, strike.BreaksArmor);
                    demo.Impact(strike.BreaksArmor ? 0.038f : 0.022f, strike.BreaksArmor ? 0.17f : 0.092f);
                }
            }
            else if (blocked)
            {
                demo.Effects.Sound(3);
                demo.Message(demo.BossShielded ? "Los mecas mantienen activo el escudo del Ojo." :
                    "Rompe la guardia con un golpe pesado.", 2f);
            }
            else if (radial)
                demo.Impact(0.020f, 0.12f);
        }

        public bool TryFollowLaunch(out float horizontal)
        {
            horizontal = 0f;
            if (followWindow <= 0f || !launched || !launched.Alive ||
                player.TerrainBetween(player.Position, launched.Position))
                return false;
            followWindow = 0f;
            horizontal = Mathf.Clamp((launched.Position.x - player.Position.x) * 3f, -6.5f, 6.5f);
            return true;
        }

        internal bool TryCutProjectile(Ray path, float travel, out float distance)
        {
            distance = 0f;
            if (!BladeActive || path.direction.x * attackFacing > 0.45f)
                return false;

            Vector2 origin = player.Position + Vector2.up * 0.17f;
            var sweep = new Bounds((Vector3)(origin + Vector2.right * attackFacing * 0.57f),
                new Vector3(1.51f, 1.54f, 0.3f));
            if (!sweep.Contains(path.origin) && (!sweep.IntersectRay(path, out distance) || distance > travel))
                return false;
            Vector2 contact = path.GetPoint(distance);
            if (player.TerrainBetween(origin, contact))
                return false;
            demo.Effects.Deflection(contact);
            demo.Effects.Sound(3);
            demo.Impact(0.043f, 0.105f);
            return true;
        }

        public void CancelAttack()
        {
            Attacking = struck = diving = false;
        }

        public void ResetCombat()
        {
            CancelAttack();
            parryWindow = parryRecovery = parryBuffer = counterWindow = chainMemory = bufferTime = 0f;
            lightStep = 0;
            followWindow = 0f;
            launched = null;
            queued = null;
        }

        public bool TryParry(EnemyBase2D attacker, Vector2 origin, bool projectile)
        {
            if (!Parrying)
                return false;

            player.Face(origin.x < player.Position.x ? -1 : 1);
            if (!projectile)
            {
                attacker.Stagger(0.91f);
                parryWindow = 0f;
                parryRecovery = 0.18f;
                counterWindow = 1.02f;
            }
            else
                demo.Effects.Deflection(origin);
            demo.Effects.Guard(player.Position, player.Facing, true);
            demo.Effects.Sound(3);
            demo.Impact(projectile ? 0.064f : 0.105f, 0.14f);
            demo.Message(projectile ? "Proyectil devuelto." : "¡Parry! Ataca para contraatacar.", 1.2f);
            return true;
        }

        internal bool TryIntercept(EnemyBase2D owner, Ray path, float travel, out float distance)
        {
            distance = 0f;
            if (!Parrying)
                return false;
            Vector2 center = player.Position + Vector2.up * 0.10f;
            Vector2 separation = (Vector2)path.origin - center;
            if (Vector2.Dot(separation, path.direction) > 0.03f)
                return false;
            Vector2 radii = new(GuardRadius, GuardHeight);
            Vector2 point = separation / radii;
            Vector2 contact = (Vector2)path.direction / radii;
            float outside = point.sqrMagnitude - 1f;
            if (outside > 0f)
            {
                float alignment = Vector2.Dot(point, contact);
                float discriminant = alignment * alignment - contact.sqrMagnitude * outside;
                if (discriminant < 0f)
                    return false;
                distance = (-alignment - Mathf.Sqrt(discriminant)) / contact.sqrMagnitude;
                if (distance < 0f || distance > travel)
                    return false;
            }
            Vector2 intercept = path.GetPoint(distance);
            return !player.TerrainBetween(center, intercept) && TryParry(owner, intercept, true);
        }
    }
}
