using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace MoonShadow
{
    public sealed partial class MoonDemoSmoke
    {
        private static bool TouchReviewRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-moonTouchTest") >= 0;

        private IEnumerator RecordTouch()
        {
            Check(TouchControls.Enabled, "The Android touch overlay is active");
            var touch = InputSystem.AddDevice<Touchscreen>();
            CampaignProgress.NewGame();
            yield return Until(() => (demo = FindAnyObjectByType<RuinsDemo>()) && demo.IntroPlaying, 7f, "Touch review loads the campaign");
            yield return TapXbox(new XboxState { Buttons = 1 << 13 });
            yield return Until(() => demo.Playing, 1.8f, "Touch review reaches gameplay");
            foreach (var enemy in demo.Enemies)
                enemy.HideEnemy();
            var controller = FindAnyObjectByType<TouchControls>();
            var player = demo.Player;
            player.WarpTo(new Vector2(.9f, .82f));
            yield return Until(() => player.Grounded && Field<float>(player, "hurtTime") <= 0f, 1f, "Kyle can use the touch controls");
            Rect[] areas = Field<Rect[]>(controller, "areas");
            int ammo = demo.Ammo;
            Finger(1, 1, true);
            Finger(2, 2, true);
            yield return Until(() => player.Velocity.x > 4f && demo.Ammo <= ammo - 2, 1f,
                "Two fingers move Kyle while aiming and firing continuously");
            Check(player.Input.UsingTouch && player.Input.ReloadKey == "R en pantalla", "Hints identify the touch controls");
            CaptureScene("android-touch-controls");
            Finger(1, 1, false);
            Finger(2, 2, false);
            yield return Frames(6);
            float reloadAmmo = demo.Ammo;
            Finger(3, 8, true);
            yield return Frames(7);
            Finger(3, 8, false);
            yield return Until(() => player.Reloading, .5f, "Touch R begins reloading");
            yield return Until(() => !player.Reloading && demo.Ammo > reloadAmmo, 1.5f, "Touch reload fills the magazine");
            Finger(4, 3, true);
            yield return Until(() => !player.Grounded, .5f, "Touch SALTO jumps");
            Finger(4, 3, false);
            yield return Until(() => player.Grounded, 1.6f, "Kyle lands after a touch jump");
            Finger(5, 13, true);
            yield return Frames(7);
            Finger(5, 13, false);
            yield return Until(() => demo.Paused, .5f, "Touch PAUSA opens the shop");
            yield return Frames(4);
            areas = Field<Rect[]>(controller, "areas");
            Finger(6, 13, true);
            yield return Frames(7);
            Finger(6, 13, false);
            yield return Until(() => demo.Playing, .5f, "Touch PAUSA resumes the game");
            Finger(7, 12, true);
            yield return Frames(7);
            Finger(7, 12, false);
            yield return Until(() => demo.InventoryOpen, .5f, "Touch BOLSA opens inventory");
            yield return Frames(4);
            areas = Field<Rect[]>(controller, "areas");
            Finger(8, 15, true);
            yield return Frames(7);
            Finger(8, 15, false);
            yield return Until(() => !demo.InventoryOpen && demo.Playing, .5f, "Touch VOLVER closes inventory");
            InputSystem.RemoveDevice(touch);
            Debug.Log("MOON_TOUCH_REVIEW_PASSED");
            Application.Quit(0);
            yield break;

            void Finger(int id, int area, bool down)
            {
                Vector2 point = areas[area].center;
                if (area == 1)
                    point.x += areas[area].width * .32f;
                if (area == 2)
                    point += areas[area].size * new Vector2(.31f, .11f);
                InputSystem.QueueStateEvent(touch, new TouchState
                {
                    touchId = id,
                    position = point,
                    phase = down ? TouchPhase.Began : TouchPhase.Ended
                });
            }
        }
    }
}
