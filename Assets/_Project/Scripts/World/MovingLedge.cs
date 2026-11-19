using UnityEngine;

namespace MoonShadow
{
    [DefaultExecutionOrder(-100)]
    public sealed class MovingLedge : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D platform;
        [SerializeField] private float rise;
        private Vector2 start;
        public float VerticalSpeed { get; private set; }

        public void Configure(Rigidbody2D body, float travel) { platform = body; rise = travel; }

        private void Awake() => start = platform.position;

        private void FixedUpdate()
        {
            float phase = 0.5f - 0.5f * Mathf.Cos(Time.time * 1.03f);
            Vector2 target = start + Vector2.up * (phase * rise);
            VerticalSpeed = (target.y - platform.position.y) / Time.fixedDeltaTime;
            platform.MovePosition(target);
        }
    }
}
