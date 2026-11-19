using System;
using System.Linq;
using UnityEngine;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly Color32[] AttackColors =
        {
            new(232, 144, 66, 255), new(134, 221, 72, 255), new(234, 75, 137, 255),
            new(246, 93, 35, 255), new(238, 107, 54, 255), new(60, 185, 245, 255),
            new(193, 100, 249, 255), new(238, 92, 153, 255), new(199, 136, 81, 255),
            new(125, 134, 247, 255)
        };

        private static void CreateCombatSprites()
        {
            for (int style = 0; style < RuinsEffects.AttackStyles; style++)
            for (int frame = 0; frame < RuinsEffects.AttackFrames; frame++)
            {
                int family = style, phase = frame;
                WritePixelSprite($"PixelHostile{style}_{frame}", 38, 19, (x, y) => HostilePixel(x, y, family, phase));
                WritePixelSprite($"PixelSpecial{style}_{frame}", 116, 32, (x, y) => HazardPixel(x, y, family, phase));
            }
            for (int i = 0; i < 8; i++)
            {
                int frame = i;
                WritePixelSprite("PixelTorch" + i, 100, 38, (x, y) => TorchPixel(x, y, frame));
            }
            WritePixelSprite("PixelDangerOutline", 112, 28, WarningPixel);
            WritePixelSprite("PixelBladeImpact", 26, 26, SparkPixel);
            WritePixelSprite("PixelStasisFrame", 70, 112, StasisFrame);
            WritePixelSprite("PixelStasisGlass", 32, 100, StasisGlass);
        }

        private static void BindCombatSprites(RuinsEffects effects)
        {
            Sprite[] Sequence(string prefix) => Enumerable.Range(0, RuinsEffects.AttackStyles)
                .SelectMany(style => Enumerable.Range(0, RuinsEffects.AttackFrames)
                    .Select(frame => Sprite($"{prefix}{style}_{frame}"))).ToArray();
            effects.ConfigureAttackArt(Sequence("PixelHostile"), Sequence("PixelSpecial"),
                Sprite("PixelDangerOutline"), Sprite("PixelBladeImpact"));
            effects.ConfigureMoveArt(Enum.GetValues(typeof(EnemyBase2D.Technique)).Cast<EnemyBase2D.Technique>()
                .Select(move => move is EnemyBase2D.Technique.None or >= EnemyBase2D.Technique.ZombieRake
                    ? null : Sprite("PixelMoonMove" + move)).ToArray(),
                new[] { "Down", "Sweep", "Up", "Deflect", "Wake", "Impact", "Return", "Debris" }
                    .Select(name => Sprite("PixelKatana" + name)).ToArray());
        }

        private static Color32 HostilePixel(int x, int y, int style, int frame)
        {
            float v = Mathf.Abs(y - 9f);
            float u = (x - 18f) / 18f;
            Color32 tone = AttackColors[style];
            Color32 bright = Color32.Lerp(tone, new Color32(255, 250, 229, 255), 0.77f);
            if (style == 4)
            {
                if (x >= 10 && x <= 32 && v <= (x > 26 ? (33 - x) * 0.58f : 3.4f))
                    return x % 8 is 1 or 2 ? tone : v < 1.5f ? new Color32(205, 223, 230, 255) : new Color32(83, 101, 123, 255);
                if (x < 16 && v <= (x + frame * 2) % 9 * 0.55f)
                    return x < 7 ? tone : bright;
                if (x is >= 10 and <= 18 && v < 6)
                    return new Color32(99, 114, 131, 255);
                return default;
            }
            if (style is 5 or 6 or 9)
            {
                float distance = Mathf.Abs(u * 1.35f) + v / 11f;
                if (distance < 0.40f)
                    return bright;
                if (Mathf.Abs(distance - 0.76f) < 0.18f && (x + y + frame * 2) % 8 != 1)
                    return tone;
                if (distance < 0.76f)
                    return Color32.Lerp(tone, new Color32(99, 67, 150, 255), 0.55f);
                return default;
            }
            float width = style == 2 ? 2.65f : style == 3 ? 5.9f : 4.3f;
            float taper = 1f - Mathf.Abs(u * (u > 0f ? 1.02f : 0.77f));
            if (style == 3)
                taper += Mathf.Sin(x * 0.67f + frame * 1.9f) * 0.22f;
            float thickness = width * taper;
            if (v > thickness || x < 2)
                return default;
            if (style == 0 && x < 11)
                return v < thickness * 0.55f ? new Color32(200, 178, 146, 255) : new Color32(120, 78, 62, 255);
            return v < thickness * 0.46f ? bright : tone;
        }

        private static Color32 HazardPixel(int x, int y, int style, int frame)
        {
            float t = x / 115f;
            float amplitude = Mathf.Sin(t * Mathf.PI);
            float phase = x * (style is 1 or 3 or 8 ? 0.22f : 0.13f) + frame * 1.63f;
            float spine = 15.5f + Mathf.Sin(phase) * amplitude *
                (style is 1 or 3 or 7 or 8 ? 6.0f : 3.1f);
            float edge = Mathf.Abs(y - spine);
            float core = style switch
            {
                1 or 3 or 8 => 4f + amplitude * (3.0f + Mathf.Abs(Mathf.Sin(phase * 0.77f)) * 8f),
                2 => 2.4f + amplitude * 2.1f,
                7 => 2.2f + amplitude * 5.2f,
                _ => 3.1f + amplitude * 4.9f
            };
            Color32 glow = AttackColors[style];
            Color32 hot = Color32.Lerp(glow, new Color32(255, 248, 229, 255), 0.72f);
            if (edge <= core * (style == 7 ? amplitude : 1f))
                return edge < core * 0.26f ? hot : edge < core * 0.64f ? Color32.Lerp(hot, glow, 0.42f) : glow;
            bool shard = (x * 17 + y * 7 + frame * 13) % 43 < 2 && edge < core + 7.3f;
            float rail = Mathf.Abs(edge - core * 1.36f);
            if (shard || (style is 5 or 6 or 9 && rail < 1.15f && (x + frame * 5) % 24 > 6))
                return new Color32(glow.r, glow.g, glow.b, 169);
            return default;
        }

        private static Color32 WarningPixel(int x, int y)
        {
            int h = Mathf.Min(y, 27 - y);
            int v = Mathf.Min(x, 111 - x);
            if ((h <= 2 && (v < 11 || x % 14 < 9)) || (v < 3 && y % 10 < 6))
                return new Color32(255, 255, 255, 235);
            int alpha = (x - y * 2 + 200) % 20 <= 1 ? 58 : 20;
            return new Color32(255, 255, 255, (byte)alpha);
        }

        private static Color32 SparkPixel(int x, int y)
        {
            float ax = Mathf.Abs(x - 12.5f), ay = Mathf.Abs(y - 12.5f);
            float spike = 1f - (ax + ay) / 17f;
            if (spike <= 0f || (ax > 1.6f && ay > 1.6f && Mathf.Abs(ax - ay) > 0.8f))
                return default;
            return new Color32(255, 255, 255, (byte)(208 + 47 * spike));
        }

        private static Color32 TorchPixel(int x, int y, int frame)
        {
            float t = x / 99f;
            float front = 0.75f + frame * 0.033f;
            float baseLine = 18f + (Mathf.Sin(t * 10f - frame * 0.96f) * 3.6f + 2.9f * t) * t;
            float outer = Mathf.Lerp(2.5f, 17f, Mathf.Pow(t, 0.73f)) *
                (0.76f + 0.18f * Mathf.Sin(t * 22f - frame * 1.34f) + 0.17f * Mathf.Cos(t * 39f + frame));
            float band = Mathf.Abs(y - baseLine) / outer;
            float holes = Mathf.Sin(x * 0.28f - frame * 1.05f) * Mathf.Sin(y * 0.46f + frame * 0.34f);
            if (t > front || band > 1.0f || (t > 0.54f && band > 0.54f && holes > 0.78f))
                return t > 0.50f && band < 1.34f && (13 * x + 7 * y - frame * 17 + 200) % 79 == 0
                    ? new Color32(255, 128, 47, 182) : default;
            if (t < 0.13f && band < 0.87f)
                return band < 0.40f ? new Color32(242, 250, 255, 255) : new Color32(99, 175, 239, 245);
            if (band < 0.28f + holes * 0.07f && t < 0.82f)
                return new Color32(255, 242, 160, 255);
            if (band < 0.53f + holes * 0.1f)
                return new Color32(255, 178, 62, 255);
            return band < 0.79f ? new Color32(252, 106, 37, 235) : new Color32(173, 46, 32, 186);
        }

        private static Color32 StasisFrame(int x, int y)
        {
            int edge = Mathf.Min(x, 69 - x);
            int tip = Mathf.Min(y, 111 - y);
            if (edge + tip < 11 || (tip < 4 && edge < 17) || edge < 2)
                return default;
            var alloy = new Color32(63, 86, 109, 255);
            var cold = new Color32(117, 201, 231, 255);
            if (tip < 17)
            {
                if ((tip == 15 || tip == 4) && edge > 13) return cold;
                if (edge % 10 == 1 && tip > 6 && tip < 12) return new Color32(218, 166, 95, 255);
                return tip < 7 ? new Color32(108, 143, 165, 255) : alloy;
            }
            if (edge is >= 8 and <= 12)
                return edge == 8 || y % 28 < 3 ? cold : alloy;
            if (edge < 8) return new Color32(26, 40, 61, 255);
            return new Color32(24, 45, 75, 226);
        }

        private static Color32 StasisGlass(int x, int y)
        {
            if (y is < 2 or > 97 || x < 1)
                return default;
            if (x is 1 or 30 || y is 2 or 97)
                return new Color32(126, 220, 247, 239);
            float diagonal = Mathf.Abs(x * 1.3f - y * 0.38f - 6f);
            if (diagonal < 2.3f || Mathf.Abs(diagonal - 13f) < 1.1f)
                return new Color32(128, 223, 247, 148);
            return new Color32(59, 143, 196, (byte)(y % 18 == 3 ? 142 : 104));
        }
    }
}
