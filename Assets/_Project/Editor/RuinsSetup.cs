using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/RuinasUrbanas.unity";
        private const int Terrain = 8;
        private const int Actors = 9;
        private static readonly int Mask = 1 << Terrain;

        [MenuItem("Moon Shadow/Preparar escena inicial")]
        public static void CreateScene()
        {
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("La escena de Ruinas Urbanas ya existe.");

            ImportSprites();
            SetLayers();
            Directory.CreateDirectory(Root + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var env = new GameObject("Ruinas urbanas").transform;
            var backgrounds = new GameObject("Paisaje").transform;
            Transform[] layers = { Layer(backgrounds, "Sky", -32, 39, 3.7f, 100, 17.0f),
                Layer(backgrounds, "FarCity", -31, 39, 3.43f, 102, 11.2f),
                Layer(backgrounds, "NearCity", -30, 40, 2.75f, 112, 9.5f) };
            var floors = new GameObject("Pavimento y plataformas").transform;
            floors.SetParent(env, false);

            Floor(floors, "Sector A / avenida", -2f, 22f, 0f, 2.7f);
            Floor(floors, "Sector B / control militar", 22f, 49f, 0f, 2.7f);
            Floor(floors, "Sector C / extracción", 57.35f, 74f, 0f, 2.7f);
            Floor(floors, "Techo del refugio", 7.5f, 12.3f, 2.26f, 0.48f);
            Floor(floors, "Pasarela del sector B", 28.2f, 34.5f, 2.35f, 0.48f);
            Floor(floors, "Plataforma de acceso", 61f, 65f, 2.04f, 0.42f);
            Barrier(floors, -2.35f);
            Barrier(floors, 74.35f);

            for (int i = 0; i < 24; i++)
            {
                float x = i * 3.09f + 0.4f;
                if (x > 48.2f && x < 58.1f)
                    continue;
                Art("Ruinas", "Debris" + i % 4, new Vector2(x, 0.37f),
                    new Vector2(1.3f + (i % 3) * 0.39f, 0.83f + i % 2 * 0.13f), -9, env);
            }
            foreach (float x in new[] { 3f, 14.3f, 23.5f, 38f, 46.5f, 58.6f, 70.5f })
                Art("Farola", "Lamp", new Vector2(x, 1.87f), new Vector2(1.35f, 3.75f), -8, env);
            foreach (float x in new[] { 5.0f, 17.7f, 31.5f, 44.5f, 66.9f })
                Art("Viga de acero", "Pylon", new Vector2(x, 2.15f), new Vector2(1.85f, 4.3f), -12, env);
            Sign(env, new Vector2(4.9f, 2.65f), "SECTOR A", new Color(0.54f, 0.74f, 0.82f));
            Sign(env, new Vector2(37.9f, 2.65f), "RELÉ", new Color(0.56f, 0.88f, 0.95f));
            Sign(env, new Vector2(47.15f, 1.2f), "BRECHA  >", new Color(1f, 0.67f, 0.40f));
            Sign(env, new Vector2(69.0f, 3.3f), "SALIDA", new Color(0.53f, 0.91f, 0.84f));

            var relay = Art("Relé de la milicia", "Relay", new Vector2(40f, 0.86f), new Vector2(1.43f, 1.72f), -2, env);
            var relayLight = Art("Indicador", "Pixel", new Vector2(40.10f, 0.94f), new Vector2(0.52f, 0.13f), -1, relay.transform);
            var door = Art("Compuerta de salida", "Door", new Vector2(69f, 1.42f), new Vector2(2.3f, 2.85f), -2, env);
            var doorLight = Art("Indicador", "Pixel", new Vector2(69f, 2.47f), new Vector2(0.56f, 0.08f), -1, door.transform);
            var exit = new GameObject("Punto de extracción").transform;
            exit.position = new Vector2(69f, 0.84f);
            exit.SetParent(env, true);

            var controller = CreatePlayer(new Vector2(1.65f, 0.87f));
            var enemies = new[]
            {
                CreateEnemy("Caminante", EnemyBase2D.Kind.Walker, new Vector2(18f, 0.90f), true),
                CreateEnemy("Corredor", EnemyBase2D.Kind.Runner, new Vector2(35.5f, 0.90f), true),
                CreateEnemy("Caminante · salida", EnemyBase2D.Kind.Walker, new Vector2(66f, 0.90f), false)
            };

            var effects = new GameObject("Audio y efectos").AddComponent<RuinsEffects>();
            string[] soundNames = { "Sword", "Impact", "Dash", "Parry", "Hurt", "Relay" };
            AudioClip[] clips = soundNames.Select(id =>
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/{id}.wav")).ToArray();
            effects.Configure(Sprite("Pixel"), Sprite("Slash"), Sprite("Guard"), Sprite("Medkit"), clips, Mask);
            var demo = new GameObject("Progresión del nivel").AddComponent<RuinsDemo>();
            demo.Configure(controller, enemies, effects, relay.transform, exit, relayLight, doorLight);
            PopulateEncounter(demo);

            var camera = new GameObject("Cámara", typeof(Camera), typeof(AudioListener),
                typeof(UniversalAdditionalCameraData), typeof(RuinsCamera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5.35f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(17, 24, 42, 255);
            camera.transform.position = new Vector3(7.5f, 2.45f, -10f);
            camera.GetComponent<RuinsCamera>().Configure(demo, layers, new[] { 0.05f, 0.15f, 0.26f });

            new GameObject("Interfaz").AddComponent<RuinsHud>().Configure(demo);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("No se pudo guardar la escena.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("MOON_SCENE_CREATED " + ScenePath);
        }

        [MenuItem("Moon Shadow/Actualizar encuentros de Ruinas Urbanas")]
        public static void UpgradeCombat()
        {
            if (!File.Exists(ScenePath))
            {
                CreateScene();
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ImportSprites();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var demo = Object.FindAnyObjectByType<RuinsDemo>();
            if (!demo)
                throw new InvalidOperationException("Falta la progresión de Ruinas Urbanas.");
            PopulateEncounter(demo);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("No se pudieron guardar los encuentros.");
            Debug.Log($"MOON_ENCOUNTERS_READY {demo.Enemies.Length}");
        }

        private static void PopulateEncounter(RuinsDemo demo)
        {
            var enemies = demo.Enemies.ToList();
            AddEnemy(enemies, "Caminante · avenida", EnemyBase2D.Kind.Walker, 10.2f, false);
            AddEnemy(enemies, "Corredor · callejón", EnemyBase2D.Kind.Runner, 24.1f, false);
            AddEnemy(enemies, "Bruto · control militar", EnemyBase2D.Kind.Brute, 29.7f, true);
            AddEnemy(enemies, "Centinela · relé", EnemyBase2D.Kind.Sentry, 38.5f, true);
            AddEnemy(enemies, "Corredor · terraplén", EnemyBase2D.Kind.Runner, 59.8f, false);
            AddEnemy(enemies, "Centinela · extracción", EnemyBase2D.Kind.Sentry, 67.7f, false);
            AddEnemy(enemies, "Bruto · extracción", EnemyBase2D.Kind.Brute, 72.0f, false);
            demo.SetEnemies(enemies.ToArray());
            foreach (var enemy in enemies)
                EnsureWarning(enemy);

            var player = demo.Player;
            var visuals = player.GetComponentInChildren<SpriteRenderer>();
            player.Configure(Mask, visuals);
        }

        private static void AddEnemy(List<EnemyBase2D> enemies, string label, EnemyBase2D.Kind kind, float x, bool guard)
        {
            if (!enemies.Any(enemy => enemy.name == label))
                enemies.Add(CreateEnemy(label, kind, new Vector2(x, 0.91f), guard));
        }

        private static void EnsureWarning(EnemyBase2D enemy)
        {
            if (enemy.transform.Find("Aviso de ataque"))
                return;

            Vector2 position = (Vector2)enemy.transform.position + new Vector2(0f,
                enemy.Archetype == EnemyBase2D.Kind.Brute ? 1.30f : 1.06f);
            var cue = Art("Aviso de ataque", "Pixel", position, new Vector2(0.4f, 0.08f), 13, enemy.transform);
            cue.enabled = false;
            enemy.SetWarning(cue);
        }

        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
                CreateScene();

            string dir = Path.GetFullPath("Builds/Windows");
            Directory.CreateDirectory(dir);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(dir, "Moon Shadow.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Compilación fallida ({report.summary.totalErrors} errores).");
            Debug.Log($"MOON_BUILD_READY {report.summary.outputPath} ({report.summary.totalSize} bytes)");
        }

        private static void ImportSprites()
        {
            foreach (string file in Directory.GetFiles(Root + "/Art", "*.png"))
            {
                string path = file.Replace('\\', '/');
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    throw new InvalidOperationException("No se importó " + path);

                if (Path.GetFileName(path).StartsWith("Cinema", StringComparison.Ordinal) &&
                    !path.EndsWith("CinemaStreet.png", StringComparison.Ordinal))
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                    continue;
                }

                string filename = Path.GetFileName(path);
                bool cast = filename.StartsWith("Cast", StringComparison.Ordinal);
                bool originalKyle = filename == "PixelKyleRestored.png";
                bool highRes = cast || filename == "CinemaStreet.png" || filename.StartsWith("KyleWeest", StringComparison.Ordinal);
                FilterMode mode = highRes && !cast ? FilterMode.Bilinear : FilterMode.Point;
                int pixelsPerUnit = originalKyle || filename.StartsWith("PixelKyleDuster", StringComparison.Ordinal) ? 160 :
                    filename.StartsWith("PixelKyleHi", StringComparison.Ordinal) ? 80 :
                    highRes ? 60 : filename.StartsWith("Pixel", StringComparison.Ordinal) &&
                    filename != "Pixel.png" ? 40 : 32;
                if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == mode &&
                    Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = mode;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                if (cast)
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                if (originalKyle)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(0.64f, 0.022f);
                }
                importer.SetTextureSettings(settings);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void SetLayers()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = settings.FindProperty("layers");
            layers.GetArrayElementAtIndex(Terrain).stringValue = "Terreno";
            layers.GetArrayElementAtIndex(Actors).stringValue = "Actores";
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite Sprite(string key) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/{key}.png") ??
            throw new InvalidOperationException("Sprite ausente: " + key);

        private static SpriteRenderer Art(string name, string key, Vector2 center, Vector2 size,
            int order, Transform parent)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.position = (Vector3)center;
            renderer.sprite = Sprite(key);
            Vector3 native = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
            renderer.sortingOrder = order;
            if (parent)
                renderer.transform.SetParent(parent, true);
            return renderer;
        }

        private static Transform Layer(Transform parent, string texture, int order, float x, float y, float w, float h)
        {
            var carrier = new GameObject(texture).transform;
            carrier.SetParent(parent, false);
            Art(texture, texture, new Vector2(x, y), new Vector2(w, h), order, carrier);
            return carrier;
        }

        private static void Floor(Transform parent, string label, float from, float to, float surface, float depth,
            string art = null)
        {
            var result = new GameObject(label);
            result.transform.SetParent(parent, false);
            result.transform.position = new Vector2((from + to) * 0.5f, surface - depth * 0.5f);
            result.layer = Terrain;
            result.AddComponent<BoxCollider2D>().size = new Vector2(to - from, depth);
            int pieces = Mathf.CeilToInt((to - from) / 2.15f);
            float width = (to - from) / pieces;
            for (int i = 0; i < pieces; i++)
                Art("Losa", art ?? "Floor" + i % 5, new Vector2(from + width * (i + 0.5f), surface - depth * 0.5f),
                    new Vector2(width + 0.012f, depth), 0, result.transform);
        }

        private static void Barrier(Transform parent, float x, float height = 12f)
        {
            var wall = new GameObject("Límite");
            wall.layer = Terrain;
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector2(x, height * 0.5f - 2f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, height);
        }

        private static void Sign(Transform parent, Vector2 position, string caption, Color tint)
        {
            Art("Placa", "Pixel", position, new Vector2(caption.Length * 0.18f + 0.39f, 0.43f), -5, parent)
                .color = new Color(0.08f, 0.12f, 0.19f);
            var text = new GameObject(caption).AddComponent<TextMesh>();
            text.transform.SetParent(parent, false);
            text.transform.position = position;
            text.text = caption;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 42;
            text.characterSize = 0.053f;
            text.fontStyle = FontStyle.Bold;
            text.color = tint;
            text.GetComponent<MeshRenderer>().sortingOrder = -4;
        }

        private static GameObject Actor(string label, Vector2 spawn, Vector2 hitbox, float gravity)
        {
            var obj = new GameObject(label);
            obj.layer = Actors;
            obj.transform.position = spawn;
            var body = obj.AddComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = gravity;
            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = hitbox;
            collider.excludeLayers = 1 << Actors;
            return obj;
        }

        private static KyleController2D CreatePlayer(Vector2 spawn)
        {
            var obj = Actor("Kyle Weest", spawn, new Vector2(0.61f, 1.57f), 3f);
            var view = Art("Kyle · sprite", "Kyle0", spawn + Vector2.up * 0.08f,
                new Vector2(1.85f, 1.86f), 10, obj.transform);
            var player = obj.AddComponent<KyleController2D>();
            player.Configure(Mask, view);
            return player;
        }

        private static EnemyBase2D CreateEnemy(string label, EnemyBase2D.Kind kind, Vector2 spawn, bool guards)
        {
            bool brute = kind == EnemyBase2D.Kind.Brute;
            var obj = Actor(label, spawn, brute ? new Vector2(0.85f, 1.77f) : new Vector2(0.68f, 1.57f), 2.8f);
            string type = kind.ToString();
            var view = Art("Mutante · sprite", type + "0", spawn + Vector2.up * 0.08f,
                brute ? new Vector2(2.46f, 2.07f) : new Vector2(1.94f, 1.88f), 11, obj.transform);
            var controller = obj.AddComponent<EnemyBase2D>();
            var frames = Enumerable.Range(0, 4).Select(i => Sprite(type + i)).ToArray();
            controller.Configure(kind, Mask, view, frames, guards);
            return controller;
        }
    }
}

