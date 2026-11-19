using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly string[][] Districts =
        {
            new[] { "AVENIDA PRINCIPAL", "ESTACIÓN DE METRO", "AZOTEAS DE LA CIUDAD", "BÚNKER MILITAR" },
            new[] { "CATACUMBAS SUPERIORES", "CÁMARAS DE CRIOGENIA", "LABORATORIOS BIOMÉDICOS", "ASCENSOR GEOTÉRMICO" },
            new[] { "LADERAS DE OBSIDIANA", "CAÑONES Y NIDOS", "ESTACIÓN DE PRUEBAS", "PLATAFORMA DE LANZAMIENTO" },
            new[] { "MUELLES DE CARGA GRAV-ZERO", "HANGARES DE SEGURIDAD", "PASARELAS DEL HANGAR", "SALA DE MANDO" },
            new[] { "PASILLOS ESTÉRILES", "EXTERIOR DE GRAVEDAD REDUCIDA", "OBSERVATORIO EXTERIOR", "NÚCLEO Y CONTENCIÓN" }
        };

        private static readonly Color[] DistrictColors =
        {
            new(0.40f, 0.76f, 0.88f), new(0.76f, 0.61f, 0.90f), new(0.95f, 0.49f, 0.25f),
            new(0.60f, 0.88f, 1f), new(0.61f, 0.66f, 0.97f)
        };

        private static void InstallDistricts(RuinsDemo world)
        {
            int act = world.Stage.Act;
            var names = Districts[act];
            if (world.Map)
            {
                foreach (var enemy in world.Map.CollectActors())
                    ApplyEnemyArt(world, enemy);
                DecorateInteriors(world.Map.transform, names, act);
                return;
            }

            var tint = DistrictColors[act];
            var map = new GameObject("Mapa de sectores").AddComponent<CampaignMap>();
            var root = map.transform;
            var camera = Object.FindAnyObjectByType<RuinsCamera>();
            float roof = act == 0 ? 2.26f : Layouts[act][act == 1 ? 4 : 3].Y;
            float arenaLeft = 238f;
            float refugeLeft = 338f;
            float arenaWidth = 55f + act * 3f;
            var arena = MakeDistrict(root, names[1], arenaLeft, arenaWidth, 8.8f, tint, act);
            var refuge = MakeDistrict(root, names[2], refugeLeft, 60f, 14f, tint, act);

            arena.MainDoor = DistrictDoor(root, names[1], new Vector2(5.9f, 0.82f), tint, false);
            arena.MainArrival = Point(root, "Regreso desde la arena", new Vector2(7.4f, 0.83f));
            refuge.MainDoor = DistrictDoor(root, names[2], new Vector2(11.6f, roof + 0.82f), tint, false);
            refuge.MainArrival = Point(root, "Regreso desde el refugio", new Vector2(12.5f, roof + 0.83f));
            arena.Exit = DistrictDoor(root, "REGRESO", new Vector2(arena.Left + 4f, 0.82f), tint);
            arena.Shortcut = DistrictDoor(root, "ATAJO A LA TERMINAL", new Vector2(arena.Right - 4f, 0.82f), tint);
            refuge.Exit = DistrictDoor(root, "REGRESO", new Vector2(refuge.Left + 4f, 0.82f), tint);
            refuge.Shortcut = DistrictDoor(root, "ATAJO DE EVACUACIÓN", new Vector2(refuge.Right - 6.5f, 8.65f), tint);
            var relay = (Transform)new SerializedObject(world).FindProperty("relay").objectReferenceValue;
            var returnPoint = Point(root, "Atajo a la terminal", relay.position + Vector3.right * 1.65f);

            var shutters = new[] { ArenaShutter(root, arena.Left + 2f), ArenaShutter(root, arena.Right - 2f) };
            arena.Waves = MakeWaves(world, root, arena, act);

            string deck = "PixelDeck" + act;
            Floor(root, "Tramo intermedio", refuge.Left + 7f, refuge.Left + 13.5f, 2.05f, 0.38f, deck);
            Floor(root, "Descanso", refuge.Left + 14f, refuge.Left + 21.3f, 4.05f, 0.38f, deck);
            Floor(root, "Pasarela", refuge.Left + 22.1f, refuge.Left + 27.9f, 5.90f, 0.38f, deck);
            Floor(root, "Refugio elevado", refuge.Left + 31.5f, refuge.Right - 3.7f, 7.85f, 0.95f, deck);
            var lift = Lift(root, "Elevador de servicio", refuge.Left + 0.15f, refuge.Left + 3.3f, 1.3f, 4.4f, deck);
            Sign(lift, new Vector2(refuge.Left + 1.725f, 2.7f), "ELEVADOR", tint);
            var grab = Point(root, "Consola del cable", new Vector2(refuge.Left + 23.8f, 0.82f));
            var landing = Point(root, "Anclaje superior", new Vector2(refuge.Left + 32.3f, 8.69f));
            Sign(root, (Vector2)grab.position + Vector2.up * 0.89f, "CABLE DE SERVICIO", tint);
            Art("Anclaje", "Relay", (Vector2)landing.position + Vector2.up * 0.95f, new Vector2(0.62f, 0.72f), 0, root);
            var cable = Art("Cable", "Pixel", Vector2.zero, new Vector2(1f, 0.045f), -1, root);
            cable.color = tint;
            cable.enabled = false;

            world.Stage.Rescue.position = new Vector2(refuge.Right - 10.2f, 8.65f);
            refuge.Waves = new[]
            {
                new CampaignMap.Wave
                {
                    Enemies = new[]
                    {
                        DistrictEnemy(root, world, EnemyBase2D.Kind.Brute, refuge.Left + 42.8f, 7.85f, act, true),
                        DistrictEnemy(root, world, EnemyBase2D.Kind.Sentry, refuge.Left + 47.6f, 7.85f, act),
                        DistrictEnemy(root, world, EnemyBase2D.Kind.Runner, refuge.Left + 38.8f, 7.85f, act)
                    }
                }
            };

            Transform cache = null;
            if (act == 0)
            {
                var weapon = Art("Lanzallamas recuperable", "PixelIncinerator", new Vector2(arena.Left + 5.75f, 0.74f),
                    new Vector2(1.26f, 0.52f), 5, root);
                Sign(weapon.transform, new Vector2(arena.Left + 5.75f, 1.47f), "LANZALLAMAS", tint);
                cache = weapon.transform;
            }

            float finale = world.Stage.Boss.transform.position.x - 9f;
            Sign(root, new Vector2(finale - 1f, world.Stage.Boss.transform.position.y + 2.1f), names[3], tint);
            map.Configure(world, camera, names[0], names[3], finale, arena, refuge, shutters, cache,
                grab, landing, cable, returnPoint);
            world.SetMap(map);
            DecorateInteriors(root, names, act);
        }

        private static void DecorateInteriors(Transform root, string[] names, int act)
        {
            if (!root.Find("Pasarela del elevador"))
                Floor(root, "Pasarela del elevador", 341.38f, 349.74f, 5.67f, 0.40f, "PixelDeck" + act);
            var lift = root.Find("Elevador de servicio");
            if (lift.position.x > 340.8f)
            {
                lift.position += Vector3.left * 1.175f;
                lift.localScale = new Vector3(3.15f / 4f, 1f, 1f);
            }
            if (act != 4)
                PlaceDistrictWall(root.Find(names[1]), 238f, 55f + act * 3f, act);
            if (act != 0)
                PlaceDistrictWall(root.Find(names[2]), 338f, 60f, act);
        }

        private static void PlaceDistrictWall(Transform room, float left, float width, int act)
        {
            if (!room || room.Find("Fondo del sector"))
                return;
            Art("Fondo del sector", "PixelDistrictWall" + act, new Vector2(left + width * 0.5f, 6.80f),
                new Vector2(width + 4f, 23f), -22, room);
        }

        private static CampaignMap.Room MakeDistrict(Transform parent, string title, float left, float width,
            float ceiling, Color tint, int act)
        {
            var district = new GameObject(title).transform;
            district.SetParent(parent, false);
            float right = left + width;
            string deck = "PixelDeck" + act;
            Floor(district, "Suelo", left, right, 0f, 3.0f, deck);
            Barrier(district, left - 0.35f, ceiling + 5f);
            Barrier(district, right + 0.35f, ceiling + 5f);
            Decorate(district, act, new Platform(left + 4f, right, 0f, 3f));
            for (float x = left + 8f; x < right; x += 11.5f)
            {
                var support = Art("Columna estructural", "Pylon", new Vector2(x, 5.05f),
                    new Vector2(2.10f, 10.4f), -16, district);
                support.color = Color.Lerp(tint, Color.black, 0.30f);
                Art("Lámpara de mantenimiento", "Lamp", new Vector2(x + 3.6f, 3.45f),
                    new Vector2(1.42f, 6.9f), -10, district).color = tint;
            }

            if (ceiling < 10f)
            {
                Floor(district, "Pasarela de la arena", left + 15f, left + 24f, 2.35f, 0.39f, deck);
                Floor(district, "Pasarela de la arena", right - 17f, right - 8.5f, 2.45f, 0.39f, deck);
            }
            Sign(district, new Vector2(left + 9.5f, 3.05f), title, tint);
            return new CampaignMap.Room
            {
                Title = title, Left = left, Right = right, Ceiling = ceiling,
                Entrance = Point(district, "Llegada", new Vector2(left + 4.25f, 0.83f))
            };
        }

        private static CampaignMap.Wave[] MakeWaves(RuinsDemo world, Transform parent, CampaignMap.Room room, int act)
        {
            var waves = new CampaignMap.Wave[3];
            for (int wave = 0; wave < waves.Length; wave++)
            {
                int count = 6 + wave * 2 + act / 2;
                var foes = new EnemyBase2D[count];
                for (int i = 0; i < count; i++)
                {
                    int index = (i + wave + act) % 6;
                    var kind = index < 2 ? EnemyBase2D.Kind.Walker : index < 4 ? EnemyBase2D.Kind.Runner :
                        index == 4 ? EnemyBase2D.Kind.Sentry : EnemyBase2D.Kind.Brute;
                    float x = wave == 0 ? room.Left + 8f + i * 3.5f :
                        i % 2 == 0 ? room.Left + 5f + i * 1.9f : room.Right - 5f - i * 1.9f;
                    foes[i] = DistrictEnemy(parent, world, kind, x, 0f, act);
                }
                waves[wave] = new CampaignMap.Wave { Enemies = foes };
            }
            return waves;
        }

        private static EnemyBase2D DistrictEnemy(Transform parent, RuinsDemo world, EnemyBase2D.Kind kind,
            float x, float floor, int act, bool elite = false)
        {
            string[] names = { "Caminante de enjambre", "Acechador del sector", "Blindado del sector", "Vigía del sector" };
            var foe = CreateEnemy(elite ? "Custodio de élite" : names[(int)kind], kind, new Vector2(x, floor + 0.91f), false);
            foe.transform.SetParent(parent, true);
            foe.SetActTier(act);
            foe.SetVeteran(elite);
            ApplyEnemyArt(world, foe);
            foe.gameObject.SetActive(false);
            return foe;
        }

        private static Transform DistrictDoor(Transform parent, string title, Vector2 position, Color tint,
            bool drawSign = true)
        {
            var model = Art("Paso: " + title, "Door", position + Vector2.up * 0.40f, new Vector2(1.90f, 2.35f), -2, parent);
            model.color = Color.Lerp(tint, Color.white, 0.52f);
            if (drawSign)
                Sign(parent, position + Vector2.up * 1.74f, title, tint);
            return Point(parent, title, position);
        }

        private static GameObject ArenaShutter(Transform parent, float x)
        {
            var shutter = new GameObject("Compuerta de combate");
            shutter.layer = Terrain;
            shutter.transform.SetParent(parent, false);
            shutter.transform.position = new Vector2(x, 4.25f);
            shutter.AddComponent<BoxCollider2D>().size = new Vector2(0.48f, 8.5f);
            for (int i = 0; i < 3; i++)
            {
                var beam = Art("Haz", "Pixel", new Vector2(x + (i - 1) * 0.16f, 4.2f),
                    new Vector2(i == 1 ? 0.16f : 0.085f, 8.4f), 3 + i, shutter.transform);
                beam.color = i == 1 ? new Color(1f, 0.71f, 0.39f, 0.90f) : new Color(0.95f, 0.23f, 0.17f, 0.90f);
            }
            shutter.SetActive(false);
            return shutter;
        }

        private static Transform Point(Transform parent, string label, Vector2 position)
        {
            var point = new GameObject(label).transform;
            point.SetParent(parent, false);
            point.position = position;
            return point;
        }

        private static Transform Lift(Transform parent, string name, float from, float to, float y, float travel, string deck)
        {
            Floor(parent, name, from, to, y, 0.42f, deck);
            var ledge = parent.GetChild(parent.childCount - 1);
            var body = ledge.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            ledge.gameObject.AddComponent<MovingLedge>().Configure(body, travel);
            return ledge;
        }
    }
}
