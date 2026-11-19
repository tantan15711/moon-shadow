using UnityEngine;

namespace MoonShadow
{
    public sealed class EnemyBolt2D : MonoBehaviour
    {
        private const float Radius = 0.13f;
        public const float DefaultSpeed = 10.8f;
        private float speed;
        private RuinsDemo demo;
        private RuinsEffects effects;
        private EnemyBase2D source;
        private SpriteRenderer visual;
        private LayerMask terrain;
        private Vector2 direction;
        private float life = 2.3f;
        private bool reflected;
        private float elapsed, trailClock;
        private Color tracer;
        private EnemyBase2D.Technique technique;

        public void Configure(RuinsDemo world, RuinsEffects feedback, EnemyBase2D shooter, SpriteRenderer sprite,
            Vector2 heading, LayerMask mask, float flightSpeed, EnemyBase2D.Technique move, Color color)
        {
            demo = world;
            effects = feedback;
            source = shooter;
            visual = sprite;
            terrain = mask;
            speed = flightSpeed;
            technique = move;
            tracer = color;
            Face(heading);
        }

        private void FixedUpdate()
        {
            if (!demo.Playing)
                return;
            life -= Time.fixedDeltaTime;
            elapsed += Time.fixedDeltaTime;
            trailClock -= Time.fixedDeltaTime;
            Vector2 start = transform.position;
            Sprite frame = reflected ? effects.ReflectedRound : effects.EnemyRound(technique, (int)(elapsed * 21f));
            if (frame)
                visual.sprite = frame;
            if (trailClock <= 0f)
            {
                trailClock = 0.045f;
                effects.Streak(start - direction * 0.12f, direction * 1.9f, tracer,
                    technique is EnemyBase2D.Technique.MissileSalvo or EnemyBase2D.Technique.GuardianRocket ? 0.39f : 0.25f, 0.11f);
            }
            float travel = (reflected ? 23.5f : speed) * Time.fixedDeltaTime;
            var terrainHit = Physics2D.CircleCast(start, Radius, direction, travel, terrain);
            float clearTravel = terrainHit ? terrainHit.distance : travel;
            var ray = new Ray(start, direction);

            if (reflected)
            {
                EnemyBase2D victim = null;
                float nearest = clearTravel;
                foreach (var enemy in demo.Enemies)
                {
                    if (enemy.Alive && Intersects(enemy.Hurtbox, ray, nearest, out float distance))
                    {
                        nearest = distance;
                        victim = enemy;
                    }
                }

                if (victim)
                {
                    victim.Hit(3, direction.x < 0f ? -1 : 1, true, false);
                    demo.RecordHits(1);
                    End(start + direction * nearest);
                    return;
                }
            }
            else if (demo.Player.InterceptProjectile(source, ray, clearTravel, out float deflection))
            {
                Reflect(start + direction * deflection);
                return;
            }
            else if (Intersects(demo.Player.Hurtbox, ray, clearTravel, out float contact))
            {
                Vector2 point = start + direction * contact;
                int reset = demo.Resets;
                HitResponse result = demo.Player.TakeHit(source, point - direction * 0.3f, source.RangedDamage, true);
                if (demo.Resets != reset)
                    return;

                if (result == HitResponse.Parried)
                {
                    Reflect(point);
                    return;
                }
                if (result == HitResponse.Damaged)
                {
                    End(point);
                    return;
                }
            }

            Vector2 next = start + direction * clearTravel;
            if (terrainHit || life <= 0f)
                End(next);
            else
                transform.position = next;
        }

        private static bool Intersects(Bounds box, Ray ray, float maxDistance, out float distance)
        {
            box.Expand(new Vector3(Radius * 2f, Radius * 2f, 0.1f));
            distance = 0f;
            return box.Contains(ray.origin) ||
                (box.IntersectRay(ray, out distance) && distance <= maxDistance);
        }

        private void Face(Vector2 heading)
        {
            direction = heading.sqrMagnitude > 0.0001f ? heading : Vector2.right;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void Reflect(Vector2 point)
        {
            reflected = true;
            life = 1.45f;
            tracer = new Color(0.43f, 0.94f, 1f);
            visual.color = Color.white;
            Sprite art = effects.ReflectedRound;
            if (art)
            {
                visual.sprite = art;
                transform.localScale = new Vector2(0.46f, 0.22f) / (Vector2)art.bounds.size;
            }
            transform.position = point;
            Face(source.Alive ? (source.Position - point).normalized : -direction);
        }

        private void End(Vector2 point)
        {
            if (!reflected && technique is (EnemyBase2D.Technique.MissileSalvo or
                EnemyBase2D.Technique.GuardianRocket or EnemyBase2D.Technique.FireBreath))
                effects.Explosion(point, technique == EnemyBase2D.Technique.FireBreath ? 0.69f : 1.62f);
            else
                effects.BladeImpact(point, direction.x < 0f ? -1 : 1, false, tracer);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}

