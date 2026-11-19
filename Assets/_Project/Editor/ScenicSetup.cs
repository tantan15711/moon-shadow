using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly string[] VistaNames =
        {
            "PixelVistaCity", "PixelVistaGothic", "PixelVistaVolcano", "PixelVistaOrbital", "PixelVistaLunar"
        };
        private static readonly Color[] RoomTint =
        {
            new(0.53f, 0.65f, 0.74f, 0.61f), new(0.78f, 0.69f, 0.89f, 0.15f),
            new(0.87f, 0.51f, 0.45f, 0.11f), new(0.66f, 0.81f, 0.89f, 0.20f),
            new(0.63f, 0.74f, 0.95f, 0.20f)
        };

        private static void BindScenery(RuinsDemo world)
        {
            var camera = Object.FindAnyObjectByType<RuinsCamera>();
            int act = world.Stage.Act;
            var layer = GameObject.Find("Vista del acto");
            if (!layer)
                layer = new GameObject("Vista del acto", typeof(SpriteRenderer));
            var sprite = layer.GetComponent<SpriteRenderer>();
            sprite.sprite = Sprite(VistaNames[act]);
            sprite.sortingOrder = -24;
            sprite.color = new Color(0.78f, 0.80f, 0.87f);
            camera.SetVista(sprite);

            if (world.Map)
            {
                foreach (var wall in world.Map.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(view => view.name == "Fondo del sector"))
                    wall.color = RoomTint[act];
            }
            if (world.Stage.Final)
                StageRescueCapsule(world);
            if (act == 0)
                StageVaneGate(world);
        }

        private static void StageRescueCapsule(RuinsDemo world)
        {
            var director = new SerializedObject(world);
            var exit = (Transform)director.FindProperty("exit").objectReferenceValue;
            var captive = exit.parent.Cast<Transform>().First(child => child != exit && child.name == "Sofy");
            const float height = 3.28f;
            Vector2 floor = (Vector2)exit.position - Vector2.up * 0.83f;
            Vector2 center = floor + Vector2.up * (height * 0.5f);
            var chamber = exit.parent.Find("Cápsula de estasis");
            if (!chamber)
                chamber = new GameObject("Cápsula de estasis").transform;
            chamber.SetParent(exit.parent, false);
            for (int i = chamber.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(chamber.GetChild(i).gameObject);
            var doorLight = (SpriteRenderer)director.FindProperty("exitLight").objectReferenceValue;
            doorLight.enabled = false;
            doorLight.transform.parent.GetComponent<SpriteRenderer>().enabled = false;
            Art("Carcasa", "PixelLunarCapsule", center, new Vector2(2.18f, height), -2, chamber);
            Vector2 window = center - Vector2.up * 0.012f;
            var left = Art("Hoja izquierda", "PixelLunarGlass", window + Vector2.left * 0.283f,
                new Vector2(0.59f, 2.28f), 3, chamber);
            var right = Art("Hoja derecha", "PixelLunarGlass", window + Vector2.right * 0.283f,
                new Vector2(0.59f, 2.28f), 3, chamber);
            left.flipX = true;
            left.color = right.color = new Color(0.78f, 0.94f, 1f, 0.63f);

            captive.GetComponent<SpriteRenderer>().enabled = false;
            var previous = captive.Find("Modelo de personaje");
            if (previous)
                Object.DestroyImmediate(previous.gameObject);
            captive.localScale = Vector3.one;
            captive.position = floor + Vector2.up * 0.40f;
            const float sisterHeight = 1.69f;
            var sofysArt = Sprite("PixelSofyLunar");
            Vector2 size = (Vector2)sofysArt.bounds.size * (sisterHeight / sofysArt.bounds.size.y);
            var sofysFeet = (Vector2)captive.position;
            var sister = Art("Modelo de personaje", sofysArt.name, sofysFeet + Vector2.up * (sisterHeight * 0.5f),
                size, 1, captive);
            SetRig(sister, sofysArt, CastRig.Role.Civilian);

            var label = exit.parent.GetComponentsInChildren<TextMesh>().FirstOrDefault(text => text.text == "SOFY");
            if (label)
            {
                var plate = exit.parent.Cast<Transform>().FirstOrDefault(child =>
                    child.name == "Placa" && (child.position - label.transform.position).sqrMagnitude < 0.04f);
                label.transform.position = floor + Vector2.up * (height + 0.20f);
                if (plate)
                    plate.position = label.transform.position;
            }
            var rescue = world.Stage.Epilogue ? world.Stage.Epilogue : world.Stage.gameObject.AddComponent<LunarRescueSequence>();
            rescue.Configure(captive, sister, left, right);
            world.Stage.SetEpilogue(rescue);
        }

        private static void StageVaneGate(RuinsDemo world)
        {
            if (world.Stage.EncounterGate)
                return;
            var boss = world.Stage.Boss.transform;
            float x = boss.position.x - 13.5f;
            var gate = new GameObject("Compuerta del búnker");
            gate.transform.position = new Vector2(x, 3.7f);
            gate.layer = Terrain;
            gate.AddComponent<BoxCollider2D>().size = new Vector2(0.86f, 7.4f);
            Art("Plancha blindada", "Pylon", gate.transform.position, new Vector2(1.14f, 7.4f), 4, gate.transform)
                .color = new Color(0.69f, 0.70f, 0.74f);
            for (int i = 0; i < 7; i++)
                Art("Remache", "Pixel", new Vector2(x - 0.34f, 0.65f + 0.92f * i), new Vector2(0.10f, 0.18f), 5, gate.transform)
                    .color = new Color(0.85f, 0.63f, 0.29f);
            gate.SetActive(false);
            world.Stage.SetEncounterGate(gate);
        }

        private static void BindTitleVista(SpriteRenderer background)
        {
            background.sprite = Sprite(VistaNames[0]);
            Vector2 native = background.sprite.bounds.size;
            background.transform.localScale = Vector3.one * (23.6f / native.x);
            background.transform.position = Vector3.up * 0.83f;
            background.color = new Color(0.82f, 0.88f, 0.96f);
        }
    }
}
