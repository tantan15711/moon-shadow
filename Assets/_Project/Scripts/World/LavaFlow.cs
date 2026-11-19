using UnityEngine;

namespace MoonShadow
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class LavaFlow : MonoBehaviour
    {
        private SpriteRenderer surface;
        private Vector3 center;

        private void Awake()
        {
            surface = GetComponent<SpriteRenderer>();
            center = transform.localPosition;
        }

        private void LateUpdate()
        {
            float motion = Time.time * 2.7f + center.x * 0.36f;
            transform.localPosition = center + Vector3.up * (Mathf.Round(Mathf.Sin(motion) * 2f) * 0.025f);
            float warmth = 0.94f + Mathf.Sin(motion * 0.7f) * 0.06f;
            surface.color = new Color(1f, warmth, 0.86f + warmth * 0.14f);
        }
    }
}
