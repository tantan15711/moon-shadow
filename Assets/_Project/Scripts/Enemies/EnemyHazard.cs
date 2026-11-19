using UnityEngine;

namespace MoonShadow
{
    /// <summary>A danger region with a warning followed by a visible damage window.</summary>
    public sealed class EnemyHazard : MonoBehaviour
    {
        private RuinsDemo stage;
        private EnemyBase2D owner;
        private SpriteRenderer marker;
        private Color color;
        private float remaining;
        private float activeTime;
        private Vector2 drift;
        private Vector2 size;
        private int damage;
        private bool hit;
        private int revision;
        private LayerMask terrain;
        private Sprite[] rocks;
        private bool rolling;
        private float elapsed;
        private bool lit;
        private EnemyBase2D.Technique technique;
        private Vector2 beamOrigin;
        private float beamLength;
        private float beamStart;
        private float beamTurn;
        private Sprite authored;
        private float trailClock;

        public void Configure(RuinsDemo world, EnemyBase2D source, SpriteRenderer view, Color tint,
            Vector2 extent, float warning, float duration, int power, Vector2 velocity,
            LayerMask obstacles, Sprite[] stoneFrames, bool rolls, EnemyBase2D.Technique move)
        {
            stage = world; owner = source; marker = view; color = tint; size = extent;
            remaining = warning + duration; activeTime = duration; damage = power; drift = velocity;
            revision = source.AttackRevision;
            terrain = obstacles;
            rocks = stoneFrames;
            rolling = rolls;
            technique = move;
            authored = stage.Effects.AttackArt(technique);
            if (technique == EnemyBase2D.Technique.MineArc && authored)
            {
                marker.sprite = authored;
                marker.transform.localScale = size / (Vector2)authored.bounds.size;
            }
            marker.color = WithAlpha(0.23f);
        }

        public void ConfigureSweep(Vector2 origin, float length, float startAngle, float rotation)
        {
            beamOrigin = origin;
            beamLength = length;
            beamStart = startAngle;
            beamTurn = rotation;
            AimBeam(0f);
            marker.transform.localScale = size / (Vector2)marker.sprite.bounds.size;
        }

        private void AimBeam(float progress)
        {
            float angle = beamStart + beamTurn * progress;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            var obstruction = Physics2D.Raycast(beamOrigin, direction, beamLength, terrain);
            size.x = obstruction ? obstruction.distance : beamLength;
            transform.SetPositionAndRotation(beamOrigin + direction * (size.x * 0.5f),
                Quaternion.Euler(0f, 0f, angle));
        }

        private void FixedUpdate()
        {
            if (!stage.Playing)
                return;
            remaining -= Time.fixedDeltaTime;
            if (remaining <= 0f || !owner.Alive || (remaining > activeTime && revision != owner.AttackRevision))
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            bool active = remaining <= activeTime;
            if (!active)
            {
                marker.color = WithAlpha(0.56f + Mathf.PingPong(remaining * 2.8f, 0.34f));
                return;
            }

            if (!lit)
            {
                lit = true;
                if (technique is EnemyBase2D.Technique.MineArc or EnemyBase2D.Technique.EmberRain)
                    stage.Effects.Explosion(transform.position - transform.up * (size.y * 0.33f), 1.26f);
                else
                    stage.Effects.Burst(transform.position, color, damage > 1 ? 11 : 6, 4f);
            }
            elapsed += Time.fixedDeltaTime;
            if (beamLength > 0f)
                AimBeam(Mathf.Clamp01(elapsed / activeTime));
            int index = (int)(elapsed * 19f);
            bool stone = rocks?.Length > 0;
            Sprite art = authored ? authored : stone ? rocks[index % rocks.Length] : stage.Effects.SpecialFrame(technique, index);
            if (art)
            {
                marker.sprite = art;
                bool vertical = size.y > size.x && !stone;
                bool rotateToVertical = vertical && (!authored || art.bounds.size.x > art.bounds.size.y * 1.14f);
                float turn = rotateToVertical ? 90f : rolling ? -Mathf.Sign(drift.x) * elapsed * 145f : 0f;
                Vector2 length = rotateToVertical ? new Vector2(size.y, size.x) : size;
                marker.transform.localScale = length / (Vector2)art.bounds.size;
                marker.transform.localRotation = Quaternion.Euler(0f, 0f, turn);
            }
            float fade = Mathf.Clamp01(remaining / 0.073f);
            marker.color = art ? new Color(1f, 1f, 1f, Mathf.Lerp(0.76f, 1f, fade)) : WithAlpha(0.95f);

            if (drift.sqrMagnitude > 0.1f)
            {
                trailClock -= Time.fixedDeltaTime;
                if (trailClock <= 0f)
                {
                    trailClock = 0.068f;
                    stage.Effects.Streak(transform.position, -drift * 0.31f, color, 0.44f, 0.17f);
                }
            }

            Vector2 destination = (Vector2)transform.position + drift * Time.fixedDeltaTime;
            if (drift.sqrMagnitude > 0.1f && Physics2D.Linecast(transform.position, destination, terrain))
            {
                remaining = 0f;
                return;
            }
            transform.position = destination;
            if (hit || size.x < 0.05f)
                return;
            Bounds player = stage.Player.Hurtbox;
            Vector2 local = transform.InverseTransformPoint(player.center);
            Vector3 extents = player.extents;
            // Local dimensions are expressed in world units for all beam directions.
            float sx = Mathf.Abs(transform.right.x) * extents.x + Mathf.Abs(transform.right.y) * extents.y;
            float sy = Mathf.Abs(transform.up.x) * extents.x + Mathf.Abs(transform.up.y) * extents.y;
            if (Mathf.Abs(local.x) <= size.x * 0.5f + sx && Mathf.Abs(local.y) <= size.y * 0.5f + sy)
                hit = stage.Player.TakeHit(owner, transform.position, damage, parryable: false) != HitResponse.Ignored;
        }

        private Color WithAlpha(float alpha)
        {
            var c = color;
            c.a = alpha;
            return c;
        }
    }
}

