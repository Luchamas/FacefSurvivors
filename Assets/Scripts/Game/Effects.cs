using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>
    /// Efeitos visuais com pool: partículas, números de dano, golpes, raios e anéis.
    /// </summary>
    public class Effects : MonoBehaviour
    {
        public static Effects Instance { get; private set; }

        class Particle
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public Vector2 Vel;
            public float Life, MaxLife, Size;
            public Color Color;
        }

        class Number
        {
            public Transform Tr;
            public TextMesh Tm;
            public TextMesh[] Outline;
            public float Life;
            public Vector3 Pos;
        }

        class FxSprite
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public float Life, MaxLife;
            public Vector3 ScaleFrom, ScaleTo;
            public Color Color;
        }

        const int MaxNumbers = 90;
        const int MaxParticles = 500;
        const float NumberLife = 0.6f;

        readonly List<Particle> parts = new List<Particle>();
        readonly Stack<Particle> partPool = new Stack<Particle>();
        readonly List<Number> nums = new List<Number>();
        readonly Stack<Number> numPool = new Stack<Number>();
        readonly List<FxSprite> sprites = new List<FxSprite>();
        readonly Stack<FxSprite> spritePool = new Stack<FxSprite>();

        void Awake()
        {
            Instance = this;
        }

        // ------------------------------------------------------------ API

        public void Burst(Vector2 pos, Color color, int count, float speed, float size)
        {
            for (int i = 0; i < count && parts.Count < MaxParticles; i++)
            {
                var p = partPool.Count > 0 ? partPool.Pop() : NewParticle();
                p.Vel = Random.insideUnitCircle.normalized * Random.Range(0.3f, 1f) * speed;
                p.Life = p.MaxLife = Random.Range(0.25f, 0.5f);
                p.Size = size * Random.Range(0.7f, 1.3f);
                p.Color = color;
                p.Tr.position = pos;
                p.Tr.localScale = Vector3.one * p.Size;
                p.Sr.color = color;
                p.Tr.gameObject.SetActive(true);
                parts.Add(p);
            }
        }

        public void DamageNumber(Vector2 pos, float dmg)
        {
            if (!GameSettings.ShowDamageNumbers || nums.Count >= MaxNumbers) return;
            var n = numPool.Count > 0 ? numPool.Pop() : NewNumber();
            string s = Mathf.Max(1, Mathf.RoundToInt(dmg)).ToString();
            n.Tm.text = s;
            foreach (var o in n.Outline) o.text = s;
            n.Life = NumberLife;
            n.Pos = new Vector3(pos.x + Random.Range(-0.15f, 0.15f), pos.y, 0f);
            n.Tr.position = n.Pos;
            n.Tr.gameObject.SetActive(true);
            nums.Add(n);
        }

        /// <summary>Golpe em arco. <paramref name="size"/> em unidades do mundo.</summary>
        public void Slash(Vector2 pos, bool right, Vector2 size)
        {
            var f = GetSprite(Art.Slash, 30);
            f.Tr.position = pos;
            f.Sr.flipX = !right;
            var sc = new Vector3(size.x / 2f, size.y, 1f); // o sprite tem 2 x 1 unidades
            f.ScaleFrom = new Vector3(sc.x * 0.55f, sc.y * 0.8f, 1f);
            f.ScaleTo = sc;
            f.Life = f.MaxLife = 0.22f;
            f.Color = new Color(0.3f, 0.5f, 1f, 0.9f);
        }

        public void Lightning(Vector2 pos, float scale)
        {
            var f = GetSprite(Art.Lightning(Random.Range(0, 2)), 40);
            f.Tr.position = pos;
            f.Sr.flipX = Random.value < 0.5f;
            f.ScaleFrom = f.ScaleTo = new Vector3(1.2f, 1.9f, 1f);
            f.Life = f.MaxLife = 0.25f;
            f.Color = Color.white;

            var g = GetSprite(Art.Dot, 39);
            g.Tr.position = pos;
            g.ScaleFrom = Vector3.one * 1.4f * scale;
            g.ScaleTo = Vector3.one * 2.6f * scale;
            g.Life = g.MaxLife = 0.3f;
            g.Color = new Color(0.35f, 0.6f, 1f, 0.8f);

            RingPulse(pos, new Color(0.25f, 0.5f, 1f, 0.9f), 0.3f * scale, 2f * scale, 0.3f);
        }

        public void RingPulse(Vector2 pos, Color c, float from, float to, float duration)
        {
            var f = GetSprite(Art.Ring, 25);
            f.Tr.position = pos;
            f.ScaleFrom = Vector3.one * from;
            f.ScaleTo = Vector3.one * to;
            f.Life = f.MaxLife = duration;
            f.Color = c;
        }

        // ------------------------------------------------------------ criação

        Particle NewParticle()
        {
            var go = new GameObject("Particle");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Dot;
            sr.sortingOrder = 20;
            return new Particle { Tr = go.transform, Sr = sr };
        }

        Number NewNumber()
        {
            var go = new GameObject("DamageNumber");
            go.transform.SetParent(transform, false);
            var n = new Number { Tr = go.transform };
            // contorno preto (4 cópias deslocadas) para ler bem em qualquer fundo
            const float o = 0.03f;
            var offsets = new[] { new Vector3(o, o), new Vector3(-o, o), new Vector3(o, -o), new Vector3(-o, -o) };
            n.Outline = new TextMesh[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
                n.Outline[i] = MakeText(go.transform, offsets[i], new Color(0f, 0f, 0f, 0.95f), 99);
            n.Tm = MakeText(go.transform, Vector3.zero, Color.white, 100);
            return n;
        }

        static TextMesh MakeText(Transform parent, Vector3 offset, Color color, int order)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var tm = go.AddComponent<TextMesh>();
            tm.font = UI.Font;
            tm.fontSize = 48;
            tm.characterSize = 0.075f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UI.Font.material;
            mr.sortingOrder = order;
            return tm;
        }

        FxSprite GetSprite(Sprite sprite, int order)
        {
            FxSprite f;
            if (spritePool.Count > 0) f = spritePool.Pop();
            else
            {
                var go = new GameObject("Fx");
                go.transform.SetParent(transform, false);
                f = new FxSprite { Tr = go.transform, Sr = go.AddComponent<SpriteRenderer>() };
            }
            f.Sr.sprite = sprite;
            f.Sr.sortingOrder = order;
            f.Sr.flipX = false;
            f.Tr.rotation = Quaternion.identity;
            f.Tr.gameObject.SetActive(true);
            sprites.Add(f);
            return f;
        }

        // ------------------------------------------------------------ atualização

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    p.Tr.gameObject.SetActive(false);
                    partPool.Push(p);
                    parts[i] = parts[parts.Count - 1];
                    parts.RemoveAt(parts.Count - 1);
                    continue;
                }
                p.Tr.position += (Vector3)(p.Vel * dt);
                p.Vel *= Mathf.Exp(-4f * dt);
                float k = p.Life / p.MaxLife;
                p.Tr.localScale = Vector3.one * (p.Size * (0.4f + 0.6f * k));
                p.Sr.color = new Color(p.Color.r, p.Color.g, p.Color.b, k);
            }

            for (int i = nums.Count - 1; i >= 0; i--)
            {
                var n = nums[i];
                n.Life -= dt;
                if (n.Life <= 0f)
                {
                    n.Tr.gameObject.SetActive(false);
                    numPool.Push(n);
                    nums[i] = nums[nums.Count - 1];
                    nums.RemoveAt(nums.Count - 1);
                    continue;
                }
                float prog = 1f - n.Life / NumberLife;
                float rise = 1f - (1f - prog) * (1f - prog);
                n.Tr.position = n.Pos + new Vector3(0f, rise * 0.55f, 0f);
                float pop = prog < 0.15f ? Mathf.Lerp(1.5f, 1f, prog / 0.15f) : 1f;
                n.Tr.localScale = Vector3.one * pop;
                float a = prog > 0.6f ? 1f - (prog - 0.6f) / 0.4f : 1f;
                n.Tm.color = new Color(1f, 1f, 1f, a);
                var oc = new Color(0f, 0f, 0f, 0.95f * a);
                foreach (var ot in n.Outline) ot.color = oc;
            }

            for (int i = sprites.Count - 1; i >= 0; i--)
            {
                var f = sprites[i];
                f.Life -= dt;
                if (f.Life <= 0f)
                {
                    f.Tr.gameObject.SetActive(false);
                    spritePool.Push(f);
                    sprites[i] = sprites[sprites.Count - 1];
                    sprites.RemoveAt(sprites.Count - 1);
                    continue;
                }
                float k = 1f - f.Life / f.MaxLife;
                float ease = 1f - (1f - k) * (1f - k);
                f.Tr.localScale = Vector3.Lerp(f.ScaleFrom, f.ScaleTo, ease);
                f.Sr.color = new Color(f.Color.r, f.Color.g, f.Color.b, f.Color.a * (1f - k));
            }
        }
    }
}
