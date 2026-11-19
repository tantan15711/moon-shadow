using UnityEngine;

namespace MoonShadow
{
    public sealed partial class EnemyBase2D
    {
        [SerializeField] private bool infected;
        private Technique previousMove;
        private float recoveryStep;
        private float reactionTime;
        private float vaultTime;
        private int vaultDirection;
        private bool jumped;

        public bool IsZombie => infected;
        public void SetZombie(bool value) => infected = value;

        private float ActiveDuration => technique == Technique.None ? Moves.Active : Specials[(int)technique].Active;
        private float RecoveryDuration =>
            (technique == Technique.None ? Moves.Recovery : Specials[(int)technique].Recovery) *
            (IsBoss ? (Phase == 3 ? 0.50f : 0.59f) : veteran ? 0.73f : actTier >= 3 ? 0.91f : 1f);

        private bool ShouldMakeRoom(Vector2 offset)
        {
            return kind == Kind.Sentry && !IsZombie && !IsBoss && demo.Player.Dashing &&
                Mathf.Abs(offset.y) < 1.1f && Mathf.Abs(offset.x) < (veteran ? 3.2f : 2.65f) &&
                HasFooting(-facing * 2.2f);
        }

        private Technique BossResponse(float gap, Technique planned)
        {
            if (!IsBoss || attackCount < 1 || (attackCount % 6 != 1 && !demo.Player.Dashing))
                return planned;

            var player = demo.Player;
            int toward = player.Position.x < Position.x ? -1 : 1;
            bool retreat = player.Velocity.x * toward > 2.8f || (player.Dashing && player.Facing == toward);
            bool elevated = !player.Grounded && player.Position.y > Position.y + 0.45f;
            bool aiming = player.ShowingAim && gap > 3.4f;
            Technique counter = boss switch
            {
                Boss.Vane when retreat => Technique.VaneRush,
                Boss.Vane when gap < 3.3f && !elevated => Technique.Rupture,
                Boss.Vance when elevated => Technique.VanceNeedles,
                Boss.Vance when aiming => Technique.Feint,
                Boss.Vulkano when elevated => Technique.FireBreath,
                Boss.Vulkano when gap < 3.8f => Technique.TailSweep,
                Boss.Orbital when elevated => Technique.MissileSalvo,
                Boss.Orbital when gap < 3.0f => Technique.OrbitalShock,
                Boss.Ojo when retreat || aiming => Technique.MoonLattice,
                Boss.Ojo when elevated => Technique.MoonOrbs,
                _ => planned
            };
            if (counter != previousMove && gap <= Specials[(int)counter].Range)
                return counter;
            return planned;
        }

        private Vector2 LeadTarget(Technique planned)
        {
            var player = demo.Player;
            bool predictive = planned is Technique.Burst or Technique.FireBreath or Technique.VanceNeedles or
                Technique.MissileSalvo or Technique.MoonOrbs or Technique.GuardianRocket;
            return predictive ? player.Position + Vector2.ClampMagnitude(player.Velocity * 0.13f, 1.05f) : player.Position;
        }

        private float MoveTactically(Vector2 offset, bool ranged, bool inRange)
        {
            float gap = Mathf.Abs(offset.x);
            if (IsBoss)
            {
                if (ranged && inRange && gap < 2.0f)
                    return -facing * (boss is Boss.Orbital or Boss.Vulkano ? 1.7f : 2.65f);
                return gap > (ranged ? 2.5f : 1.25f) ? Moves.MoveSpeed * 1.34f * facing : 0f;
            }

            float spacing = 0f;
            bool allyCommitted = false;
            foreach (var ally in demo.Enemies)
            {
                if (ally == this || !ally.Alive)
                    continue;
                Vector2 offsetToAlly = Position - ally.Position;
                float separation = Mathf.Abs(offsetToAlly.x);
                if (Mathf.Abs(offsetToAlly.y) > 1.22f || separation > 4.0f)
                    continue;
                if (separation < 0.86f)
                    spacing += (offsetToAlly.x > 0f ? 1f : -1f) * (0.86f - separation) * 1.8f;
                if (ally.CommittedMelee)
                    allyCommitted = true;
            }

            float desired = IsZombie ? (kind == Kind.Sentry ? 1.72f : 1.10f) : ranged ? 2.9f : 1.38f;
            float pace = Moves.MoveSpeed * (kind == Kind.Brute ? 1.23f :
                kind == Kind.Runner && demo.Player.ShowingAim ? 1.20f : 1f);
            if (IsZombie && kind == Kind.Sentry)
                pace = 2.66f;
            if (allyCommitted && !ranged && gap < 3.7f)
            {
                // Seek space beside the first attacker without crowding the same wind-up.
                if (gap < desired * 0.91f)
                    return -facing * 1.1f + Mathf.Clamp(spacing, -1.5f, 1.5f);
                pace *= kind == Kind.Runner ? 0.85f : 0.57f;
            }
            if (ranged && gap < desired)
                return -facing * 1.5f + Mathf.Clamp(spacing, -1.0f, 1.0f);
            if (gap <= desired * 0.78f)
                return Mathf.Clamp(spacing, -1.35f, 1.35f);
            return facing * pace + Mathf.Clamp(spacing, -1.4f, 1.4f);
        }

        private bool TryVault(Vector2 offset)
        {
            if (reactionTime > 0f || !Grounded() || Mathf.Abs(offset.x) < 2.9f ||
                Mathf.Abs(offset.x) > 8.8f || (demo.Player.Grounded && Mathf.Abs(demo.Player.Velocity.x) < 2.6f))
                return false;
            var target = Physics2D.Raycast(Position + new Vector2(facing * 2.6f, 0.5f), Vector2.down, 3.1f, terrain);
            if (!target || Physics2D.Raycast(Position, Vector2.up, hull.bounds.extents.y + 0.72f, terrain))
                return false;
            reactionTime = boss == Boss.Ojo && Phase == 3 ? 1.72f : 2.22f;
            vaultTime = 0.30f;
            state = State.Chase;
            vaultDirection = facing;
            body.linearVelocity = new Vector2(0f, boss == Boss.Vulkano ? 7.5f : 6.5f);
            jumped = true;
            return true;
        }

        private void EnterRecovery(Vector2 offset)
        {
            state = State.Recovery;
            timer = RecoveryDuration;
            recoveryStep = 0f;
            if (!IsBoss || Mathf.Abs(offset.y) > 1.8f)
                return;

            int toward = offset.x < 0f ? -1 : 1;
            float gap = Mathf.Abs(offset.x);
            if (gap < 2.3f)
                recoveryStep = -toward * (boss == Boss.Vance ? 5.0f : 3.3f);
            else if (gap > 4.1f && boss != Boss.Orbital)
                recoveryStep = toward * (boss == Boss.Ojo ? 5.3f : 3.8f);
        }

    }
}
