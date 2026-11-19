using UnityEngine;

namespace MoonShadow
{
    public sealed class AmmoSupply : MonoBehaviour
    {
        [SerializeField] private RuinsDemo world;
        [SerializeField] private int amount;
        [SerializeField] private string label;
        private Vector2 anchor;
        private float phase;

        public void Configure(RuinsDemo demo, int units, string supplyName)
        {
            world = demo;
            amount = units;
            label = supplyName;
        }

        private void Awake() => anchor = transform.position;

        private void Update()
        {
            if (!world.Playing)
                return;
            phase += Time.deltaTime * 3.25f;
            transform.position = anchor + Vector2.up * (Mathf.Sin(phase) * 0.066f);
            Vector2 player = world.Player.Position;
            if (Vector2.SqrMagnitude(player - anchor) > 0.83f || world.Player.TerrainBetween(player, anchor))
                return;
            int transferred = world.Restock(amount);
            if (transferred == 0)
                return;
            amount -= transferred;
            world.Message($"{label}  ·  +{transferred} a la reserva", 2.1f);
            world.Effects.Sound(5);
            if (amount == 0)
                gameObject.SetActive(false);
        }
    }
}
