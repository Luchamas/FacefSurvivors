using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>
    /// Base das armas. Cada arma dispara sozinha; o jogador só precisa se mover.
    /// Os atributos efetivos combinam os da arma com os bônus do jogador.
    /// </summary>
    public abstract class Weapon
    {
        public WeaponDef Def;
        public int Level = 1;
        public WeaponStats Raw;
        public Player Owner;
        public float TotalDamage;
        public int Slot;

        protected float timer;
        protected static readonly List<Enemy> Query = new List<Enemy>(128);

        public void Init(WeaponDef def, Player owner, int slot)
        {
            Def = def;
            Owner = owner;
            Slot = slot;
            Raw = def.Base.Clone();
            timer = 0.4f;
            OnInit();
        }

        public bool IsMaxed => Level >= Def.MaxLevel;

        public string NextLevelDescription => IsMaxed ? "" : Def.Levels[Level - 1].Desc;

        public void LevelUp()
        {
            if (IsMaxed) return;
            Def.Levels[Level - 1].Apply(Raw);
            Level++;
            OnLevelUp();
        }

        public float Damage => Raw.Damage * Owner.Stats.Might;
        public float Cooldown => Mathf.Max(0.15f, Raw.Cooldown * Owner.Stats.CooldownMult);
        public int Amount => Mathf.Max(1, Raw.Amount + Owner.Stats.Amount);
        public float Area => Raw.Area * Owner.Stats.Area;
        public float Speed => Raw.Speed * Owner.Stats.ProjSpeed;
        public float Duration => Raw.Duration * Owner.Stats.Duration;

        public abstract void Tick(float dt);

        protected virtual void OnInit() { }

        protected virtual void OnLevelUp() { }

        public void DealDamage(Enemy e, float dmg, Vector2 dir, float knock)
        {
            TotalDamage += EnemyManager.Instance.Damage(e, dmg, dir, knock);
        }

        /// <summary>Limita a frequência com que esta arma acerta o mesmo inimigo.</summary>
        protected bool Ready(Enemy e, float interval)
        {
            float t = Time.time;
            if (t - e.HitTimes[Slot] < interval) return false;
            e.HitTimes[Slot] = t;
            return true;
        }

        protected static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }

    /// <summary>Arma que dispara uma rajada de N projéteis a cada recarga.</summary>
    public abstract class BurstWeapon : Weapon
    {
        int burstLeft;
        int burstIndex;
        float burstTimer;
        protected float burstInterval = 0.08f;

        public override void Tick(float dt)
        {
            if (burstLeft > 0)
            {
                burstTimer -= dt;
                if (burstTimer <= 0f)
                {
                    burstTimer = burstInterval;
                    FireOne(burstIndex++);
                    burstLeft--;
                }
                return;
            }
            timer -= dt;
            if (timer > 0f) return;
            if (CanFire())
            {
                burstLeft = Amount;
                burstIndex = 0;
                burstTimer = 0f;
                timer = Cooldown;
            }
            else timer = 0.2f;
        }

        protected virtual bool CanFire() => true;

        protected abstract void FireOne(int index);
    }

    // ==================================================================== TypeScript

    public class TypeScriptWeapon : BurstWeapon
    {
        const float Range = 14f;

        protected override bool CanFire()
        {
            return EnemyManager.Instance.Nearest(Owner.Pos, Range) != null;
        }

        protected override void FireOne(int index)
        {
            var target = EnemyManager.Instance.Nearest(Owner.Pos, Range);
            Vector2 dir = target != null ? (target.Pos - Owner.Pos).normalized : Owner.FacingDir;
            dir = Rotate(dir, Random.Range(-5f, 5f));
            var p = ProjectileManager.Instance.Fire(this, Art.LanguageInWorld("TypeScript"), Owner.Pos, dir * Speed, Damage, Raw.Pierce,
                0.28f * Area, 2.2f, Area, Raw.Knockback);
            p.Spin = 360f; // gira como uma carta arremessada
            AudioManager.Play(Sfx.Shoot, 0.35f, 0.1f);
        }
    }

    // ==================================================================== Dart

    public class DartWeapon : BurstWeapon
    {
        protected override void OnInit()
        {
            burstInterval = 0.07f;
        }

        protected override void FireOne(int index)
        {
            Vector2 dir = Owner.FacingDir;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 pos = Owner.Pos + perp * Random.Range(-0.25f, 0.25f);
            var p = ProjectileManager.Instance.Fire(this, Art.LanguageInWorld("Dart"), pos, Rotate(dir, Random.Range(-3f, 3f)) * Speed,
                Damage, Raw.Pierce, 0.22f * Area, 1.3f, 0.8f * Area, Raw.Knockback);
            p.Angle = 0f; // logo sempre em pé
            p.Tr.rotation = Quaternion.identity;
            AudioManager.Play(Sfx.Shoot, 0.3f, 0.15f);
        }
    }

    // ==================================================================== C

    public class CWeapon : BurstWeapon
    {
        protected override void OnInit()
        {
            burstInterval = 0.12f;
        }

        protected override void FireOne(int index)
        {
            float side = (index % 2 == 0 ? 1f : -1f) * Owner.FacingX;
            var vel = new Vector2(side * Random.Range(1f, 3.2f), Random.Range(9.5f, 11.5f)) * Speed;
            var p = ProjectileManager.Instance.Fire(this, Art.LanguageInWorld("C"), Owner.Pos, vel, Damage, Raw.Pierce,
                0.42f * Area, 2.6f, 1.3f * Area, Raw.Knockback);
            p.Gravity = 20f;
            p.Spin = -side * 540f;
            AudioManager.Play(Sfx.Throw, 0.45f, 0.1f);
        }
    }

    // ==================================================================== Bash

    public class BashWeapon : Weapon
    {
        struct PendingStrike
        {
            public float Delay, Side, YOffset;
        }

        readonly List<PendingStrike> pending = new List<PendingStrike>();

        public override void Tick(float dt)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var s = pending[i];
                s.Delay -= dt;
                if (s.Delay <= 0f)
                {
                    Strike(s.Side, s.YOffset);
                    pending.RemoveAt(i);
                }
                else pending[i] = s;
            }

            timer -= dt;
            if (timer > 0f) return;
            timer = Cooldown;
            float facing = Owner.FacingX;
            for (int k = 0; k < Amount; k++)
            {
                pending.Add(new PendingStrike
                {
                    Delay = k * 0.13f,
                    Side = k % 2 == 0 ? facing : -facing,
                    YOffset = (k / 2) * 0.55f,
                });
            }
        }

        void Strike(float side, float yOffset)
        {
            Vector2 size = new Vector2(3.4f, 1.0f) * Area;
            Vector2 center = Owner.Pos + new Vector2(side * (size.x * 0.5f + 0.1f), 0.15f + yOffset);
            EnemyManager.Instance.QueryBox(center, size * 0.5f, Query);
            foreach (var e in Query) DealDamage(e, Damage, new Vector2(side, 0f), Raw.Knockback);
            Effects.Instance.Slash(center, side > 0f, new Vector2(size.x, size.y * 1.4f));
            AudioManager.Play(Sfx.Whip, 0.5f, 0.1f);
        }
    }

    // ==================================================================== Lua

    public class LuaWeapon : Weapon
    {
        readonly List<SpriteRenderer> moons = new List<SpriteRenderer>();
        bool spinning;
        float angle;

        protected override void OnInit()
        {
            timer = 0.5f;
        }

        void EnsureMoons(int n)
        {
            while (moons.Count < n)
            {
                var go = new GameObject("Lua");
                go.transform.SetParent(Owner.WeaponRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.LanguageInWorld("Lua");
                sr.sortingOrder = 15;
                sr.enabled = false;
                moons.Add(sr);
            }
        }

        public override void Tick(float dt)
        {
            timer -= dt;
            if (!spinning)
            {
                if (timer <= 0f)
                {
                    spinning = true;
                    timer = Duration;
                }
                return;
            }

            int n = Amount;
            EnsureMoons(n);
            angle += 3.2f * Speed * dt;
            float radius = 1.7f * Area;
            float hitR = 0.42f * Area;
            float grow = Mathf.Clamp01((Duration - timer) / 0.2f);
            float shrink = Mathf.Clamp01(timer / 0.2f);
            float s = Mathf.Min(grow, shrink) * (0.55f + 0.35f * Area);

            for (int i = 0; i < moons.Count; i++)
            {
                var m = moons[i];
                if (i >= n)
                {
                    m.enabled = false;
                    continue;
                }
                float a = angle + i * Mathf.PI * 2f / n;
                Vector2 pos = Owner.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                m.enabled = true;
                m.transform.position = pos;
                m.transform.localScale = new Vector3(s, s, 1f);

                EnemyManager.Instance.Query(pos, hitR, Query);
                foreach (var e in Query)
                    if (Ready(e, Raw.HitInterval))
                        DealDamage(e, Damage, (e.Pos - Owner.Pos).normalized, Raw.Knockback);
            }

            if (timer <= 0f)
            {
                spinning = false;
                timer = Cooldown;
                foreach (var m in moons) m.enabled = false;
            }
        }
    }

    // ==================================================================== HTML/CSS

    public class HtmlCssWeapon : Weapon
    {
        static readonly Color HtmlOrange = new Color(0.89f, 0.31f, 0.15f);
        static readonly Color CssBlue = new Color(0.08f, 0.45f, 0.71f);

        SpriteRenderer aura, ring;
        float bubbleTimer;
        bool cssBubble;

        protected override void OnInit()
        {
            var go = new GameObject("HtmlCssAura");
            go.transform.SetParent(Owner.transform, false);
            aura = go.AddComponent<SpriteRenderer>();
            aura.sprite = Art.Disc;
            aura.color = new Color(HtmlOrange.r, HtmlOrange.g, HtmlOrange.b, 0.2f);
            aura.sortingOrder = -40;

            var rgo = new GameObject("HtmlCssRing");
            rgo.transform.SetParent(Owner.transform, false);
            ring = rgo.AddComponent<SpriteRenderer>();
            ring.sprite = Art.Ring;
            ring.color = new Color(CssBlue.r, CssBlue.g, CssBlue.b, 0.45f);
            ring.sortingOrder = -39;
        }

        public override void Tick(float dt)
        {
            float radius = 1.35f * Area;
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.04f;
            float d = radius * 2f * pulse;
            aura.transform.localScale = new Vector3(d, d, 1f);
            ring.transform.localScale = new Vector3(d, d, 1f);

            bubbleTimer -= dt;
            if (bubbleTimer <= 0f)
            {
                bubbleTimer = 0.12f;
                cssBubble = !cssBubble;
                var c = cssBubble ? CssBlue : HtmlOrange;
                Effects.Instance.Burst(Owner.Pos + Random.insideUnitCircle * radius, new Color(c.r, c.g, c.b, 0.7f), 1, 0.6f, 0.08f);
            }

            EnemyManager.Instance.Query(Owner.Pos, radius, Query);
            float interval = Mathf.Max(0.2f, Raw.HitInterval * Owner.Stats.CooldownMult);
            foreach (var e in Query)
                if (Ready(e, interval))
                    DealDamage(e, Damage, (e.Pos - Owner.Pos).normalized, Raw.Knockback);
        }
    }

    // ==================================================================== SQL

    public class SqlWeapon : Weapon
    {
        readonly List<Enemy> targets = new List<Enemy>(16);
        readonly List<Vector2> targetPos = new List<Vector2>(16);
        int next;
        float strikeTimer;

        public override void Tick(float dt)
        {
            if (next < targets.Count)
            {
                strikeTimer -= dt;
                if (strikeTimer <= 0f)
                {
                    strikeTimer = 0.1f;
                    var e = targets[next];
                    Strike(e.Alive ? e.Pos : targetPos[next]);
                    next++;
                }
            }

            timer -= dt;
            if (timer > 0f) return;
            EnemyManager.Instance.RandomVisible(Amount, targets);
            if (targets.Count == 0)
            {
                timer = 0.3f;
                return;
            }
            targetPos.Clear();
            foreach (var e in targets) targetPos.Add(e.Pos);
            next = 0;
            strikeTimer = 0f;
            timer = Cooldown;
        }

        void Strike(Vector2 pos)
        {
            float r = 0.9f * Area;
            Effects.Instance.Lightning(pos, Area);
            AudioManager.Play(Sfx.Lightning, 0.4f, 0.12f);
            GameController.Instance.Cam.Shake(0.05f, 0.08f);
            EnemyManager.Instance.Query(pos, r, Query);
            foreach (var e in Query)
            {
                Vector2 dir = e.Pos - pos;
                DealDamage(e, Damage, dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up, Raw.Knockback);
            }
        }
    }
}
