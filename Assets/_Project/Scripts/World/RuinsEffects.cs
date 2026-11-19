using System.Collections.Generic;
using UnityEngine;

namespace MoonShadow
{
    [RequireComponent(typeof(AudioSource))]
    public sealed partial class RuinsEffects : MonoBehaviour
    {
        private sealed class Fleck
        {
            public SpriteRenderer View;
            public Vector2 Velocity;
            public float Remaining;
            public float Duration;
            public float Gravity;
            public float Spin;
            public Color Tint;
            public Sprite[] Frames;
        }

        private sealed class Afterimage
        {
            public MeshRenderer View;
            public Mesh Pose;
            public readonly MaterialPropertyBlock Properties = new();
            public float Remaining;
        }

        private readonly Afterimage[] dashGhosts = new Afterimage[6];
        private static readonly int GhostTint = Shader.PropertyToID("_Tint");
        private int nextGhost;

        [SerializeField] private Sprite pixel;
        [SerializeField] private Sprite slash;
        [SerializeField] private Sprite ring;
        [SerializeField] private Sprite medkit;
        [SerializeField] private Sprite bullet, orb, beam;
        [SerializeField] private Sprite[] blueSlash;
        [SerializeField] private Sprite[] rocks;
        [SerializeField] private AudioClip[] sounds;
        [SerializeField] private LayerMask terrain;
        private readonly List<Fleck> active = new();
        private readonly Stack<Fleck> spareFlecks = new();
        private readonly Stack<SpriteRenderer> pool = new();
        private readonly List<(SpriteRenderer View, int EnemyIndex)> medkits = new();
        private readonly List<PlayerRound2D> rounds = new();
        private readonly Stack<PlayerRound2D> spareRounds = new();
        private readonly List<EnemyBolt2D> bolts = new();
        private readonly List<EnemyHazard> hazards = new();
        private AudioSource speaker;
        private RuinsDemo demo;

        public void Configure(Sprite dot, Sprite arc, Sprite guard, Sprite heal, AudioClip[] clips, LayerMask mask)
        {
            pixel = dot;
            slash = arc;
            ring = guard;
            medkit = heal;
            sounds = clips;
            terrain = mask;
        }

        public void ConfigurePixelArt(Sprite cartridge, Sprite plasma, Sprite light, Sprite[] sword, Sprite[] stones)
        {
            bullet = cartridge;
            orb = plasma;
            beam = light;
            blueSlash = sword;
            rocks = stones;
        }

        private void Awake() => speaker = GetComponent<AudioSource>();
        private void Start() => demo = FindAnyObjectByType<RuinsDemo>();

        private void Update()
        {
            if (!demo.Playing)
                return;
            float delta = Time.deltaTime;
            foreach (var echo in dashGhosts)
            {
                if (echo == null || !echo.View.enabled)
                    continue;
                echo.Remaining -= delta;
                echo.View.enabled = echo.Remaining > 0f;
                echo.Properties.SetColor(GhostTint, new Color(0.34f, 0.75f, 1f, Mathf.Max(0f, echo.Remaining * 2.15f)));
                echo.View.SetPropertyBlock(echo.Properties);
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var fleck = active[i];
                fleck.Remaining -= delta;
                if (fleck.Remaining <= 0f)
                {
                    Recycle(fleck.View);
                    active.RemoveAt(i);
                    spareFlecks.Push(fleck);
                    continue;
                }

                fleck.Velocity += Vector2.down * (fleck.Gravity * delta);
                fleck.View.transform.position += (Vector3)(fleck.Velocity * delta);
                if (fleck.Spin != 0f)
                    fleck.View.transform.Rotate(0f, 0f, fleck.Spin * delta);
                if (fleck.Frames != null)
                {
                    int frame = Mathf.Min(fleck.Frames.Length - 1,
                        (int)((1f - fleck.Remaining / fleck.Duration) * fleck.Frames.Length));
                    fleck.View.sprite = fleck.Frames[frame];
                }
                Color c = fleck.Tint;
                c.a *= Mathf.Clamp01(fleck.Remaining / (fleck.Duration * 0.65f));
                fleck.View.color = c;
            }
            for (int i = medkits.Count - 1; i >= 0; i--)
            {
                var (kit, enemyIndex) = medkits[i];
                if ((demo.Player.Position - (Vector2)kit.transform.position).sqrMagnitude > 1.18f ||
                    Physics2D.Linecast(demo.Player.Position, kit.transform.position, terrain))
                    continue;

                if (CampaignProgress.ClaimMedkit(enemyIndex))
                    demo.Message("Botiquín guardado · H para usar", 2.2f);
                MedkitSpark(kit.transform.position);
                Sound(5);
                Recycle(kit);
                medkits.RemoveAt(i);
            }
        }

        private SpriteRenderer Rent(Sprite art, Vector2 position, Color color, int order, Vector2 scale)
        {
            SpriteRenderer visual;
            if (pool.Count > 0)
                visual = pool.Pop();
            else
            {
                visual = new GameObject("Efecto").AddComponent<SpriteRenderer>();
                visual.transform.SetParent(transform, false);
            }
            visual.sprite = art;
            visual.flipX = false;
            visual.color = color;
            visual.sortingOrder = order;
            visual.transform.SetPositionAndRotation(position, Quaternion.identity);
            visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            visual.gameObject.SetActive(true);
            return visual;
        }

        private void Recycle(SpriteRenderer visual)
        {
            visual.gameObject.SetActive(false);
            pool.Push(visual);
        }

        private void Fade(SpriteRenderer visual, Vector2 motion, float duration, float gravity = 0f, float spin = 0f,
            Sprite[] frames = null)
        {
            var fleck = spareFlecks.Count > 0 ? spareFlecks.Pop() : new Fleck();
            fleck.View = visual;
            fleck.Velocity = motion;
            fleck.Duration = fleck.Remaining = duration;
            fleck.Gravity = gravity;
            fleck.Tint = visual.color;
            fleck.Spin = spin;
            fleck.Frames = frames;
            active.Add(fleck);
        }

        public void Burst(Vector2 position, Color tint, int count, float speed)
        {
            if (RuntimePerformance.Mobile)
                count = Mathf.Min(Mathf.CeilToInt(count * .6f), Mathf.Max(0, 180 - active.Count));
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Random.insideUnitCircle * speed + Vector2.up * (speed * 0.29f);
                float size = Random.Range(0.05f, 0.14f);
                var view = Rent(pixel, position, tint, 22, Vector2.one * size / pixel.bounds.size.x);
                view.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
                Fade(view, velocity, Random.Range(0.13f, 0.41f), 12f);
            }
        }

        [SerializeField] private Sprite[] dashShards;

        public void ConfigureDash(Sprite[] sprites) => dashShards = sprites;

        internal void LandingDust(Vector2 floor, int facing, float weight)
        {
            Color tint = demo.Stage.Act switch
            {
                2 => new Color(0.66f, 0.39f, 0.29f, 0.65f),
                4 => new Color(0.69f, 0.79f, 0.93f, 0.63f),
                _ => new Color(0.65f, 0.70f, 0.76f, 0.62f)
            };
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f;
                Vector2 size = new(Random.Range(0.045f, 0.11f), Random.Range(0.032f, 0.068f));
                var chip = Rent(pixel, floor + new Vector2(side * Random.Range(0.10f, 0.37f), 0.065f), tint, 9, size);
                Vector2 drift = new(side * Random.Range(0.8f, 2.4f) - facing * 0.45f, Random.Range(0.64f, 1.46f));
                Fade(chip, drift * weight, Random.Range(0.14f, 0.26f), 4.3f, Random.Range(-170f, 170f));
            }
        }

        public void DashSparks(Vector2 position, int facing, int count)
        {
            if (dashShards == null || dashShards.Length == 0)
                return;
            for (int i = 0; i < count; i++)
            {
                var art = dashShards[Random.Range(0, dashShards.Length)];
                float width = Random.Range(0.12f, 0.26f);
                var shard = Rent(art, position + Random.insideUnitCircle * 0.19f, Color.white, 22,
                    Vector2.one * (width / art.bounds.size.x));
                shard.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-35f, 30f));
                Vector2 impulse = new(-facing * Random.Range(2.4f, 6.2f), Random.Range(-0.6f, 2.2f));
                Fade(shard, impulse, Random.Range(0.13f, 0.29f), 4f, Random.Range(-150f, 150f));
            }
        }

        internal void Ghost(CastRig actor)
        {
            if (!actor || !actor.Visible)
                return;
            var echo = dashGhosts[nextGhost];
            if (echo == null)
            {
                var model = new GameObject("Estela del dash", typeof(MeshRenderer), typeof(MeshFilter));
                model.transform.SetParent(transform, false);
                var pose = Instantiate(actor.Deformation);
                model.GetComponent<MeshFilter>().sharedMesh = pose;
                echo = new Afterimage { View = model.GetComponent<MeshRenderer>(), Pose = pose };
                dashGhosts[nextGhost] = echo;
            }
            nextGhost = (nextGhost + 1) % dashGhosts.Length;
            actor.CopyAfterimage(echo.View, echo.Pose, echo.Properties);
            echo.Remaining = 0.18f;
            echo.Properties.SetColor(GhostTint, new Color(0.34f, 0.75f, 1f, 0.38f));
            echo.View.SetPropertyBlock(echo.Properties);
            echo.View.enabled = true;
        }

        private void OnDestroy()
        {
            foreach (var echo in dashGhosts)
                if (echo != null)
                    Destroy(echo.Pose);
        }

        public void PlayerStrike(Vector2 position, int facing, float angle, bool radial, bool heavy, bool rising, Color color,
            int art, float reach)
        {
            if (radial)
            {
                Vector2 floor = position + Vector2.down * 0.52f;
                var wave = Rent(ring, floor, color, 20, new Vector2(3.38f, 0.68f));
                Fade(wave, Vector2.zero, 0.20f);
                for (int i = -1; i <= 1; i += 2)
                    Streak(floor + Vector2.right * (i * 0.40f), new Vector2(i * 12f, 0.6f), color, 1.04f, 0.16f);
                Burst(floor, color, 24, 6f);
                return;
            }

            var crescentArt = ArtAt(chainArt, art) ?? KatanaArt(Mathf.Clamp(art, 0, 2));
            if (crescentArt)
            {
                Vector2 center = position + new Vector2(facing * (reach * 0.51f + 0.08f), rising ? 0.34f : 0.13f);
                float height = art switch { 0 => 1.48f, 1 => 2.45f, 2 when !rising => 1.28f, _ => 2.27f };
                var curve = Rent(crescentArt, center, Color.Lerp(Color.white, color, 0.20f), 21,
                    Shape(crescentArt, new Vector2(reach + 0.4f, height)));
                curve.flipX = facing < 0;
                curve.transform.rotation = Quaternion.Euler(0f, 0f, facing * (angle * 0.26f + (rising ? -14f : 11f)));
                Fade(curve, new Vector2(facing * 3.4f, rising ? 1.22f : -0.48f), heavy ? 0.15f : 0.11f,
                    spin: facing * (rising ? 245f : -245f));
                var wakeArt = ArtAt(chainArt, 4);
                if (wakeArt)
                {
                    var wake = Rent(wakeArt, center + Vector2.right * (facing * 0.42f),
                        new Color(0.56f, 0.86f, 1f, 0.64f), 20, Shape(wakeArt, new Vector2(1.54f, 0.42f)));
                    wake.flipX = facing < 0;
                    wake.transform.rotation = curve.transform.rotation;
                    Fade(wake, new Vector2(facing * 6.2f, 0.08f), 0.11f);
                }
                return;
            }

            if (blueSlash?.Length > 0)
            {
                float arcScale = heavy ? 1.37f : 1.13f;
                Vector2 center = position + new Vector2(facing * 0.60f, 0.14f);
                var crescent = Rent(blueSlash[0], center, Color.Lerp(Color.white, color, 0.29f), 21, Vector2.one * arcScale);
                crescent.flipX = facing < 0;
                crescent.transform.rotation = Quaternion.Euler(0f, 0f, facing * angle * 0.57f);
                Vector2 sweep = new(facing * 2.9f, rising ? 1.35f : -0.38f);
                float spinRate = facing * (rising ? 190f : -190f);
                Fade(crescent, sweep, heavy ? 0.186f : 0.147f, spin: spinRate, frames: blueSlash);
                var wake = Rent(blueSlash[1], center + new Vector2(-facing * 0.12f, -0.07f),
                    new Color(0.42f, 0.74f, 1f, 0.52f), 20, Vector2.one * (arcScale * 1.09f));
                wake.flipX = crescent.flipX;
                wake.transform.rotation = Quaternion.Euler(0f, 0f, facing * angle * 0.57f + facing * 12f);
                Fade(wake, sweep * 0.62f, 0.12f, spin: spinRate * 0.78f);
                for (int i = 0; i < 5; i++)
                    Streak(center + Vector2.right * (facing * 0.38f) + Random.insideUnitCircle * 0.59f,
                        new Vector2(facing * Random.Range(9f, 17f), Random.Range(-3.6f, 3.6f)), color,
                        Random.Range(0.19f, 0.49f), 0.093f);
                return;
            }

            position += new Vector2(facing * 0.91f, 0.17f);
            Vector2 impulse = new(facing * (heavy ? 5.8f : 7.7f), heavy ? 2.4f : 0f);
            float size = heavy ? 1.27f : 1.03f;
            float spin = facing * (rising ? 325f : -325f);
            var tail = Rent(slash, position - Vector2.right * (facing * 0.12f),
                new Color(color.r, color.g, color.b, 0.42f), 18, new Vector2(size * 1.26f, size * 1.19f));
            var arc = Rent(slash, position, color, 20, new Vector2(size * 1.34f, size * 0.91f));
            var edge = Rent(slash, position + Vector2.right * (facing * 0.15f), Color.white,
                21, new Vector2(size * 1.25f, size * 0.59f));
            tail.flipX = arc.flipX = edge.flipX = facing < 0;
            tail.transform.rotation = Quaternion.Euler(0f, 0f, facing * (angle - 16f));
            arc.transform.rotation = edge.transform.rotation = Quaternion.Euler(0f, 0f, facing * angle);
            Fade(tail, impulse * 0.53f, 0.15f, spin: spin * 0.57f);
            Fade(arc, impulse, 0.105f, spin: spin);
            Fade(edge, impulse * 1.27f, 0.061f, spin: spin);
            for (int i = 0; i < 8; i++)
            {
                Vector2 travel = new(facing * Random.Range(7f, 17f), Random.Range(-2.6f, 2.6f));
                var streak = Rent(pixel, position + (Vector2)Random.insideUnitCircle * 0.82f,
                    i % 3 == 0 ? Color.white : color, 19, new Vector2(Random.Range(7f, 17f), 0.7f));
                streak.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg);
                Fade(streak, travel, Random.Range(0.044f, 0.10f));
            }
        }

        public void EnemySlash(Vector2 position, int facing, EnemyBase2D.Kind kind, EnemyBase2D.Technique move)
        {
            bool brute = kind == EnemyBase2D.Kind.Brute;
            Sprite art = AttackArt(move);
            Vector2 point = position + Vector2.right * (facing * (brute ? 1.0f : 0.67f));
            if (art)
            {
                TechniqueFlash(move, point, facing, brute ? new Vector2(2.57f, 1.31f) : new Vector2(1.64f, 1.18f), 0.16f);
                return;
            }
            var view = Rent(slash, point, new Color(1f, 0.43f, 0.35f, 0.82f), 18,
                Vector2.one * (brute ? 1.32f : 0.73f));
            view.flipX = facing < 0;
            view.transform.rotation = Quaternion.Euler(0, 0, brute ? -35f * facing : 0f);
            float speed = kind == EnemyBase2D.Kind.Runner ? 9f : 1f;
            Fade(view, Vector2.right * (speed * facing),
                brute ? 0.24f : 0.18f);
            if (brute)
                Sound(1);
        }

        public void FireBolt(EnemyBase2D source, Vector2 position, Vector2 heading,
            float speed = EnemyBolt2D.DefaultSpeed, Color? tint = null)
        {
            bolts.RemoveAll(bolt => !bolt);
            var shot = new GameObject("Proyectil de centinela").AddComponent<SpriteRenderer>();
            shot.transform.SetParent(transform, false);
            shot.transform.position = position;
            var technique = source.ProjectileTechnique;
            shot.sprite = EnemyRound(technique, 0) ?? orb ?? pixel;
            shot.transform.localScale = Shape(shot.sprite, RoundSize(technique));
            Color tracer = tint ?? new Color(1f, 0.62f, 0.37f);
            shot.color = Color.white;
            shot.sortingOrder = 17;
            var bolt = shot.gameObject.AddComponent<EnemyBolt2D>();
            bolt.Configure(demo, this, source, shot, heading, terrain, speed, technique, tracer);
            bolts.Add(bolt);
            BladeImpact(position, heading.x < 0f ? -1 : 1, false, tracer);
            Sound(2);
        }

        public void ClearBolts()
        {
            foreach (var bolt in bolts)
            {
                if (!bolt)
                    continue;
                bolt.gameObject.SetActive(false);
                Destroy(bolt.gameObject);
            }
            bolts.Clear();
            foreach (var round in rounds)
            {
                if (!round)
                    continue;
                round.gameObject.SetActive(false);
                spareRounds.Push(round);
            }
            rounds.Clear();
            foreach (var hazard in hazards)
            {
                if (!hazard)
                    continue;
                hazard.gameObject.SetActive(false);
                Destroy(hazard.gameObject);
            }
            hazards.Clear();
        }

        internal void ClearTransientEffects()
        {
            foreach (var fleck in active)
            {
                Recycle(fleck.View);
                spareFlecks.Push(fleck);
            }
            active.Clear();
            foreach (var echo in dashGhosts)
                if (echo != null)
                    echo.View.enabled = false;
        }

        public EnemyHazard Hazard(EnemyBase2D source, Vector2 location, Vector2 size, float angle, Color color,
            float delay, float activeTime, int damage, Vector2 drift = default)
        {
            hazards.RemoveAll(hazard => !hazard);
            var root = new GameObject("Ataque especial");
            root.transform.SetParent(transform, false);
            root.transform.SetPositionAndRotation(location, Quaternion.Euler(0f, 0f, angle));
            var view = new GameObject("Área de peligro").AddComponent<SpriteRenderer>();
            view.transform.SetParent(root.transform, false);
            bool rolling = drift.sqrMagnitude > 0.1f && source.CurrentTechnique == EnemyBase2D.Technique.GroundSmash;
            bool rupture = source.CurrentTechnique == EnemyBase2D.Technique.Rupture;
            Sprite[] stoneFrames = rolling || rupture ? rocks : null;
            view.sprite = dangerOutline ? dangerOutline : pixel;
            view.transform.localScale = Shape(view.sprite, size);
            view.sortingOrder = 17;
            var hazard = root.AddComponent<EnemyHazard>();
            hazard.Configure(demo, source, view, color, size, delay, activeTime, damage, drift,
                terrain, stoneFrames, rolling, source.CurrentTechnique);
            hazards.Add(hazard);
            return hazard;
        }

        public void Cue(Vector2 position, Color color, float delay, float size)
        {
            color.a = 0.59f;
            var marker = Rent(ring, position, color, 15, Vector2.one * size);
            Fade(marker, Vector2.zero, delay, spin: 40f);
        }

        public void Gunshot(Vector2 start, Vector2 direction, Sidearm weapon)
        {
            bool legendary = weapon == Sidearm.Legendary;
            bool experimental = weapon == Sidearm.Experimental;
            Color tracer = legendary ? new Color(0.88f, 0.64f, 1f) : new Color(1f, 0.76f, 0.43f);
            var roundArt = legendary || experimental ? EnergyRound(legendary) : bullet;
            if (!roundArt)
                roundArt = orb ? orb : pixel;
            var movement = spareRounds.Count > 0 ? spareRounds.Pop() :
                new GameObject("Bala de Kyle", typeof(SpriteRenderer), typeof(PlayerRound2D)).GetComponent<PlayerRound2D>();
            var round = movement.gameObject;
            round.transform.SetParent(transform, false);
            round.transform.position = start;
            var sprite = round.GetComponent<SpriteRenderer>();
            sprite.sprite = roundArt;
            sprite.sortingOrder = 19;
            round.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            round.transform.localScale = new Vector3((legendary ? 0.38f : 0.31f) / roundArt.bounds.size.x,
                (experimental || legendary ? 0.22f : 0.16f) / roundArt.bounds.size.y, 1f);
            movement.Configure(demo, direction, terrain, weapon);
            rounds.Add(movement);
            round.SetActive(true);
            var muzzleArt = ArtAt(explosionFrames, 0);
            if (muzzleArt && !legendary && !experimental)
            {
                var flash = Rent(muzzleArt, start + direction * 0.085f, Color.white, 22,
                    Shape(muzzleArt, new Vector2(0.36f, 0.23f)));
                flash.transform.rotation = round.transform.rotation;
                Fade(flash, direction * 0.82f, 0.043f);
            }
            for (int i = 0; i < 3; i++)
                Streak(start, direction * Random.Range(2.6f, 5.2f) + Random.insideUnitCircle * 0.78f,
                    tracer, Random.Range(0.07f, 0.17f), 0.079f);
            WeaponSound(weapon);
        }

        internal void ReturnRound(PlayerRound2D round)
        {
            if (!round.gameObject.activeSelf)
                return;
            round.gameObject.SetActive(false);
            rounds.Remove(round);
            spareRounds.Push(round);
        }

        public void Guard(Vector2 position, int facing, bool connected)
        {
            if (guardArt)
            {
                Vector2 anchor = position + Vector2.up * 0.10f;
                var shield = Rent(guardArt, anchor, connected ? Color.white : new Color(0.78f, 0.95f, 1f, 0.7f),
                    20, Shape(guardArt, new Vector2(KyleCombat2D.GuardRadius * 2f, KyleCombat2D.GuardHeight * 2f)));
                Fade(shield, Vector2.zero, connected ? 0.17f : KyleCombat2D.GuardTime, spin: facing * 74f);
                return;
            }
            var color = connected ? new Color(1f, 0.86f, 0.47f) : new Color(0.50f, 0.92f, 1f);
            Vector2 center = position + Vector2.right * (facing * 0.38f);
            var view = Rent(ring, center, color, 21, Vector2.one * (connected ? 1.22f : 0.79f));
            Fade(view, Vector2.zero, connected ? 0.34f : 0.16f);
            if (connected)
                Burst(center, color, 22, 7f);
        }

        public void DropMedkit(Vector2 position, int enemyIndex)
        {
            var floor = Physics2D.Raycast(position + Vector2.up * 0.2f, Vector2.down, 6f, terrain);
            if (floor)
            {
                float scale = 0.49f / medkit.bounds.size.y;
                medkits.Add((Rent(medkit, floor.point + Vector2.up * 0.29f, Color.white, 7, Vector2.one * scale), enemyIndex));
            }
        }

        public void ClearMedkits()
        {
            foreach (var (kit, _) in medkits)
                Recycle(kit);
            medkits.Clear();
        }

        public void Sound(int id) => speaker.PlayOneShot(sounds[id], id == 5 ? 0.50f : 0.40f);
    }
}

