using UnityEngine;

namespace MoonShadow
{
    public sealed partial class RuinsEffects
    {
        [SerializeField] private Sprite[] healingArt;

        public void ConfigureHealingArt(Sprite[] art) => healingArt = art;

        public void RestoreHealth(Vector2 center, float floor)
        {
            if (healingArt == null || healingArt.Length < 4)
                return;
            Sprite loop = healingArt[2];
            var spiral = Rent(loop, new Vector2(center.x, floor + 0.22f), Color.white, 13,
                Shape(loop, new Vector2(1.38f, 0.50f)));
            Fade(spiral, Vector2.up * 1.7f, 0.54f, spin: -30f);
            Sprite cross = healingArt[0];
            Fade(Rent(cross, center + Vector2.up * 0.58f, Color.white, 23, Shape(cross, Vector2.one * 0.44f)),
                Vector2.up * 1.68f, 0.62f);

            for (int i = 0; i < 11; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.55f;
                Sprite shape = healingArt[i % 3 == 0 ? 3 : 1];
                float size = Random.Range(0.12f, 0.24f);
                var fleck = Rent(shape, center + offset, Color.white, i % 2 == 0 ? 21 : 11,
                    Shape(shape, Vector2.one * size));
                Vector2 rise = new(offset.x * 1.6f, Random.Range(1.8f, 3.1f));
                Fade(fleck, rise, Random.Range(0.35f, 0.59f), spin: Random.Range(-80f, 80f));
            }
        }

        private void MedkitSpark(Vector2 point)
        {
            if (healingArt == null || healingArt.Length < 4)
                return;
            Sprite star = healingArt[3];
            Fade(Rent(star, point, Color.white, 20, Shape(star, Vector2.one * 0.36f)),
                Vector2.up * 1.25f, 0.29f);
        }
    }
}
