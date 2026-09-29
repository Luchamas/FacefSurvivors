using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    public enum UpgradeKind { NewWeapon, WeaponLevel, NewPassive, PassiveLevel, Heal }

    public class UpgradeOption
    {
        public UpgradeKind Kind;
        public WeaponDef Weapon;
        public PassiveDef Passive;
        public int NextLevel;
        public string Title;
        public string Description;
        public Sprite Icon;
        public float IconRotation;
        public float Weight = 1f;

        public bool IsNew => Kind == UpgradeKind.NewWeapon || Kind == UpgradeKind.NewPassive;
    }

    /// <summary>Sorteia e aplica as melhorias do level up e dos baús.</summary>
    public static class UpgradeSystem
    {
        static List<UpgradeOption> AllOptions(Player p)
        {
            var list = new List<UpgradeOption>();
            foreach (var w in p.Weapons)
            {
                if (w.IsMaxed) continue;
                list.Add(new UpgradeOption
                {
                    Kind = UpgradeKind.WeaponLevel, Weapon = w.Def, NextLevel = w.Level + 1,
                    Title = w.Def.Name, Description = w.NextLevelDescription,
                    Icon = w.Def.Icon(), IconRotation = w.Def.IconRotation, Weight = 1.3f,
                });
            }
            foreach (var pa in p.Passives)
            {
                if (pa.IsMaxed) continue;
                list.Add(new UpgradeOption
                {
                    Kind = UpgradeKind.PassiveLevel, Passive = pa.Def, NextLevel = pa.Level + 1,
                    Title = pa.Def.Name, Description = pa.Def.Description,
                    Icon = pa.Def.Icon(), Weight = 1f,
                });
            }
            if (p.Weapons.Count < Player.MaxWeapons)
            {
                foreach (var wd in Database.Weapons)
                {
                    if (p.GetWeapon(wd.Id) != null) continue;
                    list.Add(new UpgradeOption
                    {
                        Kind = UpgradeKind.NewWeapon, Weapon = wd, NextLevel = 1,
                        Title = wd.Name, Description = wd.Description,
                        Icon = wd.Icon(), IconRotation = wd.IconRotation, Weight = 1f,
                    });
                }
            }
            if (p.Passives.Count < Player.MaxPassives)
            {
                foreach (var pd in Database.Passives)
                {
                    if (p.GetPassive(pd.Id) != null) continue;
                    list.Add(new UpgradeOption
                    {
                        Kind = UpgradeKind.NewPassive, Passive = pd, NextLevel = 1,
                        Title = pd.Name, Description = pd.Description,
                        Icon = pd.Icon(), Weight = 0.8f,
                    });
                }
            }
            return list;
        }

        public static UpgradeOption HealOption()
        {
            return new UpgradeOption
            {
                Kind = UpgradeKind.Heal, Title = "Coxinha", Description = "Recupera 30 de vida.",
                Icon = Art.Coxinha, NextLevel = 0,
            };
        }

        /// <summary>Sorteia até <paramref name="count"/> opções diferentes para o level up.</summary>
        public static List<UpgradeOption> Roll(Player p, int count)
        {
            var pool = AllOptions(p);
            var result = new List<UpgradeOption>();
            while (result.Count < count && pool.Count > 0)
            {
                int i = PickWeighted(pool);
                result.Add(pool[i]);
                pool.RemoveAt(i);
            }
            if (result.Count == 0) result.Add(HealOption());
            return result;
        }

        /// <summary>Melhoria aleatória para o baú (prefere o que o jogador já tem).</summary>
        public static UpgradeOption RandomChestUpgrade(Player p)
        {
            var all = AllOptions(p);
            var owned = all.FindAll(o => !o.IsNew);
            var pool = owned.Count > 0 ? owned : all;
            if (pool.Count == 0) return HealOption();
            return pool[PickWeighted(pool)];
        }

        static int PickWeighted(List<UpgradeOption> pool)
        {
            float total = 0f;
            foreach (var o in pool) total += o.Weight;
            float r = Random.value * total;
            for (int i = 0; i < pool.Count; i++)
            {
                r -= pool[i].Weight;
                if (r <= 0f) return i;
            }
            return pool.Count - 1;
        }

        public static void Apply(Player p, UpgradeOption o)
        {
            switch (o.Kind)
            {
                case UpgradeKind.NewWeapon:
                    p.AddWeapon(o.Weapon);
                    break;
                case UpgradeKind.WeaponLevel:
                    p.GetWeapon(o.Weapon.Id)?.LevelUp();
                    break;
                case UpgradeKind.NewPassive:
                    p.AddPassive(o.Passive);
                    break;
                case UpgradeKind.PassiveLevel:
                    var pi = p.GetPassive(o.Passive.Id);
                    if (pi != null) p.LevelUpPassive(pi);
                    break;
                case UpgradeKind.Heal:
                    p.Heal(30f);
                    break;
            }
        }
    }
}
