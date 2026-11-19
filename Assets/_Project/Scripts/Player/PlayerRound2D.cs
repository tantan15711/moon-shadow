using UnityEngine;

namespace MoonShadow
{
    public sealed class PlayerRound2D : MonoBehaviour
    {
        private const float Speed = 27f;
        private float remaining;
        private Vector2 direction;
        private LayerMask terrain;
        private RuinsDemo world;
        private Sidearm weapon;
        private float trailClock;

        public void Configure(RuinsDemo campaign, Vector2 heading, LayerMask obstacles, Sidearm sidearm)
        {
            world = campaign;
            direction = heading;
            terrain = obstacles;
            weapon = sidearm;
            trailClock = 0f;
            remaining = sidearm switch { Sidearm.Legendary => 21f, Sidearm.Experimental => 19f, _ => 15f };
        }

        private void FixedUpdate()
        {
            if (!world.Playing)
                return;

            Vector2 start = transform.position;
            trailClock -= Time.fixedDeltaTime;
            if (trailClock <= 0f)
            {
                trailClock = 0.04f;
                var tone = weapon switch
                {
                    Sidearm.Experimental => new Color(0.25f, 0.79f, 1f),
                    Sidearm.Legendary => new Color(0.80f, 0.42f, 1f),
                    _ => new Color(1f, 0.70f, 0.31f)
                };
                world.Effects.Streak(start - direction * 0.20f, direction * 4.6f, tone, 0.34f, 0.09f);
            }
            float travel = Mathf.Min(remaining, Speed * Time.fixedDeltaTime);
            var obstruction = Physics2D.Raycast(start, direction, travel, terrain);
            float distance = obstruction ? obstruction.distance : travel;
            EnemyBase2D target = null;
            var path = new Ray(start, direction);
            foreach (var enemy in world.Enemies)
            {
                if (!enemy.Alive)
                    continue;

                var body = enemy.Hurtbox;
                body.Expand(0.10f);
                if (!body.IntersectRay(path, out float next) || next > distance)
                    continue;

                distance = next;
                target = enemy;
            }

            transform.position = start + direction * distance;
            remaining -= distance;
            if (target)
            {
                var profile = SidearmCatalog.Get(weapon);
                if (target.Hit(profile.Damage, direction.x < 0f ? -1 : 1, weapon == Sidearm.Legendary, false))
                    world.RecordHits(1);
            }
            if (target || obstruction || remaining <= 0.001f)
            {
                if (target || obstruction)
                    world.Effects.Burst(transform.position, new Color(1f, 0.77f, 0.44f), 4, 2.7f);
                world.Effects.ReturnRound(this);
            }
        }
    }
}

