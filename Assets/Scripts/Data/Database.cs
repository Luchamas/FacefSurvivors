using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>
    /// Conteúdo do jogo: personagens, armas, itens passivos e inimigos.
    /// Para balancear o jogo, mexa nos números daqui.
    /// </summary>
    public static class Database
    {
        // ================================================================ Armas (linguagens de programação)

        public static readonly List<WeaponDef> Weapons = new List<WeaponDef>
        {
            new WeaponDef
            {
                Id = WeaponId.TypeScript,
                Name = "TypeScript",
                Description = "Nenhum erro escapa: arremessa TS no inimigo mais próximo.",
                Icon = () => Art.Language("TypeScript"),
                Base = new WeaponStats { Damage = 10f, Cooldown = 1.2f, Amount = 1, Speed = 9f, Pierce = 1, Knockback = 1.5f },
                Levels = new[]
                {
                    new WeaponLevel("+1 TS por disparo.", s => s.Amount += 1),
                    new WeaponLevel("Recarga 0,2s mais rápida.", s => s.Cooldown -= 0.2f),
                    new WeaponLevel("+1 TS por disparo.", s => s.Amount += 1),
                    new WeaponLevel("+10 de dano.", s => s.Damage += 10f),
                    new WeaponLevel("+1 TS por disparo.", s => s.Amount += 1),
                    new WeaponLevel("Atravessa +1 inimigo.", s => s.Pierce += 1),
                    new WeaponLevel("+10 de dano.", s => s.Damage += 10f),
                },
                Create = () => new TypeScriptWeapon(),
            },
            new WeaponDef
            {
                Id = WeaponId.Dart,
                Name = "Dart",
                Description = "Dispara dardos rápidos na direção em que você anda.",
                Icon = () => Art.Language("Dart"),
                Base = new WeaponStats { Damage = 7f, Cooldown = 1.0f, Amount = 1, Speed = 14f, Pierce = 1, Knockback = 1f },
                Levels = new[]
                {
                    new WeaponLevel("+1 dardo por disparo.", s => s.Amount += 1),
                    new WeaponLevel("+1 dardo e +5 de dano.", s => { s.Amount += 1; s.Damage += 5f; }),
                    new WeaponLevel("+1 dardo por disparo.", s => s.Amount += 1),
                    new WeaponLevel("Atravessa +1 inimigo.", s => s.Pierce += 1),
                    new WeaponLevel("+1 dardo por disparo.", s => s.Amount += 1),
                    new WeaponLevel("+1 dardo e +5 de dano.", s => { s.Amount += 1; s.Damage += 5f; }),
                    new WeaponLevel("Atravessa +1 inimigo.", s => s.Pierce += 1),
                },
                Create = () => new DartWeapon(),
            },
            new WeaponDef
            {
                Id = WeaponId.Haskell,
                Name = "Haskell",
                Description = "Um map() em forma de golpe: atinge todos os inimigos à frente.",
                Icon = () => Art.Language("Haskell"),
                Base = new WeaponStats { Damage = 12f, Cooldown = 1.35f, Amount = 1, Area = 1f, Knockback = 4f },
                Levels = new[]
                {
                    new WeaponLevel("Golpeia também para trás.", s => s.Amount += 1),
                    new WeaponLevel("+5 de dano.", s => s.Damage += 5f),
                    new WeaponLevel("+10% de área e +5 de dano.", s => { s.Area += 0.1f; s.Damage += 5f; }),
                    new WeaponLevel("+5 de dano.", s => s.Damage += 5f),
                    new WeaponLevel("+10% de área e +5 de dano.", s => { s.Area += 0.1f; s.Damage += 5f; }),
                    new WeaponLevel("+5 de dano.", s => s.Damage += 5f),
                    new WeaponLevel("+10 de dano.", s => s.Damage += 10f),
                },
                Create = () => new HaskellWeapon(),
            },
            new WeaponDef
            {
                Id = WeaponId.Lua,
                Name = "Lua",
                Description = "Luas giram ao seu redor atingindo quem chegar perto.",
                Icon = () => Art.Language("Lua"),
                Base = new WeaponStats { Damage = 10f, Cooldown = 3f, Amount = 1, Area = 1f, Speed = 1f, Duration = 3f, Knockback = 2.5f, HitInterval = 0.5f },
                Levels = new[]
                {
                    new WeaponLevel("+1 lua.", s => s.Amount += 1),
                    new WeaponLevel("+25% de velocidade e área.", s => { s.Speed += 0.25f; s.Area += 0.25f; }),
                    new WeaponLevel("+10 de dano.", s => s.Damage += 10f),
                    new WeaponLevel("+1 lua.", s => s.Amount += 1),
                    new WeaponLevel("+25% de velocidade e área.", s => { s.Speed += 0.25f; s.Area += 0.25f; }),
                    new WeaponLevel("Duram 0,5s a mais.", s => s.Duration += 0.5f),
                    new WeaponLevel("+1 lua e +10 de dano.", s => { s.Amount += 1; s.Damage += 10f; }),
                },
                Create = () => new LuaWeapon(),
            },
            new WeaponDef
            {
                Id = WeaponId.Elixir,
                Name = "Elixir",
                Description = "Aura de elixir que causa dano contínuo ao redor.",
                Icon = () => Art.Language("Elixir"),
                Base = new WeaponStats { Damage = 5f, Area = 1f, Knockback = 1f, HitInterval = 0.85f },
                Levels = new[]
                {
                    new WeaponLevel("+20% de área e +2 de dano.", s => { s.Area += 0.2f; s.Damage += 2f; }),
                    new WeaponLevel("Acerta mais rápido e +1 de dano.", s => { s.HitInterval -= 0.1f; s.Damage += 1f; }),
                    new WeaponLevel("+20% de área e +1 de dano.", s => { s.Area += 0.2f; s.Damage += 1f; }),
                    new WeaponLevel("+2 de dano.", s => s.Damage += 2f),
                    new WeaponLevel("+20% de área.", s => s.Area += 0.2f),
                    new WeaponLevel("Acerta mais rápido e +1 de dano.", s => { s.HitInterval -= 0.1f; s.Damage += 1f; }),
                    new WeaponLevel("+20% de área e +2 de dano.", s => { s.Area += 0.2f; s.Damage += 2f; }),
                },
                Create = () => new ElixirWeapon(),
            },
            new WeaponDef
            {
                Id = WeaponId.Zig,
                Name = "Zig",
                Description = "Raios em zigue-zague caem sobre inimigos aleatórios na tela.",
                Icon = () => Art.Language("Zig"),
                Base = new WeaponStats { Damage = 18f, Cooldown = 4f, Amount = 2, Area = 1f, Knockback = 0.5f },
                Levels = new[]
                {
                    new WeaponLevel("+1 raio.", s => s.Amount += 1),
                    new WeaponLevel("+40% de área e +10 de dano.", s => { s.Area += 0.4f; s.Damage += 10f; }),
                    new WeaponLevel("+1 raio.", s => s.Amount += 1),
                    new WeaponLevel("+40% de área e +20 de dano.", s => { s.Area += 0.4f; s.Damage += 20f; }),
                    new WeaponLevel("+1 raio.", s => s.Amount += 1),
                    new WeaponLevel("+40% de área e +20 de dano.", s => { s.Area += 0.4f; s.Damage += 20f; }),
                    new WeaponLevel("+1 raio e recarga 0,5s mais rápida.", s => { s.Amount += 1; s.Cooldown -= 0.5f; }),
                },
                Create = () => new ZigWeapon(),
            },
            new WeaponDef
            {
                Id = WeaponId.Fortran,
                Name = "Fortran",
                Description = "Fórmulas pesadas lançadas para cima que caem esmagando tudo.",
                Icon = () => Art.Language("Fortran"),
                Base = new WeaponStats { Damage = 20f, Cooldown = 2.2f, Amount = 1, Area = 1f, Speed = 1f, Pierce = 3, Knockback = 2f },
                Levels = new[]
                {
                    new WeaponLevel("+1 fórmula.", s => s.Amount += 1),
                    new WeaponLevel("+20 de dano.", s => s.Damage += 20f),
                    new WeaponLevel("Atravessa +2 inimigos.", s => s.Pierce += 2),
                    new WeaponLevel("+1 fórmula.", s => s.Amount += 1),
                    new WeaponLevel("+20 de dano.", s => s.Damage += 20f),
                    new WeaponLevel("Atravessa +2 inimigos.", s => s.Pierce += 2),
                    new WeaponLevel("+1 fórmula e +20 de dano.", s => { s.Amount += 1; s.Damage += 20f; }),
                },
                Create = () => new FortranWeapon(),
            },
        };

        // ================================================================ Passivos (linguagens de programação)

        public static readonly List<PassiveDef> Passives = new List<PassiveDef>
        {
            new PassiveDef { Id = PassiveId.Php, Name = "PHP", Description = "Todo ano dizem que morreu: +20% de vida máxima.",
                Icon = () => Art.Language("Php"), Apply = (s, l) => s.MaxHp *= 1f + 0.2f * l },
            new PassiveDef { Id = PassiveId.Swift, Name = "Swift", Description = "Rápido como o nome: +10% de velocidade de movimento.",
                Icon = () => Art.Language("Swift"), Apply = (s, l) => s.Speed *= 1f + 0.1f * l },
            new PassiveDef { Id = PassiveId.CSharp, Name = "C#", Description = "A linguagem deste jogo: +10% de dano.",
                Icon = () => Art.Language("CSharp"), Apply = (s, l) => s.Might += 0.1f * l },
            new PassiveDef { Id = PassiveId.Go, Name = "Go", Description = "Goroutines: armas recarregam 8% mais rápido.",
                Icon = () => Art.Language("Go"), Apply = (s, l) => s.CooldownMult *= 1f - 0.08f * l },
            new PassiveDef { Id = PassiveId.JavaScript, Name = "JavaScript", Description = "Roda em todo lugar: +10% de área de ataque.",
                Icon = () => Art.Language("JavaScript"), Apply = (s, l) => s.Area += 0.1f * l },
            new PassiveDef { Id = PassiveId.Python, Name = "Python", Description = "Tem biblioteca pra tudo: +30% de raio de coleta de XP.",
                Icon = () => Art.Language("Python"), Apply = (s, l) => s.Magnet *= 1f + 0.3f * l },
            new PassiveDef { Id = PassiveId.Java, Name = "Java", Description = "Garbage collector: recupera +0,2 de vida por segundo.",
                Icon = () => Art.Language("Java"), Apply = (s, l) => s.Regen += 0.2f * l },
            new PassiveDef { Id = PassiveId.Rust, Name = "Rust", Description = "Memória blindada: +1 de armadura (reduz o dano recebido).",
                Icon = () => Art.Language("Rust"), Apply = (s, l) => s.Armor += l },
            new PassiveDef { Id = PassiveId.Cpp, Name = "C++", Description = "O operador ++: +1 projétil em todas as armas.",
                Icon = () => Art.Language("Cpp"), MaxLevel = 2, Apply = (s, l) => s.Amount += l },
        };

        // ================================================================ Personagens

        public static readonly List<CharacterDef> Characters = new List<CharacterDef>
        {
            new CharacterDef
            {
                Id = "mascote", Name = "Mascote FACEF",
                Description = "Uma pilha de livros com pernas. Pequeno, rápido e cheio de conhecimento.",
                Bonus = "Equilibrado, sem pontos fracos.",
                IdleSprite = "Sprites/Mascot_Idle", RunSprite = "Sprites/Mascot_Run",
                StartWeapon = WeaponId.TypeScript,
                Modify = s => { },
            },
        };

        // ================================================================ Inimigos (estudantes: Kenney Toon Characters)

        public static readonly EnemyDef Freshman = new EnemyDef
        {
            Name = "Calouro", Walk = () => Art.Student("MaleAdventurer"), Hp = 9f, Speed = 1.15f, Damage = 6f, Xp = 1,
            DeathColor = new Color(0.3f, 0.66f, 0.3f),
        };

        public static readonly EnemyDef Latecomer = new EnemyDef
        {
            Name = "Atrasado", Walk = () => Art.Student("FemaleAdventurer", "run"), Hp = 5f, Speed = 2.3f, Damage = 5f, Radius = 0.35f,
            Xp = 1, Bouncy = true, DeathColor = new Color(0.4f, 0.6f, 0.85f),
        };

        public static readonly EnemyDef Sleepy = new EnemyDef
        {
            Name = "Sonolento", Walk = () => Art.Student("Zombie"), Hp = 22f, Speed = 0.95f, Damage = 9f, Xp = 2,
            DeathColor = new Color(0.4f, 0.75f, 0.45f),
        };

        public static readonly EnemyDef Senior = new EnemyDef
        {
            Name = "Veterano", Walk = () => Art.Student("FemalePerson"), Hp = 38f, Speed = 1.35f, Damage = 12f, Xp = 3,
            DeathColor = new Color(0.45f, 0.35f, 0.7f),
        };

        public static readonly EnemyDef Nerd = new EnemyDef
        {
            Name = "Nerd", Walk = () => Art.Student("Robot"), Hp = 28f, Speed = 2f, Damage = 10f, Xp = 3,
            DeathColor = new Color(0.4f, 0.55f, 0.75f),
        };

        public static readonly EnemyDef Repeater = new EnemyDef
        {
            Name = "Repetente", Walk = () => Art.Student("MalePerson"), Hp = 160f, Speed = 0.85f, Damage = 18f, Radius = 0.7f,
            Scale = 1.8f, KnockResist = 0.6f, Xp = 10, DeathColor = new Color(0.95f, 0.6f, 0.25f),
        };

        public static readonly EnemyDef BossQuiz = new EnemyDef
        {
            Name = "Prova Surpresa", Sprite = () => Art.PaperBoss, Hp = 2200f, Speed = 1.5f, Damage = 25f, Radius = 1.2f,
            Scale = 3.2f, KnockResist = 0.95f, Xp = 60, Boss = true, DeathColor = new Color(0.85f, 0.3f, 0.3f),
            Tint = new Color(0.96f, 0.93f, 0.84f), // folha branca tingida de papel creme: aparece sobre o piso branco
        };

        public static readonly EnemyDef BossThesis = new EnemyDef
        {
            Name = "O TCC", Sprite = () => Art.ThesisBoss, Hp = 9000f, Speed = 1.75f, Damage = 35f, Radius = 1.5f,
            Scale = 4f, KnockResist = 0.98f, Xp = 0, Boss = true, FinalBoss = true, DeathColor = new Color(0.8f, 0.5f, 1f),
        };

        // ================================================================ Utilidades

        public static WeaponDef GetWeapon(WeaponId id)
        {
            foreach (var w in Weapons) if (w.Id == id) return w;
            return null;
        }

        public static PassiveDef GetPassive(PassiveId id)
        {
            foreach (var p in Passives) if (p.Id == id) return p;
            return null;
        }
    }

    /// <summary>Dados que atravessam cenas (menu → jogo).</summary>
    public static class GameSession
    {
        static CharacterDef selected;

        public static CharacterDef SelectedCharacter
        {
            get => selected ?? Database.Characters[0];
            set => selected = value;
        }
    }
}
