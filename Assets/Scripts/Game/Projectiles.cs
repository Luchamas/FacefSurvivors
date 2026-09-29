using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    public class Projectile
    {
        public GameObject Go;
        public Transform Tr;
        public SpriteRenderer Sr;
        public Weapon Owner;
        public Vector2 Pos, Vel;
        public float Gravity, Damage, Life, Radius, Knock, Spin, Angle;
        public int Pierce;
        public bool Align;
        public int Index;
        public readonly List<Enemy> HitList = new List<Enemy>(8);
    }

    /// <summary>Projéteis com pool (logos de TypeScript, Dart, C...).</summary>
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }

        readonly List<Projectile> active = new List<Projectile>(256);
        readonly Stack<Projectile> pool = new Stack<Projectile>();
        readonly List<Enemy> query = new List<Enemy>(64);

        void Awake()
        {
            Instance = this;
        }

        public Projectile Fire(Weapon owner, Sprite sprite, Vector2 pos, Vector2 vel, float damage, int pierce,
            float radius, float life, float scale, float knock)
        {
            Projectile p;
            if (pool.Count > 0) p = pool.Pop();
            else
            {
                p = new Projectile();
                p.Go = new GameObject("Projectile");
                p.Tr = p.Go.transform;
                p.Tr.SetParent(transform, false);
                p.Sr = p.Go.AddComponent<SpriteRenderer>();
                p.Sr.sortingOrder = 10;
            }
            p.Owner = owner;
            p.Pos = pos;
            p.Vel = vel;
            p.Damage = damage;
            p.Pierce = Mathf.Max(1, pierce);
            p.Radius = radius;
            p.Life = life;
            p.Knock = knock;
            p.Gravity = 0f;
            p.Spin = 0f;
            p.Align = false;
            p.Angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            p.HitList.Clear();
            p.Sr.sprite = sprite;
            p.Tr.localScale = Vector3.one * scale;
            p.Tr.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, p.Angle));
            p.Go.SetActive(true);
            p.Index = active.Count;
            active.Add(p);
            return p;
        }

        void Release(Projectile p)
        {
            p.Go.SetActive(false);
            int last = active.Count - 1;
            var moved = active[last];
            active[p.Index] = moved;
            moved.Index = p.Index;
            active.RemoveAt(last);
            pool.Push(p);
        }

        void Update()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.State != GameState.Playing) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var em = EnemyManager.Instance;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    Release(p);
                    continue;
                }
                p.Vel.y -= p.Gravity * dt;
                p.Pos += p.Vel * dt;
                if (p.Align) p.Angle = Mathf.Atan2(p.Vel.y, p.Vel.x) * Mathf.Rad2Deg;
                else p.Angle += p.Spin * dt;
                p.Tr.SetPositionAndRotation(p.Pos, Quaternion.Euler(0f, 0f, p.Angle));

                em.Query(p.Pos, p.Radius, query);
                bool spent = false;
                foreach (var e in query)
                {
                    if (p.HitList.Contains(e)) continue;
                    p.HitList.Add(e);
                    Vector2 kdir = p.Vel.sqrMagnitude > 0.01f ? p.Vel.normalized : (e.Pos - p.Pos).normalized;
                    p.Owner.DealDamage(e, p.Damage, kdir, p.Knock);
                    if (--p.Pierce <= 0)
                    {
                        spent = true;
                        break;
                    }
                }
                if (spent)
                {
                    Effects.Instance.Burst(p.Pos, new Color(1f, 0.75f, 0.2f), 3, 2f, 0.07f);
                    Release(p);
                }
            }
        }
    }
}
