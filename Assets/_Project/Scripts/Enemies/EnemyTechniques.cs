using UnityEngine;

namespace MoonShadow
{
    public sealed partial class EnemyBase2D
    {
        public enum Technique
        {
            None, DoubleClaw, Pounce, GroundSmash, Burst, Rupture, Feint, FireBreath, Cannon, Scan, LaserGrid, CrossCut,
            MineArc, VaneRush, VanceNeedles, VanceLock, EmberRain, TailSweep, MissileSalvo, OrbitalShock, MoonLattice, MoonOrbs,
            GuardianRake, GuardianRocket, GuardianEmp, ZombieRake, ZombieLunge, ZombieHammer, ZombieClutch
        }

        private readonly struct Special
        {
            public readonly float Range, Windup, Active, Recovery;
            public readonly bool Remote;

            public Special(float range, float windup, float active, float recovery, bool remote = false)
            {
                Range = range; Windup = windup; Active = active; Recovery = recovery; Remote = remote;
            }
        }

        private static readonly Special[] Specials =
        {
            default,
            new(1.66f, 0.54f, 0.31f, 0.63f),
            new(4.05f, 0.48f, 0.34f, 0.62f),
            new(5.2f, 0.85f, 0.16f, 1.02f, true),
            new(9.0f, 0.57f, 0.54f, 1.12f, true),
            new(7.1f, 0.71f, 0.24f, 0.86f, true),
            new(6.7f, 0.55f, 0.30f, 0.74f),
            new(7.8f, 0.75f, 0.88f, 0.92f, true),
            new(9.1f, 0.82f, 0.33f, 0.97f, true),
            new(7.4f, 0.62f, 0.34f, 0.83f, true),
            new(8.4f, 0.55f, 0.35f, 0.78f, true),
            new(5.9f, 0.43f, 0.28f, 0.71f),
            new(9.5f, 0.89f, 0.28f, 1.08f, true),
            new(7.2f, 0.64f, 0.42f, 0.85f),
            new(9.2f, 0.63f, 0.57f, 0.82f, true),
            new(7.3f, 0.88f, 0.35f, 0.92f, true),
            new(10.0f, 0.97f, 0.32f, 1.22f, true),
            new(6.7f, 0.68f, 0.32f, 0.91f, true),
            new(10.4f, 0.71f, 0.62f, 1.04f, true),
            new(8.1f, 0.98f, 0.33f, 1.03f, true),
            new(10.4f, 0.94f, 0.32f, 1.11f, true),
            new(9.6f, 0.65f, 0.52f, 0.86f, true),
            new(3.6f, 0.58f, 0.28f, 0.69f),
            new(9.4f, 0.72f, 0.52f, 0.82f, true),
            new(5.2f, 0.89f, 0.38f, 0.92f, true),
            new(1.68f, 0.46f, 0.23f, 0.47f),
            new(3.25f, 0.38f, 0.33f, 0.50f),
            new(2.42f, 0.60f, 0.26f, 0.68f),
            new(2.32f, 0.43f, 0.32f, 0.54f)
        };

        private Technique technique;
        private int pulse;
        private Vector2 landAt;
        private bool canFeint;
        public Technique CurrentTechnique => state is State.Windup or State.Strike ? technique : Technique.None;
        internal Technique ProjectileTechnique => CurrentTechnique != Technique.None ? CurrentTechnique : boss switch
        {
            Boss.Vane => Technique.Burst,
            Boss.Vance => Technique.VanceNeedles,
            Boss.Vulkano => Technique.FireBreath,
            Boss.Orbital => Technique.MissileSalvo,
            Boss.Ojo => Technique.MoonOrbs,
            _ => Technique.Burst
        };
        private float CurrentWindup => (technique == Technique.None ? Moves.Windup : Specials[(int)technique].Windup) *
            (IsBoss ? (Phase == 3 ? 0.70f : 0.80f) : veteran ? 0.87f : 1f);

        private Technique ChooseTechnique(float distance)
        {
            if (IsZombie)
                return Technique.ZombieRake + (int)kind;
            if (veteran && !IsBoss)
            {
                Technique elite = (attackCount % 3) switch
                {
                    1 => kind == Kind.Sentry ? Technique.GuardianRocket : Technique.GroundSmash,
                    2 => Technique.GuardianEmp,
                    _ => kind == Kind.Sentry ? Technique.GuardianRocket : Technique.GuardianRake
                };
                if (attackCount > 0 && (attackCount & 1) != 0)
                {
                    if (kind == Kind.Sentry && !demo.Player.Grounded)
                        elite = Technique.GuardianRocket;
                    else if (distance < 3.1f && demo.Player.ShowingAim && previousMove != Technique.GuardianEmp)
                        elite = Technique.GuardianEmp;
                }
                if (distance <= Specials[(int)elite].Range)
                    return elite;
            }
            if (!IsBoss && (attackCount & 1) != 0 && kind != Kind.Sentry)
                return Technique.None;
            Technique move = boss switch
            {
                Boss.Vane => Technique.Rupture, Boss.Vance => Technique.Feint,
                Boss.Vulkano => Technique.FireBreath, Boss.Orbital => Technique.Cannon,
                Boss.Ojo when Phase == 2 => Technique.LaserGrid,
                Boss.Ojo when Phase == 3 => Technique.CrossCut,
                Boss.Ojo => Technique.Scan,
                _ => kind switch
                {
                    Kind.Walker => Technique.DoubleClaw, Kind.Runner => Technique.Pounce,
                    Kind.Brute => Technique.GroundSmash, _ => Technique.Burst
                }
            };
            if (IsBoss)
            {
                int pattern = attackCount % 3;
                Technique later = (boss, pattern) switch
                {
                    (Boss.Vane, 1) => Technique.MineArc,
                    (Boss.Vane, 2) => Technique.VaneRush,
                    (Boss.Vance, 1) => Technique.VanceNeedles,
                    (Boss.Vance, 2) => Technique.VanceLock,
                    (Boss.Vulkano, 1) => Technique.EmberRain,
                    (Boss.Vulkano, 2) => Technique.TailSweep,
                    (Boss.Orbital, 1) => Technique.MissileSalvo,
                    (Boss.Orbital, 2) => Technique.OrbitalShock,
                    (Boss.Ojo, 1) => Technique.MoonOrbs,
                    (Boss.Ojo, 2) => Technique.MoonLattice,
                    _ => move
                };
                if (distance <= Specials[(int)later].Range)
                    return later;
            }
            return distance <= Specials[(int)move].Range ? move : Technique.None;
        }

        private void PrepareTechnique()
        {
            canFeint = false;
            Color tint = TechniqueColor();
            switch (technique)
            {
                case Technique.DoubleClaw:
                case Technique.Pounce:
                case Technique.GuardianRake:
                    demo.Effects.TellTechnique(technique, Position, facing, tint, CurrentWindup);
                    break;
                case Technique.GuardianEmp:
                    for (int i = -1; i <= 1; i += 2)
                        MarkGround(Position.x + i * 1.60f, 2.35f, 1.90f, CurrentWindup + (i + 1) * 0.085f,
                            0.36f, tint, 2);
                    break;
                case Technique.Burst:
                case Technique.FireBreath:
                case Technique.VanceNeedles:
                case Technique.GuardianRocket:
                case Technique.MissileSalvo:
                case Technique.MoonOrbs:
                    demo.Effects.TellProjectile(Muzzle(), aim, tint, CurrentWindup);
                    break;
                case Technique.Rupture:
                    for (int i = 1; i <= 4; i++)
                        MarkGround(Position.x + facing * (1.45f * i), 0.72f, 1.45f,
                            CurrentWindup + (i - 1) * 0.10f, 0.23f, tint, 2);
                    break;
                case Technique.Feint:
                    Vector2 target = aim + Vector2.right * facing * 1.92f;
                    var ground = Physics2D.Raycast(target + Vector2.up, Vector2.down, 3f, terrain);
                    if (ground && !Physics2D.OverlapBox(landAt = (ground.point + Vector2.up * (hull.bounds.extents.y + 0.05f)),
                        hull.size * 0.8f, 0f, terrain))
                    {
                        canFeint = true;
                        demo.Effects.Cue(landAt, tint, CurrentWindup, 1.05f);
                    }
                    break;
                case Technique.GroundSmash:
                    demo.Effects.Cue(FootPoint(), tint, CurrentWindup, 1.25f);
                    break;
                case Technique.Cannon:
                    Vector2 start = Muzzle();
                    Vector2 heading = (aim - start).normalized;
                    float turn = facing * (Health * 2 <= MaximumHealth ? 57f : 39f);
                    float angle = DirectionAngle(heading) - turn * 0.38f;
                    demo.Effects.Hazard(this, start, new Vector2(12f, 0.42f), angle, tint,
                        CurrentWindup, 0.48f, 2).ConfigureSweep(start, 12f, angle, turn);
                    break;
                case Technique.LaserGrid:
                    for (int i = -1; i <= 1; i++)
                        MarkGround(aim.x + i * 2.55f, 0.60f, 3.5f, CurrentWindup + Mathf.Abs(i) * 0.17f,
                            0.33f, tint, 2);
                    break;
                case Technique.CrossCut:
                    demo.Effects.Cue(aim, tint, CurrentWindup, 0.74f);
                    break;
                case Technique.MineArc:
                    for (int i = -1; i <= 1; i++)
                        MarkGround(aim.x + i * 2.1f, 1.22f, 1.37f, CurrentWindup + (1 + i) * 0.14f, 0.29f, tint, 2);
                    break;
                case Technique.VaneRush:
                    MarkGround(Position.x + facing * 2.55f, 5.3f, 0.26f, CurrentWindup, 0.26f, tint, 1);
                    break;
                case Technique.VanceLock:
                    for (int i = -1; i <= 1; i += 2)
                        demo.Effects.Hazard(this, aim + new Vector2(i * 1.34f, 0.56f), new Vector2(0.45f, 3.42f),
                            i * 19f, tint, CurrentWindup, 0.36f, 2, new Vector2(-i * 4.1f, 0f));
                    break;
                case Technique.EmberRain:
                    for (int i = -2; i <= 2; i++)
                        MarkGround(aim.x + i * 1.48f, 0.82f, 5.7f, CurrentWindup + Mathf.Abs(i) * 0.16f, 0.42f, tint, 2);
                    break;
                case Technique.TailSweep:
                    MarkGround(Position.x + facing * 3.7f, 7.8f, 0.72f, CurrentWindup, 0.38f, tint, 2);
                    break;
                case Technique.OrbitalShock:
                    for (int i = -1; i <= 1; i += 2)
                        for (int j = 1; j <= 3; j++)
                            MarkGround(Position.x + i * j * 2.3f, 0.87f, 2.72f,
                                CurrentWindup + 0.12f * j, 0.25f, tint, 2);
                    break;
                case Technique.MoonLattice:
                    Vector2 eye = Position + new Vector2(facing * 0.65f, 0.64f);
                    float tilt = facing * 28f;
                    float beam = DirectionAngle((aim - eye).normalized) + tilt * 0.6f;
                    demo.Effects.Hazard(this, eye, new Vector2(11.5f, 0.31f), beam, tint,
                        CurrentWindup, 0.39f, 2).ConfigureSweep(eye, 11.5f, beam, -tilt);
                    for (int i = -1; i <= 1; i++)
                        MarkGround(aim.x + i * 2.8f, 0.57f, 5.9f, CurrentWindup + 0.39f, 0.31f, tint, 2);
                    break;
            }
        }

        private void StartTechnique()
        {
            pulse = 0;
            var tint = TechniqueColor();
            switch (technique)
            {
                case Technique.ZombieLunge:
                    body.linearVelocity = new Vector2(facing * 8.8f, 4.75f);
                    break;
                case Technique.Pounce:
                    body.linearVelocity = new Vector2(facing * 9.6f, 5.3f);
                    Flash(1.38f, 1.51f, 0.20f);
                    demo.Effects.Burst(FootPoint(), tint, 14, 3.2f);
                    break;
                case Technique.GroundSmash:
                    foreach (int direction in new[] { -1, 1 })
                    {
                        Vector2 start = FootPoint() + new Vector2(direction * 0.77f, 0.31f);
                        demo.Effects.Hazard(this, start, new Vector2(1.42f, 0.56f), 0f, tint,
                            0f, 0.54f, 2, Vector2.right * (direction * 8.2f));
                    }
                    demo.Effects.Burst(FootPoint(), tint, 19, 4f);
                    break;
                case Technique.Feint:
                    if (canFeint)
                    {
                        demo.Effects.Burst(Position, tint, 17, 4.5f);
                        body.position = landAt;
                        facing = aim.x < landAt.x ? -1 : 1;
                        demo.Effects.Burst(landAt, tint, 20, 4.5f);
                    }
                    Flash(2.6f, 1.45f, 0.19f);
                    break;
                case Technique.Scan:
                    Vector2 p = FootPoint() + new Vector2(facing * 1.03f, 0.85f);
                    demo.Effects.Hazard(this, p, new Vector2(0.43f, 1.60f), 0f, tint, 0.10f, 0.78f,
                        2, Vector2.right * (facing * 9.3f));
                    break;
                case Technique.CrossCut:
                    demo.Effects.Hazard(this, aim, new Vector2(3.4f, 0.18f), 30f, tint, 0.06f, 0.16f, 2);
                    demo.Effects.Hazard(this, aim, new Vector2(3.4f, 0.18f), -30f, tint, 0.11f, 0.16f, 2);
                    Flash(1.94f, 1.26f, 0.19f);
                    break;
                case Technique.DoubleClaw:
                    Flash(1.80f, 1.28f, 0.15f);
                    break;
                case Technique.GuardianRake:
                    Flash(2.32f, 1.30f, 0.22f);
                    demo.Effects.Sound(1);
                    break;
                case Technique.Cannon:
                    demo.Effects.Burst(Muzzle(), tint, 20, 7.6f);
                    demo.Effects.Sound(1);
                    break;
                case Technique.FireBreath:
                    Flash(1.81f, 0.91f, 0.18f);
                    break;
            }
        }

        private float AdvanceTechnique(Vector2 offset)
        {
            float elapsed = Specials[(int)technique].Active - timer;
            switch (technique)
            {
                case Technique.ZombieRake:
                    if (elapsed > 0.058f)
                        ResolveMelee(offset, reach: 1.74f, height: 0.96f);
                    return facing * 2.6f;
                case Technique.ZombieLunge:
                    if (elapsed > 0.068f)
                        ResolveMelee(offset, reach: 1.43f, height: 1.2f);
                    return facing * 8.55f;
                case Technique.ZombieHammer:
                    if (elapsed > 0.077f)
                        ResolveMelee(offset, reach: 2.04f, height: 0.91f);
                    return facing * 2.25f;
                case Technique.ZombieClutch:
                    if (elapsed > 0.071f)
                        ResolveMelee(offset, reach: 1.85f, height: 1.20f);
                    return facing * 5.25f;
                case Technique.DoubleClaw:
                    if (pulse == 0 && elapsed > 0.16f)
                    {
                        pulse++;
                        resolved = false;
                        Flash(1.93f, 1.45f, 0.14f);
                    }
                    ResolveMelee(offset);
                    return facing * 2.0f;
                case Technique.Pounce:
                    ResolveMelee(offset, reach: 1.30f, height: 1.26f);
                    return facing * 10.2f;
                case Technique.GuardianRake:
                    ResolveMelee(offset, reach: 2.02f, height: 1.18f);
                    return facing * 8.5f;
                case Technique.Feint:
                    ResolveMelee(offset, reach: 1.76f, height: 1.27f);
                    return facing * 11.8f;
                case Technique.CrossCut:
                    ResolveMelee(offset);
                    return facing * 15f;
                case Technique.VaneRush:
                    ResolveMelee(offset, reach: 1.65f, height: 1.16f);
                    return facing * 15f;
                case Technique.Burst:
                    if (pulse < 3 && elapsed >= pulse * 0.16f)
                    {
                        FireAimed(0f, 12.1f);
                        pulse++;
                    }
                    break;
                case Technique.FireBreath:
                    if (pulse < 6 && elapsed >= pulse * 0.13f)
                    {
                        float arc = Mathf.Lerp(17f, -21f, pulse / 5f);
                        FireAimed(arc, 8.5f);
                        FireAimed(arc + 11f, 7.3f);
                        pulse++;
                    }
                    break;
                case Technique.VanceNeedles:
                case Technique.MissileSalvo:
                case Technique.MoonOrbs:
                    if (pulse < 4 && elapsed >= pulse * 0.15f)
                    {
                        float angle = (pulse - 1.5f) * 12.0f;
                        FireAimed(angle, technique == Technique.VanceNeedles ? 17.1f : 10.2f);
                        if (technique == Technique.MoonOrbs)
                            FireAimed(-angle + 5f, 7.5f);
                        pulse++;
                    }
                    break;
                case Technique.GuardianRocket:
                    if (pulse < 2 && elapsed >= pulse * 0.25f)
                    {
                        FireAimed(pulse == 0 ? -7f : 10f, 11.6f);
                        pulse++;
                    }
                    break;
            }
            return 0f;
        }

        private void FireAimed(float angle, float speed)
        {
            Vector2 muzzle = Muzzle();
            Vector2 direction = (aim - muzzle).normalized;
            demo.Effects.FireBolt(this, muzzle, Quaternion.Euler(0f, 0f, facing * angle) * direction,
                speed * (IsBoss ? 1.18f : veteran ? 1.09f : 1f), TechniqueColor());
        }

        private void Flash(float width, float height, float life) => demo.Effects.TechniqueFlash(technique,
            Position + new Vector2(facing * width * 0.47f, 0.19f), facing, new Vector2(width, height), life);

        private void MarkGround(float x, float width, float height, float delay, float duration, Color tint, int damage)
        {
            var floor = Physics2D.Raycast(new Vector2(x, Position.y + 1.2f), Vector2.down, 4f, terrain);
            if (floor)
                demo.Effects.Hazard(this, floor.point + Vector2.up * (height * 0.5f),
                    new Vector2(width, height), 0f, tint, delay, duration, damage);
        }

        private Vector2 Muzzle() => Position + new Vector2(facing * (boss == Boss.Vulkano ? 1.12f : 0.55f), 0.20f);
        private Vector2 FootPoint() => new(Position.x, hull.bounds.min.y + 0.025f);
        private static float DirectionAngle(Vector2 heading) => Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;

        private Color TechniqueColor() => technique switch
        {
            Technique.Rupture => new(0.72f, 0.60f, 1f),
            Technique.MineArc or Technique.VaneRush => new(1f, 0.43f, 0.28f),
            Technique.Feint or Technique.VanceNeedles or Technique.VanceLock => new(1f, 0.32f, 0.55f),
            Technique.Cannon or Technique.MissileSalvo or Technique.OrbitalShock => new(0.39f, 0.91f, 1f),
            Technique.Scan or Technique.LaserGrid or Technique.CrossCut or Technique.MoonLattice or Technique.MoonOrbs => new(0.93f, 0.41f, 1f),
            Technique.FireBreath or Technique.EmberRain or Technique.TailSweep => new(1f, 0.43f, 0.17f),
            Technique.GuardianRake or Technique.GuardianEmp => new(0.46f, 0.83f, 1f),
            _ => new(1f, 0.69f, 0.32f)
        };
    }
}

