using UnityEngine;

namespace MoonShadow
{
    public sealed class RuinsCamera : MonoBehaviour
    {
        [SerializeField] private RuinsDemo demo;
        [SerializeField] private Transform[] backdrop;
        [SerializeField] private float[] parallax;
        [SerializeField] private float leftEdge = -2f;
        [SerializeField] private float rightEdge = 74f;
        [SerializeField] private float top = 5.2f;
        [SerializeField] private SpriteRenderer vista;

        private Vector3 anchor;
        private Vector3 velocity;
        private Camera lens;
        private int lastReset;
        private float lead;
        private Vector3 roomLimits;
        private bool inRoom, snap;
        private float mainZoom, targetZoom;
        public void SetVista(SpriteRenderer scenery) => vista = scenery;

        public void FocusRoom(float left, float right, float ceiling, bool combat)
        {
            roomLimits = new Vector3(left, right, ceiling);
            inRoom = snap = true;
            targetZoom = mainZoom + (combat ? 0.62f : 0.25f);
        }

        public void FocusMain()
        {
            inRoom = false;
            snap = true;
            targetZoom = mainZoom;
        }

        public void Configure(RuinsDemo world, Transform[] layers, float[] factors)
        {
            demo = world;
            backdrop = layers;
            parallax = factors;
        }

        public void SetLimits(float left, float right, float upper)
        {
            leftEdge = left;
            rightEdge = right;
            top = upper;
        }

        private void Awake()
        {
            lens = GetComponent<Camera>();
            mainZoom = targetZoom = lens.orthographicSize;
            anchor = transform.position;
        }

        private void LateUpdate()
        {
            if (!demo.Playing)
                return;
            TrackPlayer();
        }

        public void SnapToPlayer()
        {
            snap = true;
            lead = 0f;
            TrackPlayer();
        }

        private void TrackPlayer()
        {
            Vector2 target = snap || lastReset != demo.Resets ? demo.Player.Position : demo.Player.transform.position;
            lens.orthographicSize = snap ? targetZoom : Mathf.MoveTowards(lens.orthographicSize, targetZoom, Time.deltaTime * 2f);
            lead = Mathf.MoveTowards(lead, demo.Player.Facing * 1.0f, Time.deltaTime * 3f);
            float halfWidth = lens.orthographicSize * lens.aspect;
            float left = inRoom ? roomLimits.x : leftEdge;
            float right = inRoom ? roomLimits.y : rightEdge;
            float min = halfWidth + left;
            float max = Mathf.Max(min, right - halfWidth);
            Vector3 aim = new(Mathf.Clamp(target.x + lead, min, max),
                Mathf.Clamp(target.y + 1.9f, 2.45f, inRoom ? roomLimits.z : top), -10f);
            if (snap || lastReset != demo.Resets)
            {
                snap = false;
                lastReset = demo.Resets;
                anchor = aim;
                velocity = Vector3.zero;
            }
            anchor = Vector3.SmoothDamp(anchor, aim, ref velocity, 0.13f, 30f, Time.deltaTime);
            Vector3 jitter = (Vector3)Random.insideUnitCircle * demo.Shake;
            transform.position = anchor + jitter;
            if (vista)
            {
                Vector2 native = vista.sprite.bounds.size;
                float scale = Mathf.Max((halfWidth * 2f + 8.3f) / native.x, (lens.orthographicSize * 2f + 5f) / native.y);
                float travel = anchor.x - left;
                vista.transform.localScale = new Vector3(scale, scale, 1f);
                vista.transform.position = new Vector3(anchor.x + Mathf.Sin(travel * 0.031f) * -3.3f,
                    anchor.y + 0.76f - Mathf.Clamp((anchor.y - 2.45f) * 0.18f, 0f, 1.65f), 1f);
            }
            for (int i = 0; i < backdrop.Length; i++)
            {
                Vector3 p = backdrop[i].localPosition;
                float offset = left - leftEdge;
                p.x = offset - parallax[i] * (anchor.x - offset - 7.5f);
                p.y = -parallax[i] * (anchor.y - 2.45f) * 0.4f;
                backdrop[i].localPosition = p;
            }
        }
    }
}

