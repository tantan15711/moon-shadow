namespace MoonShadow
{
    public enum Sidearm { Service, Experimental, Legendary, Incinerator, Shotgun }

    public static class SidearmCatalog
    {
        public const int BaseMagazine = 30;
        public const int BaseReserve = 60;
        public readonly struct Entry
        {
            public readonly string Name, Description;
            public readonly int Damage;
            public readonly int Magazine;
            public readonly float Cooldown, ReloadSeconds, Length;
            public bool UsesAmmo => Magazine > 0;

            public Entry(string name, string description, int damage, float cooldown, int magazine, float reload, float length)
            {
                Name = name;
                Description = description;
                Damage = damage;
                Cooldown = cooldown;
                Magazine = magazine;
                ReloadSeconds = reload;
                Length = length;
            }
        }

        public const int Count = 5;

        private static readonly Entry[] Items =
        {
            new("Pistola de servicio", "Pistola automática de Kyle con cargador de 30 balas.", 1, 0.115f, BaseMagazine, 0.66f, 0.46f),
            new("Pistola experimental", "Pulso automático de mayor alcance con una celda recargable.", 2, 0.145f, BaseMagazine, 0.74f, 0.60f),
            new("Emisor legendario", "Ráfaga ilimitada de energía que rompe blindaje.", 2, 0.165f, 0, 0f, 1.20f),
            new("Lanzallamas", "Chorro continuo de largo alcance. Recarga el depósito con R o B.", 1, 0.105f, BaseMagazine, 0.94f, 1.24f),
            new("Escopeta de asalto", "Escopeta de tambor. Mantén el gatillo para encadenar disparos.", 3, 0.28f, BaseMagazine, 0.88f, 1.02f)
        };

        public static Entry Get(Sidearm weapon) => Items[(int)weapon];

        public static bool IsValid(Sidearm weapon) => (uint)weapon < Count;
    }
}

