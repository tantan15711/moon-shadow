using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonShadow
{
    internal sealed class KyleInput : IDisposable
    {
        private readonly InputActionMap map = new("Kyle");
        private static bool usingGamepad;
        private readonly InputAction pointerMotion;
        private bool mouseAim;
        public bool UsingGamepad => usingGamepad;
        public bool UsingTouch => TouchControls.Enabled && TouchControls.IsTouchPad(Gamepad.current);
        public string InteractKey => UsingTouch ? "USAR" : UsingGamepad ? "↓" : "E";
        public string ReloadKey => UsingTouch ? "R en pantalla" : UsingGamepad ? "B" : "R";

        public InputAction Move { get; }
        public InputAction Aim { get; }
        public InputAction Jump { get; }
        public InputAction Attack { get; }
        public InputAction Heavy { get; }
        public InputAction Fire { get; }
        public InputAction Reload { get; }
        public InputAction Heal { get; }
        public InputAction Walk { get; }
        public InputAction Accept { get; }
        public InputAction DialogueAdvance { get; }
        public InputAction Inventory { get; }
        public InputAction Interact { get; }
        public InputAction Dash { get; }
        public InputAction Parry { get; }
        public InputAction Visor { get; }
        public InputAction Pause { get; }
        public InputAction Back { get; }
        public InputAction Restart { get; }

        public KyleInput()
        {
            Move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Axis");
            Move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            Move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick/x").WithProcessor("axisDeadzone(min=0.18,max=0.95)");
            Move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Gamepad>/dpad/left").With("Positive", "<Gamepad>/dpad/right");
            Aim = map.AddAction("Aim", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            pointerMotion = map.AddAction("PointerMotion", InputActionType.PassThrough, "<Mouse>/delta");
            Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Attack = Button("Attack", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
            Heavy = Button("Heavy", "<Keyboard>/l", "<Gamepad>/buttonNorth");
            Fire = Button("Fire", "<Keyboard>/f", "<Gamepad>/rightTrigger");
            Reload = Button("Reload", "<Keyboard>/r", "<Gamepad>/buttonEast");
            Heal = Button("Heal", "<Keyboard>/h", "<Gamepad>/dpad/up");
            Walk = Button("Walk", "<Keyboard>/leftCtrl", "<Keyboard>/rightCtrl");
            Accept = Button("Accept", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            DialogueAdvance = Button("DialogueAdvance", "<Keyboard>/enter", "<Mouse>/leftButton", "<Gamepad>/buttonSouth");
            Inventory = Button("Inventory", "<Keyboard>/i", "<Gamepad>/select");
            Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/dpad/down");
            Dash = Button("Dash", "<Keyboard>/leftShift", "<Gamepad>/rightShoulder");
            Parry = Button("Parry", "<Keyboard>/k", "<Mouse>/rightButton", "<Gamepad>/leftShoulder");
            Visor = Button("Visor", "<Keyboard>/q", "<Gamepad>/rightStickPress");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            Back = Button("Back", "<Gamepad>/buttonEast");
            Restart = Button("Restart", "<Keyboard>/r");
            map.actionTriggered += TrackControls;
            map.Enable();
        }

        private void TrackControls(InputAction.CallbackContext context)
        {
            if (!context.performed || context.control == null)
                return;
            if ((context.action == Move && Mathf.Abs(context.ReadValue<float>()) < 0.25f) ||
                (context.action == Aim && context.ReadValue<Vector2>().sqrMagnitude < 0.09f))
                return;
            if (context.action == pointerMotion)
            {
                if (context.ReadValue<Vector2>().sqrMagnitude < 4f)
                    return;
                mouseAim = true;
            }
            usingGamepad = context.control.device is Gamepad;
        }

        public Vector2 ShotDirection(Camera camera, Vector2 origin, int facing)
        {
            if (usingGamepad)
            {
                var stick = Aim.ReadValue<Vector2>();
                if (stick.sqrMagnitude >= 0.09f)
                    return stick.normalized;
            }
            else if (mouseAim && Mouse.current != null && camera)
            {
                Vector2 cursor = Mouse.current.position.ReadValue();
                Vector2 point = camera.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, -camera.transform.position.z));
                if ((point - origin).sqrMagnitude > 0.018f)
                    return (point - origin).normalized;
            }
            return Vector2.right * facing;
        }

        private InputAction Button(string name, params string[] paths)
        {
            var action = map.AddAction(name, InputActionType.Button);
            foreach (string path in paths)
                action.AddBinding(path);
            return action;
        }

        public void Dispose() => map.Dispose();
    }
}

