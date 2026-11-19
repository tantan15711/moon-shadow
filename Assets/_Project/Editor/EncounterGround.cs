using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly float[] GroundExtensions = { 34f, 30f, 32f, 34f, 36f };
        private static readonly string[] ExitSigns = { "SALIDA", "ASCENSOR", "ESTACIÓN", "TRANSBORDADOR", "SOFY" };

        private static void ExpandEncounterGround(RuinsDemo world)
        {
            const string ExtensionName = "Ampliación de la zona de combate";
            int act = world.Stage.Act;
            var follow = new SerializedObject(Object.FindAnyObjectByType<RuinsCamera>());
            var director = new SerializedObject(world);
            var exit = (Transform)director.FindProperty("exit").objectReferenceValue;
            var environment = exit.parent;
            var ground = environment.Find(act == 0 ? "Pavimento y plataformas" : "Terreno");
            if (ground.Find(ExtensionName))
                return;

            float right = follow.FindProperty("rightEdge").floatValue;
            float extension = GroundExtensions[act];
            float end = right + extension;
            var lastFloor = act == 0 ? new Platform(57.35f, 74f, 0f, 2.7f) :
                Layouts[act].Where(platform => platform.Depth > 2f).OrderBy(platform => platform.To).Last();
            var largerGround = new GameObject(ExtensionName).transform;
            largerGround.SetParent(ground, false);
            string deck = "PixelDeck" + act;
            Floor(largerGround, "Piso de la arena", right, end, lastFloor.Y, lastFloor.Depth, deck);
            Floor(largerGround, "Pasarela de acceso", right + 1f, right + 7f, lastFloor.Y + 2.14f, 0.42f, deck);
            Floor(largerGround, "Pasarela elevada", right + 4.8f, right + 9f, lastFloor.Y + 3.94f, 0.40f, deck);
            Decorate(largerGround, act, new Platform(right, end, lastFloor.Y));

            var rightBarrier = ground.Cast<Transform>().Where(child => child.name == "Límite")
                .OrderByDescending(child => child.position.x).First();
            rightBarrier.position += Vector3.right * extension;

            var exitLight = (SpriteRenderer)director.FindProperty("exitLight").objectReferenceValue;
            var sign = environment.Find(ExitSigns[act]);
            var plate = environment.Cast<Transform>().First(child => child.name == "Placa" &&
                Vector3.SqrMagnitude(child.position - sign.position) < 0.01f);
            Vector3 exitShift = Vector3.right * extension;
            exit.position += exitShift;
            exitLight.transform.parent.position += exitShift;
            sign.position += exitShift;
            plate.position += exitShift;
            if (world.Stage.Final)
            {
                var civilian = environment.Cast<Transform>().First(child => child.name == "Sofy" && child != exit);
                civilian.position += exitShift;
            }

            var stage = new SerializedObject(world.Stage);
            var boss = world.Stage.Boss.transform;
            var checkpoint = (Transform)stage.FindProperty("bossCheckpoint").objectReferenceValue;
            Vector3 bossShift = Vector3.right * (end - 13f - boss.position.x);
            boss.position += bossShift;
            checkpoint.position += bossShift;

            if (act == 0)
            {
                var crowd = world.Enemies.Where(enemy => !enemy.IsBoss && !enemy.GuardsRelay &&
                        enemy.transform.position.x >= 65f)
                    .OrderBy(enemy => enemy.transform.position.x).ToArray();
                for (int i = 0; i < crowd.Length; i++)
                {
                    Vector3 spawn = crowd[i].transform.position;
                    spawn.x = Mathf.Lerp(69f, right + 9f, i / (float)Mathf.Max(1, crowd.Length - 1));
                    crowd[i].transform.position = spawn;
                }
            }
            else
            {
                var flank = world.Enemies.Where(enemy => !enemy.IsBoss && !enemy.GuardsRelay)
                    .OrderBy(enemy => enemy.transform.position.x).Last();
                flank.transform.position = new Vector3(right + 8.5f, flank.transform.position.y, 0f);
            }

            var mechas = stage.FindProperty("mechs");
            if (mechas.arraySize >= 2)
            {
                var firstMech = (EnemyBase2D)mechas.GetArrayElementAtIndex(0).objectReferenceValue;
                var secondMech = (EnemyBase2D)mechas.GetArrayElementAtIndex(1).objectReferenceValue;
                firstMech.transform.position = new Vector3(right - 7f, firstMech.transform.position.y, 0f);
                secondMech.transform.position = new Vector3(right + 12f, secondMech.transform.position.y, 0f);
            }

            var layers = follow.FindProperty("backdrop");
            var parallax = follow.FindProperty("parallax");
            for (int i = 0; i < layers.arraySize; i++)
            {
                var layer = (Transform)layers.GetArrayElementAtIndex(i).objectReferenceValue;
                var background = layer.GetComponentInChildren<SpriteRenderer>();
                float extra = extension * (1f + parallax.GetArrayElementAtIndex(i).floatValue);
                float width = background.bounds.size.x;
                Vector3 scale = background.transform.localScale;
                scale.x *= (width + extra) / width;
                background.transform.localScale = scale;
                background.transform.position += Vector3.right * (extra * 0.5f);
            }
            follow.FindProperty("rightEdge").floatValue = end;
            follow.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEnemyHealthBar(RuinsDemo world, EnemyBase2D enemy, float height)
        {
            const string BarName = "Vida";
            var existing = enemy.transform.Find(BarName);
            if (existing)
            {
                existing.localPosition = Vector3.up * height;
                return;
            }

            var bar = new GameObject(BarName).AddComponent<EnemyHealthBar>();
            bar.transform.SetParent(enemy.transform, false);
            bar.transform.localPosition = Vector3.up * height;
            float length = enemy.IsBoss ? 1.18f : 0.67f;
            float thickness = enemy.IsBoss ? 0.083f : 0.065f;
            var track = Art("Fondo", "Pixel", bar.transform.position,
                new Vector2(length + 0.055f, thickness + 0.047f), 13, bar.transform);
            var fill = Art("Indicador", "Pixel", bar.transform.position, new Vector2(length, thickness), 14, bar.transform);
            track.enabled = fill.enabled = false;
            bar.Configure(enemy, world, track, fill, length);
        }
    }
}
