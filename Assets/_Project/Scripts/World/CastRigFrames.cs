using System.Collections.Generic;
using UnityEngine;

namespace MoonShadow
{
    public sealed partial class CastRig
    {
        [SerializeField] private Texture2D authoredSkin;
        private static readonly Dictionary<Texture2D, Sprite[]> PoseSheets = new();
        private Sprite[] authoredPoses;
        private SpriteRenderer authoredView;

        public void ConfigureFrames(Texture2D sheet) => authoredSkin = sheet;

        private void DrawAuthored(Motion motion, float progress, int facing, float speed, Color tint)
        {
            if (authoredPoses == null)
            {
                if (!PoseSheets.TryGetValue(authoredSkin, out authoredPoses))
                {
                    authoredPoses = new Sprite[16];
                    float cell = authoredSkin.width / 4f;
                    for (int i = 0; i < authoredPoses.Length; i++)
                    {
                        var rect = new Rect((i % 4) * cell, authoredSkin.height - (i / 4 + 1) * cell, cell, cell);
                        var pose = Sprite.Create(authoredSkin, rect, new Vector2(.49f, .055f), cell,
                            0, SpriteMeshType.FullRect);
                        pose.name = skin.name + " · pose " + i;
                        authoredPoses[i] = pose;
                    }
                    PoseSheets.Add(authoredSkin, authoredPoses);
                }
                var actor = new GameObject("Animación original", typeof(SpriteRenderer));
                actor.transform.SetParent(transform, false);
                authoredView = actor.GetComponent<SpriteRenderer>();
                authoredView.sortingLayerID = source.sortingLayerID;
                authoredView.sortingOrder = source.sortingOrder;
                float scale = skin.bounds.size.y * (infected ? 136f / 174f : 1f) / .885f;
                actor.transform.localScale = Vector3.one * scale;
            }
            int index = motion switch
            {
                Motion.Windup => progress < .54f ? 8 : 9,
                Motion.Claw or Motion.Grab => progress < .23f ? 9 : progress < .64f ? 10 : 11,
                Motion.Strike or Motion.Pound or Motion.Surge => progress < .47f ? 10 : 11,
                Motion.Stun => 12,
                Motion.Recover => progress < .32f ? 13 : progress < .75f ? 14 : 15,
                Motion.Travel when speed > .18f => (int)(phase * 8f) % 8,
                Motion.Gesture => progress < .5f ? 8 : 15,
                _ => 15
            };
            renderer.enabled = false;
            source.enabled = false;
            authoredView.enabled = true;
            authoredView.sprite = authoredPoses[index];
            authoredView.flipX = facing < 0;
            authoredView.color = tint;
        }
    }
}
