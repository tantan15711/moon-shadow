using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MoonShadow
{
    public sealed partial class MoonDemoSmoke : MonoBehaviour
    {
        private RuinsDemo demo;
        private Keyboard keyboard;
        private XInputControllerWindows xbox;
        private bool openingVerified;

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        private struct XboxState : IInputStateTypeInfo
        {
            [FieldOffset(0)] public ushort Buttons;
            [FieldOffset(3)] public byte RightTrigger;
            [FieldOffset(4)] public short X;
            [FieldOffset(6)] public short Y;
            [FieldOffset(8)] public short RX;
            [FieldOffset(10)] public short RY;
            public FourCC format => new('X', 'I', 'N', 'P');
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-moonSmoke") >= 0 || MotionReviewRequested || TouchReviewRequested)
                DontDestroyOnLoad(new GameObject("Campaign QA", typeof(MoonDemoSmoke)));
        }

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            xbox = InputSystem.AddDevice<XInputControllerWindows>();
            yield return Frames(5);
            if (TouchReviewRequested)
            {
                yield return RecordTouch();
                yield break;
            }
            if (MotionReviewRequested)
            {
                yield return RecordMotion();
                yield break;
            }
            Check(SceneManager.GetActiveScene().name == "Inicio" && FindAnyObjectByType<CampaignMenu>(),
                "Title scene loads first");
            var menu = FindAnyObjectByType<CampaignMenu>();
            var selection = typeof(CampaignMenu).GetField("selection",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            int selected = (int)selection.GetValue(menu);
            yield return Tap(Key.S);
            Check((int)selection.GetValue(menu) != selected, "S moves down in the title menu");
            yield return Tap(Key.W);
            Check((int)selection.GetValue(menu) == selected, "W moves back up in the title menu");
            yield return TapXbox(new XboxState { Y = short.MinValue });
            Check((int)selection.GetValue(menu) != selected, "Xbox left stick navigates the title menu");
            yield return TapXbox(new XboxState { Buttons = 1 << 0 });
            Check((int)selection.GetValue(menu) == selected, "Xbox D-pad navigates the title menu");
            Check(GameObject.Find("Suelo de la azotea"), "Kyle has a rooftop in the title scene");
            CaptureScene("campaign-title");
            if (selected == 0)
                yield return TapXbox(new XboxState { Y = short.MinValue });
            yield return TapXbox(new XboxState { Buttons = 1 << 12 });
            if (selected == 0)
            {
                yield return TapXbox(new XboxState { Buttons = 1 << 13 });
                Check((int)selection.GetValue(menu) == 0, "Xbox B cancels the new game confirmation");
                yield return TapXbox(new XboxState { Y = short.MinValue });
                yield return TapXbox(new XboxState { Buttons = 1 << 12 });
                yield return TapXbox(new XboxState { Buttons = 1 << 12 });
            }

            for (int act = 0; act < 5; act++)
            {
                int index = act;
                yield return StageReady(index);
                if (index == 0)
                {
                    yield return VerifyXbox();
                    yield return VerifyInventoryPauses();
                    yield return VerifyDashDamage();
                    yield return VerifyKatanaDeflection();
                    yield return VerifyWideParry();
                    yield return VerifyFourCuts();
                    yield return VerifyBladeReach();
                    yield return VerifyGunAndRescue();
                    yield return VerifyMedkits();
                    yield return VerifyEquip();
                    yield return VerifyMinorTechniques();
                }
                if (index == 2)
                {
                    Check(FindObjectsByType<LavaFlow>().Length == 2, "Both volcanic hazards use animated lava artwork");
                    yield return Cross(28.28f, 0f, 36.3f, 0.20f, false, "First lava gap");
                    CaptureScene("nov15-lava");
                }
                if (index == 3)
                    yield return Cross(37.34f, 0f, 47.5f, 1.20f, true, "Orbital approach gap");
                if (index == 4)
                {
                    yield return VerifyLunarLanding();
                    yield return Cross(39.31f, 0f, 50.3f, 2.20f, true, "First lunar gap in reduced gravity", 4.42f);
                }

                if (index == 2)
                {
                    Check(!demo.Map.ArenaCleared, "Volcanic arena starts incomplete");
                    yield return UnlockTerminal();
                    Check(!demo.Map.ArenaCleared, "Vulkano unlocks independently of the optional canyon waves");
                }
                yield return VerifyDistrictArena(index);
                if (index != 2)
                    yield return UnlockTerminal();
                if (index == 0)
                {
                    Check(demo.Player.AirDashUnlocked && !demo.Player.DoubleJumpUnlocked, "Act I awards air dash");
                    yield return CheckShop();
                    yield return Cross(48.33f, 0f, 57.9f, 0f, false, "Ruins gap");
                }
                if (index == 1)
                {
                    Check(demo.Player.DoubleJumpUnlocked, "Act II awards double jump");
                    yield return TestBacktrack();
                    yield return Cross(42.10f, 0f, 47.35f, 3.10f, true, "Double jump climbs the laboratory shelf");
                }
                if (index == 2)
                    yield return Cross(60.25f, 0.20f, 69.57f, 0.45f, false, "Second lava gap");
                if (index == 3)
                    yield return Cross(71.3f, 1.20f, 81.0f, 0.8f, false, "Orbital launch bridge gap");
                if (index == 4)
                    yield return Cross(71.25f, 2.2f, 84.2f, 0.45f, true, "Second lunar gap");

                yield return VerifyWiderArena(index);
                yield return VerifyBoss(index);
                if (index == 0)
                    yield return VerifyResume();
                if (index == 4)
                {
                    Position(demo.Player.GetComponent<Rigidbody2D>(), demo.Target - new Vector2(2.45f, 0f));
                    FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
                    CaptureScene("rescate-capsula-cerrada");
                }
                Position(demo.Player.GetComponent<Rigidbody2D>(), demo.Target);
                yield return Until(() => demo.Finished, 1f, "Completed Act " + (index + 1));
                if (index == 4)
                {
                    Check(demo.LiveEnding && demo.Stage.Epilogue.Playing && demo.StoryLength == 4,
                        "Sofy's capsule opens in the live ending");
                    yield return new WaitForSecondsRealtime(2.95f);
                    var sister = Field<Transform>(demo.Stage.Epilogue, "sofy");
                    Check(sister.position.x - demo.Player.transform.position.x > 0.55f,
                        "Kyle approaches Sofy visibly while the simulation is paused");
                    var view = Field<SpriteRenderer>(demo.Stage.Epilogue, "sisterVisual");
                    Check(view.GetComponent<CastRig>().Visible, "Sofy uses her articulated rig during the rescue");
                    var window = Field<SpriteRenderer>(demo.Stage.Epilogue, "leftGlass");
                    Check(view.sprite.name == "PixelSofyLunar" && view.sprite.texture.height >= 250 &&
                        view.sprite.texture.filterMode == FilterMode.Point && view.sortingOrder == 10,
                        "Sofy uses her detailed sprite in front of the open capsule");
                    var kyle = demo.Player.GetComponentInChildren<CastRig>();
                    Check(kyle.Visible && kyle.Skin.name == "PixelKyleRestored" &&
                        kyle.Skin.texture.filterMode == FilterMode.Point,
                        "Kyle keeps his original jacket and rig during the rescue");
                    Check(window.bounds.size.x < 0.22f && window.color.a < 0.40f &&
                        Camera.main.orthographicSize < 2.6f, "Glass panels open and the camera moves in for the reunion");
                    CaptureScene("story-sofy-rescue");
                    yield return Tap(Key.Enter);
                    Check(demo.Finished && demo.StoryStep == 1, "Ending dialogue advances");
                    yield return TapXbox(new XboxState { Buttons = 1 << 12 });
                    Check(demo.StoryStep == 2 && demo.StoryArt && demo.StoryArt.name == "PixelVistaDawn",
                        "The ending shows the new view of Earth");
                    yield return TapXbox(new XboxState { Buttons = 1 << 12 });
                    Check(demo.Credits && CampaignProgress.SavedCompleted, "Thank-you screen and campaign completion");
                }
                yield return TapXbox(new XboxState { Buttons = 1 << 12 });
            }

            yield return Until(() => SceneManager.GetActiveScene().name == "Inicio", 5f, "Ending returns to title");
            Check(CampaignProgress.SavedCompleted && CampaignProgress.SavedAct == 4, "Campaign completion is saved");
            yield return VerifyLegacyLoadout();
            ReportPerformance();
            Debug.Log("MOON_CAMPAIGN_PASSED");
            Application.Quit(0);
        }

        private IEnumerator StageReady(int index)
        {
            yield return Until(() =>
            {
                demo = FindAnyObjectByType<RuinsDemo>();
                return demo && demo.Stage && demo.Stage.Act == index && demo.Briefing;
            }, 8f, "Loaded Act " + (index + 1));

            if (index == 0 && CampaignProgress.Active.Checkpoint == 0)
            {
                Check(demo.IntroPlaying && !demo.StoryArt, "New game starts the live opening");
                if (!openingVerified)
                {
                    var camera = Camera.main;
                    Vector3 before = camera.transform.position;
                    yield return Until(() => demo.Stage.Opening.Elapsed > 4.1f, 5.5f, "Mech approaches the street");
                    var mech = GameObject.Find("Unidad de captura").GetComponent<CastRig>();
                    var mechJoints = (Vector2[])Field<Vector2[]>(mech, "positions").Clone();
                    yield return Until(() => JointMoved(mech, mechJoints, 3, 0.025f) &&
                        JointMoved(mech, mechJoints, 7, 0.05f), 0.8f,
                        "The cinematic mech articulates its elbow and knee while walking");
                    yield return Until(() => demo.Stage.Opening.Elapsed > 8.6f, 10f, "Intro camera reaches the capture");
                    Check(Vector3.Distance(camera.transform.position, before) > 1f,
                        "Opening cuts and changes camera position as it plays");
                    CaptureScene("opening-capture");
                    var sofy = GameObject.Find("Sofy").GetComponent<CastRig>();
                    var struggle = (Vector2[])Field<Vector2[]>(sofy, "positions").Clone();
                    yield return Until(() => JointMoved(sofy, struggle, 3, 0.05f) &&
                        JointMoved(sofy, struggle, 9, 0.035f), 0.8f,
                        "Sofy twists her arm and kicks while held");
                    Check(demo.Stage.Opening.Caption.Contains("No te acerques"), "The prologue shows Sofy's scripted line");
                    yield return Until(() => demo.Stage.Opening.Elapsed > 11.39f, 4f, "Sonic pulse interrupts Kyle's leap");
                    Check(GameObject.Find("Pulso sónico").GetComponent<SpriteRenderer>().enabled,
                        "Sonic projectile is visible");
                    CaptureScene("story-prologue-pulse");
                    yield return Until(() => demo.Stage.Opening.Elapsed > 14.6f, 8f, "Opening frames the transport");
                    CaptureScene("opening-transport");
                    yield return Until(() => demo.Playing && !demo.IntroPlaying, 15.2f, "Opening ends automatically");
                    yield return Frames(8);
                    openingVerified = true;
                }
                else
                    yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            }
            else
            {
                int cards = demo.StoryLength;
                for (int card = 0; card < cards; card++)
                    yield return TapXbox(new XboxState { Buttons = 1 << 12 });
            }
            yield return Until(() => demo.Playing, 1f, "Xbox controller closes the briefing");
            Check(demo.Player.GetComponentInChildren<SpriteRenderer>().sprite.name.StartsWith("Pixel",
                StringComparison.Ordinal), "Pixel Kyle is bound to gameplay");
            var scenery = GameObject.Find("Vista del acto").GetComponent<SpriteRenderer>();
            Check(scenery.sprite.name.StartsWith("PixelVista", StringComparison.Ordinal), "Act has an original pixel-art vista");
            var lamps = FindObjectsByType<SpriteRenderer>().Where(view => view.sprite && view.sprite.name.StartsWith("PixelLamp")).ToArray();
            Check(lamps.Any(view => view.sprite.name != "PixelLampHalo") && lamps.Any(view => view.sprite.name == "PixelLampHalo"),
                "Act uses new lamppost artwork and lens glow");
            Check(demo.Enemies.All(enemy => enemy.CharacterArt && enemy.CharacterArt.name.StartsWith("Cast",
                StringComparison.Ordinal)), "New enemies and boss art is bound in Act " + (index + 1));
            Check(demo.Stage.Rescue.GetComponentInChildren<CastRig>(true),
                "Act has its own rescue character");
            var actors = demo.Map.CollectActors();
            Check(Field<EnemyBase2D[]>(demo, "enemies").Count(enemy => enemy.IsReinforcement) >= 7 &&
                actors.Count(enemy => enemy.IsReinforcement) >= 8,
                "Act has reinforcements on its route and side encounters");
            var firstAdded = Array.FindIndex(actors, enemy => enemy.IsReinforcement);
            Check(firstAdded > 0 && actors.Skip(firstAdded).All(enemy => enemy.IsReinforcement),
                "New wave enemies preserve the order of legacy medkit IDs");
            if (index == 4)
            {
                var shell = GameObject.Find("Cápsula de estasis").transform.Find("Carcasa").GetComponent<SpriteRenderer>();
                Check(shell.sprite.name == "PixelLunarCapsule" &&
                    Field<SpriteRenderer>(demo.Stage.Epilogue, "sisterVisual").sprite.name == "PixelSofyLunar",
                    "The lunar scene uses the authored capsule and Sofy artwork");
            }
            if (index == 0)
                Check(demo.Enemies.Where(enemy => !enemy.IsBoss).All(enemy => enemy.IsZombie) &&
                    demo.Enemies.Where(enemy => enemy.IsZombie).Select(enemy => enemy.Archetype).Distinct().Count() == 4,
                    "Every ordinary enemy in the first act is one of four zombies");
            if (index >= 2)
                Check(demo.Player.AirDashUnlocked && demo.Player.DoubleJumpUnlocked, "Movement abilities persist between acts");
        }

        private IEnumerator VerifyXbox()
        {
            var player = demo.Player;
            var body = player.GetComponent<Rigidbody2D>();
            var stores = Field<Ammunition>(demo, "ammunition");
            Check(System.Enum.GetValues(typeof(Sidearm)).Cast<Sidearm>()
                .Where(weapon => SidearmCatalog.Get(weapon).UsesAmmo)
                .All(weapon => stores.Loaded(weapon) == 30 && stores.Reserve(weapon) == 60),
                "Ballistic guns start with a 30-round magazine and two reserves");
            Check(Field<AudioClip[]>(demo.Effects, "weaponAudio").Distinct().Count() == 5 &&
                Field<Sprite[]>(demo.Effects, "dashShards").All(sprite => sprite && sprite.name.StartsWith("PixelDashShard")),
                "All weapon sounds are distinct and the dash has dedicated artwork");
            var authored = System.Enum.GetValues(typeof(EnemyBase2D.Technique)).Cast<EnemyBase2D.Technique>()
                .Where(move => move is > EnemyBase2D.Technique.None and < EnemyBase2D.Technique.ZombieRake)
                .Select(demo.Effects.AttackArt).ToArray();
            Check(authored.All(sprite => sprite) && authored.Distinct().Count() == authored.Length && authored.Length == 24,
                "Every special technique has a distinct imported sprite");
            Position(body, new Vector2(1.65f, 0.82f));
            yield return Until(() => player.Grounded, 0.5f, "Kyle rests on the floor before Xbox input");
            Queue(Key.A);
            yield return Frames(2);
            Check(demo.TutorialText?.Contains("A / D") == true, "First movement popup reflects the keyboard");
            Queue();
            QueueXbox(new XboxState { X = short.MaxValue });
            yield return Frames(2);
            Check(demo.TutorialText?.Contains("stick izquierdo") == true, "Movement popup updates to Xbox while visible");
            QueueXbox(default);
            yield return TapXbox(new XboxState { Buttons = 1 << 5 });
            Check(demo.InventoryOpen && Time.timeScale == 0f, "Xbox View opens inventory");
            yield return TapXbox(new XboxState { Y = short.MinValue });
            yield return TapXbox(new XboxState { Buttons = 1 << 12 });
            Check(demo.InventoryOpen && demo.Weapon == Sidearm.Service, "Xbox stick and A select a weapon");
            yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            Check(demo.Playing && !demo.InventoryOpen, "Xbox B closes inventory");
            yield return TapXbox(new XboxState { Buttons = 1 << 4 });
            Check(demo.Paused, "Xbox Menu pauses");
            yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            Check(demo.Playing && demo.Player == player, "Xbox B resumes without reloading");
            yield return TapXbox(new XboxState { Buttons = 1 << 4 });
            yield return TapXbox(new XboxState { Buttons = 1 << 12 });
            Check(demo.Playing && player.Grounded, "Xbox A resumes without causing a jump");

            float startX = player.Position.x;
            QueueXbox(new XboxState { X = short.MaxValue });
            yield return Until(() => player.Position.x > startX + 0.6f, 0.5f, "Xbox analog stick moves Kyle");
            QueueXbox(default);
            yield return Frames(5);
            yield return TapXbox(new XboxState { Buttons = 1 << 12 });
            yield return Until(() => !player.Grounded, 0.4f, "Xbox A jumps during gameplay");
            yield return Until(() => player.Grounded, 1f, "Kyle lands");
            int ammo = demo.Ammo;
            yield return TapXbox(new XboxState { RightTrigger = byte.MaxValue });
            Check(demo.Ammo == ammo - 1, "Xbox RT fires a bullet");
            yield return VerifyReload();
            ammo = demo.Ammo;
            QueueXbox(new XboxState { RightTrigger = byte.MaxValue, RX = 22000, RY = 24000 });
            yield return Until(() => demo.Ammo <= ammo - 4, 0.9f, "Hold RT fires at least four rounds");
            yield return Until(() => FindObjectsByType<PlayerRound2D>().Any(round => Field<Vector2>(round, "direction").y > 0.65f),
                0.6f, "Right stick aims a continuous burst upwards");
            QueueXbox(new XboxState { RightTrigger = byte.MaxValue, RX = -23000, RY = 16000 });
            yield return Until(() => FindObjectsByType<PlayerRound2D>().Any(round => Field<Vector2>(round, "direction").x < -0.7f),
                0.6f, "A burst changes direction without releasing RT");
            QueueXbox(default);
            yield return Frames(5);
            ammo = demo.Ammo;
            Queue(Key.F);
            yield return Until(() => demo.Ammo <= ammo - 3, 0.8f, "Holding F fires at least three rounds");
            Queue();
            yield return Frames(5);
            yield return TapXbox(new XboxState { Buttons = 1 << 9 });
            yield return Until(() => player.Dashing, 0.4f, "Xbox RB dashes");
            yield return Until(() => !player.Dashing, 0.5f, "Xbox dash ends");
            yield return TapXbox(new XboxState { Buttons = 1 << 7 });
            Check(demo.VisorActive, "Xbox R3 activates the visor");
            var combat = player.GetComponent<KyleCombat2D>();
            yield return TapXbox(new XboxState { Buttons = 1 << 14 });
            Check(combat.Attacking, "Xbox X starts a light sword strike");
            yield return Until(() => !combat.Attacking, 0.75f, "Xbox light strike completes");
            yield return TapXbox(new XboxState { Buttons = 1 << 8 });
            Check(combat.Parrying, "Xbox LB parries");
            yield return Until(() => !combat.Parrying, 0.5f, "Xbox parry window ends");
        }

        private IEnumerator VerifyKatanaDeflection()
        {
            var body = demo.Player.GetComponent<Rigidbody2D>();
            var source = demo.Enemies.First(enemy => enemy.Archetype == EnemyBase2D.Kind.Sentry);
            Position(body, new Vector2(1.6f, 0.83f));
            yield return Until(() => Field<float>(demo.Player, "hurtTime") <= 0f && demo.Player.Grounded, 0.8f,
                "Kyle settles before cutting a projectile");
            demo.Player.Face(1);
            Queue(Key.J);
            var blade = demo.Player.GetComponent<KyleCombat2D>();
            yield return Until(() => Field<bool>(blade, "struck"), 0.28f, "The cut becomes active promptly");
            int health = demo.Player.Health;
            demo.Effects.FireBolt(source, demo.Player.Position + new Vector2(1.23f, 0.17f), Vector2.left);
            var round = FindObjectsByType<EnemyBolt2D>().First(shot =>
                Vector2.Distance(shot.transform.position, demo.Player.Position) < 1.45f);
            yield return Until(() => round && Field<bool>(round, "reflected"), 0.32f, "Katana redirects a bullet during its active frames");
            Check(demo.Player.Health == health && round.GetComponent<SpriteRenderer>().sprite.name == "PixelKatanaReturn",
                "The redirected round uses new art without damaging Kyle");
            Queue();
            demo.Effects.ClearBolts();
            yield return Until(() => !blade.Attacking, 0.6f, "Katana recovers after reflecting a bullet");
        }

        private IEnumerator VerifyFourCuts()
        {
            var blade = demo.Player.GetComponent<KyleCombat2D>();
            blade.ResetCombat();
            demo.Effects.ClearTransientEffects();
            for (int i = 0; i < 4; i++)
            {
                string sprite = "PixelWideCut" + i;
                Queue(Key.J);
                yield return Until(() => demo.Effects.GetComponentsInChildren<SpriteRenderer>()
                    .Any(view => view.sprite && view.sprite.name == sprite), 0.32f,
                    "Chain advances to katana cut " + (i + 1));
                Queue();
                yield return Frames(2);
            }
            CaptureScene("presion-combo");
            yield return Until(() => !blade.Attacking, 0.6f, "Kyle recovers from the final chain cut");
        }

        private IEnumerator VerifyWideParry()
        {
            var blade = demo.Player.GetComponent<KyleCombat2D>();
            var source = demo.Enemies.First(enemy => enemy.Archetype == EnemyBase2D.Kind.Sentry);
            var player = demo.Player;
            blade.ResetCombat();
            int health = player.Health;
            QueueXbox(new XboxState { Buttons = 1 << 8 });
            yield return Until(() => blade.Parrying, 0.25f, "LB activates the wider parry");
            QueueXbox(default);
            yield return new WaitForSeconds(0.215f);
            Check(blade.Parrying && demo.Effects.GetComponentsInChildren<SpriteRenderer>()
                .Any(view => view.sprite && view.sprite.name == "PixelWideGuard"),
                "Parry is active past the previous timing window and displays its radius");
            demo.Effects.FireBolt(source, player.Position + new Vector2(2.11f, 0.18f), Vector2.left);
            var round = FindObjectsByType<EnemyBolt2D>().First(shot =>
                Vector2.Distance(shot.transform.position, player.Position) < 2.25f);
            yield return Until(() => round && Field<bool>(round, "reflected"), 0.24f, "Parry intercepts a distant projectile");
            Check(player.Health == health && Vector2.Distance(player.Position, round.transform.position) > 1.45f,
                "Reflected projectile stays well outside the hurtbox");
            FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
            CaptureScene("katana-amplia-parry");
            demo.Effects.ClearBolts();
            yield return Until(() => !blade.Parrying, 0.7f, "Guard ends normally");
        }

        private IEnumerator VerifyBladeReach()
        {
            var player = demo.Player;
            var blade = player.GetComponent<KyleCombat2D>();
            var walker = demo.Enemies.First(enemy => !enemy.GuardsRelay && enemy.Archetype == EnemyBase2D.Kind.Walker);
            walker.ResetEnemy();
            Position(player.GetComponent<Rigidbody2D>(), new Vector2(1.65f, 0.83f));
            Position(walker.GetComponent<Rigidbody2D>(), new Vector2(4.72f, 0.85f));
            walker.Stagger(0.45f);
            blade.ResetCombat();
            yield return Until(() => player.Grounded, 0.45f, "Kyle settles before testing the sword reach");
            int health = walker.Health;
            yield return Tap(Key.J);
            yield return Until(() => walker.Health < health, 0.32f, "Wider katana hits an enemy about three metres away");
            walker.ResetEnemy();
        }

        private IEnumerator VerifyReload()
        {
            int partial = demo.Ammo;
            int reserve = demo.ReserveAmmo;
            yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            Check(demo.Player.Reloading && demo.Ammo == partial && demo.ReserveAmmo == reserve,
                "Xbox B begins a timed reload");
            yield return TapXbox(new XboxState { RightTrigger = byte.MaxValue });
            Check(demo.Player.Reloading && demo.Ammo == partial, "Shooting cannot interrupt a reload");
            yield return Until(() => !demo.Player.Reloading, 1.3f, "Xbox reload completes");
            Check(demo.Ammo == partial + 1 && demo.ReserveAmmo == reserve - 1,
                "Reload transfers a single missing round from the reserve");

            yield return Tap(Key.F);
            Check(demo.Ammo == partial, "Keyboard fires from the same magazine");
            yield return TapXbox(new XboxState { Buttons = 1 << 5 });
            yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            Check(!demo.InventoryOpen && !demo.Player.Reloading && demo.Ammo == partial,
                "Xbox B closing inventory does not reload");
            yield return Tap(Key.R);
            Check(demo.Player.Reloading, "R starts a reload from the keyboard");
            yield return Until(() => !demo.Player.Reloading, 1.3f, "Keyboard reload completes");
            Check(demo.Ammo == partial + 1 && demo.ReserveAmmo == reserve - 2,
                "Keyboard reload fills the magazine and deducts from reserve");
        }

        private IEnumerator VerifyWiderArena(int act)
        {
            float previousEdge = new[] { 74f, 106f, 107f, 114f, 120f }[act];
            var arena = GameObject.Find("Piso de la arena").GetComponent<BoxCollider2D>();
            Check(arena.bounds.size.x >= 30f && Mathf.Abs(arena.bounds.min.x - previousEdge) < 0.02f &&
                arena.bounds.max.x - demo.Stage.Boss.SpawnPosition.x >= 12f,
                $"Act {act + 1} has a wide, connected boss arena");
            Check(demo.Enemies.All(enemy => enemy.GetComponentInChildren<EnemyHealthBar>()),
                $"Act {act + 1} has overhead health bars on all enemies");

            var body = demo.Player.GetComponent<Rigidbody2D>();
            Position(body, new Vector2(previousEdge - 1.2f, arena.bounds.max.y + 0.82f));
            yield return Until(() => demo.Player.Grounded && Field<float>(demo.Player, "hurtTime") <= 0f,
                1.2f, "Kyle can move at the arena approach");
            QueueXbox(new XboxState { X = short.MaxValue, Buttons = 1 << 9 });
            yield return Until(() => demo.Player.Dashing, 0.6f, "Xbox dash crosses the guarded arena entrance");
            QueueXbox(new XboxState { X = short.MaxValue });
            yield return Until(() => demo.Player.Position.x > previousEdge + 3f, 3.0f,
                $"Xbox crosses into the enlarged arena in Act {act + 1}");
            QueueXbox(default);
            Check(demo.Player.Grounded, "The added terrain supports Kyle");
            yield return Frames(5);
        }

        private IEnumerator VerifyDashDamage()
        {
            var player = demo.Player.GetComponent<Rigidbody2D>();
            var rig = player.GetComponentInChildren<CastRig>();
            Position(player, new Vector2(0.8f, 0.82f));
            yield return Until(() => demo.Player.Grounded && rig.Visible && rig.FootPlanted(0) && rig.FootPlanted(1), 0.65f,
                "The original short-jacket rig is grounded with both feet planted");
            Check(rig.Skin.name == "PixelKyleRestored" && rig.Skin.texture.filterMode == FilterMode.Point &&
                !rig.GetComponent<KyleDusterVisual>(), "Kyle is wearing his original short jacket without a duster component");
            FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
            CaptureScene("rig-kyle-idle");
            var rest = (Vector2[])Field<Vector2[]>(rig, "positions").Clone();
            Queue(Key.D, Key.LeftCtrl);
            yield return Until(() => player.linearVelocity.x > 1.7f && JointMoved(rig, rest, 7, 0.065f), 0.6f,
                "The walking knee flexes with Ctrl held");
            Queue(Key.D);
            yield return Until(() => player.linearVelocity.x > 5.04f &&
                (rig.FootPlanted(0) != rig.FootPlanted(1)), 0.9f, "Kyle runs with alternating grounded feet");
            FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
            CaptureScene("rig-kyle-run");
            Queue();
            var walker = demo.Enemies.First(enemy => !enemy.IsBoss && !enemy.GuardsRelay &&
                enemy.Archetype == EnemyBase2D.Kind.Walker);
            Position(player, new Vector2(1.65f, 0.82f));
            Position(walker.GetComponent<Rigidbody2D>(), new Vector2(3.65f, 0.82f));
            int health = walker.Health;
            yield return Until(() => demo.Player.Grounded, 0.7f, "Grounded before damaging dash");
            Queue(Key.D, Key.LeftShift);
            yield return Until(() => demo.Player.Dashing, 0.4f, "Keyboard begins dash");
            yield return Frames(2);
            Check(rig.Visible && JointMoved(rig, rest, 0, 0.06f), "Kyle lowers his body into an articulated dash");
            yield return Frames(2);
            Check(FindObjectsByType<MeshRenderer>().Any(mesh => mesh.name == "Estela del dash" && mesh.enabled),
                "The dash leaves an afterimage sampled from the moving rig");
            CaptureScene("rig-kyle-dash");
            Queue();
            yield return Until(() => !demo.Player.Dashing, 0.6f, "Dash ends");
            yield return new WaitForSeconds(0.12f);
            Check(walker.Health == health - 1, "One dash through the same enemy deals exactly one point");
            walker.ResetEnemy();
            Position(player, new Vector2(1.65f, 0.82f));
        }

        private static bool JointMoved(CastRig rig, Vector2[] previous, int index, float distance) =>
            (Field<Vector2[]>(rig, "positions")[index] - previous[index]).sqrMagnitude > distance * distance;

        private IEnumerator VerifyMinorTechniques()
        {
            var techniques = new[] { EnemyBase2D.Technique.ZombieRake, EnemyBase2D.Technique.ZombieLunge,
                EnemyBase2D.Technique.ZombieHammer, EnemyBase2D.Technique.ZombieClutch };
            float[] distance = { 1.35f, 2.76f, 2.10f, 1.90f };
            for (int i = 0; i < techniques.Length; i++)
            {
                int which = i;
                var enemy = demo.Enemies.First(unit => !unit.IsBoss && (int)unit.Archetype == which);
                enemy.ResetEnemy();
                Position(demo.Player.GetComponent<Rigidbody2D>(), new Vector2(1.65f, 0.82f));
                Position(enemy.GetComponent<Rigidbody2D>(), new Vector2(1.65f + distance[i], 0.82f));
                yield return Until(() => enemy.CurrentTechnique == techniques[which], 1.4f,
                    enemy.name + " enters " + techniques[i]);
                var rig = enemy.GetComponentInChildren<CastRig>(true);
                var posture = (Vector2[])Field<Vector2[]>(rig, "positions").Clone();
                yield return Until(() => rig.Visible && enemy.CommittedMelee && JointMoved(rig, posture, 3, 0.05f),
                    0.85f, enemy.name + " moves its actual elbow in its bare-hand attack");
                Check(!enemy.CommittedRanged, enemy.name + " has no ranged windup");
                if (i == 3)
                    CaptureScene("rig-zombie-clutch");
                enemy.ResetEnemy();
            }
        }

        private IEnumerator VerifyGunAndRescue()
        {
            yield return Until(() => Field<float>(demo.Player, "hurtTime") <= 0f, 0.8f,
                "Kyle recovers from contact in the previous melee test");
            var target = demo.Enemies.First(enemy => !enemy.GuardsRelay && enemy.Archetype == EnemyBase2D.Kind.Walker);
            Position(target.GetComponent<Rigidbody2D>(), new Vector2(5.95f, 0.82f));
            int startingHealth = target.Health;
            int ammo = demo.Ammo;
            Queue(Key.F);
            yield return Until(() => FindAnyObjectByType<PlayerRound2D>() != null, 0.36f,
                "Kyle's shot appears as a travelling bullet");
            Queue();
            yield return Until(() => target.Health == startingHealth - 1 && demo.Ammo == ammo - 1, 0.6f,
                "Pistol hits an enemy and uses ammunition");
            var fill = target.GetComponentInChildren<EnemyHealthBar>().transform.Find("Indicador").GetComponent<SpriteRenderer>();
            yield return Frames(4);
            Check(fill.enabled && fill.bounds.size.x < 0.65f, "Regular enemy's overhead health bar shows damage");
            demo.Effects.FireBolt(target, target.Position, Vector2.right);
            var shot = FindAnyObjectByType<EnemyBolt2D>();
            Check(shot && shot.GetComponent<SpriteRenderer>().sprite.name.StartsWith("PixelMoonMove"),
                "Enemy shots have dedicated pixel-art sprites");
            demo.Effects.ClearBolts();
            CaptureScene("campaign-kyle");
            var refuge = Field<CampaignMap.Room>(demo.Map, "refuge");
            var player = demo.Player.GetComponent<Rigidbody2D>();
            Position(player, refuge.MainDoor.position);
            yield return Tap(Key.E);
            Check(demo.Map.InSideRoom && demo.Map.Area == refuge.Title, "Roof door leads to a vertical rescue district");
            yield return VerifyLift();
            Position(demo.Player.GetComponent<Rigidbody2D>(), demo.Stage.Rescue.position);
            yield return Frames(6);
            var assistance = demo.Stage.Rescue.GetComponent<CivilianAssistance>();
            var wounded = Field<SpriteRenderer>(assistance, "civilian");
            Check(demo.Stage.Rescue.gameObject.activeSelf && Mathf.Abs(wounded.transform.eulerAngles.z - 278f) < 1f,
                "The civilian lies injured and the escort prevents an early rescue");
            yield return Tap(Key.E);
            Check(!assistance.Helping, "Cannot assist while the escort is alive");
            Position(player, Field<Transform>(demo.Map, "cableStart").position);
            yield return Tap(Key.E);
            yield return Until(() => demo.Player.Position.y > 8.2f, 1.4f, "Service cable reaches the rooftop");
            yield return Frames(12);
            foreach (var guard in refuge.Waves[0].Enemies)
                guard.Hit(99, 1, true, false);
            Position(player, demo.Stage.Rescue.position);
            yield return Tap(Key.E);
            Check(assistance.Helping && !demo.Playing, "Interact starts the assistance animation");
            yield return new WaitForSecondsRealtime(0.82f);
            CaptureScene("nov15-rescue");
            yield return Until(() => demo.CivilianRescued && assistance.Recovered && demo.Weapon != Sidearm.Service, 1.3f,
                "Kyle raises the civilian and earns an exclusive weapon");
            Check(demo.Stage.Rescue.gameObject.activeSelf && Mathf.Abs(wounded.transform.eulerAngles.z) < 1f,
                "The rescued civilian stays upright in the scene");
            Check(demo.Coins >= 35 && CampaignProgress.Active.rescues == 1, "Rescue reward is recorded");
            Position(player, refuge.Shortcut.position);
            yield return Tap(Key.E);
            Check(!demo.Map.InSideRoom && demo.Player.Position.x < 100f, "Evacuation shortcut rejoins the main route");
        }

        private IEnumerator VerifyLift()
        {
            var lift = FindAnyObjectByType<MovingLedge>();
            var deck = lift.GetComponent<BoxCollider2D>();
            var body = demo.Player.GetComponent<Rigidbody2D>();
            yield return Until(() => lift.VerticalSpeed > 0.4f, 3.5f, "Service lift starts moving up");
            float top = deck.bounds.max.y;
            Position(body, new Vector2(deck.bounds.center.x, top + demo.Player.Hurtbox.extents.y + 0.025f));
            yield return Until(() => demo.Player.Grounded && demo.Player.Position.y > top + 0.98f,
                0.9f, "Kyle remains grounded on the ascending lift");
            yield return Until(() => lift.VerticalSpeed < -0.4f, 3.5f, "Service lift starts moving down");
            Check(demo.Player.Grounded &&
                Mathf.Abs(demo.Player.Hurtbox.min.y - deck.bounds.max.y) < 0.17f,
                "Kyle keeps standing while the lift descends");
            var rig = demo.Player.GetComponentInChildren<CastRig>();
            Check(rig.FootPlanted(0) && rig.FootPlanted(1) &&
                Mathf.Abs(rig.FootPosition(0).y - deck.bounds.max.y) < 0.23f,
                "The skeletal ankles remain on the descending service lift");
            Check(GameObject.Find("Pasarela del elevador").GetComponent<BoxCollider2D>().bounds.max.y > 5.5f,
                "The upper end of the lift serves a catwalk");
        }

        private IEnumerator VerifyDistrictArena(int act)
        {
            var map = demo.Map;
            var room = Field<CampaignMap.Room>(map, "arena");
            var body = demo.Player.GetComponent<Rigidbody2D>();
            Position(body, map.ArenaEntrance);
            yield return TapXbox(new XboxState { Buttons = 1 << 1 });
            Check(map.InSideRoom && map.Fighting, "Xbox Down enters the combat district in Act " + (act + 1));
            var shutters = Field<GameObject[]>(map, "shutters");
            Check(shutters.All(shutter => shutter.activeSelf), "Laser shutters lock the encounter");
            int initialCoins = demo.Coins;
            if (act == 0)
            {
                demo.Player.FallIntoHazard();
                Check(!map.InSideRoom && !map.ArenaCleared && shutters.All(shutter => !shutter.activeSelf),
                    "A defeat resets the room and unlocks the route back in");
                Position(body, map.ArenaEntrance);
                yield return Tap(Key.E);
                Check(map.Fighting && !map.ArenaCleared, "The first wave restarts on reentry");
            }
            for (int i = 0; i < room.Waves.Length; i++)
            {
                var enemies = room.Waves[i].Enemies;
                yield return Until(() => enemies.All(enemy => enemy.Alive), 2f, $"Act {act + 1} wave {i + 1} deploys");
                Check(enemies.Length >= 6, "The wave contains a group of enemies");
                if (act == 0 && i == 0)
                {
                    Position(body, Field<Transform>(map, "cache").position);
                    yield return Until(() => demo.Weapon == Sidearm.Incinerator, 0.4f, "Kyle finds and equips a flamethrower in the metro");
                    var first = enemies[0];
                    var second = enemies[1];
                    Position(first.GetComponent<Rigidbody2D>(), demo.Player.Position + new Vector2(1.55f, 0f));
                    Position(second.GetComponent<Rigidbody2D>(), demo.Player.Position + new Vector2(2.48f, 0f));
                    demo.Player.Face(1);
                    int firstHealth = first.Health, secondHealth = second.Health, ammo = demo.Ammo;
                    QueueXbox(new XboxState { RightTrigger = byte.MaxValue });
                    yield return Until(() => first.Health < firstHealth && second.Health < secondHealth, 0.65f,
                        "A flame pulse damages multiple enemies");
                    yield return Frames(5);
                    CaptureScene("equipment-flamethrower");
                    yield return Until(() => demo.Ammo <= ammo - 2, 0.85f, "Holding RT sustains the flamethrower");
                    QueueXbox(default);
                }
                foreach (var enemy in enemies)
                    if (enemy.Alive)
                        enemy.Hit(99, 1, true, false);
                if (act == 2 && i == 0)
                {
                    yield return Until(() => CampaignProgress.Active.arenaWaves[act] == 1, 0.65f, "Canyon banks a checkpoint at wave two");
                    int savedCoins = demo.Coins;
                    demo.Player.FallIntoHazard();
                    Check(map.CanyonFight && demo.Coins == savedCoins && enemies.All(enemy => !enemy.Alive),
                        "Canyon retries only the current wave and retains the banked reward");
                    CaptureScene("story-canyons");
                }
            }
            yield return Until(() => map.ArenaCleared, 0.6f, "Cleared arena is saved");
            Check(shutters.All(shutter => !shutter.activeSelf) && demo.Coins > initialCoins,
                "Shutters open and the encounter pays its reward");
            if (act == 0)
            {
                var supply = FindObjectsByType<AmmoSupply>().First(item => item.name == "Combustible");
                demo.SpendShot();
                demo.Reload(demo.Weapon);
                int depleted = demo.ReserveAmmo;
                Position(body, supply.transform.position + Vector3.up * 0.56f);
                yield return Until(() => demo.ReserveAmmo > depleted, 0.6f, "The fuel sprite supplies the reserve");
            }
            Position(body, room.Exit.position);
            yield return Tap(Key.E);
            Check(!map.InSideRoom, "Arena exit returns to the original district");
        }

        private IEnumerator VerifyMedkits()
        {
            var player = demo.Player.GetComponent<Rigidbody2D>();
            Position(player, new Vector2(1.70f, 0.80f));
            int starting = demo.Medkits;
            const int enemyIndex = 6;
            demo.Effects.DropMedkit(demo.Player.Position, enemyIndex);
            Check(FindObjectsByType<SpriteRenderer>().Any(view => view.sprite && view.sprite.name == "PixelSupplyMedical"),
                "A dropped medkit uses the industrial case artwork");
            yield return Until(() => demo.Medkits == starting + 1, 0.5f, "Botiquín se guarda incluso con la vida llena");
            yield return Tap(Key.H);
            Check(demo.Medkits == starting + 1, "H conserva el botiquín si la vida está llena");
            demo.Player.TakeHit(demo.Enemies[enemyIndex], demo.Player.Position - Vector2.right, 1, parryable: false);
            int hurt = demo.Player.Health;
            var hud = FindAnyObjectByType<RuinsHud>();
            yield return Frames(3);
            float target = (float)hurt / demo.Player.CurrentMaxHealth;
            Check(Field<float>(hud, "displayedLife") > target, "Health display eases toward the new value");
            yield return TapXbox(new XboxState { Buttons = 1 << 0 });
            Check(demo.Medkits == starting && demo.Player.Health == hurt + 1, "Xbox D-pad Up uses a stored medkit");
            var effects = demo.Effects.GetComponentsInChildren<SpriteRenderer>();
            Check(effects.Any(view => view.sprite && view.sprite.name == "PixelCureCross") &&
                effects.Any(view => view.sprite && view.sprite.name == "PixelCureLoop") &&
                effects.Any(view => view.sprite && view.sprite.name == "PixelCureDrop"),
                "Healing emits the new green cross, loop and particles");
            FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
            CaptureScene("curacion-verde");
            Check(!CampaignProgress.CanClaimMedkit(enemyIndex), "La misma recogida no se duplica después de usarla");
        }

        private IEnumerator VerifyInventoryPauses()
        {
            yield return Tap(Key.I);
            Check(demo.InventoryOpen && !demo.Playing && Time.timeScale == 0f, "I opens the inventory and pauses simulation");
            Check(!demo.EquipSidearm(Sidearm.Legendary), "Unowned sidearm cannot be equipped");
            int ammo = demo.Ammo;
            Vector2 start = demo.Player.Position;
            Queue(Key.F, Key.D);
            yield return new WaitForSecondsRealtime(0.18f);
            Queue();
            Check(demo.Ammo == ammo && Vector2.Distance(start, demo.Player.Position) < 0.03f,
                "Movement and shooting remain paused with inventory open");
            yield return Tap(Key.I);
            Check(!demo.InventoryOpen && demo.Playing && Time.timeScale == 1f, "I closes the inventory and resumes");
            yield return Tap(Key.Escape);
            yield return Tap(Key.I);
            Check(demo.Paused && demo.InventoryOpen, "I opens inventory from the pause menu");
            yield return Tap(Key.Escape);
            Check(demo.Paused && !demo.InventoryOpen, "Escape closes inventory and restores pause");
            yield return Tap(Key.Escape);
            Check(demo.Playing, "Gameplay resumes from pause");
        }

        private IEnumerator VerifyEquip()
        {
            Sidearm rescued = demo.Weapon;
            int coins = demo.Coins;
            Check(CampaignProgress.Owns(rescued) && CampaignProgress.Owns(Sidearm.Service), "Both pistols are owned");
            Check(!demo.EquipSidearm(Sidearm.Service), "Weapon switching requires inventory");
            yield return Tap(Key.I);
            for (int presses = 0; presses < (int)rescued; presses++)
                yield return Tap(Key.UpArrow);
            yield return Tap(Key.Enter);
            Check(demo.Weapon == Sidearm.Service && demo.Coins == coins, "Keyboard selects and equips a different sidearm");
            yield return Tap(Key.I);
            Check(demo.Playing, "Inventory closes after equipping");
        }

        private IEnumerator VerifyLegacyLoadout()
        {
            PlayerPrefs.SetString("MoonShadow.Campaign.v1",
                "{\"act\":0,\"coins\":73,\"weapon\":2,\"checkpoints\":[0,0,0,0,0],\"defeated\":[0,0,0,0,0]}");
            CampaignProgress.ContinueGame();
            yield return StageReady(0);
            Check(demo.Weapon == Sidearm.Legendary && CampaignProgress.Owns(Sidearm.Service) &&
                CampaignProgress.Owns(Sidearm.Legendary) && demo.Coins == 73,
                "Older save retains the equipped weapon and original pistol");
            yield return VerifyLegendaryMouse();
        }

        private IEnumerator VerifyLegendaryMouse()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var body = demo.Player.GetComponent<Rigidbody2D>();
            Position(body, new Vector2(1.8f, 0.83f));
            FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
            yield return Frames(3);
            Vector2 screenAim = Camera.main.WorldToScreenPoint(demo.Player.Position + new Vector2(3f, 4f));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenAim, delta = new Vector2(15f, 12f) });
            yield return Frames(3);
            Queue(Key.F);
            yield return Until(() => FindObjectsByType<PlayerRound2D>().Any(round => Field<Vector2>(round, "direction").y > 0.70f),
                1.1f, "The mouse aims a legendary shot diagonally");
            Vector2 newAim = Camera.main.WorldToScreenPoint(demo.Player.Position + new Vector2(7f, 1.2f));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = newAim, delta = newAim - screenAim });
            yield return Until(() => FindObjectsByType<PlayerRound2D>().Any(round => Field<Vector2>(round, "direction").x > 0.95f),
                0.8f, "Holding F tracks a moving mouse target");
            yield return new WaitForSeconds(5.15f);
            Check(demo.Ammo == 0 && demo.ReserveAmmo == 0 && FindObjectsByType<PlayerRound2D>().Length > 0,
                "Legendary emitter still fires after more than thirty shots");
            Queue();
            yield return Tap(Key.R);
            Check(!demo.Player.Reloading, "Legendary emitter needs no reload");
            InputSystem.RemoveDevice(mouse);
        }

        private IEnumerator UnlockTerminal()
        {
            Position(demo.Player.GetComponent<Rigidbody2D>(), demo.Target);
            yield return Frames(7);
            Check(!demo.RelayActive && demo.Guards > 0, "Guards lock the terminal");
            foreach (var guard in demo.Enemies.Where(enemy => enemy.GuardsRelay && enemy.Alive))
                guard.Hit(99, 1, true, false);
            yield return Until(() => demo.RelayActive, 1f, "Defeating guards enables terminal");
        }

        private IEnumerator CheckShop()
        {
            int coins = demo.Coins;
            demo.TogglePause();
            Check(demo.Buy(0) && demo.Coins == coins - 65 && demo.BonusDamage == 1, "Shop buys and saves sword upgrade");
            Check(demo.Buy(4) && CampaignProgress.Owns(Sidearm.Shotgun) && demo.Weapon == Sidearm.Shotgun,
                "Shop sells and equips the military shotgun");
            Check(demo.Buy(8) && demo.Player.MaxStamina == 120f, "Shop increases maximum stamina");
            demo.TogglePause();
            int ammo = demo.Ammo;
            yield return Tap(Key.F);
            Check(demo.Ammo == ammo - 1, "Shotgun consumes one shell from its drum");
            demo.ToggleInventory();
            Check(demo.EquipSidearm(Sidearm.Service), "Inventory equips the original sidearm");
            demo.ToggleInventory();
            yield return Frames(5);
        }

        private IEnumerator TestBacktrack()
        {
            Position(demo.Player.GetComponent<Rigidbody2D>(), new Vector2(-1.45f, 0.83f));
            yield return Frames(6);
            yield return Tap(Key.Enter);
            yield return Until(() =>
            {
                demo = FindAnyObjectByType<RuinsDemo>();
                return demo && demo.Stage && demo.Stage.Act == 0 && demo.Playing;
            }, 5f, "Returning to Act I starts directly in gameplay");
            Check(Vector2.Distance(demo.Player.Position, demo.Target) is > 1.5f and < 3.1f &&
                Mathf.Abs(Camera.main.transform.position.x - demo.Player.Position.x) < 7.5f,
                "Kyle and the camera arrive together near the previous exit");
            Check(!demo.BossAlive && demo.Player.DoubleJumpUnlocked, "Backtracking preserves abilities and defeated boss");
            Position(demo.Player.GetComponent<Rigidbody2D>(), demo.Target);
            yield return Until(() => demo.Finished, 0.8f, "Revisited act still leads to next area");
            yield return Tap(Key.Enter);
            yield return StageReady(1);
            Check(demo.RelayActive && demo.Player.DoubleJumpUnlocked, "Returning restores Act II checkpoint");
            yield return new WaitForSeconds(0.65f);
        }

        private IEnumerator Cross(float x, float initialFloor, float landingX, float finalFloor, bool doubleJump,
            string name, float lateDashHeight = 0f)
        {
            int before = demo.Resets;
            var body = demo.Player.GetComponent<Rigidbody2D>();
            Position(body, new Vector2(x, initialFloor + 0.81f));
            yield return Until(() => demo.Player.Grounded, 0.6f, "Grounded before " + name);
            Queue(Key.D, Key.Space);
            yield return Until(() => demo.Player.Position.y > initialFloor + 2.14f, 0.7f, "Jump begins for " + name);
            if (doubleJump)
            {
                Queue(Key.D);
                yield return Frames(3);
                Queue(Key.D, Key.Space);
                float clearance = Mathf.Max(lateDashHeight, Mathf.Max(initialFloor + 3.25f, finalFloor + 0.85f));
                yield return Until(() => demo.Player.Position.y > clearance,
                    0.65f, "Double jump gains height for " + name);
            }
            Queue(Key.D, Key.Space, Key.LeftShift);
            yield return Until(() => demo.Player.Dashing, 0.65f, "Air dash begins for " + name);
            Queue(Key.D, Key.Space);
            yield return Until(() => demo.Player.Position.x >= landingX && demo.Player.Grounded && demo.Resets == before,
                2.6f, name);
            Queue();
            yield return Frames(6);
        }

        private IEnumerator VerifyBoss(int index)
        {
            var boss = demo.Stage.Boss;
            int maximum = new[] { 28, 35, 42, 49, 65 }[index];
            Check(boss.IsBoss && boss.Alive && boss.Health == maximum, "Act boss has its increased health");
            if (index == 4)
            {
                Check(demo.BossShielded && !boss.Hit(4, 1, true, false), "Mechs shield the lunar boss");
                var mech = demo.Stage.ActiveMech;
                Check(Field<bool>(mech, "veteran"), "Lunar guardian has its dedicated veteran moves configured");
                mech.ResetEnemy();
                Position(demo.Player.GetComponent<Rigidbody2D>(), mech.SpawnPosition - new Vector2(1.9f, 0.05f));
                yield return Until(() => mech.CurrentTechnique is EnemyBase2D.Technique.GuardianRake or
                    EnemyBase2D.Technique.GuardianRocket, 3.2f, "Lunar guardian uses a miniboss technique");
                while (demo.Stage.ActiveMech)
                    demo.Stage.ActiveMech.Hit(99, 1, true, false);
                Check(!demo.BossShielded, "Destroying both mechs exposes the lunar boss");
            }
            boss.ResetEnemy();
            Vector2 position = boss.SpawnPosition - new Vector2(1.86f, index == 2 ? 0.40f : 0.20f);
            Position(demo.Player.GetComponent<Rigidbody2D>(), position);
            yield return Until(() => demo.Player.Grounded, 0.65f, "Grounded near boss");
            if (!demo.BossReady)
                yield return VerifyBossDialogue(index);
            var signature = new[] { EnemyBase2D.Technique.Rupture, EnemyBase2D.Technique.Feint,
                EnemyBase2D.Technique.FireBreath, EnemyBase2D.Technique.Cannon, EnemyBase2D.Technique.Scan }[index];
            yield return Until(() => boss.CurrentTechnique == signature, 1.7f, boss.name + " prepares " + signature);
            var beam = index == 3
                ? FindObjectsByType<EnemyHazard>().First(hazard => Field<float>(hazard, "beamLength") > 0f)
                : null;
            float warned = beam ? beam.transform.eulerAngles.z : 0f;
            int health = boss.Health;
            yield return TapXbox(new XboxState { Buttons = 1 << 15 });
            yield return Until(() => boss.Health < health, 1.05f, "Xbox Y heavy attack damages " + boss.name);
            if (index == 3)
                yield return Until(() => beam && Mathf.Abs(Mathf.DeltaAngle(warned, beam.transform.eulerAngles.z)) > 6f,
                    1.25f, "Unit-Zero's cannon sweeps after the warning");
            var bar = boss.GetComponentInChildren<EnemyHealthBar>();
            var fill = bar.transform.Find("Indicador").GetComponent<SpriteRenderer>();
            yield return Frames(4);
            Check(fill.enabled && fill.bounds.size.x < 1.16f, "Boss health is shown overhead and shrinks on damage");
            if (index == 0)
            {
                yield return Until(() => Mathf.Abs(Camera.main.transform.position.x - demo.Player.Position.x) < 3f, 2f,
                    "Camera follows the player into Vane's arena");
                CaptureScene("xbox-vane-arena");
                Check(demo.Stage.EncounterGate.activeSelf, "Vane closes the steel gate after Kyle enters");
            }

            Check(!demo.Banter.Active && demo.Playing, "Boss damage does not launch dialogue midfight");

            if (index < 4)
            {
                var alternates = new[]
                {
                    (EnemyBase2D.Technique.MineArc, EnemyBase2D.Technique.VaneRush),
                    (EnemyBase2D.Technique.VanceNeedles, EnemyBase2D.Technique.VanceLock),
                    (EnemyBase2D.Technique.EmberRain, EnemyBase2D.Technique.TailSweep),
                    (EnemyBase2D.Technique.MissileSalvo, EnemyBase2D.Technique.OrbitalShock)
                };
                var pair = alternates[index];
                yield return WaitForTechnique(boss, pair.Item1);
                yield return WaitForTechnique(boss, pair.Item2);
                if (index == 2)
                {
                    var dragon = boss.GetComponentInChildren<CastRig>();
                    var before = (Vector2[])Field<Vector2[]>(dragon, "positions").Clone();
                    yield return Until(() => JointMoved(dragon, before, 15, 0.095f), 0.75f,
                        "Vulkano articulates the tip of his tail during the sweep");
                    CaptureScene("rig-vulkano-tail");
                }
            }

            if (index == 4)
            {
                yield return WaitForTechnique(boss, EnemyBase2D.Technique.MoonOrbs);
                yield return WaitForTechnique(boss, EnemyBase2D.Technique.MoonLattice);
                boss.Hit(boss.Health - maximum * 2 / 3, 1, true, false);
                Check(boss.Phase == 2, "Ojo enters phase two");
                yield return Until(() => boss.CurrentTechnique == EnemyBase2D.Technique.LaserGrid, 1.6f,
                    "Ojo starts its laser grid");
                CaptureScene("campaign-lunar");
                boss.Hit(boss.Health - maximum / 3, 1, true, false);
                Check(boss.Phase == 3 && boss.Alive, "Ojo enters phase three");
                yield return Until(() => boss.CurrentTechnique == EnemyBase2D.Technique.CrossCut, 1.6f,
                    "Ojo starts its cross cut");
            }
            boss.Hit(99, 1, true, false);
            Check(!boss.Alive && demo.Player.Health == demo.Player.CurrentMaxHealth &&
                CampaignProgress.Active.Checkpoint == 2, "Boss victory saves a checkpoint and heals Kyle");
            if (index == 0)
                Check(!demo.Stage.EncounterGate.activeSelf, "Defeating Vane opens the steel gate");
            yield return Frames(2);
            Check(!fill.gameObject.activeInHierarchy || !fill.enabled, "Defeated boss health bar disappears");
        }

        private IEnumerator VerifyBossDialogue(int act)
        {
            var speech = demo.Banter;
            var boss = demo.Stage.Boss;
            yield return Until(() => speech.Speaking && speech.VisibleCharacters > 0, 2.4f,
                "Dialogue starts at approach before combat in Act " + (act + 1));
            Check(!demo.Playing && Time.timeScale == 0f && !boss.Hit(10, 1, true, false) &&
                boss.Health == boss.MaximumHealth && speech.Line.Speaker == MoonStory.Encounters[act][0].Speaker,
                "Boss and player cannot fight during the dialogue");
            var hero = demo.Player.GetComponentInChildren<CastRig>();
            var opponent = boss.GetComponentInChildren<CastRig>();
            var hello = (Vector2[])Field<Vector2[]>(opponent, "positions").Clone();
            Check(hero.Visible && hero.Skin.name == "PixelKyleRestored" && hero.Skin.texture.filterMode == FilterMode.Point,
                "Kyle talks with the same point-filtered rig used in combat");
            yield return Until(() => JointMoved(opponent, hello, 3, 0.017f), 0.85f,
                "The boss gestures while the conversation waits for input");
            bool kyle = speech.IsKyle;
            if (act == 0)
            {
                string line = speech.Line.Text;
                yield return new WaitForSecondsRealtime(5.0f);
                Check(speech.Line.Text == line && speech.VisibleCharacters == line.Length && !demo.Playing,
                    "Dialogue waits indefinitely for user input after the text is fully readable");
                var mouse = InputSystem.AddDevice<Mouse>();
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
                yield return Frames(5);
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return Frames(5);
                Check(speech.Speaking && speech.Line.Text != line, "Left click advances the dialogue");
                InputSystem.RemoveDevice(mouse);
            }
            for (int i = 0; i < 8 && speech.IsKyle == kyle; i++)
            {
                if (act == 1)
                    yield return TapXbox(new XboxState { Buttons = 1 << 12 });
                else
                    yield return Tap(Key.Enter);
            }
            Check(!demo.Playing && speech.Speaking && speech.IsKyle != kyle,
                "The dialogue bubble switches between Kyle and the boss");
            if (act == 1)
            {
                FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
                CaptureScene("presion-encuentro");
            }
            yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            Check(demo.BossReady && demo.Playing && Time.timeScale > 0f && CampaignProgress.HeardBoss(act),
                "Closing the dialogue releases both fighters and remembers the introduction");
        }

        private IEnumerator WaitForTechnique(EnemyBase2D boss, EnemyBase2D.Technique technique)
        {
            var actor = demo.Player.GetComponent<Rigidbody2D>();
            float gap = technique is EnemyBase2D.Technique.EmberRain or EnemyBase2D.Technique.MissileSalvo ? 4.7f : 2.05f;
            yield return Until(() =>
            {
                if (boss.CurrentTechnique == technique)
                    return true;
                if (demo.Player.Health <= 3)
                    demo.Player.FullHeal();
                float separation = boss.Position.x - actor.position.x;
                if (separation < gap - 0.55f || separation > gap + 0.55f ||
                    Mathf.Abs(actor.position.y - boss.SpawnPosition.y) > 1.4f)
                    Position(actor, new Vector2(boss.Position.x - gap, boss.SpawnPosition.y - 0.18f));
                return false;
            }, 9.0f, $"{boss.name} also uses {technique}");
        }

        private IEnumerator VerifyResume()
        {
            int coins = demo.Coins;
            CampaignProgress.Menu();
            yield return Until(() => SceneManager.GetActiveScene().name == "Inicio", 4f, "Title reachable from campaign");
            CampaignProgress.ContinueGame();
            yield return StageReady(0);
            Check(!demo.BossAlive && demo.RelayActive && demo.Coins == coins && demo.BonusDamage == 1,
                "Continue restores checkpoint, coins, shop upgrade and boss defeat");
            Check(demo.Weapon == Sidearm.Service &&
                (CampaignProgress.Owns(Sidearm.Experimental) || CampaignProgress.Owns(Sidearm.Legendary)),
                "Continue preserves the selected secondary weapon and the rescued weapon");
        }

        private static void Position(Rigidbody2D body, Vector2 position)
        {
            body.transform.position = position;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).GetValue(owner);

        private static void CaptureScene(string name)
        {
            Camera camera = Camera.main;
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var original = RenderTexture.active;
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            var frame = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            frame.Apply();
            string output = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", name + ".png"));
            System.IO.File.WriteAllBytes(output, frame.EncodeToPNG());
            Destroy(frame);
            RenderTexture.active = original;
            RenderTexture.ReleaseTemporary(target);
        }

        private void Queue(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private void QueueXbox(XboxState state) => InputSystem.QueueStateEvent(xbox, state);

        private IEnumerator TapXbox(XboxState state)
        {
            QueueXbox(state);
            yield return Frames(5);
            QueueXbox(default);
            yield return Frames(5);
        }

        private IEnumerator Tap(Key key)
        {
            Queue(key);
            yield return Frames(5);
            Queue();
            yield return Frames(5);
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        private static IEnumerator Until(Func<bool> condition, float seconds, string name)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Check(condition(), name);
        }

        private static void Check(bool condition, string name)
        {
            if (condition)
                Debug.Log("PASS " + name);
            else
            {
                Debug.LogError("FAIL " + name);
                var world = FindAnyObjectByType<RuinsDemo>();
                if (world)
                    Debug.LogError($"Player {world.Player.Position} grounded={world.Player.Grounded} resets={world.Resets}");
                Application.Quit(1);
                throw new InvalidOperationException(name);
            }
        }
    }
}
