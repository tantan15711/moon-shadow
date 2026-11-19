using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MoonShadow.Editor
{
    public static partial class RuinsSetup
    {
        private static readonly string[] Themes = { "", "Gothic", "Volcanic", "Orbital", "Lunar" };
        private const string TitlePath = Root + "/Scenes/Inicio.unity";
        private const float StartX = -3f;
        private const string SkinPath = Root + "/Art/CastSkin.mat";

        private readonly struct Platform
        {
            public readonly float From, To, Y, Depth;
            public Platform(float from, float to, float y, float depth = 2.65f)
            {
                From = from;
                To = to;
                Y = y;
                Depth = depth;
            }
        }

        private readonly struct Foe
        {
            public readonly EnemyBase2D.Kind Kind;
            public readonly float X, Y;
            public readonly bool Guard;
            public Foe(EnemyBase2D.Kind kind, float x, float y = 0f, bool guard = false)
            {
                Kind = kind;
                X = x;
                Y = y;
                Guard = guard;
            }
        }

        private static readonly Platform[][] Layouts =
        {
            Array.Empty<Platform>(),
            new Platform[]
            {
                new(-3f, 43f, 0f), new(46.5f, 55.0f, 3.1f), new(55f, 64f, 3.1f),
                new(64f, 106f, 0.8f), new(8f, 15f, 2.08f, 0.38f), new(22f, 28f, 2.1f, 0.38f),
                new(69f, 77f, 2.85f, 0.36f)
            },
            new Platform[]
            {
                new(-3f, 29f, 0f), new(35.8f, 61f, 0.20f), new(69f, 107f, 0.45f),
                new(7.4f, 14.5f, 2.02f, 0.38f), new(42f, 47.5f, 2.3f, 0.37f),
                new(76f, 81f, 2.50f, 0.38f)
            },
            new Platform[]
            {
                new(-3f, 38f, 0f), new(46.8f, 72f, 1.20f), new(80.5f, 114f, 0.8f),
                new(8.3f, 15.8f, 2.10f, 0.41f), new(20f, 26f, 2.1f, 0.39f),
                new(51f, 56.2f, 3.25f, 0.42f), new(87f, 92.8f, 3.05f, 0.45f)
            },
            new Platform[]
            {
                new(-3f, 40f, 0f), new(49.4f, 72f, 2.20f), new(83.5f, 120f, 0.45f),
                new(9f, 16f, 2.42f, 0.50f), new(22f, 28f, 2.60f, 0.45f),
                new(56f, 60.9f, 4.58f, 0.48f), new(90f, 96f, 2.85f, 0.44f)
            }
        };

        private static readonly Foe[][] Encounters =
        {
            Array.Empty<Foe>(),
            new Foe[]
            {
                new(EnemyBase2D.Kind.Walker, 15f), new(EnemyBase2D.Kind.Runner, 20f),
                new(EnemyBase2D.Kind.Sentry, 26f, 0, true), new(EnemyBase2D.Kind.Brute, 31f, 0, true),
                new(EnemyBase2D.Kind.Runner, 34.5f, 0, true), new(EnemyBase2D.Kind.Sentry, 58f, 3.1f),
                new(EnemyBase2D.Kind.Runner, 70f, 0.8f), new(EnemyBase2D.Kind.Brute, 79f, 0.8f)
            },
            new Foe[]
            {
                new(EnemyBase2D.Kind.Runner, 16f), new(EnemyBase2D.Kind.Walker, 23f),
                new(EnemyBase2D.Kind.Brute, 40f, 0.20f, true), new(EnemyBase2D.Kind.Sentry, 48f, 0.20f, true),
                new(EnemyBase2D.Kind.Runner, 53f, 0.20f, true), new(EnemyBase2D.Kind.Sentry, 73f, 0.45f),
                new(EnemyBase2D.Kind.Brute, 83f, 0.45f), new(EnemyBase2D.Kind.Runner, 98f, 0.45f)
            },
            new Foe[]
            {
                new(EnemyBase2D.Kind.Sentry, 16f), new(EnemyBase2D.Kind.Runner, 23f),
                new(EnemyBase2D.Kind.Brute, 30f), new(EnemyBase2D.Kind.Sentry, 50f, 1.20f, true),
                new(EnemyBase2D.Kind.Runner, 57f, 1.20f, true), new(EnemyBase2D.Kind.Brute, 63f, 1.20f, true),
                new(EnemyBase2D.Kind.Sentry, 85f, 0.8f), new(EnemyBase2D.Kind.Brute, 93f, 0.8f)
            },
            new Foe[]
            {
                new(EnemyBase2D.Kind.Sentry, 18f), new(EnemyBase2D.Kind.Runner, 25f),
                new(EnemyBase2D.Kind.Brute, 31f), new(EnemyBase2D.Kind.Sentry, 55f, 2.20f, true),
                new(EnemyBase2D.Kind.Brute, 61.6f, 2.20f, true), new(EnemyBase2D.Kind.Runner, 65f, 2.20f, true),
                new(EnemyBase2D.Kind.Sentry, 88f, 0.45f), new(EnemyBase2D.Kind.Brute, 96.5f, 0.45f),
                new(EnemyBase2D.Kind.Sentry, 110f, 0.45f)
            }
        };

        [MenuItem("Moon Shadow/Preparar campaña")]
        public static void InstallCampaign()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            bool refreshLunar = Array.IndexOf(Environment.GetCommandLineArgs(), "-moonRefreshLunar") >= 0;
            if (!File.Exists(ScenePath))
                CreateScene();
            CreateArsenalSprites();
            CreateCombatSprites();
            CreateLampGlow();
            ImportSprites();
            var first = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var firstStage = Object.FindAnyObjectByType<RuinsDemo>();
            if (!firstStage.Stage)
                AttachChapter(firstStage, 0, new Vector2(65.8f, 0f), new Vector2(9.9f, 2.26f));
            else
            {
                ApplyKyle(firstStage.Player);
                ConfigureCinematics(firstStage.Stage);
                ApplyCast(firstStage);
            }
            ExpandEncounterGround(firstStage);
            InstallDistricts(firstStage);
            ReinforceCampaign(firstStage);
            BindScenery(firstStage);
            DressEquipment(firstStage);
            DressFoundry(firstStage);
            Save(first, ScenePath);

            for (int index = 1; index < CampaignProgress.Chapters.Length; index++)
            {
                string path = ChapterPath(index);
                if (!File.Exists(path) || (index == 4 && refreshLunar))
                    CreateChapter(index);
                else
                {
                    var existing = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var world = Object.FindAnyObjectByType<RuinsDemo>();
                    foreach (var foe in world.Enemies)
                        foe.SetActTier(index);
                    if (!GameObject.Find("<  REGRESAR"))
                        Sign(null, new Vector2(-1.25f, 1.8f), "<  REGRESAR", new Color(0.62f, 0.86f, 0.94f));
                    ApplyKyle(world.Player);
                    ConfigureCinematics(world.Stage);
                    ApplyCast(world);
                    ExpandEncounterGround(world);
                    InstallDistricts(world);
                    ReinforceCampaign(world);
                    BindScenery(world);
                    DressEquipment(world);
                    DressFoundry(world);
                    Save(existing, path);
                }
            }
            if (!File.Exists(TitlePath) || refreshLunar)
                CreateTitle();
            else
            {
                var titleScene = EditorSceneManager.OpenScene(TitlePath, OpenSceneMode.Single);
                UpdateTitleArt();
                Save(titleScene, TitlePath);
            }

            var paths = new[] { TitlePath }.Concat(Enumerable.Range(0, CampaignProgress.Chapters.Length)
                .Select(ChapterPath));
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"MOON_CAMPAIGN_READY {EditorBuildSettings.scenes.Length} scenes");
        }

        private static string ChapterPath(int index) => Root + "/Scenes/" + CampaignProgress.Chapters[index].Scene + ".unity";

        private static void CreateTitle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Art("Tierra eclipsada", "LunarSky", new Vector2(4f, 1f), new Vector2(41f, 14f), -32, null);
            Art("Ruinas", "LunarDepth", new Vector2(1f, -2.8f), new Vector2(34f, 10f), -31, null);
            Art("Kyle Weest", "KyleWeestPortrait", new Vector2(5.79f, -0.20f), new Vector2(5.55f, 8.10f), 10, null);
            CreateCampaignCamera(new Vector3(0f, 0f, -10f), 5.6f);
            new GameObject("Menú principal").AddComponent<CampaignMenu>();
            UpdateTitleArt();
            Save(scene, TitlePath);
        }

        private static void UpdateTitleArt()
        {
            var hero = GameObject.Find("Kyle Weest").GetComponent<SpriteRenderer>();
            hero.sprite = Sprite("PixelKyleRestored");
            hero.transform.localScale = Vector3.one * (5.15f / hero.sprite.bounds.size.y);
            hero.transform.position = new Vector3(5.75f, -3.96f, 0f);
            if (!GameObject.Find("Suelo de la azotea"))
            {
                var roof = new GameObject("Suelo de la azotea").transform;
                for (int i = 0; i < 13; i++)
                    Art("Revestimiento", "Floor0", new Vector2(-13.8f + i * 2.31f, -4.76f),
                        new Vector2(2.32f, 1.6f), 8, roof);
            }

            var background = GameObject.Find("Tierra eclipsada").GetComponent<SpriteRenderer>();
            BindTitleVista(background);
            GameObject.Find("Ruinas")?.SetActive(false);
        }

        private static void CreateChapter(int act)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var env = new GameObject(CampaignProgress.Chapters[act].Name).transform;
            string theme = Themes[act];
            var geometry = Layouts[act];
            float right = geometry.Max(platform => platform.To);
            var backgrounds = new GameObject("Paisaje").transform;
            Transform[] layers =
            {
                Layer(backgrounds, theme + "Sky", -32, right * 0.51f, 4.7f, right + 37f, 19f),
                Layer(backgrounds, theme + "Depth", -30, right * 0.49f, 3.3f, right + 58f, 13.1f)
            };
            var ground = new GameObject("Terreno").transform;
            ground.SetParent(env, false);
            foreach (var block in geometry)
            {
                Floor(ground, "Plataforma", block.From, block.To, block.Y, block.Depth, theme + "Tile");
                if (block.Depth > 2f)
                    Decorate(ground, act, block);
            }
            Barrier(ground, StartX - 0.35f);
            Barrier(ground, right + 0.35f);
            if (act == 2)
            {
                Lava(ground, 29f, 35.8f);
                Lava(ground, 61f, 69f);
            }

            Vector2 relay = act switch
            {
                1 => new Vector2(38.7f, 0.85f), 2 => new Vector2(55.7f, 1.05f),
                3 => new Vector2(67.0f, 2.05f), _ => new Vector2(68.2f, 3.05f)
            };
            var last = geometry.Where(block => block.Depth > 2f).OrderBy(block => block.To).Last();
            Vector2 gate = new(right - 3.7f, last.Y + 0.84f);
            var console = Art("Terminal", "Relay", relay, new Vector2(1.43f, 1.72f), -2, env);
            var consoleLight = Art("Indicador", "Pixel", relay + new Vector2(0.12f, 0.08f),
                new Vector2(0.47f, 0.13f), -1, console.transform);
            var door = Art(act == 4 ? "Cámara de Sofy" : "Salida", "Door", gate + Vector2.up * 0.59f,
                new Vector2(2.45f, 2.86f), -2, env);
            var doorLight = Art("Indicador", "Pixel", gate + Vector2.up * 1.65f, new Vector2(0.58f, 0.09f),
                -1, door.transform);
            var exit = new GameObject(act == 4 ? "Sofy" : "Salida").transform;
            exit.position = gate;
            exit.SetParent(env, true);
            if (act == 4)
                Art("Sofy", "Civilian", gate + new Vector2(0.13f, -0.04f), new Vector2(1.20f, 1.72f), -1, env)
                    .color = new Color(0.89f, 0.88f, 0.85f);
            Sign(env, relay + new Vector2(0f, 1.13f), "TERMINAL", new Color(0.52f, 0.91f, 1f));
            Sign(env, new Vector2(-1.25f, 1.8f), "<  REGRESAR", new Color(0.62f, 0.86f, 0.94f));
            Sign(env, gate + new Vector2(0f, 2.18f), act switch
            {
                1 => "ASCENSOR", 2 => "ESTACIÓN", 3 => "TRANSBORDADOR", _ => "SOFY"
            }, new Color(0.6f, 0.94f, 0.91f));
            if (act == 1)
                Sign(env, new Vector2(42f, 1.1f), "DOBLE SALTO  ↑", new Color(1f, 0.72f, 0.47f));
            if (act == 4)
                Sign(env, new Vector2(33f, 1.1f), "GRAVEDAD REDUCIDA", new Color(0.76f, 0.83f, 0.97f));

            var player = CreatePlayer(new Vector2(1.65f, 0.87f));
            var foes = new List<EnemyBase2D>();
            foreach (var template in Encounters[act])
            {
                string label = template.Kind switch
                {
                    EnemyBase2D.Kind.Walker => act == 1 ? "Sanguinario" : "Guerrero",
                    EnemyBase2D.Kind.Runner => act == 1 ? "Acechador" : act == 2 ? "Escamado" : "Autómata ligero",
                    EnemyBase2D.Kind.Brute => act == 2 ? "Escamado blindado" : "Blindado",
                    _ => act == 2 ? "Draconiano" : "Centinela"
                };
                var enemy = CreateEnemy(label, template.Kind, new Vector2(template.X, template.Y + 0.90f), template.Guard);
                enemy.SetActTier(act);
                EnsureWarning(enemy);
                foes.Add(enemy);
            }
            var effects = CreateChapterEffects();
            var director = new GameObject("Progresión del nivel").AddComponent<RuinsDemo>();
            director.Configure(player, foes.ToArray(), effects, console.transform, exit, consoleLight, doorLight);
            AttachChapter(director, act, new Vector2(right - 13.5f, last.Y),
                new Vector2(12.3f, geometry.First(platform => platform.Depth < 1f).Y));
            var camera = CreateCampaignCamera(new Vector3(7.5f, 2.45f, -10f), 5.35f);
            var follow = camera.gameObject.AddComponent<RuinsCamera>();
            follow.Configure(director, layers, new[] { 0.09f, 0.23f });
            follow.SetLimits(StartX, right, 8.2f);
            new GameObject("Interfaz").AddComponent<RuinsHud>().Configure(director);
            ExpandEncounterGround(director);
            InstallDistricts(director);
            ReinforceCampaign(director);
            BindScenery(director);
            DressEquipment(director);
            DressFoundry(director);
            Save(scene, ChapterPath(act));
        }

        private static void AttachChapter(RuinsDemo director, int act, Vector2 bossFloor, Vector2 rescueFloor)
        {
            var player = director.Player;
            ApplyKyle(player);
            var profile = (EnemyBase2D.Boss)(act + 1);
            var boss = CreateBoss(profile, CampaignProgress.Chapters[act].Boss, bossFloor);
            director.SetEnemies(director.Enemies.Append(boss).ToArray());
            var rescue = Art("Civil atrapado", "Civilian", rescueFloor + Vector2.up * 0.65f,
                new Vector2(1.28f, 1.65f), 4, null).transform;
            Sign(rescue, rescueFloor + new Vector2(0f, 1.76f), "RESCATE", new Color(0.60f, 0.97f, 0.82f));
            var safe = new GameObject("Punto de control tras el jefe").transform;
            safe.position = bossFloor + new Vector2(-2.0f, 0.83f);
            var mechs = act == 4
                ? director.Enemies.Where(enemy => !enemy.IsBoss && enemy.transform.position.x > 94f).ToArray()
                : Array.Empty<EnemyBase2D>();
            for (int i = 0; i < mechs.Length; i++)
                mechs[i].name = "Meca guardián " + (i + 1);
            var stage = new GameObject("Capítulo").AddComponent<CampaignStage>();
            stage.Configure(act, boss, rescue, safe, mechs);
            ConfigureCinematics(stage);
            director.SetStage(stage);
            var music = new GameObject("Ambiente").AddComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/Acto{act + 1}.wav");
            music.volume = 0.31f;
            music.loop = true;
            music.playOnAwake = true;
            ApplyCast(director);
        }

        private static readonly string[] CivilianArt = { "Medic", "Technician", "Miner", "Mechanic", "Engineer" };

        private static void ApplyCast(RuinsDemo world)
        {
            BindArsenal(world.Effects);
            world.Stage.ConfigureMechAttacks();
            BindCombatAudio(world.Effects);
            BindCombatSprites(world.Effects);
            world.Effects.ConfigureChainArt(Enumerable.Range(0, 7).Select(i => Sprite("PixelWideCut" + i))
                .Append(Sprite("PixelChainFx7")).ToArray(), Sprite("PixelWideGuard"));
            world.Effects.ConfigureHealingArt(new[] { "Cross", "Drop", "Loop", "Star" }
                .Select(shape => Sprite("PixelCure" + shape)).ToArray());
            world.Effects.ConfigurePixelArt(Sprite("PixelHostile0_0"), Sprite("PixelOrb"), Sprite("PixelBeam"),
                Enumerable.Range(0, 4).Select(i => Sprite("PixelSlash" + i)).ToArray(),
                Enumerable.Range(0, 4).Select(i => Sprite("PixelRock" + i)).ToArray());
            ApplyEnvironmentArt(world.Stage.Act);
            foreach (var enemy in world.Enemies)
                ApplyEnemyArt(world, enemy);

            var rescue = world.Stage.Rescue;
            SetCivilianArt(rescue.GetComponent<SpriteRenderer>(), "Cast" + CivilianArt[world.Stage.Act], 0.65f);
            var rig = rescue.GetComponentInChildren<CastRig>(true);
            var visual = rig.GetComponent<SpriteRenderer>();
            SetRig(visual, visual.sprite, CastRig.Role.Civilian);
            var assistance = rescue.GetComponent<CivilianAssistance>();
            if (!assistance)
                assistance = rescue.gameObject.AddComponent<CivilianAssistance>();
            var hero = Sprite("PixelKyleRestored");
            var skin = SkinMaterial();
            assistance.Configure(visual, rig, hero, skin, Mask);
            Vector3 marker = rescue.position + Vector3.up * 1.47f;
            var label = rescue.Find("RESCATE");
            var plate = rescue.Find("Placa");
            if (label)
                label.position = marker;
            if (plate)
                plate.position = marker;
        }

        private static void ApplyEnemyArt(RuinsDemo world, EnemyBase2D enemy)
        {
            string key = enemy.IsBoss ? enemy.Profile.ToString() : enemy.Archetype.ToString();
            bool zombie = world.Stage.Act == 0 && !enemy.IsBoss;
            float height = enemy.Profile switch
            {
                EnemyBase2D.Boss.Vane => 2.34f,
                EnemyBase2D.Boss.Vance => 2.15f,
                EnemyBase2D.Boss.Vulkano => 2.51f,
                EnemyBase2D.Boss.Orbital => 2.49f,
                EnemyBase2D.Boss.Ojo => 2.46f,
                _ => enemy.Archetype == EnemyBase2D.Kind.Brute ? 2.04f : 1.74f
            };
            enemy.SetZombie(zombie);
            if (zombie)
            {
                var names = new[] { "Zombi caminante", "Zombi corredor", "Zombi pesado", "Zombi acechador" };
                enemy.name = names[(int)enemy.Archetype];
            }
            Sprite art = Sprite(zombie ? "CastZombie" + key + "0" : "Cast" + key);
            enemy.SetCharacterArt(art, zombie ? height * 174f / 136f : height);
            SetRig(enemy.GetComponentInChildren<SpriteRenderer>(true), art,
                (CastRig.Role)Enum.Parse(typeof(CastRig.Role), key), zombie: zombie);
            var animated = enemy.GetComponentInChildren<CastRig>(true);
            animated.ConfigureFrames(zombie && enemy.Archetype == EnemyBase2D.Kind.Walker
                ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Art/PixelZombieWalkerMotion.png") : null);
            EnsureWarning(enemy);
            var cue = enemy.transform.Find("Aviso de ataque");
            float foot = -enemy.GetComponent<BoxCollider2D>().size.y * 0.5f;
            cue.localPosition = new Vector3(0f, foot + height + 0.16f, 0f);
            EnsureEnemyHealthBar(world, enemy, cue.localPosition.y + 0.16f);
        }

        private static void SetCivilianArt(SpriteRenderer old, string key, float baseOffset)
        {
            const string ChildName = "Modelo de personaje";
            var parent = old.transform;
            var existing = parent.Find(ChildName);
            var model = existing ? existing.GetComponent<SpriteRenderer>() :
                new GameObject(ChildName).AddComponent<SpriteRenderer>();
            model.transform.SetParent(parent, false);
            model.sprite = Sprite(key);
            model.sortingOrder = old.sortingOrder;
            float scale = 1.78f / model.sprite.bounds.size.y;
            Vector3 parentScale = parent.lossyScale;
            model.transform.localScale = new Vector3(scale / parentScale.x, scale / parentScale.y, 1f);
            model.transform.localPosition = new Vector3(0f, -baseOffset / parentScale.y, 0f);
            SetRig(model, model.sprite, CastRig.Role.Civilian, true);
            old.enabled = false;
        }

        private static void ApplyEnvironmentArt(int act)
        {
            Sprite deck = Sprite("PixelDeck" + act);
            Sprite city = act == 0 ? Sprite("PixelCityDepth") : null;
            foreach (var view in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!view.sprite)
                    continue;

                string art = view.sprite.name;
                Sprite replacement = view.gameObject.name == "Losa" ? deck : art == "NearCity" ? city : null;
                if (!replacement || view.sprite == replacement)
                    continue;

                Vector2 oldSize = view.sprite.bounds.size;
                Vector2 newSize = replacement.bounds.size;
                view.sprite = replacement;
                var scale = view.transform.localScale;
                view.transform.localScale = new Vector3(scale.x * oldSize.x / newSize.x,
                    scale.y * oldSize.y / newSize.y, scale.z);
            }

            var atmosphere = Object.FindAnyObjectByType<SectorAtmosphere>();
            if (!atmosphere)
                atmosphere = new GameObject("Ambiente pixelado").AddComponent<SectorAtmosphere>();
            atmosphere.Configure(Sprite("Pixel"), Camera.main, act);
        }

        private static Material SkinMaterial()
        {
            var saved = AssetDatabase.LoadAssetAtPath<Material>(SkinPath);
            if (saved)
                return saved;
            var shader = Shader.Find("Moon Shadow/Animated Sprite");
            if (!shader)
                throw new InvalidOperationException("Falta el material para la animación 2D.");
            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, SkinPath);
            return material;
        }

        private static void SetRig(SpriteRenderer visual, Sprite art, CastRig.Role role, bool idle = false, bool zombie = false)
        {
            var rig = visual.GetComponent<CastRig>();
            if (!rig)
                rig = visual.gameObject.AddComponent<CastRig>();
            rig.Configure(art, SkinMaterial(), role, idle, zombie);
        }

        private static void ConfigureCinematics(CampaignStage stage)
        {
            stage.SetCinematics(
                stage.Act == 0 ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Art/CinemaIntro.png") : null,
                stage.Final ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Art/CinemaFinal.png") : null,
                stage.Final ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Art/PixelVistaDawn.png") : null);
            stage.Boss.name = stage.Chapter.Boss;
            if (stage.Act == 0)
            {
                var opening = stage.GetComponent<OpeningSequence>();
                if (!opening)
                    opening = stage.gameObject.AddComponent<OpeningSequence>();
                Material surface = SkinMaterial();
                Sprite sofy = Sprite("CastSofy");
                Sprite capture = Sprite("CastOrbital");
                opening.Configure(Sprite("PixelStreet"), Sprite("PixelKyleRestored"), sofy, capture, Sprite("Pixel"),
                    surface, AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/OpeningVane.wav"),
                    Sprite("PixelTransport"));
                opening.SetActionArt(Sprite("CastMedic"), Sprite("PixelHostile5_0"), Sprite("PixelSlash1"));
                stage.SetOpening(opening);
            }
        }

        private static void ApplyKyle(KyleController2D player)
        {
            var view = player.GetComponentInChildren<SpriteRenderer>();
            var art = Sprite("PixelKyleRestored");
            var collider = player.GetComponent<BoxCollider2D>();
            view.sprite = art;
            view.transform.localPosition = Vector3.down * (collider.size.y * 0.5f);
            view.transform.localScale = Vector3.one * (1.77f / art.bounds.size.y);
            var duster = view.GetComponent<KyleDusterVisual>();
            if (duster)
                Object.DestroyImmediate(duster);
            SetRig(view, art, CastRig.Role.Kyle);
            player.Configure(Mask, view);
            Vector3 spawn = player.transform.position;
            spawn.y = collider.size.y * 0.5f;
            player.transform.position = spawn;
        }

        private static EnemyBase2D CreateBoss(EnemyBase2D.Boss profile, string name, Vector2 floor)
        {
            bool dragon = profile == EnemyBase2D.Boss.Vulkano;
            float height = dragon ? 2.35f : 1.94f;
            var spawn = floor + Vector2.up * (height * 0.5f + 0.05f);
            var actor = Actor(name, spawn, new Vector2(dragon ? 1.43f : 0.92f, height), 2.72f);
            string key = "Boss" + profile;
            var view = Art("Sprite", key + "0", spawn + Vector2.up * 0.10f,
                dragon ? new Vector2(4.25f, 2.68f) : new Vector2(3.12f, 2.18f), 11, actor.transform);
            var boss = actor.AddComponent<EnemyBase2D>();
            boss.Configure(profile == EnemyBase2D.Boss.Vance || profile == EnemyBase2D.Boss.Ojo ?
                EnemyBase2D.Kind.Runner : EnemyBase2D.Kind.Brute, Mask, view,
                Enumerable.Range(0, 4).Select(i => Sprite(key + i)).ToArray(), false);
            boss.SetBoss(profile);
            Vector2 above = spawn + Vector2.up * (height * 0.5f + 0.45f);
            var cue = Art("Aviso de ataque", "Pixel", above, new Vector2(0.64f, 0.09f), 13, actor.transform);
            cue.enabled = false;
            boss.SetWarning(cue);
            return boss;
        }

        private static void Decorate(Transform parent, int act, Platform block)
        {
            for (float x = block.From + 4f; x < block.To - 1.7f; x += 6.7f)
            {
                bool pylon = Mathf.RoundToInt(x) % 3 == 0;
                string art = pylon ? "Pylon" : "Lamp";
                var prop = Art(pylon ? "Soporte" : "Luz de seguridad", art, new Vector2(x, block.Y + 1.8f),
                    new Vector2(1.46f, 3.60f), -8, parent);
                prop.color = act switch
                {
                    1 => new Color(0.93f, 0.76f, 1f), 2 => new Color(1f, 0.68f, 0.51f),
                    3 => new Color(0.83f, 0.96f, 1f), _ => new Color(0.75f, 0.92f, 1f)
                };
                Art("Escombros", "Debris" + ((int)x % 4), new Vector2(x + 1.2f, block.Y + 0.27f),
                    new Vector2(1.4f, 0.62f), -9, parent);
            }
        }

        private static void Lava(Transform parent, float from, float to)
        {
            float width = to - from;
            Vector2 center = new((from + to) * 0.5f, -2.6f);
            var pool = Art("Lava", "Pixel", center, new Vector2(width, 1.16f), 2, parent);
            pool.color = new Color(0.83f, 0.30f, 0.19f);
            Art("Brillo de lava", "Pixel", center + Vector2.up * 0.55f, new Vector2(width, 0.14f), 3, parent)
                .color = new Color(1f, 0.67f, 0.29f);
            var trigger = new GameObject("Daño: lava");
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = center;
            var collider = trigger.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width - 0.23f, 0.88f);
            collider.isTrigger = true;
            trigger.AddComponent<HazardFloor>();
        }

        private static RuinsEffects CreateChapterEffects()
        {
            var effects = new GameObject("Audio y efectos").AddComponent<RuinsEffects>();
            string[] soundNames = { "Sword", "Impact", "Dash", "Parry", "Hurt", "Relay" };
            var clips = soundNames.Select(id => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/{id}.wav")).ToArray();
            effects.Configure(Sprite("Pixel"), Sprite("Slash"), Sprite("Guard"), Sprite("Medkit"), clips, Mask);
            return effects;
        }

        private static Camera CreateCampaignCamera(Vector3 position, float size)
        {
            var camera = new GameObject("Cámara", typeof(Camera), typeof(AudioListener),
                typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(14, 21, 38, 255);
            camera.transform.position = position;
            return camera;
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException("No se pudo guardar: " + path);
        }
    }
}

