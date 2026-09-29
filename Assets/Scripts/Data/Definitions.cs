using System;
using UnityEngine;

namespace FacefSurvivors
{
    public enum WeaponId { TypeScript, Dart, Bash, Lua, HtmlCss, Sql, C }

    public enum PassiveId { CSharp, JavaScript, Java, Swift, Python, Rust, Go, Cpp, Php }

    /// <summary>Atributos do jogador (recalculados a partir do personagem + itens passivos).</summary>
    public class PlayerStats
    {
        public float MaxHp = 100f;
        public float Speed = 3.4f;
        public float Might = 1f;          // multiplicador de dano
        public float CooldownMult = 1f;   // multiplicador do tempo de recarga
        public float Area = 1f;
        public float Magnet = 1.6f;       // raio de coleta (unidades)
        public float Regen = 0f;          // vida por segundo
        public float Armor = 0f;
        public float ProjSpeed = 1f;
        public float Duration = 1f;
        public int Amount = 0;            // projéteis extras
    }

    /// <summary>Atributos "crus" de uma arma (antes dos bônus do jogador).</summary>
    public class WeaponStats
    {
        public float Damage = 10f;
        public float Cooldown = 1f;
        public int Amount = 1;
        public float Area = 1f;
        public float Speed = 1f;
        public float Duration = 1f;
        public int Pierce = 1;
        public float Knockback = 1f;
        public float HitInterval = 0.5f;

        public WeaponStats Clone()
        {
            return (WeaponStats)MemberwiseClone();
        }
    }

    public class WeaponLevel
    {
        public string Desc;
        public Action<WeaponStats> Apply;

        public WeaponLevel(string desc, Action<WeaponStats> apply)
        {
            Desc = desc;
            Apply = apply;
        }
    }

    public class WeaponDef
    {
        public WeaponId Id;
        public string Name;
        public string Description;
        public Func<Sprite> Icon;
        public float IconRotation;
        public WeaponStats Base;
        /// <summary>Levels[0] é o que acontece ao subir para o nível 2, e assim por diante.</summary>
        public WeaponLevel[] Levels;
        public Func<Weapon> Create;

        public int MaxLevel => Levels.Length + 1;
    }

    public class PassiveDef
    {
        public PassiveId Id;
        public string Name;
        public string Description;
        public Func<Sprite> Icon;
        public int MaxLevel = 5;
        /// <summary>Aplica o efeito acumulado do item no nível informado.</summary>
        public Action<PlayerStats, int> Apply;
    }

    public class CharacterDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string Bonus;
        /// <summary>Caminhos em Resources dos quadros parado e correndo (desenho olhando para a direita).</summary>
        public string IdleSprite, RunSprite;
        public WeaponId StartWeapon;
        public Action<PlayerStats> Modify;
    }

    public class EnemyDef
    {
        public string Name;
        /// <summary>Imagem parada (chefes).</summary>
        public Func<Sprite> Sprite;
        /// <summary>Quadros da animação de andar (estudantes); se existir, substitui <see cref="Sprite"/>.</summary>
        public Func<Sprite[]> Walk;
        public float Hp = 10f;
        public float Speed = 1f;
        public float Damage = 5f;
        public float Radius = 0.38f;
        public float Scale = 1f;
        public float KnockResist = 0f;
        public int Xp = 1;
        /// <summary>Anda saltitando, com passos rápidos (ex.: quem está atrasado).</summary>
        public bool Bouncy;
        public bool Boss;
        public bool FinalBoss;
        public Color Tint = Color.white;
        public Color DeathColor = Color.white;
    }
}
