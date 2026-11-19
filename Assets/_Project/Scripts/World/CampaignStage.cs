using UnityEngine;

namespace MoonShadow
{
    public sealed class CampaignStage : MonoBehaviour
    {
        [SerializeField, Range(0, 4)] private int act;
        [SerializeField] private EnemyBase2D boss;
        [SerializeField] private Transform rescue;
        [SerializeField] private Transform bossCheckpoint;
        [SerializeField] private EnemyBase2D[] mechs;
        [SerializeField] private Texture2D openingArt;
        [SerializeField] private Texture2D endingArt;
        [SerializeField] private Texture2D dawnArt;
        [SerializeField] private OpeningSequence opening;
        [SerializeField] private LunarRescueSequence epilogue;
        [SerializeField] private GameObject encounterGate;

        public int Act => act;
        public bool Final => act == CampaignProgress.Chapters.Length - 1;
        public EnemyBase2D Boss => boss;
        public Transform Rescue => rescue;
        public Vector2 SafeAfterBoss => bossCheckpoint.position;
        public CampaignProgress.Chapter Chapter => CampaignProgress.Chapters[act];
        public float Gravity => Final ? 0.66f : 1f;
        public string CivilianName => act switch
        {
            0 => "Mara, médica de campaña", 1 => "Inez, técnica del laboratorio",
            2 => "Damián, minero", 3 => "Lina, mecánica", _ => "Noé, ingeniero lunar"
        };
        public string BossLine => act switch
        {
            0 => "La compuerta cae a espaldas de Kyle.",
            1 => "Cristales y nanocables caen sobre la pasarela.",
            2 => "Vulkano-0  ·  PROTOCOLO DE PURGA",
            3 => "Unit-Zero  ·  Rompe el escudo con un golpe pesado.",
            _ => "El Ojo levanta su defensa entre Kyle y la cápsula."
        };
        public Texture2D OpeningArt => openingArt;
        public Texture2D EndingArt => endingArt;
        public Texture2D DawnArt => dawnArt;
        public OpeningSequence Opening => opening;
        public LunarRescueSequence Epilogue => epilogue;
        public GameObject EncounterGate => encounterGate;
        public void ConfigureMechAttacks()
        {
            if (mechs == null)
                return;
            foreach (var mech in mechs)
                if (mech)
                    mech.SetVeteran(true);
        }
        public EnemyBase2D ActiveMech
        {
            get
            {
                if (mechs != null)
                    foreach (var mech in mechs)
                        if (mech && mech.Alive)
                            return mech;
                return null;
            }
        }
        public string RelayReward => act switch
        {
            0 => "Impulso aéreo habilitado. Salta y usa el dash.",
            1 => "Doble salto habilitado. Pulsa salto otra vez en el aire.",
            4 => "Terminal abierta. Desactiva al Ojo de Acero.",
            _ => "Terminal abierta. Sigue hacia el guardián de la zona."
        };

        public void Configure(int chapter, EnemyBase2D guardian, Transform civilian, Transform safePoint, EnemyBase2D[] guards)
        {
            act = chapter;
            boss = guardian;
            rescue = civilian;
            bossCheckpoint = safePoint;
            mechs = guards;
        }

        public void SetCinematics(Texture2D opening, Texture2D ending, Texture2D dawn)
        {
            openingArt = opening;
            endingArt = ending;
            dawnArt = dawn;
        }

        public void SetOpening(OpeningSequence sequence) => opening = sequence;
        public void SetEpilogue(LunarRescueSequence sequence) => epilogue = sequence;
        public void SetEncounterGate(GameObject gate) => encounterGate = gate;
    }
}

