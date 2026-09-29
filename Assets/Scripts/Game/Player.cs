using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    public class PassiveInstance
    {
        public PassiveDef Def;
        public int Level = 1;

        public bool IsMaxed => Level >= Def.MaxLevel;
    }

    /// <summary>
    /// O personagem controlado pelo jogador: movimento, vida, XP, armas e itens.
    /// </summary>
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        public const int MaxWeapons = 6;
        public const int MaxPassives = 6;

        public CharacterDef Character { get; private set; }
        public PlayerStats Stats { get; private set; } = new PlayerStats();

        public float Hp;
        public int Level = 1;
        public float Xp;
        public float XpToNext = 5f;

        public Vector2 Pos;
        public Vector2 FacingDir = Vector2.right;
        public float FacingX = 1f;
        public float Radius = 0.4f;
        public bool Moving;
        public bool Dead;

        public readonly List<Weapon> Weapons = new List<Weapon>();
        public readonly List<PassiveInstance> Passives = new List<PassiveInstance>();

        /// <summary>Objetos visuais das armas (ex.: as luas do Lua) ficam aqui.</summary>
        public Transform WeaponRoot { get; private set; }

        /// <summary>Altura do desenho do personagem no mundo (unidades).</summary>
        const float SpriteHeight = 1.9f;
        /// <summary>Posição vertical do centro do desenho em relação ao jogador.</summary>
        const float BodyY = 0.22f;
        const float HpBarY = -0.9f;
        /// <summary>Tempo de cada quadro da animação de corrida.</summary>
        const float RunFrameTime = 0.12f;

        SpriteRenderer body;
        Transform bodyTr;
        SpriteRenderer hpFill;
        Sprite idleSprite, runSprite;
        float bodyScale;
        float invuln;
        float hurtFlash;
        float animT;

        public void Init(CharacterDef c)
        {
            Instance = this;
            Character = c;
            Pos = Vector2.zero;
            transform.position = Vector3.zero;

            idleSprite = Art.CharacterIdle(c);
            runSprite = Art.CharacterRun(c);

            var bodyGo = new GameObject("Body");
            bodyTr = bodyGo.transform;
            bodyTr.SetParent(transform, false);
            body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = idleSprite;
            body.sortingOrder = 1;
            // os dois quadros têm o mesmo tamanho de tela, com os pés alinhados embaixo
            bodyScale = SpriteHeight / idleSprite.bounds.size.y;
            bodyTr.localScale = new Vector3(bodyScale, bodyScale, 1f);
            bodyTr.localPosition = new Vector3(0f, BodyY, 0f);

            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, BodyY - SpriteHeight * 0.5f + 0.03f, 0f);
            shadow.transform.localScale = new Vector3(1.2f, 1f, 1f);
            shadow.sprite = Art.Shadow;
            shadow.sortingOrder = -50;

            // barrinha de vida embaixo do personagem
            var bg = new GameObject("HpBarBg").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(transform, false);
            bg.transform.localPosition = new Vector3(0f, HpBarY, 0f);
            bg.sprite = Art.WorldBar("White");
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(0.92f, 0.13f);
            bg.color = new Color(0.12f, 0.1f, 0.16f, 0.9f);
            bg.sortingOrder = 50;

            hpFill = new GameObject("HpBarFill").AddComponent<SpriteRenderer>();
            hpFill.transform.SetParent(transform, false);
            hpFill.sprite = Art.WorldBar("Red");
            hpFill.drawMode = SpriteDrawMode.Sliced;
            hpFill.sortingOrder = 51;

            WeaponRoot = new GameObject("PlayerWeapons").transform;

            RecalculateStats();
            Hp = Stats.MaxHp;
            AddWeapon(Database.GetWeapon(c.StartWeapon));
            UpdateHpBar();
        }

        void Update()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.State != GameState.Playing || Dead) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 move = GameInput.Move;
            Moving = move.sqrMagnitude > 0.01f;
            if (Moving)
            {
                FacingDir = move.normalized;
                if (Mathf.Abs(move.x) > 0.1f) FacingX = Mathf.Sign(move.x);
            }
            Pos = Obstacles.Resolve(Pos + move * Stats.Speed * dt, Radius);
            transform.position = new Vector3(Pos.x, Pos.y, 0f);

            if (Stats.Regen > 0f && Hp < Stats.MaxHp) Hp = Mathf.Min(Stats.MaxHp, Hp + Stats.Regen * dt);
            invuln -= dt;
            hurtFlash -= dt;

            Animate(dt);

            var c = Color.white;
            if (hurtFlash > 0f) c = new Color(1f, 0.3f, 0.3f);
            else if (invuln > 0f && Mathf.Repeat(Time.time * 16f, 1f) > 0.5f) c.a = 0.55f;
            body.color = c;
            UpdateHpBar();

            for (int i = 0; i < Weapons.Count; i++) Weapons[i].Tick(dt);
        }

        /// <summary>
        /// Correndo: alterna os dois quadros (pernas abertas / pernas juntas) e inclina para frente.
        /// Parado: quadro em pé respirando.
        /// </summary>
        void Animate(float dt)
        {
            if (Moving)
            {
                animT += dt;
                bool open = (int)(animT / RunFrameTime) % 2 == 0;
                body.sprite = open ? runSprite : idleSprite;
                bodyTr.localScale = new Vector3(bodyScale, bodyScale, 1f);
                bodyTr.localPosition = new Vector3(0f, BodyY + (open ? 0f : 0.04f), 0f);
                bodyTr.localRotation = Quaternion.Euler(0f, 0f, -FacingX * 4f);
            }
            else
            {
                animT = 0f;
                body.sprite = idleSprite;
                float breath = Mathf.Sin(Time.time * 3f) * 0.015f;
                bodyTr.localScale = new Vector3(bodyScale * (1f - breath * 0.5f), bodyScale * (1f + breath), 1f);
                bodyTr.localPosition = new Vector3(0f, BodyY + breath * SpriteHeight * 0.5f, 0f);
                bodyTr.localRotation = Quaternion.identity;
            }
            body.flipX = FacingX < 0f; // o desenho original olha para a direita
        }

        void UpdateHpBar()
        {
            float k = Stats.MaxHp > 0f ? Mathf.Clamp01(Hp / Stats.MaxHp) : 0f;
            const float w = 0.86f;
            hpFill.enabled = w * k >= 0.1f; // menor que as pontas arredondadas: some
            hpFill.size = new Vector2(w * k, 0.09f);
            hpFill.transform.localPosition = new Vector3(-w * 0.5f + w * k * 0.5f, HpBarY, 0f);
        }

        // ------------------------------------------------------------ vida / XP

        public void TakeDamage(float amount)
        {
            if (invuln > 0f || Dead) return;
            float dmg = Mathf.Max(1f, amount - Stats.Armor);
            Hp -= dmg;
            invuln = 0.45f;
            hurtFlash = 0.15f;
            AudioManager.Play(Sfx.Hurt, 0.7f);
            var gc = GameController.Instance;
            gc.Cam.Shake(0.12f, 0.2f);
            gc.Hud.FlashDamage();
            if (Hp <= 0f)
            {
                Hp = 0f;
                Dead = true;
                UpdateHpBar();
                gc.OnPlayerDied();
            }
        }

        public void Heal(float amount)
        {
            Hp = Mathf.Min(Stats.MaxHp, Hp + amount);
            Effects.Instance.RingPulse(Pos, new Color(0.5f, 1f, 0.5f, 0.8f), 0.4f, 2f, 0.4f);
        }

        public void AddXp(float amount)
        {
            Xp += amount;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                XpToNext = XpFor(Level);
                GameController.Instance.QueueLevelUp();
            }
        }

        /// <summary>XP necessário para sair do nível informado (mesma curva de Vampire Survivors).</summary>
        public static float XpFor(int level)
        {
            if (level < 20) return 5 + (level - 1) * 10;
            if (level < 40) return 195 + (level - 20) * 13;
            return 455 + (level - 40) * 16;
        }

        // ------------------------------------------------------------ armas / itens

        public Weapon GetWeapon(WeaponId id)
        {
            foreach (var w in Weapons) if (w.Def.Id == id) return w;
            return null;
        }

        public PassiveInstance GetPassive(PassiveId id)
        {
            foreach (var p in Passives) if (p.Def.Id == id) return p;
            return null;
        }

        public Weapon AddWeapon(WeaponDef def)
        {
            var w = def.Create();
            w.Init(def, this, Weapons.Count);
            Weapons.Add(w);
            return w;
        }

        public PassiveInstance AddPassive(PassiveDef def)
        {
            var p = new PassiveInstance { Def = def, Level = 1 };
            Passives.Add(p);
            RecalculateStats();
            return p;
        }

        public void LevelUpPassive(PassiveInstance p)
        {
            if (p.IsMaxed) return;
            p.Level++;
            RecalculateStats();
        }

        public void RecalculateStats()
        {
            float oldMax = Stats.MaxHp;
            var s = new PlayerStats();
            Character?.Modify?.Invoke(s);
            foreach (var p in Passives) p.Def.Apply(s, p.Level);
            Stats = s;
            if (s.MaxHp > oldMax) Hp += s.MaxHp - oldMax; // ganhou vida máxima: recebe a diferença
            Hp = Mathf.Min(Hp, s.MaxHp);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
