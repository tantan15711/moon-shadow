using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoonShadow
{
    public static class CampaignProgress
    {
        [Serializable]
        public sealed class SaveData
        {
            public int act;
            public int coins;
            public int force;
            public int health;
            public int ammo;
            public int stamina;
            public int speed;
            public bool tacticalBlade;
            public int rescues;
            public int weapon;
            public int weapons;
            public int medkits;
            public int[] medicalPickups = new int[5];
            public List<int> extraMedicalPickups = new();
            public int arenas;
            public int bossIntroductions;
            public int[] arenaWaves = new int[5];
            public int furthest;
            public int[] checkpoints = new int[5];
            public int[] defeated = new int[5];
            public bool completed;

            public int Checkpoint { get => checkpoints[act]; set => checkpoints[act] = value; }
            public int StageKills { get => defeated[act]; set => defeated[act] = value; }
        }

        public readonly struct Chapter
        {
            public readonly string Scene, Name, Boss, Opening, Defeat;

            public Chapter(string scene, string name, string boss, string opening, string defeat)
            {
                Scene = scene;
                Name = name;
                Boss = boss;
                Opening = opening;
                Defeat = defeat;
            }
        }

        public static readonly Chapter[] Chapters =
        {
            new("RuinasUrbanas", "Ruinas urbanas", "General Vane",
                "Las máquinas secuestraron a Sofy. La nave despegó rumbo a la Luna. Kyle sigue su rastro hasta un búnker en las ruinas.",
                "Vane cae. La compuerta abre el paso a los laboratorios que estudiaron la plaga."),
            new("Catacumbas", "Subterráneos góticos", "Dra. Elena Vance",
                "Bajo la ciudad, los laboratorios de Vance siguen en marcha. Su ascensor geotérmico es la única salida hacia la cordillera.",
                "Vance deja atrás los registros del primer portal. Kyle activa el ascensor hacia las cumbres."),
            new("Cumbres", "Cumbres volcánicas", "Vulkano-0",
                "Los portales deformaron la cima. Un dragón biomecánico vigila la ruta a la estación aeroespacial.",
                "Vulkano-0 baja la cabeza al apagarse su núcleo. Kyle cruza hacia la estación."),
            new("EstacionOrbital", "Estación de lanzamiento", "Comandante Unit-Zero",
                "La milicia abandonó un transbordador en la plataforma. La cuenta regresiva sigue en manos de la defensa automatizada.",
                "La plataforma queda libre. Kyle roba el transbordador y despega rumbo a la Luna."),
            new("SantuarioLunar", "Santuario de acero", "El Ojo de Acero",
                "El Ojo mantiene a Sofy en una cámara de digitalización. Desde el núcleo también controla el escudo que oculta el sol.",
                "Kyle apaga el escudo orbital y abre la cámara de Sofy. En la Tierra vuelve a amanecer.")
        };

        private const string SaveKey = "MoonShadow.Campaign.v1";
        private static SaveData active;
        private static int backwardsDestination = -1;
        public static bool HasSave => ReadSave() != null;
        public static int SavedAct => ReadSave()?.act ?? 0;
        public static bool SavedCompleted => ReadSave()?.completed ?? false;
        public static SaveData Active => active ??= new SaveData();

        private static SaveData ReadSave()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return null;
            try
            {
                var save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (save == null || save.act < 0 || save.act >= Chapters.Length ||
                    save.checkpoints?.Length != Chapters.Length || save.defeated?.Length != Chapters.Length)
                    return null;
                if (!SidearmCatalog.IsValid((Sidearm)save.weapon))
                    save.weapon = 0;
                save.weapons |= 1 | 1 << save.weapon;
                if (save.medicalPickups == null || save.medicalPickups.Length != Chapters.Length)
                    save.medicalPickups = new int[Chapters.Length];
                save.extraMedicalPickups ??= new List<int>();
                if (save.arenaWaves == null || save.arenaWaves.Length != Chapters.Length)
                    save.arenaWaves = new int[Chapters.Length];
                save.medkits = Mathf.Max(0, save.medkits);
                return save;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void Write(SaveData snapshot = null)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(snapshot ?? Active));
            PlayerPrefs.Save();
        }

        public static bool Owns(Sidearm weapon) => SidearmCatalog.IsValid(weapon) &&
            (weapon == Sidearm.Service || (Active.weapons & (1 << (int)weapon)) != 0);

        internal static bool HeardBoss(int act) => (Active.bossIntroductions & (1 << act)) != 0;

        internal static void MarkBossHeard(int act)
        {
            int bit = 1 << act;
            Active.bossIntroductions |= bit;
            var saved = ReadSave();
            if (saved == null || saved.act != act)
                return;
            saved.bossIntroductions |= bit;
            Write(saved);
        }

        public static void Grant(Sidearm weapon)
        {
            var save = Active;
            if (!SidearmCatalog.IsValid(weapon) || Owns(weapon))
                return;
            int flag = 1 << (int)weapon;
            save.weapon = (int)weapon;
            save.weapons |= flag | 1;
        }

        public static bool Equip(Sidearm weapon)
        {
            if (!Owns(weapon))
                return false;
            Active.weapon = (int)weapon;
            // Update the loadout without turning the current encounter into a checkpoint.
            var saved = ReadSave();
            if (saved != null && saved.act == Active.act)
            {
                saved.weapon = Active.weapon;
                saved.weapons = Active.weapons | 1;
                Write(saved);
            }
            return true;
        }

        public static bool CanClaimMedkit(int enemyIndex)
        {
            if (enemyIndex < 0)
                return false;
            return enemyIndex < 32 ? (Active.medicalPickups[Active.act] & (1 << enemyIndex)) == 0 :
                !Active.extraMedicalPickups.Contains(Active.act * 256 + enemyIndex);
        }

        public static bool ClaimMedkit(int enemyIndex)
        {
            if (!CanClaimMedkit(enemyIndex))
                return false;

            if (enemyIndex < 32)
                Active.medicalPickups[Active.act] |= 1 << enemyIndex;
            else
                Active.extraMedicalPickups.Add(Active.act * 256 + enemyIndex);
            Active.medkits++;
            SaveMedicalSupplies();
            return true;
        }

        public static bool SpendMedkit()
        {
            if (Active.medkits <= 0)
                return false;

            Active.medkits--;
            SaveMedicalSupplies();
            return true;
        }

        private static void SaveMedicalSupplies()
        {
            var saved = ReadSave();
            if (saved == null || saved.act != Active.act)
                return;

            saved.medkits = Active.medkits;
            saved.medicalPickups = Active.medicalPickups;
            saved.extraMedicalPickups = Active.extraMedicalPickups;
            Write(saved);
        }

        public static void NewGame()
        {
            active = new SaveData();
            Write();
            Load(0);
        }

        public static void ContinueGame()
        {
            active = ReadSave();
            if (active != null)
                Load(active.act);
        }

        public static void EnterAct(int index)
        {
            if (active == null || active.act != index)
                active = new SaveData { act = index, furthest = index };
        }

        public static void SaveRewards(int coins)
        {
            Active.coins = Mathf.Max(0, coins);
            Write();
        }

        public static void Advance(int act, int coins)
        {
            Active.coins = Mathf.Max(0, coins);
            if (act == Chapters.Length - 1)
            {
                MarkCompleted(coins);
                SceneManager.LoadScene("Inicio");
                return;
            }
            Active.act = act + 1;
            Active.furthest = Mathf.Max(Active.furthest, Active.act);
            Write();
            Load(Active.act);
        }

        internal static void MarkCompleted(int coins)
        {
            Active.coins = Mathf.Max(0, coins);
            Active.completed = true;
            Write();
        }

        public static void Backtrack(int act, int coins)
        {
            if (act <= 0)
                return;
            Active.act = act - 1;
            backwardsDestination = Active.act;
            Active.coins = coins;
            Write();
            Load(Active.act);
        }

        internal static bool TakeBacktrackArrival(int act)
        {
            bool returning = backwardsDestination == act;
            backwardsDestination = -1;
            return returning;
        }

        public static void Menu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Inicio");
        }

        private static void Load(int index)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Chapters[index].Scene);
        }
    }
}

