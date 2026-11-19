using UnityEngine;

namespace MoonShadow
{
    public sealed class LunarRescueSequence : MonoBehaviour
    {
        [SerializeField] private Transform sofy;
        [SerializeField] private SpriteRenderer sisterVisual;
        [SerializeField] private SpriteRenderer leftGlass, rightGlass;
        private KyleController2D kyle;
        private CastRig sisterRig;
        private Camera lens;
        private Vector3 sisterAt, cameraAt;
        private Vector2 heroAt, heroTo, heroPrevious;
        private Vector3 leftAt, rightAt;
        private Vector3 leftScale, rightScale;
        private float zoomAt, clock;
        public bool Playing { get; private set; }

        public void Configure(Transform sister, SpriteRenderer visual, SpriteRenderer left, SpriteRenderer right)
        {
            sofy = sister;
            sisterVisual = visual;
            leftGlass = left;
            rightGlass = right;
        }

        public void Begin(KyleController2D player)
        {
            if (Playing)
                return;
            kyle = player;
            sisterRig = sisterVisual.GetComponent<CastRig>();
            kyle.HoldForScene();
            kyle.PerformScenePose(0f, CastRig.Motion.Travel, 0f, kyle.Facing);
            sisterAt = sofy.position;
            leftAt = leftGlass.transform.position;
            rightAt = rightGlass.transform.position;
            leftScale = leftGlass.transform.localScale;
            rightScale = rightGlass.transform.localScale;
            heroAt = heroPrevious = kyle.Position;
            heroTo = new Vector2(sisterAt.x - 0.98f, heroAt.y);
            lens = Camera.main;
            cameraAt = lens.transform.position;
            zoomAt = lens.orthographicSize;
            clock = 0f;
            Playing = true;
        }

        private void Update()
        {
            if (!Playing || clock > 6.0f)
                return;
            clock += Time.unscaledDeltaTime;
            float open = Ease(0.21f, 1.50f);
            leftGlass.transform.position = leftAt + new Vector3(-0.46f, -0.08f, 0f) * open;
            rightGlass.transform.position = rightAt + new Vector3(0.46f, -0.08f, 0f) * open;
            float foreshorten = Mathf.Lerp(1f, 0.30f, open);
            leftGlass.transform.localScale = new Vector3(leftScale.x * foreshorten, leftScale.y, leftScale.z);
            rightGlass.transform.localScale = new Vector3(rightScale.x * foreshorten, rightScale.y, rightScale.z);
            var tone = leftGlass.color;
            tone.a = Mathf.Lerp(0.63f, 0.28f, open);
            leftGlass.color = rightGlass.color = tone;

            float fall = Ease(1.24f, 2.2f);
            float embrace = Ease(2.22f, 3.10f);
            sofy.position = sisterAt + new Vector3(-0.27f * fall, -0.39f * fall + 0.055f * Mathf.Sin(fall * Mathf.PI), 0f);
            sofy.rotation = Quaternion.Euler(0f, 0f, 11f * fall - 8f * embrace);
            sisterVisual.sortingOrder = fall > 0.40f ? 10 : 1;
            sisterRig?.Animate(Time.unscaledDeltaTime, 0f, -1,
                fall < 0.02f ? CastRig.Motion.Listen : CastRig.Motion.Reach,
                Mathf.Sin(fall * Mathf.PI) * 0.28f + embrace * 0.51f, Color.white);
            Vector2 heroPosition = Vector2.Lerp(heroAt, heroTo, Ease(0.75f, 2.18f));
            // Physics interpolation is frozen during the epilogue, so also move the visible actor.
            kyle.transform.position = heroPosition;
            float speed = Mathf.Abs(heroPosition.x - heroPrevious.x) / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            int facing = speed <= 0.1f || heroTo.x > heroAt.x ? 1 : -1;
            kyle.PerformScenePose(Time.unscaledDeltaTime, embrace > 0f ? CastRig.Motion.Assist : CastRig.Motion.Travel,
                embrace * 0.78f, facing, speed);
            heroPrevious = heroPosition;
            lens.transform.position = Vector3.Lerp(cameraAt,
                new Vector3(sisterAt.x - 0.48f, sisterAt.y + 1.03f, cameraAt.z), Ease(0.0f, 2.6f));
            lens.orthographicSize = Mathf.Lerp(zoomAt, 2.48f, Ease(0.1f, 2.8f));
        }

        private float Ease(float from, float to) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, clock));
    }
}
