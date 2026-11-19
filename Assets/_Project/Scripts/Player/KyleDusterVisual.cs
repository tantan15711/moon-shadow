using UnityEngine;

namespace MoonShadow
{
    public sealed class KyleDusterVisual : MonoBehaviour
    {
        [SerializeField] private Sprite[] tails;
        private SpriteRenderer fabric;
        private SpriteRenderer actor;

        public void Configure(Sprite[] art) => tails = art;

        internal void Show(int pose, int facing, Vector2 waist, float length)
        {
            if (tails == null || tails.Length != 8)
                return;
            if (!fabric)
            {
                actor = GetComponent<SpriteRenderer>();
                fabric = new GameObject("Faldones del abrigo", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                fabric.transform.SetParent(transform, false);
            }

            var art = tails[pose];
            float scale = length / art.bounds.size.y;
            float width = art.bounds.size.x * scale;
            fabric.sprite = art;
            fabric.sortingLayerID = actor.sortingLayerID;
            fabric.sortingOrder = actor.sortingOrder - 1;
            fabric.color = actor.color;
            fabric.flipX = facing < 0;
            fabric.transform.localPosition = new Vector3(facing * (waist.x - width * 0.42f), waist.y - length * 0.45f, 0f);
            fabric.transform.localScale = new Vector3(scale, scale, 1f);
            fabric.enabled = true;
        }

        internal void Hide()
        {
            if (fabric)
                fabric.enabled = false;
        }
    }
}
