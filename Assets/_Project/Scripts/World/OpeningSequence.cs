using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonShadow
{
    public sealed class OpeningSequence : MonoBehaviour
    {
        public const float Duration = 28f;
        private static readonly Vector3 SetLocation = Vector3.left * 200f;

        [SerializeField] private Sprite city, kyleArt, sofyArt, mechArt, pixel;
        [SerializeField] private Sprite transport;
        [SerializeField] private Material surface;
        [SerializeField] private AudioClip soundscape;
        [SerializeField] private Sprite evacuee, pulseArt, edgeArt;
        private Camera sceneCamera;
        private Camera lens;
        private AudioSource sound;
        private Transform set;
        private Transform plate, ship, cradle, beam, exhaust;
        private SpriteRenderer kyle, sofy, mech;
        private CastRig kyleRig;
        private CastRig sofyRig, mechRig;
        private SpriteRenderer[] escorts, civilians, nets, concrete;
        private CastRig[] escortRigs, civilianRigs;
        private SpriteRenderer drone, pulse, ripple, blade;
        private Action complete;
        private SpriteRenderer[] rain;
        private MaterialPropertyBlock polygonProperties;
        private float time;
        private readonly List<Mesh> scenery = new();
        private AudioSource[] pausedTracks;
        public bool Playing { get; private set; }
        public float Elapsed => time;
        public float Fade { get; private set; }
        public string Caption => time switch
        {
            >= 7.5f and < 11.2f => CaptionLine(0),
            >= 11.2f and < 13.6f => CaptionLine(1),
            >= 19.3f and < 27.15f => CaptionLine(2),
            _ => ""
        };

        private static string CaptionLine(int index) => $"{MoonStory.Opening[index].Speaker}  ·  {MoonStory.Opening[index].Text}";

        public void Configure(Sprite background, Sprite hero, Sprite sister, Sprite captureUnit, Sprite flat,
            Material rendererMaterial, AudioClip soundtrack, Sprite dropship)
        {
            city = background; kyleArt = hero; sofyArt = sister; mechArt = captureUnit; pixel = flat;
            surface = rendererMaterial; soundscape = soundtrack;
            transport = dropship;

        }

        public void Begin(Action onComplete)
        {
            if (Playing)
                return;
            complete = onComplete;
            sceneCamera = Camera.main;
            if (sceneCamera)
                sceneCamera.enabled = false;
            pausedTracks = Array.FindAll(FindObjectsByType<AudioSource>(FindObjectsSortMode.None), track => track.isPlaying);
            foreach (var track in pausedTracks)
                track.Pause();
            Assemble();
            time = 0f;

            Playing = true;
            UpdateShot(0f);
            sound.Play();
        }

        public void SetActionArt(Sprite civilian, Sprite sonic, Sprite slash)
        {
            evacuee = civilian;

            pulseArt = sonic;
            edgeArt = slash;
        }

        public void Finish()
        {
            if (!Playing)
                return;
            Playing = false;
            lens.enabled = false;
            sound.Stop();
            if (sceneCamera)
                sceneCamera.enabled = true;
            foreach (var track in pausedTracks)
                if (track)
                    track.UnPause();
            set.gameObject.SetActive(false);
            Destroy(set.gameObject);
            ReleaseScenery();
            Action callback = complete;
            complete = null;
            callback?.Invoke();
        }

        private void Update()
        {
            if (!Playing)
                return;
            time = Mathf.Min(Duration, time + Time.unscaledDeltaTime);
            UpdateShot(Time.unscaledDeltaTime);
            if (time >= Duration)
                Finish();
        }

        private void Assemble()
        {
            set = new GameObject("Rodaje: la captura").transform;
            set.SetParent(transform, false);
            set.position = SetLocation;
            lens = new GameObject("Cámara de la intro").AddComponent<Camera>();
            lens.transform.SetParent(set, false);
            lens.tag = "MainCamera";
            lens.orthographic = true;
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.025f, 0.035f, 0.068f);
            lens.nearClipPlane = 0.1f;
            lens.farClipPlane = 35f;
            sound = set.gameObject.AddComponent<AudioSource>();
            sound.clip = soundscape;
            sound.playOnAwake = false;
            sound.volume = 0.80f;

            plate = MakeSprite("Ciudad eclipsada", city, set, -45).transform;
            plate.localScale = ScaleToHeight(city, 15.2f);
            polygonProperties = new MaterialPropertyBlock();

            ship = new GameObject("Transporte").transform;
            ship.SetParent(set, false);
            var hull = MakeSprite("Casco del transporte", transport, ship, 5);
            hull.transform.localScale = ScaleToHeight(transport, 2.92f);
            beam = Plate("Foco de búsqueda", ship, -16, new Color(0.47f, 0.70f, 1f, 0.06f),
                new(-0.53f, -0.35f), new(0.56f, -0.35f), new(2.55f, -6.4f), new(-2.55f, -6.4f));
            Plate("Halo del foco", beam, -15, new Color(0.51f, 0.76f, 1f, 0.08f),
                new(-0.28f, -0.35f), new(0.30f, -0.35f), new(1.70f, -6.3f), new(-1.70f, -6.3f));
            exhaust = Plate("Propulsión", ship, 0, new Color(0.54f, 0.77f, 1f, 0.62f),
                new(-3.54f, -0.15f), new(-5.53f, -0.39f), new(-4.68f, -0.10f), new(-3.62f, 0.26f));
            Plate("Segundo motor", exhaust, 0, new Color(0.53f, 0.66f, 1f, 0.28f),
                new(-3.54f, 0.24f), new(-5.12f, 0.30f), new(-4.60f, 0.51f), new(-3.54f, 0.64f));

            kyle = Actor("Kyle", kyleArt, CastRig.Role.Kyle, 1.83f, 15, out kyleRig);
            sofy = Actor("Sofy", sofyArt, CastRig.Role.Civilian, 1.93f, 15, out sofyRig);
            mech = Actor("Unidad de captura", mechArt, CastRig.Role.Capture, 3.76f, 16, out mechRig);

            AssembleStreetAction();

            cradle = new GameObject("Jaula del transporte").transform;
            cradle.SetParent(set, false);
            var steel = new Color(0.23f, 0.31f, 0.44f);
            Plate("Cubierta del elevador", cradle, 20, steel,
                new(-1.22f, 0.19f), new(1.22f, 0.19f), new(0.99f, -0.22f), new(-1.01f, -0.22f));
            Bar(cradle, new Vector2(0f, -0.09f), new Vector2(1.97f, 0.10f), new Color(0.12f, 0.18f, 0.26f), 21);
            Bar(cradle, new Vector2(0f, 0.82f), new Vector2(1.98f, 0.09f), steel, 20);
            Bar(cradle, new Vector2(-0.91f, 0.48f), new Vector2(0.08f, 0.75f), steel, 20)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            Bar(cradle, new Vector2(0.91f, 0.48f), new Vector2(0.08f, 0.75f), steel, 20)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            Bar(cradle, new Vector2(0.68f, 0.11f), new Vector2(0.23f, 0.08f), new Color(1f, 0.68f, 0.31f), 21);
            for (int i = 0; i < 6; i++)
                Bar(cradle, new Vector2(-0.88f + 0.13f * i, 0.00f), new Vector2(0.035f, 0.18f),
                    new Color(0.59f, 0.43f, 0.28f), 21).transform.localRotation = Quaternion.Euler(0, 0, -26f);
            Plate("Sombra de la máquina", set, -11, new Color(0.025f, 0.029f, 0.048f, 0.46f),
                new(0.2f, streetY), new(3.24f, streetY + 0.11f), new(5.0f, streetY), new(3.0f, streetY - 0.30f));
            rain = new SpriteRenderer[56];
            for (int i = 0; i < rain.Length; i++)
            {
                rain[i] = Bar(set, Vector2.zero, new Vector2(0.013f, 0.26f + 0.018f * (i % 7)),
                    new Color(0.61f, 0.77f, 0.93f, 0.13f + i % 4 * 0.035f), 40);
                rain[i].transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            }
        }

        private void AssembleStreetAction()
        {
            escorts = new SpriteRenderer[2];
            escortRigs = new CastRig[2];
            civilians = new SpriteRenderer[2];
            civilianRigs = new CastRig[2];
            nets = new SpriteRenderer[6];
            concrete = new SpriteRenderer[13];
            for (int i = 0; i < escorts.Length; i++)
            {
                escorts[i] = Actor($"Unidad de contención {i + 2}", mechArt, CastRig.Role.Capture, 2.64f, 7 + i, out escortRigs[i]);

                civilians[i] = Actor($"Civil en la calle {i + 1}", evacuee, CastRig.Role.Civilian, 1.78f, 10 + i, out civilianRigs[i]);
            }
            for (int i = 0; i < nets.Length; i++)
                nets[i] = Bar(set, Vector2.zero, new Vector2(1f, 0.034f), new Color(0.41f, 0.88f, 1f, 0.63f), 12);
            for (int i = 0; i < concrete.Length; i++)
            {
                float size = 0.06f + i % 4 * 0.043f;
                concrete[i] = Bar(set, Vector2.zero, new Vector2(size, size * 0.85f), new Color(0.37f, 0.43f, 0.53f), 18);
            }

            drone = MakeSprite("Dron de contención", pulseArt, set, 13);
            drone.transform.localScale = ScaleToHeight(pulseArt, 0.44f);
            pulse = MakeSprite("Pulso sónico", pulseArt, set, 19);
            pulse.transform.localScale = ScaleToHeight(pulseArt, 0.60f);
            ripple = MakeSprite("Onda expansiva del dron", edgeArt, set, 14);
            ripple.transform.localScale = ScaleToHeight(edgeArt, 1.15f);
            blade = MakeSprite("Acero de Kyle", edgeArt, set, 18);
            blade.transform.localScale = ScaleToHeight(edgeArt, 1.42f);
            var gun = Plate("Torreta de azotea", set, 6, new Color(0.15f, 0.20f, 0.31f),
                new(3.63f, 1.45f), new(4.25f, 1.45f), new(4.17f, 1.73f), new(3.74f, 1.80f));
            Bar(gun, new Vector2(3.58f, 1.53f), new Vector2(0.36f, 0.08f), new Color(0.91f, 0.40f, 0.29f), 7);
        }

        private void UpdateShot(float dt)
        {
            float t = time;
            float escape = Smooth(15.3f, 22.4f);
            float approach = Smooth(2.0f, 8.1f);
            float lift = Smooth(11.0f, 16.2f);
            float capture = Smooth(7.62f, 9.7f);
            const float street = streetY;

            Vector2 camera;
            float zoom;
            if (t < 5.3f)
            {
                camera = new Vector2(Mathf.Lerp(-2.7f, -1.1f, Smooth(0f, 5.3f)), Mathf.Lerp(1.42f, 0.18f, Smooth(0.45f, 4.8f)));
                zoom = Mathf.Lerp(5.18f, 4.45f, Smooth(0f, 5.3f));
            }
            else if (t < 10.15f)
            {
                camera = new Vector2(Mathf.Lerp(1.81f, 0.77f, Smooth(5.3f, 10.15f)), -0.83f);
                zoom = 3.23f;
            }
            else if (t < 15.4f)
            {
                camera = new Vector2(Mathf.Lerp(-1.6f, 1.32f, Smooth(10.15f, 15.4f)), Mathf.Lerp(-0.5f, 0.80f, lift));
                zoom = Mathf.Lerp(2.78f, 4.75f, lift);
                float fall = Smooth(11.61f, 12.24f) * (1f - Smooth(12.72f, 15.17f));
                float frameLeap = Smooth(10.25f, 11.31f) * (1f - Smooth(11.63f, 12.11f));
                camera += new Vector2(1.53f, 0.15f) * frameLeap;
                zoom += 0.30f * frameLeap;
                camera += new Vector2(-1.25f, -1.10f) * fall;
            }
            else
            {
                camera = new Vector2(Mathf.Lerp(0.92f, 1.93f, escape), Mathf.Lerp(0.71f, 2f, escape));
                zoom = Mathf.Lerp(4.68f, 5.3f, escape);
            }

            float rumble = Mathf.Exp(-Mathf.Abs(t - 11.61f) * 11f) * 0.24f +
                Mathf.Exp(-Mathf.Abs(t - 12.20f) * 12f) * 0.12f +
                Mathf.Exp(-Mathf.Abs(t - 15.8f) * 3.5f) * 0.13f;
            camera += new Vector2(Mathf.Sin(t * 87f), Mathf.Cos(t * 59f)) * rumble;
            lens.transform.localPosition = new Vector3(camera.x, camera.y, -10f);
            lens.orthographicSize = zoom;
            plate.localPosition = new Vector3(camera.x * 0.30f, 1.41f + camera.y * 0.17f, 0f);

            ship.localPosition = new Vector3(Mathf.Lerp(12.5f, 2.1f, approach) + escape * 5.4f,
                3.25f + escape * 12.6f + Mathf.Sin(t * 3f) * 0.046f, 0f);
            ship.localRotation = Quaternion.Euler(0, 0, -3f + escape * 10f);
            beam.gameObject.SetActive(t > 2.7f && t < 17.0f);
            beam.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 5.5f);
            exhaust.localScale = new Vector3(0.95f + Mathf.Sin(t * 31f) * 0.035f + 0.29f * escape, 1f, 1f);
            float machineX = Mathf.Lerp(7.35f, 2.38f, Smooth(3.3f, 7.22f));
            float machineSpeed = dt > 0f ? Mathf.Abs(machineX - mech.transform.localPosition.x) / dt : 0f;
            mech.transform.localPosition = new Vector3(machineX, street, 0f);
            float claw = Smooth(7.05f, 8.64f) * (1f - Smooth(10.75f, 12.15f));
            mechRig.Animate(dt, machineSpeed, -1, claw > 0f ? CastRig.Motion.Reach : CastRig.Motion.Travel,
                claw, new Color(0.91f, 0.96f, 1f));

            Vector2 cage = new Vector2(Mathf.Lerp(machineX - 0.8f, ship.localPosition.x - 0.22f, lift),
                Mathf.Lerp(street + 0.63f, ship.localPosition.y - 0.53f, lift));
            cradle.localPosition = cage;
            cradle.gameObject.SetActive(t > 7.5f && t < 16.05f);
            float retreat = Mathf.Lerp(1.45f, 0.46f, Smooth(5.85f, 7.72f));
            float fallSofy = Mathf.Sin(Mathf.InverseLerp(7.04f, 7.68f, t) * Mathf.PI) * 0.28f;
            Vector2 sofysFeet = Vector2.Lerp(new Vector2(retreat, street), cage + new Vector2(-0.18f, 0.12f), capture);
            sofy.transform.localRotation = Quaternion.Euler(0f, 0f, fallSofy * -83f);
            sofysFeet += new Vector2(Mathf.Sin(t * 13f) * 0.11f, Mathf.Max(0f, Mathf.Sin(t * 10.5f)) * 0.16f) *
                (Smooth(7.2f, 8.55f) * (1f - Smooth(13.75f, 15.8f)));
            float sofysSpeed = dt > 0f ? Mathf.Abs(sofysFeet.x - sofy.transform.localPosition.x) / dt : 0f;
            sofy.transform.localPosition = sofysFeet;
            sofy.sortingOrder = t >= 7.4f ? 17 : 15;
            sofy.gameObject.SetActive(t < 16.05f);
            if (sofy.gameObject.activeSelf)
                sofyRig.Animate(dt, sofysSpeed, t < 7.15f ? -1 : 1,
                    t is >= 7.42f and < 15.9f ? CastRig.Motion.Struggle : CastRig.Motion.Travel,
                    (t - 7.42f) * (t < 11.0f ? 1.42f : 1.08f), new Color(1f, 0.93f, 0.85f));

            float x = Mathf.Lerp(-8.82f, -3.65f, Smooth(1.45f, 5.85f)) +
                Smooth(9.12f, 10.74f) * 3.08f + Smooth(10.74f, 11.61f) * 1.17f -
                Smooth(11.61f, 12.32f) * 4.65f + Smooth(16.4f, 18.95f) * 0.86f;
            float velocity = dt > 0f ? (x - kyle.transform.localPosition.x) / dt : 0f;
            float jump = Mathf.Sin(Mathf.InverseLerp(10.74f, 12.32f, t) * Mathf.PI) * 1.34f;
            float hit = Mathf.Sin(Mathf.InverseLerp(11.61f, 12.60f, t) * Mathf.PI);
            kyle.transform.localPosition = new Vector3(x, street + jump, 0f);
            kyle.transform.localRotation = Quaternion.Euler(0f, 0f, hit * 57f);
            var heroMotion = hit > 0.05f ? CastRig.Motion.Stun :
                t is >= 10.84f and < 11.61f ? CastRig.Motion.Strike : jump > 0.07f ? CastRig.Motion.Air :
                t is >= 12.30f and < 14.00f ? CastRig.Motion.Recover : CastRig.Motion.Travel;
            float heroProgress = heroMotion == CastRig.Motion.Recover ? Mathf.InverseLerp(12.30f, 14f, t) :
                Mathf.InverseLerp(10.74f, 11.61f, t);
            bool offStreet = jump > 0.06f || hit > 0.05f;
            kyleRig.Drive(dt, new Vector2(velocity, 0f), 1, heroMotion, heroProgress, new Color(0.92f, 0.96f, 1f),
                offStreet ? float.NaN : kyle.transform.position.y,
                bladeAngle: heroMotion == CastRig.Motion.Strike ? Mathf.Lerp(95f, -17f, heroProgress) : float.NaN,
                hang: offStreet ? Mathf.InverseLerp(10.74f, 12.32f, t) : float.NaN);
            UpdateStreetAction(t, dt);
            for (int i = 0; i < rain.Length; i++)
            {
                float py = Mathf.Repeat(i * 0.859f - t * (3.5f + i % 5 * 0.25f), 11.6f);
                float px = Mathf.Repeat(i * 4.67f + t * 0.87f, 31f);
                rain[i].transform.localPosition = new Vector3(px - 15.5f, py - 5.8f, 0f);
            }
            Fade = Mathf.Max(1f - Smooth(0.9f, 2.35f), Smooth(26.3f, Duration));
            Fade = Mathf.Max(Fade, Mathf.Exp(-Mathf.Abs(t - 11.62f) * 14f) * 0.28f);
            Fade = Mathf.Max(Fade, Mathf.Max(Cut(5.3f), Cut(10.15f), Cut(15.4f)));
        }

        private void UpdateStreetAction(float t, float dt)
        {
            for (int i = 0; i < escorts.Length; i++)
            {
                float advance = Smooth(2.4f + i * 0.58f, 7.6f + i * 0.37f);
                float machineX = Mathf.Lerp(9.5f + i * 2.6f, 5.48f + i * 1.66f, advance);
                float movement = dt > 0f ? Mathf.Abs(escorts[i].transform.localPosition.x - machineX) / dt : 0f;
                escorts[i].transform.localPosition = new Vector3(machineX, streetY + 0.25f + i * 0.27f, 0f);
                float fire = Smooth(5.13f + i * 0.42f, 6.1f + i * 0.42f);
                escortRigs[i].Animate(dt, movement, -1, fire > 0f ? CastRig.Motion.Reach : CastRig.Motion.Travel, fire,
                    new Color(0.52f, 0.67f, 0.83f, t > 3f ? 1f : 0.64f));
                float flight = Smooth(2.8f + i * 0.61f, 6.44f + i * 0.30f);
                var civilPosition = new Vector2(Mathf.Lerp(5.10f + i * 1.9f, 2.86f + i * 2.7f, flight), streetY + i * 0.45f);
                civilians[i].transform.localPosition = civilPosition;
                civilianRigs[i].Animate(dt, (1f - flight) * 2.05f, -1, flight > 0.96f ? CastRig.Motion.Struggle : CastRig.Motion.Travel,
                    t * 1.55f, new Color(0.47f, 0.61f, 0.76f));
                for (int j = 0; j < 3; j++)
                    SetNet(nets[i * 3 + j], civilPosition + new Vector2(-0.34f, 0.25f + j * 0.42f),
                        civilPosition + new Vector2(0.39f, 1.22f - j * 0.18f), t > 5.97f + i * 0.46f && t < 10.9f);
            }

            drone.enabled = t is >= 5.4f and < 8.9f;
            drone.transform.localPosition = new Vector3(Mathf.Lerp(8.1f, 1.8f, Smooth(5.4f, 7.1f)), 0.60f + Mathf.Sin(t * 4f) * 0.14f);
            ripple.enabled = t is >= 7.06f and < 7.67f;
            ripple.transform.localPosition = sofy.transform.localPosition + Vector3.up * 0.36f;
            ripple.color = new Color(0.41f, 0.72f, 1f, 1f - Mathf.InverseLerp(7.06f, 7.67f, t));
            pulse.enabled = t is >= 11.25f and < 11.63f;
            pulse.transform.localPosition = Vector2.Lerp(new Vector2(3.64f, 1.55f), new Vector2(0.34f, -0.49f), Smooth(11.25f, 11.62f));
            blade.enabled = t is >= 11.09f and < 11.48f;
            blade.transform.localPosition = kyle.transform.localPosition + new Vector3(0.77f, 1.13f, 0f);
            blade.color = new Color(0.52f, 0.84f, 1f, 0.86f);
            for (int i = 0; i < concrete.Length; i++)
            {
                float d = t - 12.17f - i * 0.018f;
                concrete[i].enabled = d >= 0f && d < 1.03f;
                concrete[i].transform.localPosition = new Vector3(-4.06f + d * (0.33f + i * 0.15f),
                    streetY + 0.56f + (i % 5) * 0.28f + d * (2.6f + i % 3 - 6f * d));
                concrete[i].transform.localRotation = Quaternion.Euler(0f, 0f, d * (45f + i * 16f));
            }
        }

        private void SetNet(SpriteRenderer part, Vector2 start, Vector2 end, bool visible)
        {
            part.enabled = visible;
            if (!visible) return;
            Vector2 delta = end - start;
            part.transform.localPosition = (start + end) * 0.5f;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var scale = part.transform.localScale;
            scale.x = delta.magnitude / pixel.bounds.size.x;
            part.transform.localScale = scale;
        }

        private float Smooth(float from, float to) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, time));
        private float Cut(float at) => Mathf.Max(0f, 1f - Mathf.Abs(time - at) * 19f) * 0.78f;
        private const float streetY = -2.76f;

        private SpriteRenderer Actor(string name, Sprite art, CastRig.Role role, float size, int sorting, out CastRig actorRig)
        {
            var actor = MakeSprite(name, art, set, sorting);
            actor.transform.localScale = ScaleToHeight(art, size);
            actorRig = actor.gameObject.AddComponent<CastRig>();
            actorRig.Configure(art, surface, role);
            return actor;
        }

        private SpriteRenderer Bar(Transform parent, Vector2 center, Vector2 size, Color color, int order)
        {
            var part = MakeSprite("Detalle", pixel, parent, order);
            part.transform.localPosition = center;
            part.transform.localScale = new Vector3(size.x / pixel.bounds.size.x, size.y / pixel.bounds.size.y, 1f);
            part.color = color;
            return part;
        }

        private Transform Plate(string name, Transform parent, int order, Color color, params Vector2[] outline)
        {
            var root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            root.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            scenery.Add(mesh);
            var vertices = new Vector3[outline.Length];
            var uv = new Vector2[outline.Length];
            var triangles = new int[(outline.Length - 2) * 3];
            for (int i = 0; i < outline.Length; i++)
            {
                vertices[i] = outline[i];
                uv[i] = Vector2.one * 0.5f;
                if (i < 2)
                    continue;
                triangles[(i - 2) * 3] = 0;
                triangles[(i - 2) * 3 + 1] = i - 1;
                triangles[(i - 2) * 3 + 2] = i;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            var view = root.GetComponent<MeshRenderer>();
            view.sharedMaterial = surface;
            view.sortingOrder = order;
            polygonProperties.SetTexture("_MainTex", Texture2D.whiteTexture);
            polygonProperties.SetColor("_Tint", color);
            view.SetPropertyBlock(polygonProperties);
            return root.transform;
        }

        private static SpriteRenderer MakeSprite(string name, Sprite art, Transform parent, int sorting)
        {
            var visual = new GameObject(name).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(parent, false);
            visual.sprite = art;
            visual.sortingOrder = sorting;
            return visual;
        }

        private static Vector3 ScaleToHeight(Sprite art, float height) =>
            new(height / art.bounds.size.y, height / art.bounds.size.y, 1f);

        private void ReleaseScenery()
        {
            foreach (var mesh in scenery)
                Destroy(mesh);
            scenery.Clear();
        }

        private void OnDestroy() => ReleaseScenery();
    }
}

