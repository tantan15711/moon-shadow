using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoonShadow
{
    public sealed partial class MoonDemoSmoke
    {
        private static bool MotionReviewRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-moonMotionReview") >= 0;

        private IEnumerator VerifyLunarLanding()
        {
            var player = demo.Player;
            var rig = player.GetComponentInChildren<CastRig>();
            player.WarpTo(new Vector2(1.32f, .81f));
            yield return Until(() => player.Grounded && rig.FootPlanted(0) && rig.FootPlanted(1), .66f,
                "Kyle rests on the lunar deck before jumping");
            float resting = Field<Vector2[]>(rig, "positions")[10].y;
            QueueXbox(new XboxState { Buttons = 1 << 12 });
            yield return Until(() => !player.Grounded && Field<Vector2[]>(rig, "positions")[10].y > resting + .29f,
                .68f, "Kyle raises the lead knee in reduced gravity");
            FindAnyObjectByType<RuinsCamera>().SnapToPlayer();
            CaptureScene("motion-lunar-jump");
            QueueXbox(default);
            yield return Until(() => player.Grounded && Field<float>(player, "landTime") > .01f, 2.2f,
                "The lunar jump flows into the landing pose");
        }

        private IEnumerator RecordMotion()
        {
            CampaignProgress.NewGame();
            yield return Until(() => (demo = FindAnyObjectByType<RuinsDemo>()) && demo.IntroPlaying, 6f, "Motion review loads the ruins");
            yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            yield return Until(() => demo.Playing, 1.6f, "Motion review skips the prologue");
            foreach (var enemy in demo.Enemies)
                enemy.HideEnemy();
            var player = demo.Player;
            player.WarpTo(new Vector2(-0.57f, 0.81f));
            yield return Until(() => player.Grounded, .72f, "Kyle starts the review supported by the terrain");
            FindAnyObjectByType<RuinsCamera>().enabled = false;
            var camera = Camera.main;
            camera.orthographicSize = 2.95f;

            string output = Path.Combine(Application.dataPath, "..", "..", "MotionReview");
            Directory.CreateDirectory(output);
            var target = RenderTexture.GetTemporary(1152, 648, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            var frameEnd = new WaitForEndOfFrame();
            var rig = player.GetComponentInChildren<CastRig>();
            float jumpPhase = float.NaN, stridePitch = 0f, landingLoad = 0f;
            bool spiked = false, airborne = false, fired = false, ran = false, turned = false;
            Time.captureFramerate = 30;
            for (int f = 0; f < 173; f++)
            {
                Queue(f switch
                {
                    >= 9 and < 36 => new[] { Key.D, Key.LeftCtrl },
                    >= 36 and < 62 => new[] { Key.D },
                    >= 70 and < 84 => new[] { Key.A, Key.Space },
                    >= 84 and < 90 => new[] { Key.A },
                    >= 104 and < 120 => new[] { Key.D, Key.F },
                    127 or 131 or 135 or 139 => new[] { Key.D, Key.J },
                    >= 149 and < 155 => new[] { Key.D, Key.LeftShift },
                    _ => Array.Empty<Key>()
                });
                yield return frameEnd;
                ran |= player.Velocity.x > 5f;
                turned |= player.Facing < 0 && f > 67;
                if (f is >= 72 and < 95 && !player.Grounded)
                {
                    airborne = true;
                    float phase = Field<float>(rig, "phase");
                    if (!float.IsNaN(jumpPhase))
                        spiked |= Mathf.Abs(jumpPhase - phase) > .003f;
                    jumpPhase = phase;
                }
                if (ran && f is >= 36 and < 62)
                    stridePitch = Mathf.Min(stridePitch, Field<float>(rig, "torsoLean"));
                if (f is >= 88 and < 104 && player.Grounded)
                    landingLoad = Mathf.Min(landingLoad, Field<Vector2>(rig, "hipOffset").y);
                fired |= f > 104 && FindAnyObjectByType<PlayerRound2D>();

                Vector3 focus = new(player.transform.position.x + player.Facing * .66f,
                    Mathf.Max(1.97f, player.transform.position.y + 0.62f), -10f);
                camera.transform.position = f == 0 ? focus : Vector3.Lerp(camera.transform.position, focus, .24f);
                RenderPipeline.SubmitRenderRequest(camera, request);
                var original = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                image.Apply(false);
                File.WriteAllBytes(Path.Combine(output, $"pose-{f:000}.jpg"), image.EncodeToJPG(89));
                RenderTexture.active = original;
                yield return null;
            }
            Time.captureFramerate = 0;
            Destroy(image);
            RenderTexture.ReleaseTemporary(target);
            Check(ran && turned && stridePitch < -10f, $"Kyle runs with a forward lean ({stridePitch:F1} degrees) and can reverse");
            Check(airborne && !spiked, "The gait does not cycle during the jump");
            Check(landingLoad < -.17f, $"Kyle lowers his hips after landing ({landingLoad:F2})");
            Check(fired, "The pistol fires from the animated character");
            Debug.Log("MOON_MOTION_REVIEW_PASSED");
            Application.Quit(0);
        }
    }
}
