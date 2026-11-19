using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoonShadow
{
    public sealed class RuinsDemo : MonoBehaviour
    {
        [SerializeField] private KyleController2D player;
        [SerializeField] private EnemyBase2D[] enemies;
        [SerializeField] private RuinsEffects effects;
        [SerializeField] private Transform relay;
        [SerializeField] private Transform exit;
        [SerializeField] private SpriteRenderer relayLight;
        [SerializeField] private SpriteRenderer exitLight;
        [SerializeField] private CampaignStage stage;
        [SerializeField] private CampaignMap map;
        private EnemyBase2D[] sceneActors;

        private Vector2 checkpoint;
        private int checkpointCoins;
        private int checkpointEnemies;
        private int guardCount;
        private float impactUntil;
        private float messageUntil;
        private float visorUntil;
        private bool paused;
        private bool inventoryOpen;
        private bool finished;
        private bool briefing;
        private bool firstVisit;
        private bool relayActive;
        private bool bossIntroduced;
        private readonly BossBanter banter = new();
        private string message;
        private int resets;
        private float relayBumpAt;
        private bool returnHint;
        private float comboUntil;
        private int comboHits;
        private readonly Ammunition ammunition = new();
        private FirstActTips lessons;
        private CivilianAssistance civilian;
        private bool offerHelp;
        private int resumedFrame = -1;
        private float pageBeganAt;

        public KyleController2D Player => player;
        public EnemyBase2D[] Enemies => sceneActors ?? enemies;
        public RuinsEffects Effects => effects;
        public CampaignStage Stage => stage;
        public CampaignMap Map => map;
        public bool Playing => !paused && !inventoryOpen && !finished && !briefing && !banter.Active &&
            (!civilian || !civilian.Helping);
        public bool CivilianRescued => stage && (CampaignProgress.Active.rescues & (1 << stage.Act)) != 0;
        internal BossBanter Banter => banter;
        internal bool BossReady => stage && relayActive && !BossShielded && CampaignProgress.HeardBoss(stage.Act);
        internal bool ResumedThisFrame => resumedFrame == Time.frameCount;
        public bool Paused => paused;
        public bool InventoryOpen => inventoryOpen;
        public int Medkits => CampaignProgress.Active.medkits;
        public bool Finished => finished;
        public bool Briefing => briefing;
        public bool IntroPlaying => stage && stage.Opening && stage.Opening.Playing;
        public int StoryStep { get; private set; }
        public Texture2D StoryArt => !stage ? null : briefing && firstVisit && !stage.Opening ? stage.OpeningArt :
            finished ? (LiveEnding ? null : stage.Final && StoryStep >= 2 ? stage.DawnArt : stage.EndingArt) : null;
        public int StoryLength => finished && stage && stage.Final ? 4 : StoryArt && briefing ? 3 : 1;
        internal bool Credits => finished && stage && stage.Final && StoryStep == 3;
        internal bool LiveEnding => finished && stage && stage.Final && stage.Epilogue && StoryStep < 2;
        internal float PageAge => Time.unscaledTime - pageBeganAt;
        public bool RelayActive => relayActive;
        public float Shake { get; private set; }
        public int Coins { get; private set; }
        public int Resets => resets;
        public int Guards => guardCount;
        public int ComboHits => Time.time < comboUntil ? comboHits : 0;
        public bool VisorActive => VisorRemaining > 0f;
        public float VisorRemaining => Mathf.Max(0f, visorUntil - Time.time);
        public int Ammo => ammunition.Loaded(Weapon);
        public int MaxAmmo => SidearmCatalog.Get(Weapon).Magazine;
        public int ReserveAmmo => ammunition.Reserve(Weapon);
        public int ReserveCapacity => stage ? SidearmCatalog.BaseReserve + CampaignProgress.Active.ammo * 4 : 0;
        public Sidearm Weapon => stage ? (Sidearm)CampaignProgress.Active.weapon : Sidearm.Service;
        public int BonusDamage => stage ? CampaignProgress.Active.force : 0;
        public float BladeReach => CampaignProgress.Active.tacticalBlade ? 0.30f : 0f;
        public int BonusHealth => stage ? CampaignProgress.Active.health : 0;
        public float Gravity => stage ? stage.Gravity : 1f;
        public bool BossAlive => stage && stage.Boss && stage.Boss.Alive;
        public bool BossShielded => BossAlive && stage.ActiveMech;
        public bool ShowBoss => BossAlive && relayActive && Mathf.Abs(stage.Boss.Position.x - player.Position.x) < 12f;
        public Vector2 Target => map && map.InSideRoom ? map.Target :
            !relayActive ? relay.position : BossShielded ? stage.ActiveMech.transform.position :
            BossAlive ? stage.Boss.transform.position : exit.position;
        public string StageTitle => stage ? $"ACTO {stage.Act + 1}  /  {stage.Chapter.Name.ToUpperInvariant()}" :
            "ACTO I  /  RUINAS URBANAS";
        public string MessageText => Time.time < messageUntil ? message : string.Empty;
        internal string TutorialText => lessons?.Text(this);
        internal string ContextHint => offerHelp ? $"Ayudar a levantarse  ·  {player.Input.InteractKey}" :
            returnHint ? $"Volver al acto anterior  ·  {player.Input.InteractKey}" : null;
        public string GoalText => map && map.InSideRoom ? map.Goal : !relayActive
            ? guardCount > 0 ? $"Elimina a los guardianes   ·   {guardCount}" : "Activa la terminal"
            : BossShielded ? "Destruye los mecas guardianes" : BossAlive ? $"Derrota a {stage.Chapter.Boss}" :
                stage && stage.Final ? "Libera a Sofy" : "Llega a la salida";

        public void Configure(KyleController2D controller, EnemyBase2D[] foes, RuinsEffects feedback,
            Transform console, Transform extraction, SpriteRenderer consoleLight, SpriteRenderer extractionLight)
        {
            player = controller;
            enemies = foes;
            effects = feedback;
            relay = console;
            exit = extraction;
            relayLight = consoleLight;
            exitLight = extractionLight;
        }

        public void SetEnemies(EnemyBase2D[] foes) => enemies = foes;
        public void SetStage(CampaignStage chapter) => stage = chapter;
        public void SetMap(CampaignMap campaignMap) => map = campaignMap;
        private void Start()
        {
            checkpoint = player.Position;
            if (stage)
            {
                CampaignProgress.EnterAct(stage.Act);
                if (map)
                {
                    var roomActors = map.CollectActors();
                    sceneActors = new EnemyBase2D[enemies.Length + roomActors.Length];
                    enemies.CopyTo(sceneActors, 0);
                    roomActors.CopyTo(sceneActors, enemies.Length);
                    map.Initialize();
                }
                var save = CampaignProgress.Active;
                bool returning = CampaignProgress.TakeBacktrackArrival(stage.Act);
                firstVisit = save.Checkpoint == 0 && !returning;
                if (stage.Act == 0 && firstVisit)
                    lessons = new FirstActTips();
                Coins = checkpointCoins = save.coins;
                checkpointEnemies = save.StageKills;
                ammunition.Refill(ReserveCapacity);
                player.RestoreAbilities(save.furthest > 0 || save.checkpoints[0] > 0,
                    save.furthest > 1 || save.checkpoints[1] > 0);
                if (save.Checkpoint > 0)
                {
                    relayActive = true;
                    checkpoint = save.Checkpoint == 2 ? stage.SafeAfterBoss : RelayCheckpoint();
                    player.ResetAt(checkpoint);
                }
                ApplyEnemyCheckpoint(false);
                civilian = stage.Rescue.GetComponent<CivilianAssistance>();
                if (civilian)
                    civilian.Place(CivilianRescued);
                if (returning)
                {
                    checkpoint = (Vector2)exit.position + Vector2.left * 2.65f;
                    player.ResetAt(checkpoint);
                    EnterGameplay();
                }
                else
                {
                    briefing = true;
                    Time.timeScale = 0f;
                }
                FindAnyObjectByType<RuinsCamera>()?.SnapToPlayer();
                if (briefing && firstVisit && stage.Opening)
                    stage.Opening.Begin(EnterGameplay);
            }
            CountGuards();
        }

        private void Update()
        {
            var input = player.Input;
            bool back = input.Back.WasPressedThisFrame();
            if (civilian && civilian.Helping)
                return;
            if (banter.Active)
            {
                ReadBossDialogue(input, back);
                return;
            }
            if (briefing || finished)
            {
                if (input.Accept.WasPressedThisFrame() || (IntroPlaying && (input.Pause.WasPressedThisFrame() || back)))
                    AdvanceStory();
                return;
            }
            if (stage && input.Inventory.WasPressedThisFrame())
            {
                ToggleInventory();
                return;
            }
            if (inventoryOpen)
            {
                if (input.Pause.WasPressedThisFrame() || back)
                    ToggleInventory();
                return;
            }
            if (input.Restart.WasPressedThisFrame() && paused)
            {
                Restart();
                return;
            }
            if (input.Pause.WasPressedThisFrame() || (paused && back))
            {
                TogglePause();
                return;
            }
            if (!Playing)
                return;
            lessons?.Tick(this);

            if (player.Input.Heal.WasPressedThisFrame())
                UseMedkit();

            if (Time.unscaledTime >= impactUntil)
                Time.timeScale = 1f;
            Shake = Mathf.MoveTowards(Shake, 0f, Time.unscaledDeltaTime * 1.25f);
            if (player.Input.Visor.WasPressedThisFrame())
            {
                visorUntil = Time.time + 3.1f;
                effects.Burst(player.Position, new Color(0.3f, 0.72f, 1f), 15, 4f);
                effects.Sound(5);
            }

            if (player.Position.y < -4.4f)
                Respawn();

            offerHelp = false;
            if (map)
            {
                map.ReadFrame(input.Interact.WasPressedThisFrame());
                if (map.InSideRoom)
                {
                    TryAssist(map.CanRescue, input.Interact.WasPressedThisFrame());
                    return;
                }
            }

            if (stage && stage.Act > 0 && Near(new Vector2(-1.45f, 0.84f), 2.55f))
            {
                if (!returnHint)
                    Message("Puedes volver al acto anterior.", 2.2f);
                returnHint = true;
                if (input.Accept.WasPressedThisFrame() || input.Interact.WasPressedThisFrame())
                {
                    SaveSnapshot();
                    CampaignProgress.Backtrack(stage.Act, Coins);
                    return;
                }
            }
            else
                returnHint = false;

            if (!relayActive && (Near(relay.position, 1.85f) ||
                (stage && BossAlive && Mathf.Abs(stage.Boss.Position.x - player.Position.x) < 9f)))
            {
                CountGuards();
                if (guardCount == 0)
                    ActivateRelay();
                else if (Time.time >= relayBumpAt)
                {
                    Message($"Guardianes de la terminal pendientes: {guardCount}", 2f);
                    relayBumpAt = Time.time + 2f;
                }
            }

            if (stage && !map)
                TryAssist(true, input.Interact.WasPressedThisFrame());

            if (!bossIntroduced && ShowBoss)
            {
                bossIntroduced = true;
                if (stage.EncounterGate)
                {
                    stage.EncounterGate.SetActive(true);
                    effects.Burst(stage.EncounterGate.transform.position + Vector3.down * 3.4f,
                        new Color(0.99f, 0.70f, 0.39f), 20, 5.9f);
                    Impact(0.025f, 0.21f);
                }
                if (stage.Act == 2)
                    stage.Boss.DescendForEntrance();
                effects.IntroduceBoss(stage.Boss);
                Message(stage.BossLine, 3.4f);
            }
            if (ShowBoss && !BossReady && !BossShielded && stage.Boss.ReadyToTalk && player.Grounded &&
                Mathf.Abs(stage.Boss.Position.x - player.Position.x) < 5.8f &&
                Mathf.Abs(stage.Boss.Position.y - player.Position.y) < 1.7f)
            {
                BeginBossDialogue();
                return;
            }

            relayLight.color = relayActive ? new Color(0.39f, 1f, 0.86f) :
                guardCount == 0 ? new Color(0.34f, 0.82f, 1f) : new Color(1f, 0.46f, 0.33f);
            exitLight.color = relayActive && !BossAlive ? new Color(0.45f, 1f, 0.79f) : new Color(0.49f, 0.59f, 0.68f);
            if (relayActive && !BossAlive && Near(exit.position, 2.04f))
            {
                finished = true;
                pageBeganAt = Time.unscaledTime;
                if (stage && stage.Epilogue)
                    stage.Epilogue.Begin(player);
                Time.timeScale = 0f;
                effects.Sound(5);
            }
        }

        private bool Near(Vector2 position, float radiusSquared) =>
            Vector2.SqrMagnitude(player.Position - position) < radiusSquared;

        private Vector2 RelayCheckpoint() => (Vector2)relay.position + new Vector2(1.65f, 0f);

        private void ActivateRelay()
        {
            relayActive = true;
            checkpoint = RelayCheckpoint();
            if (!stage || stage.Act == 0)
                player.RestoreAbilities(true, player.DoubleJumpUnlocked);
            else if (stage.Act == 1)
                player.RestoreAbilities(true, true);
            player.FullHeal();
            ammunition.Refill(ReserveCapacity);
            effects.Burst(relay.position, new Color(0.30f, 0.98f, 1f), 32, 7f);
            effects.Sound(5);
            if (stage)
                CampaignProgress.Active.Checkpoint = 1;
            SaveSnapshot();
            Message(stage ? stage.RelayReward : "Impulso aéreo desbloqueado. Salta y usa el dash.", 5f);
        }

        private void CountGuards()
        {
            guardCount = 0;
            foreach (var enemy in enemies)
                if (enemy.Alive && enemy.GuardsRelay)
                    guardCount++;
        }

        public void EnemyDefeated(EnemyBase2D enemy)
        {
            bool boss = stage && enemy == stage.Boss;
            if (!map || !map.RecordEncounterKill(enemy))
                Coins += boss ? 80 : 10;
            int index = System.Array.IndexOf(Enemies, enemy);
            if (index >= enemies.Length)
                index = 32 + index - enemies.Length;
            if (!boss && CampaignProgress.CanClaimMedkit(index) && Random.value < 0.45f)
                effects.DropMedkit(enemy.Position, index);
            if (boss)
            {
                banter.Stop();
                effects.Explosion(enemy.Position, 2.8f);
                if (stage.EncounterGate)
                    stage.EncounterGate.SetActive(false);
                checkpoint = stage.SafeAfterBoss;
                CampaignProgress.Active.Checkpoint = 2;
                player.FullHeal();
                ammunition.Refill(ReserveCapacity);
                SaveSnapshot();
                effects.Sound(5);
                Message(stage.Chapter.Defeat, 4.1f);
            }
            CountGuards();
            if (enemy.GuardsRelay && guardCount == 0 && !relayActive)
                Message("Terminal habilitada. Acércate para activarla.", 3.6f);
        }

        private void TryAssist(bool safe, bool interact)
        {
            if (!safe || CivilianRescued || !civilian || !Near(stage.Rescue.position, 2.1f))
                return;
            offerHelp = true;
            if (!interact)
                return;
            offerHelp = false;
            civilian.Begin(player, RescueCivilian);
        }

        private void RescueCivilian()
        {
            if (CivilianRescued)
                return;
            var save = CampaignProgress.Active;
            save.rescues |= 1 << stage.Act;
            Sidearm reward = Random.value < 0.40f ? Sidearm.Legendary : Sidearm.Experimental;
            bool hadWeapon = CampaignProgress.Owns(reward);
            CampaignProgress.Grant(reward);
            Coins += 35 + (map ? map.TakeRescueRewards() : 0);
            ammunition.Refill(ReserveCapacity);
            player.FullHeal();
            SaveSnapshot();
            effects.Sound(5);
            Message($"{stage.CivilianName} a salvo. " + (hadWeapon ? "Equipo y munición repuestos." :
                reward == Sidearm.Legendary ? "Emisor legendario obtenido." : "Pistola experimental obtenida."), 4.5f);
        }

        public bool SpendShot() => stage && ammunition.Spend(Weapon);
        private Sidearm SupplyTarget()
        {
            if (SidearmCatalog.Get(Weapon).UsesAmmo)
                return Weapon;
            Sidearm choice = Sidearm.Service;
            for (int i = 0; i < SidearmCatalog.Count; i++)
            {
                var candidate = (Sidearm)i;
                if (SidearmCatalog.Get(candidate).UsesAmmo && CampaignProgress.Owns(candidate) &&
                    ammunition.Reserve(candidate) < ammunition.Reserve(choice))
                    choice = candidate;
            }
            return choice;
        }

        public int Restock(int amount) => ammunition.Supply(SupplyTarget(), amount, ReserveCapacity);
        internal void Reload(Sidearm weapon) => ammunition.Reload(weapon);
        internal int Loaded(Sidearm weapon) => ammunition.Loaded(weapon);

        public void FindWeapon(Sidearm weapon)
        {
            CampaignProgress.Grant(weapon);
            ammunition.FillMagazine(weapon);
            Restock(ReserveCapacity);
            SaveSnapshot();
            effects.Sound(5);
            Message(SidearmCatalog.Get(weapon).Name + " obtenido  ·  F / RT", 4f);
        }

        public void CompleteArena(int reward)
        {
            CampaignProgress.Active.arenas |= 1 << stage.Act;
            Coins += reward + 20;
            ammunition.Refill(ReserveCapacity);
            SaveSnapshot();
            effects.Sound(5);
            Message("Zona asegurada. Las compuertas están abiertas.", 3.8f);
        }

        internal void BankCanyonWave(int nextWave, int reward)
        {
            CampaignProgress.Active.arenaWaves[stage.Act] = nextWave;
            Coins += reward;
            player.Heal();
            int gained = Restock(7);
            SaveSnapshot();
            string supply = gained > 0 ? $"  ·  +{gained} de reserva" : "";
            Message($"Oleada {nextWave + 1}  ·  Punto de control{supply}", 2.5f);
        }

        private void UseMedkit()
        {
            if (player.Health == player.CurrentMaxHealth)
            {
                Message("Ya tienes la vida llena.", 1.4f);
                return;
            }

            if (!CampaignProgress.SpendMedkit())
            {
                Message("No tienes botiquines.", 1.4f);
                return;
            }

            player.Heal();
            effects.RestoreHealth(player.Position, player.Hurtbox.min.y);
            effects.Sound(5);
        }

        public void ToggleInventory()
        {
            if (!stage || briefing || finished)
                return;
            inventoryOpen = !inventoryOpen;
            if (!inventoryOpen && !paused)
                resumedFrame = Time.frameCount;
            Time.timeScale = inventoryOpen || paused ? 0f : 1f;
            impactUntil = 0f;
        }

        public bool EquipSidearm(Sidearm weapon)
        {
            if (!inventoryOpen || !CampaignProgress.Equip(weapon))
                return false;
            player.CancelReload();
            return true;
        }

        public bool Buy(int index)
        {
            if (!stage || !paused || inventoryOpen)
                return false;
            var save = CampaignProgress.Active;
            int cost = ShopCost(index);
            if (cost < 0 || Coins < cost || (index == 6 && ammunition.Reserve(SupplyTarget()) >= ReserveCapacity))
                return false;
            Coins -= cost;
            switch (index)
            {
                case 0: save.force++; break;
                case 1: save.health++; player.FullHeal(); break;
                case 2: save.ammo++; Restock(4); break;
                case 3: player.FullHeal(); ammunition.Refill(ReserveCapacity); break;
                case 4: CampaignProgress.Grant(Sidearm.Shotgun); ammunition.FillMagazine(Sidearm.Shotgun); break;
                case 5: save.tacticalBlade = true; break;
                case 6: Restock(ReserveCapacity); break;
                case 7: save.medkits++; break;
                case 8: save.stamina++; break;
                case 9: save.speed++; break;
            }
            SaveSnapshot();
            return true;
        }

        public static int ShopCost(int index) => index switch
        {
            0 => CampaignProgress.Active.force < 2 ? 65 : -1,
            1 => CampaignProgress.Active.health < 3 ? 45 : -1,
            2 => CampaignProgress.Active.ammo < 3 ? 35 : -1,
            3 => 20,
            4 => CampaignProgress.Owns(Sidearm.Shotgun) ? -1 : 85,
            5 => CampaignProgress.Active.tacticalBlade ? -1 : 90,
            6 => 12,
            7 => 16,
            8 => CampaignProgress.Active.stamina < 3 ? 45 + CampaignProgress.Active.stamina * 15 : -1,
            9 => CampaignProgress.Active.speed < 2 ? 55 + CampaignProgress.Active.speed * 20 : -1,
            _ => -1
        };

        private void SaveSnapshot()
        {
            checkpointCoins = Coins;
            checkpointEnemies = 0;
            for (int i = 0; i < enemies.Length; i++)
                if (!enemies[i].Alive)
                    checkpointEnemies |= 1 << i;
            if (!stage)
                return;

            CampaignProgress.Active.StageKills = checkpointEnemies;
            CampaignProgress.SaveRewards(Coins);
        }

        private void ApplyEnemyCheckpoint(bool resetLiving)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if ((checkpointEnemies & (1 << i)) != 0 ||
                    (relayActive && enemy.SpawnPosition.x < checkpoint.x - 0.25f))
                    enemy.HideEnemy();
                else if (resetLiving)
                    enemy.ResetEnemy();
            }
        }

        public void Respawn()
        {
            Time.timeScale = 1f;
            impactUntil = 0f;
            Shake = 0f;
            resets++;
            effects.ClearMedkits();
            effects.ClearBolts();
            ResetHits();
            if (map && map.RetryCanyonWave(out Vector2 arenaSpawn))
            {
                player.ResetAt(arenaSpawn);
                Coins = checkpointCoins;
                ammunition.Refill(ReserveCapacity);
                Message("Cañones y Nidos  ·  Reintento desde la última oleada", 2.6f);
                return;
            }
            if (map)
                map.ResetAfterDeath();
            if (stage && stage.EncounterGate && BossAlive)
            {
                stage.EncounterGate.SetActive(false);
                bossIntroduced = false;
            }
            player.ResetAt(checkpoint);
            ApplyEnemyCheckpoint(true);
            Coins = checkpointCoins;
            ammunition.Refill(ReserveCapacity);
            CountGuards();
            Message("Punto de control", 1.7f);
        }

        public void AdvanceStory()
        {
            if (IntroPlaying)
            {
                stage.Opening.Finish();
                return;
            }
            if ((briefing || finished) && StoryStep + 1 < StoryLength)
            {
                StoryStep++;
                pageBeganAt = Time.unscaledTime;
                if (Credits)
                    CampaignProgress.MarkCompleted(Coins);
                return;
            }
            StoryStep = 0;
            if (briefing)
                EnterGameplay();
            else if (finished && stage)
                CampaignProgress.Advance(stage.Act, Coins);
            else if (finished)
                Restart();
        }

        private void BeginBossDialogue()
        {
            effects.ClearBolts();
            effects.ClearTransientEffects();
            player.HoldForScene();
            stage.Boss.HoldForDialogue();
            Shake = impactUntil = 0f;
            Time.timeScale = 0f;
            banter.Schedule(MoonStory.Encounters[stage.Act]);
        }

        private void ReadBossDialogue(KyleInput input, bool skip)
        {
            float delta = Time.unscaledDeltaTime;
            if (skip || input.Pause.WasPressedThisFrame())
                banter.Stop();
            else
                banter.Tick(delta, input.DialogueAdvance.WasPressedThisFrame());
            if (!banter.Active)
            {
                CampaignProgress.MarkBossHeard(stage.Act);
                player.EndDialogueGesture();
                stage.Boss.FaceAfterDialogue();
                resumedFrame = Time.frameCount;
                Time.timeScale = 1f;
                return;
            }
            int direction = stage.Boss.Position.x < player.Position.x ? -1 : 1;
            player.DialogueGesture(direction, delta, banter.Gesture, banter.Speaking && banter.IsKyle);
            stage.Boss.DialogueGesture(-direction, delta, banter.Gesture, banter.Speaking && !banter.IsKyle);
        }

        private void EnterGameplay()
        {
            briefing = false;
            resumedFrame = Time.frameCount;
            Time.timeScale = 1f;
            player.FullHeal();
            Message(GoalText, 3.5f);
        }

        public string StoryLine()
        {
            if (finished && stage && stage.Final)
                return StoryStep < MoonStory.Ending.Length
                    ? $"{MoonStory.Ending[StoryStep].Speaker}  ·  {MoonStory.Ending[StoryStep].Text}"
                    : StoryStep == 2 ? "El escudo se desintegra. El amanecer vuelve a la Tierra." : "";
            if (!StoryArt)
                return briefing ? stage.Chapter.Opening : stage.Chapter.Defeat;
            if (briefing)
                return StoryStep switch
                {
                    0 => "Abrimos puertas que debían permanecer cerradas. Las bestias reclamaron la tierra y nuestras propias máquinas reclamaron el cielo.",
                    1 => "Sofy  ·  ¡Kyle! ¡No me sueltes!",
                    _ => "Kyle  ·  ¡Sofy, resiste! Voy a encontrarte, aunque tenga que llegar hasta la Luna."
                };
            return stage.Chapter.Defeat;
        }

        public bool CanCommitMelee(EnemyBase2D candidate)
        {
            if (stage && candidate == stage.Boss)
                return BossReady;
            int capacity = map && map.Fighting ? (stage.Act <= 1 ? 2 : 3) : 2;
            foreach (var enemy in Enemies)
                if (enemy != candidate && enemy.Alive && enemy.CommittedMelee &&
                    Mathf.Abs(enemy.Position.x - candidate.Position.x) < 12f && --capacity == 0)
                    return false;
            return true;
        }

        internal bool CanCommitRanged(EnemyBase2D candidate)
        {
            if (!map || !map.CanyonFight || candidate.IsBoss || !map.OnArenaGround(candidate.Position.x))
                return true;
            foreach (var enemy in Enemies)
                if (enemy != candidate && enemy.Alive && enemy.CommittedRanged && map.OnArenaGround(enemy.Position.x))
                    return false;
            return true;
        }

        public void RecordHits(int count)
        {
            comboHits = ComboHits + count;
            comboUntil = Time.time + 1.55f;
        }

        public void ResetHits()
        {
            comboHits = 0;
            comboUntil = 0f;
        }

        public void Impact(float length, float shake)
        {
            if (!Playing)
                return;
            impactUntil = Mathf.Max(impactUntil, Time.unscaledTime + length);
            Shake = Mathf.Max(Shake, shake);
            Time.timeScale = Mathf.Min(Time.timeScale, 0.19f);
        }

        public void Message(string value, float seconds)
        {
            message = value;
            messageUntil = Time.time + seconds;
        }

        public void TogglePause()
        {
            if (finished || briefing || inventoryOpen)
                return;
            paused = !paused;
            if (!paused)
                resumedFrame = Time.frameCount;
            Time.timeScale = paused ? 0f : 1f;
            impactUntil = 0f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private void OnDestroy() => Time.timeScale = 1f;
    }
}
