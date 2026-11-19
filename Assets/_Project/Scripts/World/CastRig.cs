using UnityEngine;

namespace MoonShadow
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed partial class CastRig : MonoBehaviour
    {
        public enum Role { Kyle, Walker, Runner, Brute, Sentry, Vane, Vance, Vulkano, Orbital, Ojo, Civilian, Capture }
        public enum Motion { Travel, Windup, Strike, Stun, Dash, Reach, Struggle, Recover, Air, Assist, Gesture, Listen, Surge, Vault,
            Aim, Reload, Guard, Claw, Grab, Pound, Dive, TailSweep, Breath, CrossCut, Land, FollowThrough }

        // Joint locations are measured from the lower left of each source sprite.
        private readonly struct Markers
        {
            public readonly Vector2 Hips, Neck, Crown, NearElbow, NearHand, FarElbow, FarHand;
            public readonly Vector2 NearKnee, NearFoot, FarKnee, FarFoot;

            public Markers(Vector2 hips, Vector2 neck, Vector2 crown, Vector2 nearElbow, Vector2 nearHand,
                Vector2 farElbow, Vector2 farHand, Vector2 nearKnee, Vector2 nearFoot, Vector2 farKnee, Vector2 farFoot)
            {
                Hips = hips; Neck = neck; Crown = crown;
                NearElbow = nearElbow; NearHand = nearHand; FarElbow = farElbow; FarHand = farHand;
                NearKnee = nearKnee; NearFoot = nearFoot; FarKnee = farKnee; FarFoot = farFoot;
            }
        }

        private static readonly Markers[] Profiles =
        {
            new(new(.64f,.51f),new(.64f,.83f),new(.66f,.97f),new(.39f,.64f),new(.385f,.43f),new(.745f,.66f),new(.778f,.435f),new(.47f,.28f),new(.43f,.083f),new(.695f,.276f),new(.70f,.075f)),
            new(new(.49f,.46f),new(.73f,.73f),new(.81f,.98f),new(.30f,.53f),new(.24f,.39f),new(.81f,.58f),new(.92f,.42f),new(.70f,.30f),new(.67f,.06f),new(.28f,.23f),new(.13f,.05f)),
            new(new(.55f,.43f),new(.72f,.76f),new(.83f,.95f),new(.23f,.73f),new(.14f,.51f),new(.76f,.55f),new(.93f,.33f),new(.74f,.34f),new(.68f,.04f),new(.21f,.27f),new(.13f,.08f)),
            new(new(.52f,.38f),new(.55f,.72f),new(.70f,.94f),new(.24f,.55f),new(.20f,.37f),new(.79f,.51f),new(.90f,.37f),new(.65f,.23f),new(.77f,.06f),new(.28f,.23f),new(.22f,.06f)),
            new(new(.50f,.40f),new(.50f,.73f),new(.54f,.95f),new(.66f,.66f),new(.90f,.74f),new(.48f,.63f),new(.74f,.72f),new(.64f,.27f),new(.80f,.04f),new(.19f,.24f),new(.12f,.04f)),
            new(new(.62f,.45f),new(.62f,.78f),new(.67f,.99f),new(.26f,.62f),new(.27f,.40f),new(.79f,.59f),new(.87f,.41f),new(.73f,.24f),new(.81f,.05f),new(.40f,.26f),new(.37f,.05f)),
            new(new(.58f,.50f),new(.61f,.76f),new(.66f,.97f),new(.29f,.65f),new(.18f,.36f),new(.72f,.64f),new(.81f,.49f),new(.79f,.32f),new(.85f,.03f),new(.42f,.23f),new(.28f,.05f)),
            new(new(.49f,.32f),new(.68f,.50f),new(.90f,.68f),new(.40f,.62f),new(.23f,.88f),new(.21f,.35f),new(.05f,.53f),new(.74f,.22f),new(.88f,.06f),new(.35f,.22f),new(.34f,.05f)),
            new(new(.52f,.41f),new(.51f,.73f),new(.58f,.91f),new(.22f,.55f),new(.18f,.36f),new(.71f,.58f),new(.94f,.33f),new(.64f,.25f),new(.61f,.05f),new(.27f,.28f),new(.20f,.05f)),
            new(new(.53f,.39f),new(.54f,.72f),new(.60f,.96f),new(.40f,.59f),new(.36f,.40f),new(.63f,.58f),new(.71f,.39f),new(.54f,.22f),new(.62f,.04f),new(.43f,.20f),new(.43f,.04f)),
            new(new(.50f,.46f),new(.48f,.76f),new(.47f,.97f),new(.72f,.63f),new(.67f,.46f),new(.27f,.62f),new(.23f,.46f),new(.64f,.25f),new(.67f,.04f),new(.38f,.22f),new(.32f,.04f)),
            new(new(.52f,.41f),new(.51f,.73f),new(.58f,.91f),new(.22f,.55f),new(.18f,.36f),new(.71f,.58f),new(.94f,.33f),new(.64f,.25f),new(.61f,.05f),new(.27f,.28f),new(.20f,.05f))
        };

        private static readonly Markers[] InfectedProfiles =
        {
            new(new(.545f,.40f),new(.614f,.65f),new(.634f,.78f),new(.433f,.46f),new(.418f,.34f),
                new(.652f,.52f),new(.732f,.50f),new(.486f,.22f),new(.447f,.075f),new(.567f,.21f),new(.596f,.076f)),
            new(new(.478f,.35f),new(.621f,.56f),new(.655f,.68f),new(.446f,.39f),new(.477f,.175f),
                new(.595f,.37f),new(.650f,.23f),new(.428f,.166f),new(.397f,.06f),new(.547f,.174f),new(.549f,.047f)),
            new(new(.527f,.356f),new(.622f,.644f),new(.645f,.78f),new(.426f,.439f),new(.409f,.27f),
                new(.651f,.446f),new(.675f,.231f),new(.489f,.177f),new(.467f,.063f),new(.596f,.19f),new(.602f,.07f)),
            new(new(.501f,.392f),new(.612f,.658f),new(.642f,.78f),new(.459f,.435f),new(.460f,.246f),
                new(.619f,.469f),new(.642f,.291f),new(.485f,.188f),new(.450f,.071f),new(.552f,.21f),new(.569f,.069f))
        };

        private const int JointCount = 19;
        private const int Columns = 33;
        private const int Rows = 40;
        private const int NearFoot = 10, FarFoot = 11, Wing = 12, WingTip = 13, Tail = 14, TailTip = 15, Blade = 16;
        private const int NearHand = 17, FarHand = 18;
        private static readonly int[] Parents = { -1, 0, 0, 2, 0, 4, 0, 6, 0, 8, 7, 9, 0, 12, 0, 14, 3, 3, 5 };
        private static readonly int SpriteTexture = Shader.PropertyToID("_MainTex");
        private static readonly int Tint = Shader.PropertyToID("_Tint");

        [SerializeField] private Sprite skin;
        [SerializeField] private Material material;
        [SerializeField] private Role role;
        [SerializeField] private bool idleAutomatically;
        [SerializeField] private bool infected;
        private SpriteRenderer source;
        private MeshRenderer renderer;
        private Mesh mesh;
        private MaterialPropertyBlock properties;
        private Vector3[] original;
        private Vector3[] deformed;
        private SkinWeight[] weights;
        private readonly Vector2[] joints = new Vector2[JointCount];
        private readonly Vector2[] tips = new Vector2[JointCount];
        private readonly Vector2[] positions = new Vector2[JointCount];
        private readonly float[] bend = new float[JointCount];
        private readonly float[] cos = new float[JointCount];
        private readonly float[] sin = new float[JointCount];
        private float phase;
        private float clock;
        private float modelHeight;
        private bool layeredKyle;
        private float deferredTime;
        internal Mesh Deformation => mesh;
        internal bool Visible => authoredSkin ? authoredView && authoredView.enabled : renderer && renderer.enabled;
        internal Sprite Skin => skin;

        private readonly struct SkinWeight
        {
            public readonly int A, B;
            public readonly float Mix;

            public SkinWeight(int a, int b, float mix) { A = a; B = b; Mix = mix; }
        }

        public void Configure(Sprite sprite, Material surface, Role profile, bool autoIdle = false, bool zombie = false)
        {
            skin = sprite; material = surface; role = profile; idleAutomatically = autoIdle; infected = zombie;
        }

        private void Awake()
        {
            source = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (idleAutomatically && Time.deltaTime > 0f)
                Animate(Time.deltaTime, 0f, source.flipX ? -1 : 1, Motion.Travel, 0f, source.color);
        }

        public void ShowSprite()
        {
            if (renderer)
                renderer.enabled = false;
            if (source)
                source.enabled = true;
        }

        internal void Hide()
        {
            if (authoredView)
                authoredView.enabled = false;
            if (renderer)
                renderer.enabled = false;
            if (source)
                source.enabled = false;
        }

        public void Animate(float dt, float speed, int facing, Motion motion, float progress, Color tint)
            => Drive(dt, new Vector2(speed * facing, 0f), facing, motion, progress, tint);

        internal void Drive(float dt, Vector2 velocity, int facing, Motion motion, float progress, Color tint,
            float floorY = float.NaN, LayerMask terrain = default, float bladeAngle = float.NaN, Vector2 aim = default,
            float hang = float.NaN, float impact = 0f, float recoil = 0f)
        {
            if (!renderer)
            {
                if (!skin || !material)
                    return;
                Build();
            }

            if (!layeredKyle && renderer.enabled && !renderer.isVisible && dt > 0f)
            {
                deferredTime += dt;
                if (deferredTime < .10f)
                    return;
                dt = deferredTime;
            }
            deferredTime = 0f;
            renderer.enabled = true;
            renderer.sortingOrder = source.sortingOrder;
            source.enabled = false;
            source.flipX = facing < 0;
            renderer.transform.localScale = new Vector3(facing < 0 ? -1f : 1f, 1f, 1f);
            clock += dt;
            if (layeredKyle)
            {
                MoveKyle(dt, velocity, facing, motion, progress, floorY, terrain, bladeAngle, aim, hang, impact, recoil);
                DrawPose(tint);
                return;
            }
            bool weightBearing = motion is Motion.Travel or Motion.Aim or Motion.Reload or Motion.Surge;
            float speed = weightBearing ? Mathf.Abs(velocity.x) : 0f;
            float worldHeight = modelHeight * Mathf.Abs(transform.lossyScale.y);
            phase = Mathf.Repeat(phase + dt * speed / Mathf.Max(0.65f, worldHeight * StrideSpan(speed)), 1f);
            float travel = Mathf.Clamp01(speed / (role == Role.Kyle || role == Role.Runner ? 4.0f : 2.0f));
            float run = role == Role.Kyle ? Mathf.InverseLerp(2.8f, 5.1f, speed) : 0f;
            float tailSweep = motion == Motion.TailSweep ? Mathf.Sin(progress * Mathf.PI) : 0f;
            float breath = motion == Motion.Breath ? Mathf.Sin(Mathf.Min(progress * 1.5f, 0.5f) * Mathf.PI) : 0f;
            float crossCut = motion == Motion.CrossCut ? Mathf.Sin(progress * Mathf.PI * 0.81f + 0.22f) : 0f;
            float step = Mathf.Sin(phase * Mathf.PI * 2) * travel * Mathf.Sign(velocity.x * facing);
            float reverse = Mathf.Cos(phase * Mathf.PI * 2) * travel;
            float breathe = Mathf.Sin(clock * 3.2f);
            float strike = motion is Motion.Strike or Motion.Claw or Motion.Grab or Motion.Pound
                ? Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI * 0.75f + 0.32f) : 0f;
            float ready = motion == Motion.Windup ? progress : 0f;
            float reach = motion == Motion.Reach ? progress : 0f;
            float resistance = motion == Motion.Struggle ? 1f : 0f;
            float struggle = resistance * Mathf.Sin(progress * 2f * Mathf.PI);
            float kick = resistance * Mathf.Cos(progress * 2f * Mathf.PI);
            float dash = motion == Motion.Dash ? (0.84f + Mathf.Sin(progress * Mathf.PI) * 0.16f) : 0f;
            float stun = motion == Motion.Stun ? 1f : motion == Motion.Recover ? (1f - progress) * 1.12f : 0f;
            float airborne = motion is Motion.Air or Motion.Dive ? 0.35f + Mathf.Sin(progress * Mathf.PI) * 0.65f : 0f;
            float helping = motion == Motion.Assist ? Mathf.Sin(progress * Mathf.PI) : 0f;
            float gesturing = motion == Motion.Gesture ? Mathf.Sin(progress * Mathf.PI) : 0f;
            float listening = motion == Motion.Listen ? Mathf.Sin(progress * Mathf.PI) : 0f;
            float surge = motion == Motion.Surge ? Mathf.Sin(progress * Mathf.PI * 0.8f + 0.35f) : 0f;
            float vault = motion == Motion.Vault ? Mathf.Sin(progress * Mathf.PI) : 0f;
            float guard = motion == Motion.Guard ? 1f : 0f;
            bool dragon = role == Role.Vulkano;
            bool machine = role is Role.Orbital or Role.Capture or Role.Ojo;
            float gestureArm = dragon ? 31f : machine ? 37f : 55f;
            float gait = role == Role.Capture ? 1.36f : HeavyActor ? 0.88f : 1f;
            float smoothing = dt <= 0f ? 1f : 1f - Mathf.Exp(-dt * (motion == Motion.Strike ? 48f : 24f));

            bool capture = role == Role.Capture;
            Bend(0, -travel * 3f - run * 7f + breathe * 1.1f + ready * 6f - strike * 9f - dash * 15f + stun * 9f +
                struggle * 13f - (capture ? reach * 11f : 0f) - helping * 18f -
                gesturing * 6f + listening * 3f - surge * 19f - vault * 12f + guard * 6f - breath * 11f + tailSweep * 12f, smoothing);
            Bend(1, -breathe * 2f - ready * 6f + reach * 9f + strike * 3f - struggle * 16f +
                gesturing * (dragon ? 22f : -11f) + listening * 9f + surge * 6f + breath * 21f, smoothing);
            Bend(2, -step * 25f * gait - ready * (dragon ? 17f : 26f) + strike * (dragon ? 24f : 48f) +
                reach * (capture ? 12f : 37f) - dash * 21f + resistance * 28f + struggle * 31f + airborne * 28f - stun * 11f +
                helping * 41f + gesturing * gestureArm - listening * 6f - surge * 20f + vault * 24f + run * 15f + crossCut * 38f, smoothing);
            Bend(3, reverse * 16f - strike * 21f + ready * 11f + reach * (capture ? 7f : 15f) - kick * 34f -
                stun * 14f + helping * 26f + gesturing * 42f + vault * 20f + run * (19f + reverse * 8f) - crossCut * 26f, smoothing);
            Bend(4, step * 24f * gait + ready * 18f - strike * 24f + reach * (capture ? 22f : -9f) +
                dash * 24f - resistance * 18f - struggle * 28f - airborne * 27f + stun * 9f -
                gesturing * (machine ? 30f : 13f) + listening * 9f + surge * 26f - vault * 18f + run * 12f - crossCut * 29f, smoothing);
            Bend(5, -reverse * 14f + ready * 9f + strike * 12f + reach * (capture ? 12f : 31f) + kick * 29f + run * (19f - reverse * 8f) + crossCut * 34f, smoothing);
            Bend(6, step * 29f * gait - dash * 19f - ready * 5f + strike * 9f - stun * 8f + kick * 24f +
                airborne * 31f + helping * 25f + surge * 21f + vault * 33f, smoothing);
            Bend(7, Mathf.Max(0f, reverse) * -45f * gait + dash * 20f + ready * 6f - resistance * 14f - struggle * 19f -
                airborne * 49f - helping * 47f - surge * 27f - vault * 56f, smoothing);
            Bend(8, -step * 29f * gait + dash * 24f + ready * 7f - strike * 7f - kick * 20f -
                airborne * 23f - surge * 26f - vault * 19f, smoothing);
            Bend(9, Mathf.Min(0f, reverse) * 45f * gait - dash * 23f - ready * 4f - resistance * 12f + struggle * 14f +
                airborne * 29f + vault * 35f, smoothing);

            Bend(NearFoot, -reverse * 12f, smoothing);
            Bend(FarFoot, reverse * 12f, smoothing);
            Bend(Wing, -breathe * 4f + (airborne + vault) * 36f - strike * 17f + gesturing * 12f + breath * 21f - tailSweep * 18f, smoothing);
            Bend(WingTip, breathe * 8f + (airborne + vault) * 13f + ready * 12f, smoothing);
            Bend(Tail, Mathf.Sin(clock * 4f - 0.4f) * 11f + strike * 27f - tailSweep * 61f, smoothing);
            Bend(TailTip, Mathf.Sin(clock * 4f - 0.95f) * 16f + strike * 18f - tailSweep * 24f, smoothing);

            float compress = travel * (0.5f + Mathf.Cos(phase * Mathf.PI * 4f + 0.9f) * 0.5f) * (HeavyActor ? 0.035f : 0.023f);
            float posture = dash * -0.19f - ready * 0.035f - guard * 0.065f - helping * 0.17f - run * 0.026f -
                breath * 0.048f - compress;
            stance = Mathf.Lerp(stance, posture, smoothing);
            for (int j = 0; j < JointCount; j++)
            {
                float angle = bend[j] * Mathf.Deg2Rad;
                int parent = Parents[j];
                float a = Mathf.Cos(angle), b = Mathf.Sin(angle);
                positions[j] = parent < 0 ? joints[j] + Vector2.up * (modelHeight * (stance + (Mathf.Abs(step) * 0.023f))) :
                    positions[parent] + Rotate(joints[j] - joints[parent], cos[parent], sin[parent]);
                cos[j] = parent < 0 ? a : cos[parent] * a - sin[parent] * b;
                sin[j] = parent < 0 ? b : sin[parent] * a + cos[parent] * b;
            }

            MatchGround(floorY, velocity.x, facing, motion, terrain, dt);
            PoseHands(motion, progress, facing, bladeAngle, aim, smoothing);
            if (authoredSkin)
            {
                DrawAuthored(motion, progress, facing, speed, tint);
                return;
            }
            DrawPose(tint);
        }

        private void DrawPose(Color tint)
        {
            for (int i = 0; i < deformed.Length; i++)
            {
                SkinWeight w = weights[i];
                Vector2 from = original[i];
                Vector2 a = positions[w.A] + Rotate(from - joints[w.A], cos[w.A], sin[w.A]);
                Vector2 b = positions[w.B] + Rotate(from - joints[w.B], cos[w.B], sin[w.B]);
                deformed[i] = Vector2.LerpUnclamped(a, b, w.Mix);
            }
            mesh.SetVertices(deformed, 0, deformed.Length, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
            Properties(tint);
        }

        private void Bend(int joint, float angle, float smoothing) =>
            bend[joint] = Mathf.Lerp(bend[joint], angle, smoothing);

        private void Properties(Color tint)
        {
            properties.SetColor(Tint, tint);
            renderer.SetPropertyBlock(properties);
        }

        internal void CopyAfterimage(MeshRenderer copy, Mesh pose, MaterialPropertyBlock block)
        {
            pose.SetVertices(deformed, 0, deformed.Length, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
            copy.sharedMaterial = material;
            renderer.GetPropertyBlock(block);
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder - 1;
            copy.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
            Vector3 scale = transform.lossyScale;
            scale.x *= source.flipX ? -1f : 1f;
            copy.transform.localScale = scale;
        }

        private void Build()
        {
            source = GetComponent<SpriteRenderer>();
            var model = new GameObject("Malla articulada", typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(transform, false);
            renderer = model.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            renderer.enabled = false;
            properties = new MaterialPropertyBlock();
            properties.SetTexture(SpriteTexture, skin.texture);

            Vector2 size = skin.rect.size / skin.pixelsPerUnit;
            Vector2 bottom = -skin.pivot / skin.pixelsPerUnit;
            modelHeight = size.y;
            layeredKyle = role == Role.Kyle && skin.name == "PixelKyleRestored" &&
                skin.rect.width == 285f && skin.rect.height == 416f;
            mesh = new Mesh { name = skin.name + " / esqueleto" };
            mesh.MarkDynamic();
            if (layeredKyle)
            {
                BindKyleSkin(bottom, size);
            }
            else
            {
                BindCastSkin(bottom, size);
            }
            mesh.bounds = new Bounds(skin.bounds.center, skin.bounds.size + Vector3.one * size.y * 0.7f);
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void BindCastSkin(Vector2 bottom, Vector2 size)
        {
            Bind(infected ? InfectedProfiles[(int)role - 1] : Profiles[(int)role], bottom, size);
            int columns = RuntimePerformance.Mobile ? 23 : Columns;
            int rows = RuntimePerformance.Mobile ? 30 : Rows;
            original = new Vector3[columns * rows];
            deformed = new Vector3[original.Length];
            weights = new SkinWeight[original.Length];
            var uv = new Vector2[original.Length];
            var shade = new Color32[original.Length];
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int i = y * columns + x;
                Vector2 at = new(x / (columns - 1f), y / (rows - 1f));
                original[i] = bottom + Vector2.Scale(size, at);
                deformed[i] = original[i];
                shade[i] = new Color32(255, 255, 255, 255);
                uv[i] = (skin.rect.position + Vector2.Scale(skin.rect.size, at)) /
                    new Vector2(skin.texture.width, skin.texture.height);
                weights[i] = Assign(original[i], size.y);
            }

            var indices = new int[(columns - 1) * (rows - 1) * 6];
            int next = 0;
            for (int y = 0; y < rows - 1; y++)
            for (int x = 0; x < columns - 1; x++)
            {
                int i = y * columns + x;
                indices[next++] = i; indices[next++] = i + columns; indices[next++] = i + 1;
                indices[next++] = i + 1; indices[next++] = i + columns; indices[next++] = i + columns + 1;
            }
            mesh.vertices = original;
            mesh.uv = uv;
            mesh.colors32 = shade;
            mesh.triangles = indices;
        }

        private void Bind(Markers m, Vector2 bottom, Vector2 size)
        {
            Vector2 shoulders = Vector2.Lerp(m.Neck, m.Hips, 0.30f);
            Bone(0, m.Hips, m.Neck);
            Bone(1, m.Neck, m.Crown);
            Bone(2, role == Role.Kyle ? new Vector2(0.462f, 0.753f) :
                shoulders + new Vector2(-0.045f, 0f), m.NearElbow);
            Bone(3, m.NearElbow, m.NearHand);
            Bone(4, role == Role.Kyle ? new Vector2(0.707f, 0.748f) :
                shoulders + new Vector2(0.055f, -0.015f), m.FarElbow);
            Bone(5, m.FarElbow, m.FarHand);
            Bone(6, m.Hips + new Vector2(role == Role.Kyle ? -0.093f : 0.052f, 0f), m.NearKnee);
            Bone(7, m.NearKnee, m.NearFoot);
            Bone(8, m.Hips + new Vector2(role == Role.Kyle ? 0.059f : -0.056f, 0f), m.FarKnee);
            Bone(9, m.FarKnee, m.FarFoot);
            Bone(NearFoot, m.NearFoot, m.NearFoot + new Vector2(0.11f, -0.046f));
            Bone(FarFoot, m.FarFoot, m.FarFoot + new Vector2(0.11f, -0.046f));
            Bone(Wing, new Vector2(0.50f, 0.48f), new Vector2(0.31f, 0.72f));
            Bone(WingTip, new Vector2(0.31f, 0.72f), new Vector2(0.21f, 0.91f));
            Bone(Tail, new Vector2(0.38f, 0.25f), new Vector2(0.13f, 0.155f));
            Bone(TailTip, new Vector2(0.13f, 0.155f), new Vector2(0.08f, 0.35f));
            Bone(Blade, m.NearHand, new Vector2(0.096f, 0.206f));
            return;

            void Bone(int i, Vector2 a, Vector2 b)
            {
                joints[i] = bottom + Vector2.Scale(a, size);
                tips[i] = bottom + Vector2.Scale(b, size);
            }
        }

        private SkinWeight Assign(Vector2 p, float height)
        {
            Vector2 at = (p - (Vector2)skin.bounds.min) / (Vector2)skin.bounds.size;
            if (role == Role.Kyle && at.x < 0.44f && at.y < 0.445f &&
                Vector2.Distance(p, joints[Blade] + (tips[Blade] - joints[Blade]) *
                    Mathf.Clamp01(Vector2.Dot(p - joints[Blade], tips[Blade] - joints[Blade]) /
                        (tips[Blade] - joints[Blade]).sqrMagnitude)) < height * 0.051f)
                return new SkinWeight(Blade, Blade, 0f);
            int a = 0, b = 1;
            float first = float.MaxValue, second = float.MaxValue;
            for (int j = 0; j < JointCount; j++)
            {
                if (j >= Blade || (j >= Wing && (role != Role.Vulkano ||
                    (j <= WingTip ? at.y < 0.46f || at.x > 0.62f : at.x > 0.38f || at.y > 0.46f))))
                    continue;
                Vector2 segment = tips[j] - joints[j];
                float t = Mathf.Clamp01(Vector2.Dot(p - joints[j], segment) / segment.sqrMagnitude);
                float distance = Vector2.Distance(p, joints[j] + segment * t) * (j == 0 ? 0.84f : 1f);
                if (distance < first)
                {
                    b = a; second = first;
                    a = j; first = distance;
                }
                else if (distance < second)
                {
                    b = j; second = distance;
                }
            }
            float blend = .5f * (1f - Mathf.SmoothStep(0f, 1f, (second - first) / (height * .115f)));
            return new SkinWeight(a, b, blend);
        }

        private static Vector2 Rotate(Vector2 point, float a, float b) =>
            new(a * point.x - b * point.y, b * point.x + a * point.y);

        private void OnDestroy()
        {
            if (mesh)
                Destroy(mesh);
        }
    }
}


