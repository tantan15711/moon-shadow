using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace MoonShadow
{
    [DefaultExecutionOrder(-500)]
    internal sealed class TouchControls : MonoBehaviour
    {
        private enum Control { None, Move, Aim, Jump, Slash, Heavy, Dash, Guard, Reload, Heal, Use, Visor, Inventory, Pause, Accept, Back, Up, Down }

        private struct Finger
        {
            public int Id;
            public Control Captured;
        }

        private static TouchControls instance;
        private readonly Finger[] fingers = new Finger[10];
        private readonly Rect[] areas = new Rect[Enum.GetValues(typeof(Control)).Length];
        private readonly string[] labels =
        {
            "", "MOVER", "APUNTAR", "SALTO", "ESPADA", "FUERTE", "DASH", "PARRY", "R",
            "CURA", "USAR", "VISOR", "BOLSA", "PAUSA", "ACEPTAR", "VOLVER", "▲", "▼"
        };
        private Gamepad pad;
        private RuinsDemo world;
        private Vector2 move, aim;
        private GamepadState state;
        private ulong pressedControls;
        private GUIStyle text;
        private Texture2D circle;
        private float scale;
        private Rect safe;
        private bool menu;

        internal static bool Enabled => instance && instance.isActiveAndEnabled;
        internal static bool IsTouchPad(InputDevice device) => instance && device == instance.pad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if ((Application.platform == RuntimePlatform.Android ||
                Array.IndexOf(Environment.GetCommandLineArgs(), "-moonTouchTest") >= 0) && !instance)
                DontDestroyOnLoad(new GameObject("Controles táctiles", typeof(TouchControls)));
        }

        private void Awake()
        {
            instance = this;
            pad = InputSystem.AddDevice<Gamepad>("MoonShadowTouch");
            SceneManager.sceneLoaded += SceneLoaded;
            SceneLoaded(default, default);
            circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                byte alpha = (byte)(Mathf.Clamp01(31.5f - distance) * 255);
                pixels[y * 64 + x] = new Color32(255, 255, 255, alpha);
            }
            circle.SetPixels32(pixels);
            circle.Apply(false, true);
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            world = FindAnyObjectByType<RuinsDemo>();
            Release();
        }

        private void Update()
        {
            Arrange();
            move = aim = Vector2.zero;
            state = default;
            pressedControls = 0;
            var screen = Touchscreen.current;
            if (screen != null)
            {
                for (int i = 0; i < screen.touches.Count; i++)
                {
                    var touch = screen.touches[i];
                    if (!touch.press.isPressed)
                    {
                        fingers[i] = default;
                        continue;
                    }
                    var point = touch.position.ReadValue();
                    int id = touch.touchId.ReadValue();
                    ref Finger finger = ref fingers[i];
                    if (finger.Id != id)
                    {
                        finger.Id = id;
                        finger.Captured = Pick(point);
                    }
                    ReadFinger(finger.Captured, point);
                }
            }
            state.leftStick = move;
            state.rightStick = aim;
            state.rightTrigger = aim.sqrMagnitude > .13f && !menu ? 1f : 0f;
            InputSystem.QueueStateEvent(pad, state);
        }

        private void Arrange()
        {
            safe = Screen.safeArea;
            scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
            menu = !world || !world.Playing;
            Vector2 origin = safe.position;
            float right = safe.width / scale;
            float high = safe.height / scale;
            Array.Clear(areas, 0, areas.Length);
            Put(Control.Move, 30, 28, 180, 180);
            if (menu)
            {
                Put(Control.Up, 38, 190, 76, 68);
                Put(Control.Down, 38, 102, 76, 68);
                Put(Control.Accept, right - 156, 34, 130, 68);
                Put(Control.Back, right - 294, 34, 120, 68);
                Put(Control.Pause, right - 98, high - 76, 74, 54);
                return;
            }
            Put(Control.Aim, right - 185, 226, 150, 150);
            Put(Control.Jump, right - 108, 22, 82, 82);
            Put(Control.Slash, right - 202, 80, 84, 84);
            Put(Control.Heavy, right - 292, 144, 74, 74);
            Put(Control.Dash, right - 294, 50, 74, 74);
            Put(Control.Guard, right - 204, 184, 78, 64);
            Put(Control.Reload, right - 106, 128, 70, 54);
            Put(Control.Heal, 244, 36, 68, 58);
            Put(Control.Use, 244, 110, 68, 58);
            Put(Control.Visor, 330, 36, 68, 58);
            Put(Control.Inventory, right - 176, high - 72, 70, 50);
            Put(Control.Pause, right - 92, high - 72, 70, 50);
            return;

            void Put(Control control, float x, float y, float width, float height) =>
                areas[(int)control] = new Rect(origin.x + x * scale, origin.y + y * scale, width * scale, height * scale);
        }

        private Control Pick(Vector2 point)
        {
            for (int i = areas.Length - 1; i > 0; i--)
                if (areas[i].width > 0f && areas[i].Contains(point))
                    return (Control)i;
            return Control.None;
        }

        private void ReadFinger(Control control, Vector2 point)
        {
            if (control == Control.None || areas[(int)control].width <= 0f)
                return;
            pressedControls |= 1UL << (int)control;
            if (control is Control.Move or Control.Aim)
            {
                Rect area = areas[(int)control];
                Vector2 value = Vector2.ClampMagnitude((point - area.center) / (area.width * .35f), 1f);
                if (control == Control.Move)
                {
                    move = value;
                    if (menu)
                    {
                        if (value.y > .47f) state = state.WithButton(GamepadButton.DpadUp);
                        if (value.y < -.47f) state = state.WithButton(GamepadButton.DpadDown);
                    }
                }
                else
                {
                    aim = value;
                }
                return;
            }
            if (!areas[(int)control].Contains(point))
                return;
            GamepadButton? button = control switch
            {
                Control.Jump or Control.Accept => GamepadButton.South,
                Control.Slash => GamepadButton.West,
                Control.Heavy => GamepadButton.North,
                Control.Dash => GamepadButton.RightShoulder,
                Control.Guard => GamepadButton.LeftShoulder,
                Control.Reload or Control.Back => GamepadButton.East,
                Control.Heal or Control.Up => GamepadButton.DpadUp,
                Control.Use or Control.Down => GamepadButton.DpadDown,
                Control.Visor => GamepadButton.RightStick,
                Control.Inventory => GamepadButton.Select,
                Control.Pause => GamepadButton.Start,
                _ => null
            };
            if (button.HasValue)
                state = state.WithButton(button.Value);
        }

        private void OnGUI()
        {
            if (!circle)
                return;
            Arrange();
            GUI.depth = -30;
            text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            text.fontSize = Mathf.RoundToInt(15f * scale);
            for (int i = 1; i < areas.Length; i++)
            {
                Rect area = areas[i];
                if (area.width == 0f)
                    continue;
                bool stick = i == (int)Control.Move || i == (int)Control.Aim;
                bool pressed = (pressedControls & (1UL << i)) != 0;
                area.y = Screen.height - area.yMax;
                GUI.color = pressed ? new Color(.19f, .58f, .76f, .76f) : new Color(.025f, .07f, .12f, .57f);
                GUI.DrawTexture(area, circle);
                GUI.color = new Color(.70f, .90f, 1f, .86f);
                if (stick)
                {
                    Vector2 offset = i == (int)Control.Move ? move : aim;
                    Vector2 center = area.center + new Vector2(offset.x, -offset.y) * (area.width * .25f);
                    GUI.DrawTexture(new Rect(center.x - 16f * scale, center.y - 16f * scale, 32f * scale, 32f * scale), circle);
                    GUI.Label(new Rect(area.x, area.yMax + 2, area.width, 24f * scale), labels[i], text);
                }
                else
                    GUI.Label(area, labels[i], text);
            }
            GUI.color = Color.white;
        }

        private void Release()
        {
            Array.Clear(fingers, 0, fingers.Length);
            state = default;
            pressedControls = 0;
            if (pad != null && pad.added)
                InputSystem.QueueStateEvent(pad, state);
        }

        private void OnApplicationPause(bool paused)
        {
            Release();
            if (paused && world && world.Playing)
                world.TogglePause();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                Release();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (pad != null && pad.added)
                InputSystem.RemoveDevice(pad);
            if (circle)
                Destroy(circle);
            instance = null;
        }
    }
}
