using System.Linq;
using UnityEngine;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static void DressFoundry(RuinsDemo world)
        {
            var monitor = Sprite("PixelForgeMonitor");
            var truss = Sprite("PixelSteelColumn");
            var door = Sprite("PixelForgeDoor");
            var terminal = Sprite("PixelForgeTerminal");
            var rubble = Sprite("PixelForgeRubble");
            var catwalk = Sprite("PixelForgeCatwalk");
            var floor = Sprite("PixelForgeFloor");
            var lava = Sprite("PixelForgeLava");
            var foundation = Sprite("PixelDeck" + world.Stage.Act);
            Color metal = world.Stage.Act switch
            {
                1 => new(0.82f, 0.70f, 0.95f),
                2 => new(1f, 0.76f, 0.60f),
                3 => new(0.74f, 0.90f, 0.96f),
                4 => new(0.84f, 0.96f, 1f),
                _ => new(0.76f, 0.88f, 1f)
            };
            foreach (var view in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (!view.sprite)
                    continue;
                if (view.name == "Losa" && view.bounds.size.y >= 0.95f)
                {
                    DressGround(view, foundation, floor, metal);
                    continue;
                }
                string key = view.sprite.name;
                Sprite replacement = key switch
                {
                    "Pylon" when view.name != "Plancha blindada" => truss,
                    "PixelForgeTruss" => truss,
                    "Door" => door,
                    "Relay" when view.name == "Anclaje" => monitor,
                    "Relay" => terminal,
                    "Pixel" when view.name == "Lava" => lava,
                    _ when view.name == "Losa" => catwalk,
                    _ when key.StartsWith("Debris", System.StringComparison.Ordinal) => rubble,
                    _ => null
                };
                if (!replacement || replacement == view.sprite)
                    continue;
                ReplaceScenerySprite(view, replacement);
                view.color = replacement == lava || replacement == monitor ? Color.white : metal;
                if (replacement == lava && !view.TryGetComponent<LavaFlow>(out _))
                    view.gameObject.AddComponent<LavaFlow>();
            }
            foreach (var view in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
                if (view.name == "Brillo de lava")
                    view.enabled = false;

            world.Effects.ConfigureDash(Enumerable.Range(0, 4).Select(i => Sprite("PixelDashShard" + i)).ToArray());
        }

        private static void DressGround(SpriteRenderer tile, Sprite foundation, Sprite surface, Color metal)
        {
            ReplaceScenerySprite(tile, foundation);
            tile.color = new Color(0.84f, 0.88f, 0.95f);
            if (tile.transform.Find("Borde industrial"))
                return;

            Bounds block = tile.bounds;
            float depth = block.size.x * surface.bounds.size.y / surface.bounds.size.x;
            Vector2 crest = new(block.center.x, block.max.y - depth * 0.5f);
            Art("Borde industrial", surface.name, crest, new Vector2(block.size.x, depth),
                tile.sortingOrder + 1, tile.transform).color = metal;
        }

        private static void ReplaceScenerySprite(SpriteRenderer view, Sprite replacement)
        {
            var root = view.transform;
            var children = root.Cast<Transform>().Select(child => (child, child.position, child.localScale)).ToArray();
            Vector3 oldSize = view.sprite.bounds.size;
            Vector3 newSize = replacement.bounds.size;
            Vector3 factor = new(oldSize.x / newSize.x, oldSize.y / newSize.y, 1f);
            view.sprite = replacement;
            root.localScale = Vector3.Scale(root.localScale, factor);
            foreach (var (child, position, scale) in children)
            {
                child.position = position;
                child.localScale = new Vector3(scale.x / factor.x, scale.y / factor.y, scale.z);
            }
        }
    }
}
