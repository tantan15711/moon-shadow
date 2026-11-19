using UnityEngine;

namespace MoonShadow
{
    public sealed partial class RuinsHud
    {
        private const int LoadoutRows = 1 + SidearmCatalog.Count;
        private int inventorySelection;
        private bool inventoryWasOpen;
        private GUIStyle inventoryRow;
        private GUIStyle itemDescription;

        private void UpdateInventory()
        {
            if (!inventoryWasOpen)
            {
                inventorySelection = (int)demo.Weapon + 1;
                inventoryWasOpen = true;
            }

            inventorySelection = (inventorySelection + CampaignMenu.Navigation() + LoadoutRows) % LoadoutRows;
            if (inventorySelection > 0 && demo.Player.Input.Accept.WasPressedThisFrame())
                demo.EquipSidearm((Sidearm)(inventorySelection - 1));
        }

        private void DrawInventory(float width, float height)
        {
            inventoryRow ??= new GUIStyle(action)
            {
                alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 8, 0, 0)
            };
            itemDescription ??= new GUIStyle(text) { wordWrap = true };

            float panelWidth = Mathf.Min(width - 32f, 748f);
            float panelHeight = 436f;
            float x = (width - panelWidth) * 0.5f;
            float y = (height - panelHeight) * 0.5f;
            float leftWidth = Mathf.Min(304f, (panelWidth - 68f) * 0.46f);
            float left = x + 24f;
            float detail = left + leftWidth + 23f;
            float detailWidth = x + panelWidth - detail - 25f;
            Fill(new Rect(0, 0, width, height), new Color(0.01f, 0.02f, 0.04f, 0.77f));
            Panel(new Rect(x, y, panelWidth, panelHeight));
            Label(left, y + 16f, panelWidth - 50f, 32f, "INVENTARIO", title, White);
            Label(left, y + 49f, panelWidth - 50f, 22f,
                $"{demo.Coins} monedas   ·   {(SidearmCatalog.Get(demo.Weapon).UsesAmmo ? $"Reserva {demo.ReserveAmmo}/{demo.ReserveCapacity}" : "Energía ilimitada")}   ·   {demo.Medkits} botiquines (H)",
                bold, Cyan);
            Label(left, y + 89f, leftWidth, 20f, "ARMAS", bold, Muted);

            for (int row = 0; row < LoadoutRows; row++)
                DrawLoadoutRow(row, new Rect(left, y + 115f + row * 32f, leftWidth, 30f));

            Fill(new Rect(detail - 12f, y + 91f, 1f, 230f), new Color(0.25f, 0.38f, 0.48f));
            DrawItemDetail(detail, y + 91f, detailWidth);

            Fill(new Rect(left, y + 316f, panelWidth - 48f, 1f), new Color(0.25f, 0.38f, 0.48f));
            Label(left, y + 323f, panelWidth - 48f, 22f, "HABILIDADES", bold, Muted);
            string abilities = demo.Player.AirDashUnlocked ? "Impulso aéreo" : "Impulso aéreo (por desbloquear)";
            abilities += demo.Player.DoubleJumpUnlocked ? "   ·   Doble salto" : "   ·   Doble salto (por desbloquear)";
            Label(left, y + 347f, panelWidth - 48f, 26f, abilities, text, White);
            int rescued = 0;
            for (int act = 0; act < CampaignProgress.Chapters.Length; act++)
                if ((CampaignProgress.Active.rescues & (1 << act)) != 0)
                    rescued++;
            Label(left, y + 373f, panelWidth - 48f, 20f, $"Civiles rescatados  {rescued}/{CampaignProgress.Chapters.Length}", bold, Muted);
            Label(left, y + 397f, panelWidth - 174f, 23f, "W / S, ↑ ↓, stick o cruceta   ·   Enter / A equipa", bold, Muted);
            if (GUI.Button(new Rect(x + panelWidth - 143f, y + 392f, 119f, 29f), "Cerrar  ·  I / B", action))
                demo.ToggleInventory();
        }

        private void DrawLoadoutRow(int row, Rect area)
        {
            Sidearm weapon = (Sidearm)(row - 1);
            bool equipped = row == 0 || weapon == demo.Weapon;
            bool owned = row == 0 || CampaignProgress.Owns(weapon);
            string name = row == 0 ? SwordName : SidearmCatalog.Get(weapon).Name;
            string state = equipped ? "Equipada" : owned ? "Disponible" : "Por encontrar";
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = row == inventorySelection ? new Color(0.38f, 0.78f, 0.88f) : Color.white;
            if (GUI.Button(area, name, inventoryRow))
                inventorySelection = row;
            GUI.backgroundColor = old;
            Label(area.x + area.width - 91f, area.y + 4f, 87f, 22f, state, bold, equipped ? Cyan : Muted);
        }

        private static string SwordName => CampaignProgress.Active.tacticalBlade ? "Hoja táctica" : "Katana de Kyle";

        private void DrawItemDetail(float x, float y, float width)
        {
            if (inventorySelection == 0)
            {
                Label(x, y, width, 32f, SwordName, title, White);
                Label(x, y + 43f, width, 56f,
                    "Arma principal. Encadena ataques ligeros, rompe guardias con el pesado y contraataca tras un parry.",
                    itemDescription, Muted);
                Label(x, y + 107f, width, 24f, $"Bonificación de fuerza   +{demo.BonusDamage}", bold, Cyan);
                Label(x, y + 142f, width, 24f, "Ataques  J / L   ·   Parry  K", bold, White);
                if (CampaignProgress.Active.tacticalBlade && demo.Effects.TacticalBlade)
                    GUI.DrawTexture(new Rect(x + width - 114f, y + 165f, 114f, 36f), demo.Effects.TacticalBlade.texture, ScaleMode.ScaleToFit);
                return;
            }

            var weapon = (Sidearm)(inventorySelection - 1);
            var data = SidearmCatalog.Get(weapon);
            bool owned = CampaignProgress.Owns(weapon);
            Label(x, y, width, 32f, data.Name, title, White);
            Label(x, y + 43f, width, 62f, data.Description, itemDescription, Muted);
            string capacity = data.UsesAmmo ? $"Cargador  {(owned ? demo.Loaded(weapon) : 0)}/{data.Magazine}" : "Energía ilimitada";
            Label(x, y + 106f, width, 24f, $"Daño  {data.Damage}   ·   {capacity}", bold, Cyan);
            Label(x, y + 136f, width, 22f, data.UsesAmmo ? "Disparo F / RT  ·  Recarga R / B" : "Disparo  F / RT", bold, White);
            Sprite art = demo.Effects.WeaponArt(weapon);
            if (art)
                GUI.DrawTexture(new Rect(x + width - 112f, y + 99f, 112f, 42f), art.texture, ScaleMode.ScaleToFit);
            bool equipped = weapon == demo.Weapon;
            GUI.enabled = owned && !equipped;
            string caption = equipped ? "Equipada" : owned ? "Equipar" : weapon switch
            {
                Sidearm.Incinerator => "Se encuentra en el metro",
                Sidearm.Shotgun => "Disponible en la tienda",
                _ => "Se consigue al rescatar civiles"
            };
            if (GUI.Button(new Rect(x, y + 168f, width, 34f), caption, action))
                demo.EquipSidearm(weapon);
            GUI.enabled = true;
        }
    }
}

