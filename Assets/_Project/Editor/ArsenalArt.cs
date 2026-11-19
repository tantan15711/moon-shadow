using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly Color32[] DistrictStone =
        {
            new(31, 41, 52, 255), new(37, 35, 54, 255), new(50, 34, 38, 255),
            new(37, 48, 63, 255), new(41, 47, 66, 255)
        };
        private static readonly Color32[] DistrictMetal =
        {
            new(56, 69, 78, 255), new(67, 65, 87, 255), new(74, 51, 52, 255),
            new(64, 88, 103, 255), new(65, 81, 114, 255)
        };
        private static readonly Color32[] DistrictLights =
        {
            new(203, 154, 85, 255), new(104, 175, 183, 255), new(201, 100, 57, 255),
            new(101, 192, 222, 255), new(149, 143, 212, 255)
        };

        private static void CreateArsenalSprites()
        {
            WritePixelSprite("PixelIncinerator", 48, 20, (x, y) => WeaponPixel(x, y, true));
            WritePixelSprite("PixelShotgun", 48, 20, (x, y) => WeaponPixel(x, y, false));
            for (int i = 0; i < 6; i++)
            {
                int frame = i;
                WritePixelSprite("PixelFlame" + i, 70, 30, (x, y) => FlamePixel(x, y, frame));
            }
            for (int i = 0; i < 5; i++)
            {
                int act = i;
                WritePixelSprite("PixelDistrictWall" + act, 336, 112, (x, y) => DistrictPixel(x, y, act));
            }
        }

        private static Color32 WeaponPixel(int x, int y, bool flame)
        {
            bool stock = x is >= 2 and <= 13 && y >= 7 + (x - 2) / 4 && y <= 13;
            bool receiver = x is >= 10 and <= 32 && y is >= 8 and <= 15;
            bool barrel = x is >= 27 and <= 45 && y is >= 11 and <= 15;
            bool grip = x is >= 14 and <= 20 && y is >= 4 and <= 8;
            bool tank = flame && x >= 20 && x <= 33 && y >= 3 && y <= 10 &&
                ((x - 26f) / 7.5f) * ((x - 26f) / 7.5f) + ((y - 7f) / 5f) * ((y - 7f) / 5f) < 1f;
            if (tank)
                return (x % 6 == 0 || y == 3) ? new Color32(68, 41, 36, 255) :
                    y >= 8 ? new Color32(255, 162, 69, 255) : new Color32(164, 77, 37, 255);
            if (!stock && !receiver && !barrel && !grip)
                return default;
            if (grip || y is 8 or 15 || x is 2 or 45)
                return new Color32(29, 37, 48, 255);
            if (flame && x >= 38)
                return x % 3 == 0 ? new Color32(241, 136, 65, 255) : new Color32(125, 63, 51, 255);
            if (y >= 13)
                return new Color32(137, 165, 171, 255);
            if (x is >= 24 and <= 30 && y == 11)
                return new Color32(197, 123, 83, 255);
            return new Color32(62, 77, 86, 255);
        }

        private static Color32 FlamePixel(int x, int y, int frame)
        {
            float along = x / 69f;
            float life = frame / 6f;
            float axis = 14.5f + Mathf.Sin(along * 7f - frame * 0.85f) * along * 4.0f;
            float envelope = Mathf.Lerp(2.0f, 13.1f, along) *
                (0.89f + Mathf.Sin(x * 0.30f - frame * 1.66f) * 0.2f);
            float depth = Mathf.Abs(y - axis) / envelope + Mathf.Max(0f, along - 0.76f) * 2.6f;
            if (depth > 1f || along > (0.61f + life * 0.43f))
                return (x * 11 + y * 7 + frame * 13) % 67 == 0 && depth < 1.28f && along < 0.91f
                    ? new Color32(255, 113, 51, 147) : default;
            if (depth < 0.39f && along < 0.78f)
                return new Color32(255, 232, 157, 255);
            return depth < 0.69f ? new Color32(255, 155, 53, 235) : new Color32(213, 64, 32, 188);
        }

        private static Color32 DistrictPixel(int x, int y, int act)
        {
            int panel = x % 56;
            var dark = new Color32(20, 25, 36, 255);
            if (y > 103 || y < 9 || panel < 3 || panel > 52 || y is 14 or 15 or 73 or 94)
                return dark;
            if (y is 16 or 74 or 95 || panel is 4 or 51)
                return DistrictMetal[act];
            if (y is >= 89 and <= 91 && panel is > 15 and < 38)
                return DistrictLights[act];
            if (act == 1 && panel is > 11 and < 43 && y is > 22 and < 69 &&
                (Mathf.Abs(panel - 27) < 13f - Mathf.Abs(y - 45f) * 0.10f))
                return panel is 17 or 36 || y is 29 or 62 ? DistrictLights[act] :
                    y < 33 + Mathf.Sin(panel * 0.37f) * 4 ? new Color32(54, 100, 113, 255) : dark;
            if (act >= 2 && panel is > 9 and < 47 && y is > 37 and < 66)
            {
                if (panel is 11 or 45 || y is 40 or 63)
                    return DistrictMetal[act];
                return ((y + panel / 3) % 12 == 0 && act == 2) ? DistrictLights[act] : dark;
            }
            if (act == 0 && (y % 8 == 0 || (x + y / 8 * 6) % 16 == 0))
                return new Color32(26, 34, 46, 255);
            return (x * 29 + y * 41) % 113 < 2 ? DistrictMetal[act] : DistrictStone[act];
        }

        private static void WritePixelSprite(string name, int width, int height, Func<int, int, Color32> colorAt)
        {
            string path = $"{Root}/Art/{name}.png";
            if (File.Exists(path))
                return;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = colorAt(x, y);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
        }

        private static void BindArsenal(RuinsEffects effects) => effects.ConfigureArsenal(
            new[] { Sprite("PixelArmService"), Sprite("PixelArmExperimental"), Sprite("PixelArmLegendary"),
                Sprite("PixelArmIncinerator"), Sprite("PixelArmShotgun") }, Sprite("PixelArmBlade"),
            Enumerable.Range(0, 8).Select(frame => Sprite("PixelTorch" + frame)).ToArray(),
            Enumerable.Range(0, 4).Select(frame => Sprite("PixelBlast" + frame)).ToArray(), Sprite("PixelSupplyMedical"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/ReloadOut.wav"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/ReloadIn.wav"));

        private static void BindCombatAudio(RuinsEffects effects)
        {
            AudioClip Clip(string key) => UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/{key}.wav");
            effects.ConfigureCombatAudio(new[] { Clip("FireService"), Clip("FireExperimental"), Clip("FireLegendary"),
                Clip("FireIncinerator"), Clip("FireShotgun") }, Clip("BladeSteelSwing"), Clip("BladeSteelContact"));
        }
    }
}
