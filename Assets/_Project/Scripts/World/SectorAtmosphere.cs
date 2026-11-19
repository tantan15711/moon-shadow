using UnityEngine;

namespace MoonShadow
{
    public sealed class SectorAtmosphere : MonoBehaviour
    {
        [SerializeField] private Sprite pixel;
        [SerializeField] private Camera lens;
        [SerializeField] private int act;
        private SpriteRenderer[] flakes;
        private RuinsDemo world;
        private float elapsed;

        public void Configure(Sprite dot, Camera camera, int chapter)
        {
            pixel = dot;
            lens = camera;
            act = chapter;
        }

        private void Start()
        {
            world = FindAnyObjectByType<RuinsDemo>();
            if (!lens)
                lens = FindAnyObjectByType<RuinsCamera>()?.GetComponent<Camera>();
            int amount = RuntimePerformance.Mobile ? (act == 0 ? 28 : 16) : act == 0 ? 53 : 28;
            flakes = new SpriteRenderer[amount];
            Color color = act switch
            {
                0 => new Color(0.61f, 0.81f, 0.94f, 0.21f),
                1 => new Color(0.70f, 0.62f, 0.95f, 0.22f),
                2 => new Color(1f, 0.49f, 0.24f, 0.42f),
                3 => new Color(0.57f, 0.83f, 0.92f, 0.23f),
                _ => new Color(0.77f, 0.70f, 0.96f, 0.22f)
            };
            for (int i = 0; i < flakes.Length; i++)
            {
                var drop = new GameObject("Partícula ambiental", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                drop.transform.SetParent(transform, false);
                drop.sprite = pixel;
                drop.sortingOrder = i % 4 == 0 ? 13 : -19;
                drop.color = color;
                float width = act == 0 ? 0.025f : 0.027f + i % 3 * 0.011f;
                float length = act == 0 ? 0.22f + i % 7 * 0.045f : width;
                drop.transform.localScale = new Vector3(width / pixel.bounds.size.x, length / pixel.bounds.size.y, 1f);
                if (act == 0)
                    drop.transform.rotation = Quaternion.Euler(0f, 0f, -10f);
                flakes[i] = drop;
            }
        }

        private void LateUpdate()
        {
            if (!world || !world.Playing || !lens)
                return;

            bool outdoors = !world.Map || !world.Map.Indoors;
            elapsed += Time.deltaTime;
            float sideways = act == 0 ? 0.032f : 0.021f;
            float vertical = act == 0 ? -0.39f : act == 2 ? 0.145f : -0.063f;
            float height = lens.orthographicSize * 2f;
            Vector2 extent = new(height * lens.aspect * 1.08f, height * 1.08f);
            Vector2 camera = lens.transform.position;
            for (int i = 0; i < flakes.Length; i++)
            {
                flakes[i].enabled = outdoors;
                if (!outdoors)
                    continue;
                float vary = 0.69f + i % 7 * 0.095f;
                float x = Mathf.Repeat(i * 0.618f + elapsed * sideways * vary, 1f) - 0.5f;
                float y = Mathf.Repeat(i * 0.379f + elapsed * vertical * vary, 1f) - 0.5f;
                flakes[i].transform.position = camera + Vector2.Scale(extent, new Vector2(x, y));
            }
        }
    }
}

