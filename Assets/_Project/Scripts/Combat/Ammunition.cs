using UnityEngine;

namespace MoonShadow
{
    internal sealed class Ammunition
    {
        private readonly int[] loaded = new int[SidearmCatalog.Count];
        private readonly int[] reserves = new int[SidearmCatalog.Count];
        public int Reserve(Sidearm weapon) => reserves[(int)weapon];
        public int Loaded(Sidearm weapon) => loaded[(int)weapon];

        public void Refill(int capacity)
        {
            for (int i = 0; i < loaded.Length; i++)
            {
                reserves[i] = SidearmCatalog.Get((Sidearm)i).UsesAmmo ? capacity : 0;
                FillMagazine((Sidearm)i);
            }
        }

        public void FillMagazine(Sidearm weapon) => loaded[(int)weapon] = SidearmCatalog.Get(weapon).Magazine;

        public int Supply(Sidearm weapon, int amount, int capacity)
        {
            if (!SidearmCatalog.Get(weapon).UsesAmmo)
                return 0;
            int index = (int)weapon;
            int before = reserves[index];
            reserves[index] = Mathf.Min(capacity, before + amount);
            return reserves[index] - before;
        }

        public bool Spend(Sidearm weapon)
        {
            if (!SidearmCatalog.Get(weapon).UsesAmmo)
                return true;
            if (loaded[(int)weapon] <= 0)
                return false;
            loaded[(int)weapon]--;
            return true;
        }

        public void Reload(Sidearm weapon)
        {
            if (!SidearmCatalog.Get(weapon).UsesAmmo)
                return;
            int index = (int)weapon;
            int rounds = Mathf.Min(reserves[index], SidearmCatalog.Get(weapon).Magazine - Loaded(weapon));
            loaded[index] += rounds;
            reserves[index] -= rounds;
        }
    }
}
