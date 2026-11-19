using System;
using UnityEngine;

namespace MoonShadow
{
    public sealed class CivilianAssistance : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer civilian;
        [SerializeField] private CastRig rig;
        [SerializeField] private Sprite hero;
        [SerializeField] private Material material;
        [SerializeField] private LayerMask terrain;
        private Vector2 feet;
        private KyleController2D helper;
        private CastRig helperPose;
        private Action done;
        private float time;
        private int direction;
        public bool Helping { get; private set; }
        public bool Recovered { get; private set; }

        public void Configure(SpriteRenderer actor, CastRig motion, Sprite heroSkin, Material skin,
            LayerMask floors)
        {
            civilian = actor;
            rig = motion;
            hero = heroSkin;
            material = skin;
            terrain = floors;
        }

        public void Place(bool recovered)
        {
            var ground = Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.46f, Vector2.down, 4f, terrain);
            feet = ground ? ground.point : (Vector2)transform.position - Vector2.up * 0.8f;
            Recovered = recovered;
            SetCivilianPose(recovered ? 1f : 0f);
            if (recovered)
                ClearMarker();
        }

        public void Begin(KyleController2D player, Action completed)
        {
            if (Helping || Recovered)
                return;
            helper = player;
            done = completed;
            direction = player.Position.x < feet.x ? 1 : -1;
            var x = feet.x - direction * 0.68f;
            player.WarpTo(new Vector2(x, feet.y + player.Hurtbox.extents.y));
            player.Face(direction);
            player.HoldForScene();
            player.SetSceneSpriteVisible(false);

            var actor = new GameObject("Kyle presta ayuda", typeof(SpriteRenderer));
            actor.transform.position = new Vector2(x, feet.y + 0.025f);
            var view = actor.GetComponent<SpriteRenderer>();
            view.sortingOrder = 13;
            view.sprite = hero;
            helperPose = actor.AddComponent<CastRig>();
            helperPose.Configure(hero, material, CastRig.Role.Kyle);
            view.transform.localScale = Vector3.one * (1.77f / hero.bounds.size.y);
            time = 0f;
            Helping = true;
            Time.timeScale = 0f;
            FindAnyObjectByType<RuinsCamera>()?.SnapToPlayer();
        }

        private void Update()
        {
            if (!Helping)
                return;
            time += Time.unscaledDeltaTime;
            float pose = Mathf.Clamp01(time / 1.42f);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.27f, 0.92f, pose));
            SetCivilianPose(rise);
            helperPose.Animate(Time.unscaledDeltaTime, 0f, direction, CastRig.Motion.Assist, pose, Color.white);
            if (pose < 1f)
                return;

            Helping = false;
            Recovered = true;
            helper.SetSceneSpriteVisible(true);
            Destroy(helperPose.gameObject);
            ClearMarker();
            Time.timeScale = 1f;
            done?.Invoke();
            done = null;
        }

        private void ClearMarker()
        {
            var label = transform.Find("RESCATE");
            var plate = transform.Find("Placa");
            if (label) label.gameObject.SetActive(false);
            if (plate) plate.gameObject.SetActive(false);
        }

        private void SetCivilianPose(float rise)
        {
            civilian.transform.position = feet + new Vector2(-0.38f * (1f - rise), 0.15f * (1f - rise));
            civilian.transform.rotation = Quaternion.Euler(0f, 0f, -82f * (1f - rise));
            rig.Animate(0f, 0f, -1, CastRig.Motion.Assist, rise, Color.white);
        }
    }
}
