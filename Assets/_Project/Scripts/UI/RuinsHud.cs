using UnityEngine;

namespace MoonShadow
{
    public sealed partial class RuinsHud : MonoBehaviour
    {
        [SerializeField] private RuinsDemo demo;
        private static readonly Color Ink = new(0.025f, 0.038f, 0.07f, 0.94f);
        private static readonly Color Cyan = new(0.46f, 0.92f, 1f);
        private static readonly Color Muted = new(0.61f, 0.72f, 0.82f);
        private static readonly Color White = new(0.91f, 0.96f, 1f);
        private static readonly Color Red = new(0.91f, 0.08f, 0.16f);
        private GUIStyle title;
        private GUIStyle text;
        private GUIStyle bold;
        private GUIStyle centered;
        private GUIStyle action;
        private GUIStyle chapterText;
        private GUIStyle subtitles;
        private GUIStyle creditTitle;
        private Camera lens;
        private int pauseChoice;
        private float displayedLife = -1f;

        public void Configure(RuinsDemo world) => demo = world;

        private void Start() => lens = FindAnyObjectByType<RuinsCamera>()?.GetComponent<Camera>();

        private void Update()
        {
            float life = (float)demo.Player.Health / demo.Player.CurrentMaxHealth;
            displayedLife = displayedLife < 0f ? life : Mathf.MoveTowards(displayedLife, life, Time.unscaledDeltaTime * 0.86f);
            if (demo.InventoryOpen)
            {
                UpdateInventory();
                return;
            }
            inventoryWasOpen = false;
            if (!demo.Paused)
            {
                pauseChoice = 0;
                return;
            }
            if (!demo.Stage)
                return;
            pauseChoice = (pauseChoice + CampaignMenu.Navigation() + PauseActions.Length) % PauseActions.Length;
            if (demo.Player.Input.Accept.WasPressedThisFrame())
                SelectPause(pauseChoice);
        }

        private void OnGUI()
        {
            if (!demo || !lens)
                return;

            InitializeStyles();
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 980f, Screen.height / 560f), 0.45f, 1.5f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (demo.IntroPlaying)
            {
                DrawOpening(width, height);
                GUI.matrix = old;
                return;
            }

            Panel(new Rect(20, 20, 230, 43));
            Fill(new Rect(32, 30, 206, 11), new Color(0.23f, 0.12f, 0.17f));
            float healthWidth = 206 * Mathf.Clamp01(displayedLife);
            Fill(new Rect(32, 30, healthWidth, 11), Red);
            Fill(new Rect(32, 30, healthWidth, 2), new Color(1f, 0.33f, 0.35f));
            Fill(new Rect(32, 48, 206, 5), new Color(0.17f, 0.28f, 0.34f));
            Fill(new Rect(32, 48, 206 * (demo.Player.Stamina / demo.Player.MaxStamina), 5), Cyan);
            if (demo.Stage)
            {
                const float boxWidth = 180f;
                float x = width - boxWidth - 22f;
                float y = TouchControls.Enabled ? 76f : height - 64f;
                Panel(new Rect(x, y, boxWidth, 42f));
                string ammo = !SidearmCatalog.Get(demo.Weapon).UsesAmmo ? "Energía  ∞" :
                    demo.Player.Reloading ? "Recargando…" : $"{demo.Ammo}/{demo.MaxAmmo}  ·  Reserva {demo.ReserveAmmo}";
                Label(x + 12f, y + 5f, boxWidth - 22f, 23f, ammo, bold, White);
                if (demo.Player.Reloading)
                {
                    Fill(new Rect(x + 12f, y + 29f, boxWidth - 24f, 3f), new Color(0.17f, 0.28f, 0.34f));
                    Fill(new Rect(x + 12f, y + 29f, (boxWidth - 24f) * demo.Player.ReloadProgress, 3f), Cyan);
                }
            }

            string bossStatus = demo.ShowBoss
                ? demo.BossShielded ? "ESCUDO ACTIVO" : demo.Stage.Final ? $"FASE {demo.Stage.Boss.Phase}" : ""
                : "";
            Panel(new Rect(width - 294, 20, 274, bossStatus.Length > 0 ? 84 : 64));
            Label(width - 280, 25, 248, 22, "OBJETIVO", bold, Cyan);
            Label(width - 280, 50, 248, 23, demo.GoalText, text, White);
            if (bossStatus.Length > 0)
                Label(width - 280, 75, 248, 21, bossStatus, bold, Cyan);
            if (demo.ComboHits > 1)
            {
                float comboY = bossStatus.Length > 0 ? 116f : 96f;
                Panel(new Rect(width - 184, comboY, 164, 34));
                Label(width - 171, comboY + 3, 150, 27, $"{demo.ComboHits} GOLPES", bold, Cyan);
            }

            if (demo.VisorActive)
                DrawDirection(width, height);
            if (demo.Playing && demo.Player.ShowingAim)
            {
                Vector3 aim = lens.WorldToScreenPoint(demo.Player.AimMarker);
                float x = aim.x / scale, y = height - aim.y / scale;
                Fill(new Rect(x - 5f, y, 3f, 1f), Cyan);
                Fill(new Rect(x + 3f, y, 3f, 1f), Cyan);
                Fill(new Rect(x, y - 5f, 1f, 3f), Cyan);
                Fill(new Rect(x, y + 3f, 1f, 3f), Cyan);
            }

            string notice = demo.ContextHint ?? (demo.Map ? demo.Map.Hint : null) ?? demo.TutorialText ?? demo.MessageText;
            if (!string.IsNullOrEmpty(notice))
            {
                float boxWidth = Mathf.Min(width - 40, 520);
                float messageY = TouchControls.Enabled ? height - 215 : height - 85;
                Panel(new Rect((width - boxWidth) * 0.5f, messageY, boxWidth, 38));
                Label(0, messageY, width, 38, notice, centered, Cyan);
            }

            if (demo.Banter.Speaking)
                DrawDialogue(width, height, scale);
            if (demo.Stage && (demo.Briefing || demo.Finished))
                DrawChapter(width, height);
            else if (demo.InventoryOpen)
                DrawInventory(width, height);
            else if (demo.Paused && demo.Stage)
                DrawShop(width, height);
            else if (demo.Paused || demo.Finished)
                DrawOverlay(width, height);
            GUI.matrix = old;
        }

        private void InitializeStyles()
        {
            if (title != null)
                return;

            title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            bold = new GUIStyle(text) { fontSize = 13, fontStyle = FontStyle.Bold };
            centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            action = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
            chapterText = new GUIStyle(text) { wordWrap = true, fontSize = 18 };
            subtitles = new GUIStyle(chapterText) { alignment = TextAnchor.MiddleCenter };
            creditTitle = new GUIStyle(title) { fontSize = 38, alignment = TextAnchor.MiddleCenter };
        }

        private void DrawDialogue(float width, float height, float scale)
        {
            var speech = demo.Banter;
            Bounds actor = speech.IsKyle ? demo.Player.Hurtbox : demo.Stage.Boss.Hurtbox;
            Vector3 point = lens.WorldToScreenPoint(new Vector3(actor.center.x, actor.max.y + 0.41f, 0f));
            float anchorX = point.x / scale;
            float anchorY = height - point.y / scale;
            if (anchorX < -14f || anchorX > width + 14f || anchorY > height)
                return;
            var line = speech.Line;
            float w = Mathf.Clamp(subtitles.CalcSize(new GUIContent(line.Text)).x + 26f, 186f,
                Mathf.Min(384f, width - 36f));
            float h = subtitles.CalcHeight(new GUIContent(line.Text), w - 26f) + 15f;
            float x = Mathf.Clamp(anchorX - w * 0.5f, 14f, width - w - 14f);
            float y = Mathf.Clamp(anchorY - h - 14f, 69f, height - h - 15f);
            Color accent = speech.IsKyle ? Cyan : new Color(1f, 0.68f, 0.43f);
            Fill(new Rect(x, y, w, h), Ink);
            Fill(new Rect(x, y, w, 2f), accent);
            float tail = Mathf.Clamp(anchorX, x + 10f, x + w - 10f);
            Fill(new Rect(tail - 4f, y + h, 8f, 3f), Ink);
            Fill(new Rect(tail - 2f, y + h + 3f, 4f, 3f), accent);
            Label(x + 13f, y + 5f, w - 26f, h - 10f, line.Text[..speech.VisibleCharacters], subtitles,
                speech.IsKyle ? White : new Color(1f, 0.86f, 0.71f));
            string advance = demo.Player.Input.UsingGamepad ? "A Siguiente  ·  B Saltar" : "Enter / clic Siguiente  ·  Esc Saltar";
            Label(x, y + h + 10f, w, 22f, advance, centered, Muted);
        }

        private void DrawChapter(float width, float height)
        {
            var art = demo.StoryArt;
            if (art)
            {
                float zoom = demo.Finished && demo.Stage.Final ? Mathf.Lerp(1.09f, 1f, Mathf.Clamp01(demo.PageAge * 0.32f)) : 1f;
                GUI.DrawTexture(new Rect((1f - zoom) * width * 0.5f, (1f - zoom) * height * 0.5f,
                    width * zoom, height * zoom), art, ScaleMode.ScaleAndCrop, false);
            }
            else if (!demo.LiveEnding)
                Fill(new Rect(0, 0, width, height), new Color(0.01f, 0.016f, 0.034f, 0.83f));
            if (demo.Credits)
            {
                Fill(new Rect(0, 0, width, height), new Color(0.015f, 0.02f, 0.045f, 0.62f));
                Label(0, height * 0.52f, width, 65f, "Gracias por jugar :)", creditTitle, White);
                if (GUI.Button(new Rect((width - 234f) * 0.5f, height * 0.73f, 234f, 35f), "Volver al inicio  ·  Enter / A", action))
                    demo.AdvanceStory();
                return;
            }
            bool bottom = art || demo.LiveEnding;
            float w = Mathf.Min(width - 38, bottom ? 760 : 636);
            float boxHeight = bottom ? 158f : 236f;
            float x = (width - w) * 0.5f;
            float y = bottom ? height - boxHeight - 19f : (height - boxHeight) * 0.5f;
            Panel(new Rect(x, y, w, boxHeight));
            string caption = demo.Finished && demo.Stage.Final ? "VOLVIÓ EL AMANECER" : demo.StageTitle;
            Label(x + 25, y + (bottom ? 12 : 20), w - 50, 34, caption, title, White);
            Label(x + 25, y + (bottom ? 49 : 66), w - 50, bottom ? 57 : 92, demo.StoryLine(), chapterText, White);
            string proceed = demo.StoryStep + 1 < demo.StoryLength ? "Siguiente" :
                demo.Briefing ? "Entrar" : demo.Stage.Final ? "Volver al inicio" : "Siguiente acto";
            if (GUI.Button(new Rect(x + 25, y + boxHeight - (bottom ? 47 : 62), w - 50, 34),
                proceed + "   ·   Enter / A", action))
                demo.AdvanceStory();
        }

        private void DrawOpening(float width, float height)
        {
            var sequence = demo.Stage.Opening;
            float bars = Mathf.Min(56f, height * 0.085f);
            Fill(new Rect(0, 0, width, bars), Color.black);
            Fill(new Rect(0, height - bars, width, bars), Color.black);
            if (sequence.Fade > 0f)
                Fill(new Rect(0, 0, width, height), new Color(0f, 0f, 0f, sequence.Fade));
            if (!string.IsNullOrEmpty(sequence.Caption))
            {
                float w = Mathf.Min(width - 36f, 850f);
                float x = (width - w) * 0.5f;
                float h = subtitles.CalcHeight(new GUIContent(sequence.Caption), w - 34f) + 20f;
                Fill(new Rect(x, height - bars - h - 6f, w, h), new Color(0.008f, 0.015f, 0.03f, 0.86f));
                Label(x + 17f, height - bars - h - 6f, w - 34f, h, sequence.Caption, subtitles, White);
            }
            if (sequence.Elapsed > 0.7f && GUI.Button(new Rect(width - 147f, height - bars + 11f, 133f, 27f),
                "Saltar  ·  Esc / B", action))
                demo.AdvanceStory();
        }

        private static readonly string[] PauseActions =
            { "Continuar", "Fuerza de espada +1", "Vida máxima +1", "Capacidad de reserva +4",
                "Reponer vida y munición", "Escopeta de asalto", "Hoja táctica · más alcance", "Comprar munición",
                "Guardar un botiquín", "Estamina máxima +20", "Velocidad de movimiento", "Volver al punto de control",
                "Menú principal" };

        private void DrawShop(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(0.01f, 0.02f, 0.04f, 0.79f));
            float w = Mathf.Min(width - 38, 500);
            float x = (width - w) * 0.5f;
            const int pageSize = 9;
            const float panelHeight = 436f;
            float y = (height - panelHeight) * 0.5f;
            Panel(new Rect(x, y, w, panelHeight));
            Label(x + 24, y + 16, w - 48, 33, "PAUSA  /  TIENDA", title, White);
            Label(x + 24, y + 53, w - 48, 25, $"{demo.Coins} monedas", bold, Cyan);
            int first = Mathf.Clamp(pauseChoice - pageSize + 2, 0, PauseActions.Length - pageSize);
            for (int i = first; i < first + pageSize; i++)
            {
                int cost = i > 0 && i < PauseActions.Length - 2 ? RuinsDemo.ShopCost(i - 1) : 0;
                string label = PauseActions[i] + (cost < 0 ? "   ·   Máximo" : cost > 0 ? $"   ·   {cost} monedas" : "");
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = i == pauseChoice ? new Color(0.43f, 0.78f, 0.87f) : Color.white;
                GUI.enabled = cost >= 0 && demo.Coins >= cost;
                if (GUI.Button(new Rect(x + 24, y + 89 + (i - first) * 33, w - 48, 29), label, action))
                    SelectPause(i);
                GUI.enabled = true;
                GUI.backgroundColor = old;
            }
            Label(x + 24, y + 400, w - 48, 22, "W / S, ↑ ↓, stick o cruceta   ·   Enter / A   ·   Esc / B", bold, Muted);
        }

        private void SelectPause(int index)
        {
            if (index == 0)
                demo.TogglePause();
            else if (index == PauseActions.Length - 2)
            {
                demo.TogglePause();
                demo.Respawn();
            }
            else if (index == PauseActions.Length - 1)
                CampaignProgress.Menu();
            else
                demo.Buy(index - 1);
        }

        private void DrawDirection(float width, float height)
        {
            Vector3 p = lens.WorldToViewportPoint(demo.Target + Vector2.up * 1.85f);
            float x = Mathf.Clamp(p.x * width, 50f, width - 50f);
            float y = Mathf.Clamp((1f - p.y) * height, 138f, height - 142f);
            string arrow = p.x < 0.07f ? "‹" : p.x > 0.93f ? "›" : "↓";
            Panel(new Rect(x - 23, y - 19, 46, 38));
            Label(x - 23, y - 19, 46, 38, arrow, centered, Cyan);
        }

        private void DrawOverlay(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(0.01f, 0.02f, 0.04f, 0.79f));
            float panelWidth = Mathf.Min(432, width - 36);
            float x = (width - panelWidth) * 0.5f;
            float y = (height - 206) * 0.5f;
            Panel(new Rect(x, y, panelWidth, 206));
            Label(x + 24, y + 20, panelWidth - 48, 32,
                demo.Finished ? "RUTA DESPEJADA" : "PAUSA", title, White);
            Label(x + 25, y + 60, panelWidth - 50, 25, demo.Finished
                ? $"Fin de la demo  ·  Monedas: {demo.Coins}" : "Esc / Menú para continuar", text, Muted);

            if (demo.Paused && GUI.Button(new Rect(x + 25, y + 99, panelWidth - 50, 37), "Continuar", action))
                demo.TogglePause();
            if (GUI.Button(new Rect(x + 25, y + (demo.Paused ? 149 : 110), panelWidth - 50, 37),
                "Reiniciar  ·  R", action))
                demo.Restart();
        }

        private static void Panel(Rect area)
        {
            Fill(area, Ink);
            Fill(new Rect(area.x, area.y, area.width, 2), new Color(0.19f, 0.49f, 0.61f));
        }

        private static void Fill(Rect area, Color tone)
        {
            Color previous = GUI.color;
            GUI.color = tone;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Label(float x, float y, float w, float h, string value, GUIStyle style, Color tone)
        {
            style.normal.textColor = tone;
            GUI.Label(new Rect(x, y, w, h), value, style);
        }
    }
}

