using System.Linq;
using UnityEditor;
using UnityEngine;
using Kind = MoonShadow.EnemyBase2D.Kind;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly Foe[][] Reinforcements =
        {
            new Foe[] { new(Kind.Runner, 11.9f), new(Kind.Walker, 22.4f), new(Kind.Brute, 27.3f),
                new(Kind.Sentry, 31.6f), new(Kind.Walker, 43.2f), new(Kind.Walker, 61f), new(Kind.Sentry, 76f) },
            new Foe[] { new(Kind.Runner, 8.3f), new(Kind.Walker, 17.7f), new(Kind.Sentry, 37.2f),
                new(Kind.Runner, 50.7f, 3.1f), new(Kind.Brute, 62f, 3.1f), new(Kind.Runner, 74.8f, 0.8f),
                new(Kind.Brute, 91f, 0.8f), new(Kind.Walker, 110.6f, 0.8f) },
            new Foe[] { new(Kind.Walker, 8.9f), new(Kind.Runner, 14.4f), new(Kind.Sentry, 20.3f),
                new(Kind.Walker, 44f, 0.20f), new(Kind.Runner, 57f, 0.20f), new(Kind.Runner, 78f, 0.45f),
                new(Kind.Sentry, 89.1f, 0.45f), new(Kind.Walker, 111f, 0.45f) },
            new Foe[] { new(Kind.Runner, 10.6f), new(Kind.Walker, 20.2f), new(Kind.Brute, 26.2f),
                new(Kind.Sentry, 54f, 1.20f), new(Kind.Walker, 60.1f, 1.20f), new(Kind.Runner, 88.3f, 0.80f),
                new(Kind.Brute, 100.9f, 0.80f), new(Kind.Sentry, 118.6f, 0.80f), new(Kind.Walker, 125.6f, 0.80f) },
            new Foe[] { new(Kind.Walker, 11.9f), new(Kind.Runner, 21.8f), new(Kind.Walker, 32.7f),
                new(Kind.Walker, 53.2f, 2.20f), new(Kind.Sentry, 59.6f, 2.20f), new(Kind.Runner, 67.1f, 2.20f),
                new(Kind.Brute, 91.3f, 0.45f), new(Kind.Runner, 102.8f, 0.45f), new(Kind.Sentry, 116f, 0.45f) }
        };

        private static void ReinforceCampaign(RuinsDemo world)
        {
            const string groupName = "Refuerzos de campaña";
            var anchor = new SerializedObject(world).FindProperty("exit").objectReferenceValue as Transform;
            if (anchor.parent.Find(groupName))
                return;
            var group = new GameObject(groupName).transform;
            group.SetParent(anchor.parent, false);
            int act = world.Stage.Act;
            var added = Reinforcements[act].Select(foe =>
            {
                var actor = DistrictEnemy(group, world, foe.Kind, foe.X, foe.Y, act);
                actor.MarkReinforcement();
                actor.gameObject.SetActive(true);
                return actor;
            });
            world.SetEnemies(world.Enemies.Concat(added).ToArray());
            if (!world.Map)
                return;

            var side = new GameObject("Refuerzos de sectores").transform;
            side.SetParent(world.Map.transform, false);
            float edge = 293f + act * 3f;
            EnemyBase2D Add(Kind kind, float x, float floor, bool elite = false)
            {
                var enemy = DistrictEnemy(side, world, kind, x, floor, act, elite);
                enemy.MarkReinforcement();
                return enemy;
            }
            var waves = new EnemyBase2D[3][];
            for (int wave = 0; wave < waves.Length; wave++)
            {
                var additional = new System.Collections.Generic.List<EnemyBase2D>
                {
                    Add(wave == 0 ? Kind.Walker : Kind.Runner, 256.4f + wave * 3f, 0f),
                    Add(wave == 0 ? Kind.Runner : Kind.Brute, edge - 13.8f - wave * 2.3f, 0f, act >= 3 && wave == 2)
                };
                if (act >= 3)
                    additional.Add(Add(Kind.Sentry, edge - 8.4f + wave * 0.9f, 0f));
                waves[wave] = additional.ToArray();
            }
            world.Map.AddReinforcements(waves,
                new[] { Add(Kind.Walker, 374.4f, 7.85f), Add(Kind.Runner, 390.7f, 7.85f, act >= 2) });
        }
    }
}
