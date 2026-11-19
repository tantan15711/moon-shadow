using UnityEngine;

namespace MoonShadow
{
    public sealed partial class RuinsEffects
    {
        private enum Pattern { Cartridge, Venom, Needle, Cinder, Missile, Ion, Prism, Edge, Fracture, Lattice }
        public const int AttackStyles = 10;
        public const int AttackFrames = 4;
        [SerializeField] private Sprite[] enemyRounds, specialMoves;
        [SerializeField] private Sprite dangerOutline, impactStar;
        [SerializeField] private Sprite[] attacks, katana;
        [SerializeField] private Sprite[] chainArt;
        [SerializeField] private Sprite guardArt;
        internal Sprite ReflectedRound => KatanaArt(6);

        public void ConfigureAttackArt(Sprite[] rounds, Sprite[] hazards, Sprite outline, Sprite star)
        {
            enemyRounds = rounds;
            specialMoves = hazards;
            dangerOutline = outline;
            impactStar = star;
        }

        public void ConfigureMoveArt(Sprite[] opponents, Sprite[] sword)
        {
            attacks = opponents;
            katana = sword;
        }

        public void ConfigureChainArt(Sprite[] sprites, Sprite guard)
        {
            chainArt = sprites;
            guardArt = guard;
        }

        internal void HandImpact(Vector2 position, int facing)
        {
            var art = ArtAt(chainArt, 7);
            if (!art)
                return;
            var hit = Rent(art, position, Color.white, 22, Shape(art, new Vector2(0.83f, 0.77f)));
            hit.flipX = facing < 0;
            Fade(hit, new Vector2(facing * 1.5f, 0.45f), 0.13f);
        }

        private static Sprite ArtAt(Sprite[] sprites, int index) =>
            sprites != null && (uint)index < (uint)sprites.Length ? sprites[index] : null;
        internal Sprite AttackArt(EnemyBase2D.Technique move) => ArtAt(attacks, (int)move);
        private Sprite KatanaArt(int index) => ArtAt(katana, index);

        private static Pattern Style(EnemyBase2D.Technique technique) => technique switch
        {
            EnemyBase2D.Technique.MineArc => Pattern.Venom,
            EnemyBase2D.Technique.VanceNeedles or EnemyBase2D.Technique.VanceLock => Pattern.Needle,
            EnemyBase2D.Technique.FireBreath or EnemyBase2D.Technique.EmberRain or EnemyBase2D.Technique.TailSweep => Pattern.Cinder,
            EnemyBase2D.Technique.MissileSalvo or EnemyBase2D.Technique.GuardianRocket => Pattern.Missile,
            EnemyBase2D.Technique.Cannon or EnemyBase2D.Technique.OrbitalShock => Pattern.Ion,
            EnemyBase2D.Technique.Scan or EnemyBase2D.Technique.LaserGrid or EnemyBase2D.Technique.MoonOrbs => Pattern.Prism,
            EnemyBase2D.Technique.MoonLattice => Pattern.Lattice,
            EnemyBase2D.Technique.Rupture or EnemyBase2D.Technique.GroundSmash => Pattern.Fracture,
            EnemyBase2D.Technique.VaneRush or EnemyBase2D.Technique.CrossCut or EnemyBase2D.Technique.Feint => Pattern.Edge,
            _ => Pattern.Cartridge
        };

        private static Sprite Frame(Sprite[] sequence, Pattern style, int animationFrame)
        {
            int index = (int)style * AttackFrames + animationFrame % AttackFrames;
            return sequence != null && index < sequence.Length ? sequence[index] : null;
        }

        public Sprite EnemyRound(EnemyBase2D.Technique technique, int animationFrame)
        {
            var art = AttackArt(technique);
            return art ? art : Frame(enemyRounds, Style(technique), animationFrame);
        }

        private Sprite EnergyRound(bool legendary) => Frame(enemyRounds, legendary ? Pattern.Prism : Pattern.Ion, 0);

        internal static Vector2 RoundSize(EnemyBase2D.Technique technique) => Style(technique) switch
        {
            Pattern.Needle => new Vector2(0.45f, 0.16f),
            Pattern.Cinder => new Vector2(0.51f, 0.28f),
            Pattern.Missile => new Vector2(0.54f, 0.25f),
            Pattern.Prism => new Vector2(0.34f, 0.30f),
            _ => new Vector2(0.35f, 0.19f)
        };

        internal Sprite SpecialFrame(EnemyBase2D.Technique technique, int animationFrame) =>
            Frame(specialMoves, Style(technique), animationFrame);

        internal void TechniqueFlash(EnemyBase2D.Technique move, Vector2 point, int facing, Vector2 size, float life)
        {
            var art = AttackArt(move);
            if (!art)
                return;
            var visual = Rent(art, point, Color.white, 21, Shape(art, size));
            visual.flipX = facing < 0;
            Fade(visual, Vector2.right * (facing * 2.7f), life);
        }

        internal void TellTechnique(EnemyBase2D.Technique move, Vector2 position, int facing, Color hue, float duration)
        {
            Sprite art = AttackArt(move);
            if (!art)
                return;
            hue.a = 0.39f;
            var hint = Rent(art, position + new Vector2(facing * 0.63f, 0.26f), hue, 14,
                Shape(art, new Vector2(1.33f, 0.95f)));
            hint.flipX = facing < 0;
            Fade(hint, Vector2.zero, duration);
        }

        internal void TellProjectile(Vector2 muzzle, Vector2 target, Color hue, float duration)
        {
            Vector2 offset = target - muzzle;
            if (offset.sqrMagnitude < 0.01f)
                return;
            Vector2 direction = offset.normalized;
            hue.a = 0.58f;
            for (int i = 0; i < 3; i++)
                Streak(muzzle + direction * (0.27f + i * 0.47f), direction * 0.08f, hue, 0.24f, duration);
        }

        internal void Deflection(Vector2 contact)
        {
            var star = KatanaArt(3);
            if (star)
                Fade(Rent(star, contact, Color.white, 25, Shape(star, new Vector2(1.24f, 1.13f))),
                    Vector2.zero, 0.125f, spin: 140f);
            var flecks = KatanaArt(7);
            if (flecks)
                Fade(Rent(flecks, contact, new Color(0.67f, 0.90f, 1f, 0.80f), 24,
                    Shape(flecks, new Vector2(1.57f, 1.45f))), Vector2.up * 0.9f, 0.18f, spin: -100f);
        }

        internal void KatanaWake(Vector2 position, int facing)
        {
            var wake = KatanaArt(4);
            if (!wake)
                return;
            var spark = Rent(wake, position - Vector2.right * (facing * 0.74f), Color.white, 19,
                Shape(wake, new Vector2(1.85f, 0.65f)));
            spark.flipX = facing < 0;
            Fade(spark, -Vector2.right * (facing * 1.8f), 0.15f);
        }

        internal void KatanaContact(Vector2 position, int facing, bool heavy, Color tint)
        {
            var edge = ArtAt(chainArt, 5) ?? KatanaArt(5);
            if (!edge)
            {
                BladeImpact(position, facing, heavy, tint);
                return;
            }
            var fan = Rent(edge, position, Color.Lerp(tint, Color.white, 0.6f), 24,
                Shape(edge, heavy ? new Vector2(1.33f, 1.02f) : new Vector2(0.79f, 0.66f)));
            fan.flipX = facing < 0;
            Fade(fan, new Vector2(facing * 2.7f, 0.65f), heavy ? 0.13f : 0.091f);
            var sparks = ArtAt(chainArt, 6);
            if (sparks)
                Fade(Rent(sparks, position, Color.white, 23, Shape(sparks, Vector2.one * (heavy ? 1.04f : 0.64f))),
                    new Vector2(facing * 1.8f, 1.1f), 0.16f, spin: facing * 145f);
        }

        internal void IntroduceBoss(EnemyBase2D boss)
        {
            var style = boss.Profile;
            Color tint = style switch
            {
                EnemyBase2D.Boss.Vane => new Color(0.83f, 0.51f, 0.27f),
                EnemyBase2D.Boss.Vance => new Color(0.84f, 0.47f, 0.89f),
                EnemyBase2D.Boss.Vulkano => new Color(1f, 0.31f, 0.13f),
                EnemyBase2D.Boss.Orbital => new Color(0.33f, 0.78f, 1f),
                _ => new Color(0.70f, 0.51f, 1f)
            };
            Burst(boss.Position, tint, 16, 5.5f);
            Cue(boss.Position, tint, 0.68f, style == EnemyBase2D.Boss.Vulkano ? 1.62f : 1.16f);
        }

        public void BladeImpact(Vector2 location, int facing, bool heavy, Color tint)
        {
            if (!impactStar)
                return;
            var star = Rent(impactStar, location, tint, 24, Shape(impactStar, heavy ? new Vector2(1.26f, 0.97f) :
                new Vector2(0.84f, 0.58f)));
            star.transform.rotation = Quaternion.Euler(0f, 0f, facing * Random.Range(15f, 46f));
            Fade(star, Vector2.right * facing * 1.8f, heavy ? 0.11f : 0.072f);
            for (int i = 0; i < (heavy ? 8 : 5); i++)
            {
                Vector2 velocity = new(facing * Random.Range(3f, 11f), Random.Range(-3.5f, 6.1f));
                Streak(location, velocity, tint, Random.Range(0.12f, 0.36f), 0.095f);
            }
        }

        internal void Streak(Vector2 location, Vector2 speed, Color tint, float length, float lifetime)
        {
            var trail = Rent(impactStar ? impactStar : pixel, location, tint, 18,
                Shape(impactStar ? impactStar : pixel, new Vector2(length, length * 0.22f)));
            trail.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(speed.y, speed.x) * Mathf.Rad2Deg);
            Fade(trail, speed, lifetime);
        }

        private static Vector2 Shape(Sprite art, Vector2 size) => size / (Vector2)art.bounds.size;
    }
}
