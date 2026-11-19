using UnityEngine;

namespace MoonShadow
{
    public sealed partial class RuinsEffects
    {
        [SerializeField] private Sprite[] weaponSprites, fireFrames, explosionFrames;
        [SerializeField] private Sprite tacticalBlade;
        [SerializeField] private AudioClip releaseMagazine, loadMagazine;
        [SerializeField] private AudioClip[] weaponAudio;
        [SerializeField] private AudioClip swordAir, swordContact;

        public void ConfigureArsenal(Sprite[] weapons, Sprite blade, Sprite[] fire, Sprite[] blast, Sprite medical,
            AudioClip release, AudioClip load)
        {
            weaponSprites = weapons;
            tacticalBlade = blade;
            fireFrames = fire;
            explosionFrames = blast;
            medkit = medical;
            releaseMagazine = release;
            loadMagazine = load;
        }

        public Sprite WeaponArt(Sidearm sidearm) => weaponSprites != null && (uint)sidearm < (uint)weaponSprites.Length
            ? weaponSprites[(int)sidearm] : null;
        public Sprite TacticalBlade => tacticalBlade;

        public void ReloadSound(bool finished)
        {
            var clip = finished ? loadMagazine : releaseMagazine;
            if (clip)
                speaker.PlayOneShot(clip, 0.55f);
        }

        public void ConfigureCombatAudio(AudioClip[] guns, AudioClip swing, AudioClip impact)
        {
            weaponAudio = guns;
            swordAir = swing;
            swordContact = impact;
        }

        internal void WeaponSound(Sidearm weapon)
        {
            int index = (int)weapon;
            if (weaponAudio == null || index >= weaponAudio.Length || !weaponAudio[index])
                return;
            speaker.PlayOneShot(weaponAudio[index], weapon == Sidearm.Incinerator ? 0.37f : 0.61f);
        }

        internal void SwordSound(bool contact, bool heavy)
        {
            var clip = contact ? swordContact : swordAir;
            if (clip)
                speaker.PlayOneShot(clip, contact ? (heavy ? 0.72f : 0.52f) : 0.54f);
        }

        internal void Explosion(Vector2 center, float size)
        {
            if (explosionFrames?.Length > 0)
            {
                var art = explosionFrames[0];
                Vector2 proportions = (Vector2)art.bounds.size * (size / art.bounds.size.x);
                var flash = Rent(art, center, Color.white, 20, Shape(art, proportions));
                Fade(flash, Vector2.up * 0.54f, 0.34f + size * 0.07f, frames: explosionFrames);
            }
            Burst(center, new Color(1f, 0.53f, 0.23f), 8, 4.5f);
        }

        public void WideShot(Vector2 start, Vector2 direction, Sidearm weapon)
        {
            bool fire = weapon == Sidearm.Incinerator;
            float range = fire ? 6.5f : 9.0f;
            Vector2 tangent = new(-direction.y, direction.x);
            float headingAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int facing = direction.x < 0f ? -1 : 1;
            var sparks = fire ? new Color(1f, 0.37f, 0.11f) : new Color(1f, 0.80f, 0.52f);
            int hits = 0;
            foreach (var enemy in demo.Enemies)
            {
                if (!enemy.Alive)
                    continue;
                Vector2 contact = enemy.Hurtbox.ClosestPoint(start);
                float forward = Vector2.Dot(contact - start, direction);
                float side = Mathf.Abs(Vector2.Dot(contact - start, tangent));
                if (forward < -0.06f || forward > range || side > 0.30f + forward * (fire ? 0.15f : 0.25f) ||
                    Vector2.Dot(enemy.Position - start, direction) < -0.1f || Physics2D.Linecast(start, contact, terrain))
                    continue;
                int damage = fire ? 1 : forward < 2.1f ? 3 : 2;
                if (!enemy.Hit(damage, facing, !fire, false))
                    continue;
                hits++;
                Burst(contact, sparks, fire ? 4 : 7, 3.7f);
            }
            if (hits > 0)
            {
                demo.RecordHits(hits);
                if (!fire)
                    demo.Impact(0.048f, 0.20f);
            }

            var obstacle = Physics2D.Raycast(start, direction, range, terrain);
            float visibleRange = obstacle ? obstacle.distance : range;
            if (fire && fireFrames?.Length > 0 && visibleRange > 0.05f)
            {
                var sequence = Rent(fireFrames[0], start + direction * (visibleRange * 0.48f), Color.white, 20,
                    new Vector2(visibleRange / fireFrames[0].bounds.size.x, 1.40f / fireFrames[0].bounds.size.y));
                sequence.transform.rotation = Quaternion.Euler(0f, 0f, headingAngle);
                Fade(sequence, direction * 0.46f, 0.22f, frames: fireFrames);
                for (int i = 0; i < 5; i++)
                    Streak(start + direction * (visibleRange * Random.Range(0.60f, 0.92f)),
                        direction * Random.Range(1f, 4.2f) + Vector2.up * Random.Range(1.0f, 3.9f),
                        i < 2 ? new Color(0.32f, 0.29f, 0.35f, 0.51f) : sparks, Random.Range(0.11f, 0.29f), 0.19f);
            }
            else if (!fire)
                Explosion(start + direction * Mathf.Min(0.30f, visibleRange), 0.86f);
            for (int i = 0; !fire && i < 7; i++)
            {
                float angle = (i - 3) * 6.5f;
                var heading = (Vector2)(Quaternion.Euler(0f, 0f, angle) * direction);
                var pellet = Rent(bullet ? bullet : pixel, start + heading * 0.16f, sparks, 20,
                    new Vector2(0.24f, 0.12f) / (Vector2)(bullet ? bullet : pixel).bounds.size);
                pellet.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
                var block = Physics2D.Raycast(start, heading, range, terrain);
                Fade(pellet, heading * 38f, (block ? block.distance : range) / 38f);
            }
            Burst(start, sparks, fire ? 4 : 12, fire ? 4f : 7f);
            WeaponSound(weapon);
        }
    }
}
