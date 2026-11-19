using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonShadow
{
    public sealed class CampaignMenu : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle subtitle;
        private GUIStyle button;
        private int selection;
        private bool confirmNew;
        private bool hasSave;

        private void OnEnable()
        {
            hasSave = CampaignProgress.HasSave;
            selection = hasSave ? 0 : 1;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if (confirmNew && ((keyboard?.escapeKey.wasPressedThisFrame ?? false) ||
                (pad?.buttonEast.wasPressedThisFrame ?? false)))
            {
                confirmNew = false;
                selection = 0;
                return;
            }
            int move = Navigation();
            if (move != 0)
            {
                int entries = confirmNew ? 2 : 3;
                selection = (selection + move + entries) % entries;
                if (!confirmNew && !hasSave && selection == 0)
                    selection = move < 0 ? 2 : 1;
            }
            if ((keyboard?.enterKey.wasPressedThisFrame ?? false) || (pad?.buttonSouth.wasPressedThisFrame ?? false))
                Choose(selection);
        }

        internal static int Navigation()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if ((keyboard?.upArrowKey.wasPressedThisFrame ?? false) || (keyboard?.wKey.wasPressedThisFrame ?? false) ||
                (pad?.dpad.up.wasPressedThisFrame ?? false) || (pad?.leftStick.up.wasPressedThisFrame ?? false))
                return -1;
            if ((keyboard?.downArrowKey.wasPressedThisFrame ?? false) || (keyboard?.sKey.wasPressedThisFrame ?? false) ||
                (pad?.dpad.down.wasPressedThisFrame ?? false) || (pad?.leftStick.down.wasPressedThisFrame ?? false))
                return 1;
            return 0;
        }

        private void Choose(int index)
        {
            if (confirmNew)
            {
                if (index == 0)
                    CampaignProgress.NewGame();
                else
                    confirmNew = false;
                return;
            }
            if (index == 0 && hasSave)
                CampaignProgress.ContinueGame();
            else if (index == 1 && !hasSave)
                CampaignProgress.NewGame();
            else if (index == 1)
            {
                confirmNew = true;
                selection = 0;
            }
            else if (index == 2)
                Application.Quit();
        }

        private void OnGUI()
        {
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 57, fontStyle = FontStyle.Bold };
            subtitle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(20, 20, 0, 0) };

            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1020f, Screen.height / 580f), 0.45f, 1.8f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            Rect panel = new(48, Mathf.Max(30, (height - 416) * 0.5f), Mathf.Min(width - 94, 580), 416);
            Fill(panel, new Color(0.016f, 0.027f, 0.049f, 0.93f));
            Fill(new Rect(panel.x, panel.y, panel.width, 2), new Color(0.38f, 0.75f, 0.85f));
            GUI.Label(new Rect(panel.x + 26, panel.y + 24, panel.width - 52, 78), "MOON SHADOW", title);
            GUI.Label(new Rect(panel.x + 29, panel.y + 108, panel.width - 58, 70),
                confirmNew ? "Una partida nueva reemplazará tu guardado. Confirma para comenzar." :
                "Kyle sigue a las máquinas desde una ciudad en ruinas hasta la Luna para rescatar a su hermana.", subtitle);

            for (int i = 0; i < 3; i++)
            {
                string label = i switch
                {
                    0 => hasSave ? (CampaignProgress.SavedCompleted ? "Campaña completada · Retomar" :
                        "Continuar · " + CampaignProgress.Chapters[CampaignProgress.SavedAct].Name) : "Continuar",
                    1 => "Nueva partida",
                    _ => "Salir"
                };
                if (confirmNew && i == 0)
                    label = "Comenzar partida nueva";
                else if (confirmNew && i == 1)
                    label = "Volver";
                if (confirmNew && i == 2)
                    break;
                GUI.enabled = confirmNew || i != 0 || hasSave;
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = selection == i ? new Color(0.42f, 0.8f, 0.92f) : Color.white;
                if (GUI.Button(new Rect(panel.x + 27, panel.y + 191 + i * 52, panel.width - 54, 43), label, button))
                {
                    Choose(i);
                }
                GUI.backgroundColor = oldColor;
                GUI.enabled = true;
            }
            GUI.Label(new Rect(panel.x + 29, panel.y + 363, panel.width - 58, 25),
                confirmNew ? "Enter / A confirma   ·   Esc / B vuelve" : "W / S, ↑ ↓, stick o cruceta   ·   Enter / A acepta", subtitle);
            GUI.matrix = previous;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}

