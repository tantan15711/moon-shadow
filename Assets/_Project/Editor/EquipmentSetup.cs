using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly string[] Lamps =
            { "PixelLampUrban", "PixelLampGothic", "PixelLampFoundry", "PixelLampOrbital", "PixelLampOrbital" };
        private static readonly Color[] LampColors =
        {
            new(1f, 0.77f, 0.43f, 0.34f), new(0.80f, 0.54f, 0.98f, 0.28f), new(1f, 0.52f, 0.20f, 0.34f),
            new(0.38f, 0.82f, 1f, 0.33f), new(0.47f, 0.76f, 1f, 0.30f)
        };

        private static void CreateLampGlow()
        {
            WritePixelSprite("PixelLampHalo", 64, 64, (x, y) =>
            {
                Vector2 delta = new((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f);
                float core = Mathf.Clamp01(1f - delta.magnitude);
                byte alpha = (byte)(Mathf.Round(core * core * 12f) / 12f * 255f);
                return new Color32(255, 250, 230, alpha);
            });
        }

        private static void DressEquipment(RuinsDemo world)
        {
            int act = world.Stage.Act;
            var lamp = Sprite(Lamps[act]);
            var halo = Sprite("PixelLampHalo");
            foreach (var view in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (!view.sprite || view.sprite.name != "Lamp")
                    continue;
                float ground = view.bounds.min.y;
                float height = Mathf.Clamp(view.bounds.size.y * 0.96f, 2.90f, 4.50f);
                float scale = height / lamp.bounds.size.y;
                Vector3 position = view.transform.position;
                position.y = ground + height * 0.5f;
                view.transform.position = position;
                view.sprite = lamp;
                view.transform.localScale = new Vector3(scale, scale, 1f);
                view.flipX = Mathf.RoundToInt(position.x * 0.72f) % 2 == 0;
                view.color = new Color(0.78f, 0.82f, 0.87f);
                float width = lamp.bounds.size.x * scale;
                float side = view.flipX ? -1f : 1f;
                float lensHeight = act is 3 or 4 ? 0.40f : act == 2 ? 0.29f : 0.31f;
                Vector2 lens = new(position.x + side * width * 0.26f, position.y + height * lensHeight);
                var glow = Art("Halo de la farola", halo.name, lens + Vector2.down * 0.23f,
                    new Vector2(1.78f, 2.3f), view.sortingOrder - 1, view.transform);
                glow.color = LampColors[act];
            }

            if (!world.Map)
                return;
            var root = world.Map.transform;
            var cache = root.Find("Lanzallamas recuperable");
            if (cache)
            {
                var visual = cache.GetComponent<SpriteRenderer>();
                var icon = Sprite("PixelArmIncinerator");
                var children = cache.Cast<Transform>().Select(child => (Child: child, Position: child.position, Scale: child.lossyScale)).ToArray();
                float scale = 1.30f / icon.bounds.size.x;
                visual.sprite = icon;
                cache.localScale = new Vector3(scale, scale, 1f);
                foreach (var item in children)
                {
                    item.Child.position = item.Position;
                    item.Child.localScale = new Vector3(item.Scale.x / scale, item.Scale.y / scale, item.Scale.z);
                }
            }
            if (root.Find("Suministros de munición"))
                return;
            var supplies = new GameObject("Suministros de munición").transform;
            supplies.SetParent(root, false);

            float widthOfArena = 55f + act * 3f;
            CreateAmmo(world, supplies, act == 0 ? "PixelSupplyFuel" : "PixelSupplyRounds",
                new Vector2(238f + widthOfArena * 0.39f, 0.26f), act == 0 ? "Combustible" : "Munición", 4);
            CreateAmmo(world, supplies, "PixelSupplyShells", new Vector2(238f + widthOfArena - 7.1f, 0.27f), "Cartuchos", 5);
            CreateAmmo(world, supplies, "PixelSupplyRounds", new Vector2(338f + 11.2f, 0.27f), "Munición", 4);
        }

        private static void CreateAmmo(RuinsDemo world, Transform parent, string art, Vector2 position, string label, int count)
        {
            Sprite sprite = Sprite(art);
            Vector2 size = (Vector2)sprite.bounds.size * (0.48f / sprite.bounds.size.y);
            var view = Art(label, art, position, size, 7, parent);
            view.gameObject.AddComponent<AmmoSupply>().Configure(world, count, label);
        }
    }
}
