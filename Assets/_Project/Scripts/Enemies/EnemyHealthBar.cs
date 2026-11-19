using UnityEngine;

namespace MoonShadow
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyBase2D enemy;
        [SerializeField] private RuinsDemo world;
        [SerializeField] private SpriteRenderer track;
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private float width;

        public void Configure(EnemyBase2D target, RuinsDemo demo, SpriteRenderer background,
            SpriteRenderer foreground, float length)
        {
            enemy = target;
            world = demo;
            track = background;
            fill = foreground;
            width = length;
        }

        private void LateUpdate()
        {
            float distance = Vector2.Distance(enemy.Position, world.Player.Position);
            float visibility = 1f - Mathf.InverseLerp(7f, 11f, distance);
            bool visible = enemy.Alive && visibility > 0f && !world.Briefing && !world.Finished &&
                (!enemy.IsBoss || world.RelayActive);

            track.enabled = fill.enabled = visible;
            if (!visible)
                return;

            float health = Mathf.Clamp01((float)enemy.Health / enemy.MaximumHealth);
            float opacity = visibility * (enemy.IsBoss || health < 1f ? 0.88f : 0.47f);
            track.color = new Color(0.06f, 0.08f, 0.14f, opacity);
            fill.color = enemy.IsBoss && world.BossShielded
                ? new Color(0.37f, 0.77f, 0.96f, opacity)
                : new Color(0.95f, 0.43f, 0.48f, opacity);

            Vector3 size = fill.transform.localScale;
            size.x = width * health / fill.sprite.bounds.size.x;
            fill.transform.localScale = size;
            fill.transform.localPosition = Vector3.left * (width * (1f - health) * 0.5f);
        }
    }
}
