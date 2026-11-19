using System;
using UnityEngine;

namespace MoonShadow
{
    public sealed class CampaignMap : MonoBehaviour
    {
        [Serializable]
        public sealed class Wave
        {
            public EnemyBase2D[] Enemies;
        }

        [Serializable]
        public sealed class Room
        {
            public string Title;
            public float Left, Right, Ceiling;
            public Transform Entrance, Exit, Shortcut;
            public Transform MainDoor, MainArrival;
            public Wave[] Waves;
        }

        [SerializeField] private RuinsDemo world;
        [SerializeField] private RuinsCamera cameraRig;
        [SerializeField] private string district, finale;
        [SerializeField] private float finaleStarts;
        [SerializeField] private Room arena, refuge;
        [SerializeField] private GameObject[] shutters;
        [SerializeField] private Transform cache;
        [SerializeField] private Transform cableStart, cableEnd;
        [SerializeField] private SpriteRenderer tether;
        [SerializeField] private Transform refugeReturn;
        private Room room;
        private int wave;
        private float nextWave;
        private float traversal;
        private int waveRewards;
        private int rescueRewards;
        private bool refugeEntered;

        public bool InSideRoom => room != null;
        public bool Indoors => room != null && (room == arena ? world.Stage.Act != 4 : world.Stage.Act != 0);
        public bool ArenaCleared => (CampaignProgress.Active.arenas & (1 << world.Stage.Act)) != 0;
        public bool CanRescue => refugeEntered && Remaining(refuge.Waves[0]) == 0;
        public bool Fighting => room == arena && !ArenaCleared;
        public bool CanyonFight => Fighting && world.Stage.Act == 2;
        internal bool OnArenaGround(float x) => Fighting && x > arena.Left && x < arena.Right;
        public Vector2 ArenaEntrance => arena.MainDoor.position;
        public string Area => room?.Title ?? (world.Player.Position.x >= finaleStarts ? finale : district);
        public string Hint { get; private set; }
        public Vector2 Target => room == arena
            ? (ArenaCleared ? arena.Exit.position : arena.Entrance.position)
            : world.Stage.Rescue.position;
        public string Goal => room == arena
            ? ArenaCleared ? "Vuelve a la ruta principal" : nextWave > 0f ? $"Enemigos en camino  ·  {wave + 1}/{arena.Waves.Length}" :
                $"Oleada {wave + 1}/{arena.Waves.Length}  ·  {Remaining(arena.Waves[wave])} hostiles"
            : world.CivilianRescued ? "Explora o toma el atajo" : CanRescue ? "Ayuda al civil herido" : "Rescata al civil de esta zona";

        public void Configure(RuinsDemo campaign, RuinsCamera follow, string first, string last, float lastStarts,
            Room arenaRoom, Room rescueRoom, GameObject[] barriers, Transform weapon, Transform grabPoint,
            Transform landing, SpriteRenderer cable, Transform returnPoint)
        {
            world = campaign;
            cameraRig = follow;
            district = first;
            finale = last;
            finaleStarts = lastStarts;
            arena = arenaRoom;
            refuge = rescueRoom;
            shutters = barriers;
            cache = weapon;
            cableStart = grabPoint;
            cableEnd = landing;
            tether = cable;
            refugeReturn = returnPoint;
        }

        public EnemyBase2D[] CollectActors()
        {
            var enemies = new System.Collections.Generic.List<EnemyBase2D>();
            var added = new System.Collections.Generic.List<EnemyBase2D>();
            foreach (var group in arena.Waves)
                AppendStable(group);
            AppendStable(refuge.Waves[0]);
            enemies.AddRange(added);
            return enemies.ToArray();

            void AppendStable(Wave group)
            {
                // Legacy drops are indexed by this order in existing saves.
                foreach (var enemy in group.Enemies)
                    (enemy.IsReinforcement ? added : enemies).Add(enemy);
            }
        }

        public void AddReinforcements(EnemyBase2D[][] waveSupport, EnemyBase2D[] refugeSupport)
        {
            for (int wave = 0; wave < arena.Waves.Length; wave++)
                Append(arena.Waves[wave], waveSupport[wave]);
            Append(refuge.Waves[0], refugeSupport);

            static void Append(Wave group, EnemyBase2D[] squad)
            {
                int previous = group.Enemies.Length;
                Array.Resize(ref group.Enemies, previous + squad.Length);
                squad.CopyTo(group.Enemies, previous);
            }
        }

        public void Initialize()
        {
            if (cache)
                cache.gameObject.SetActive(!CampaignProgress.Owns(Sidearm.Incinerator));
        }

        public void ReadFrame(bool interact)
        {
            Hint = null;
            if (room == null)
            {
                if (At(arena.MainDoor, arena.Title, interact))
                    Enter(arena);
                else if (At(refuge.MainDoor, refuge.Title, interact))
                    Enter(refuge);
                return;
            }

            if (room == arena)
            {
                AdvanceWaves();
                if (cache && cache.gameObject.activeSelf && Nearby(cache, 1.4f))
                {
                    cache.gameObject.SetActive(false);
                    world.FindWeapon(Sidearm.Incinerator);
                }
            }
            if (room == refuge)
            {
                if (traversal > 0f)
                {
                    AdvanceCable();
                    return;
                }
                if (At(cableStart, "Engancharse al cable", interact))
                {
                    traversal = 0.001f;
                    tether.enabled = true;
                    return;
                }
            }

            if (Fighting)
                return;
            if (At(room.Exit, "Volver a la ruta principal", interact))
            {
                Leave(room.MainArrival.position);
                return;
            }
            if ((room != refuge || CanRescue) && At(room.Shortcut, "Tomar el atajo", interact))
                Leave(refugeReturn.position);
        }

        private bool At(Transform point, string label, bool interact)
        {
            if (!Nearby(point, 1.28f))
                return false;
            Hint = $"{label}   ·   {world.Player.Input.InteractKey}";
            return interact;
        }

        private bool Nearby(Transform point, float radius) => point &&
            Vector2.SqrMagnitude(world.Player.Position - (Vector2)point.position) < radius * radius;

        private void Enter(Room next)
        {
            room = next;
            Warp(next.Entrance.position);
            cameraRig.FocusRoom(next.Left, next.Right, next.Ceiling, next == arena);
            world.Message(next.Title, 2.5f);
            if (next == arena && !ArenaCleared)
                StartAtRecordedWave(0.8f);
            else if (next == refuge && !refugeEntered && (CampaignProgress.Active.rescues & (1 << world.Stage.Act)) == 0)
            {
                refugeEntered = true;
                Activate(refuge.Waves[0]);
            }
            else if (next == refuge)
                refugeEntered = true;
        }

        private void Warp(Vector2 destination)
        {
            world.Player.WarpTo(destination);
            world.Effects.ClearBolts();
            world.ResetHits();
        }

        private void Leave(Vector2 destination)
        {
            room = null;
            Warp(destination);
            cameraRig.FocusMain();
            world.Message(Area, 2.3f);
        }

        public void ResetAfterDeath()
        {
            room = null;
            nextWave = 0f;
            waveRewards = 0;
            traversal = 0f;
            tether.enabled = false;
            SetShutters(false);
            foreach (var sequence in arena.Waves)
            foreach (var enemy in sequence.Enemies)
                enemy.HideEnemy();
            if ((CampaignProgress.Active.rescues & (1 << world.Stage.Act)) == 0)
            {
                rescueRewards = 0;
                refugeEntered = false;
                foreach (var enemy in refuge.Waves[0].Enemies)
                    enemy.HideEnemy();
            }
            cameraRig.FocusMain();
        }

        public bool RetryCanyonWave(out Vector2 spawn)
        {
            spawn = arena.Entrance.position;
            if (!CanyonFight || CampaignProgress.Active.arenaWaves[world.Stage.Act] == 0)
                return false;

            foreach (var sequence in arena.Waves)
            foreach (var enemy in sequence.Enemies)
                enemy.HideEnemy();
            StartAtRecordedWave(1.3f);
            return true;
        }

        private void StartAtRecordedWave(float delay)
        {
            wave = Mathf.Clamp(CampaignProgress.Active.arenaWaves[world.Stage.Act], 0, arena.Waves.Length - 1);
            waveRewards = 0;
            nextWave = Time.time + delay;
            SetShutters(true);
            WarnWave(arena.Waves[wave]);
        }

        private void AdvanceWaves()
        {
            if (ArenaCleared)
                return;
            if (nextWave > 0f)
            {
                if (Time.time >= nextWave)
                {
                    nextWave = 0f;
                    Activate(arena.Waves[wave]);
                }
                return;
            }
            if (Remaining(arena.Waves[wave]) > 0)
                return;
            if (wave == arena.Waves.Length - 1)
            {
                SetShutters(false);
                world.CompleteArena(waveRewards);
                return;
            }
            wave++;
            if (CanyonFight)
            {
                world.BankCanyonWave(wave, waveRewards);
                waveRewards = 0;
            }
            else
            {
                int gained = world.Restock(5);
                world.Message(gained > 0 ? $"Oleada {wave + 1}  ·  +{gained} de reserva" : $"Oleada {wave + 1}", 1.8f);
            }
            nextWave = Time.time + 1.15f;
            WarnWave(arena.Waves[wave]);
        }

        public bool RecordEncounterKill(EnemyBase2D enemy)
        {
            if (Fighting && Array.IndexOf(arena.Waves[wave].Enemies, enemy) >= 0)
            {
                waveRewards += 10;
                return true;
            }
            if (room == refuge && Array.IndexOf(refuge.Waves[0].Enemies, enemy) >= 0)
            {
                rescueRewards += 10;
                return true;
            }
            return false;
        }

        public int TakeRescueRewards()
        {
            int reward = rescueRewards;
            rescueRewards = 0;
            return reward;
        }

        private void WarnWave(Wave group)
        {
            foreach (var actor in group.Enemies)
                world.Effects.Cue(actor.transform.position, new Color(1f, 0.56f, 0.29f), 0.70f, 0.41f);
        }

        private static void Activate(Wave group)
        {
            foreach (var actor in group.Enemies)
            {
                actor.gameObject.SetActive(true);
                actor.ResetEnemy();
            }
        }

        private static int Remaining(Wave group)
        {
            int count = 0;
            foreach (var enemy in group.Enemies)
                if (enemy.Alive)
                    count++;
            return count;
        }

        private void SetShutters(bool locked)
        {
            foreach (var barrier in shutters)
                barrier.SetActive(locked);
        }

        private Vector2 ropeFrom;

        private void AdvanceCable()
        {
            if (traversal < 0.002f)
                ropeFrom = world.Player.Position;
            traversal += Time.deltaTime * 1.8f;
            Vector2 landing = cableEnd.position;
            Vector2 next = Vector2.Lerp(ropeFrom, landing, Mathf.SmoothStep(0f, 1f, traversal));
            world.Player.WarpTo(next);
            Vector2 delta = landing - next;
            tether.transform.position = (landing + next) * 0.5f;
            tether.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            tether.transform.localScale = new Vector3(delta.magnitude / tether.sprite.bounds.size.x, 1f, 1f);
            if (traversal >= 1f)
            {
                traversal = 0f;
                tether.enabled = false;
            }
        }
    }
}
