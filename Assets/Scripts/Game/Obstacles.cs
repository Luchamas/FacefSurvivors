using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>
    /// Mesas redondas espalhadas pelo mapa infinito. Bloqueiam o jogador e os inimigos.
    /// O mapa é dividido numa grade: cada célula pode ter uma mesa, numa posição calculada
    /// a partir de um hash (sempre a mesma para a mesma semente), então não é preciso guardar nada.
    /// </summary>
    public class Obstacles : MonoBehaviour
    {
        public const float CellSize = 11f;
        /// <summary>Raio de colisão da mesa (mesa + cadeiras).</summary>
        public const float TableRadius = 1.95f;
        /// <summary>Largura do sprite da mesa no mundo (inclui a sombra).</summary>
        const float TableWorldWidth = 4.95f;
        /// <summary>Área livre ao redor do ponto inicial do jogador.</summary>
        const float SafeRadius = 6f;
        /// <summary>Chance (0-255) de uma célula ficar sem mesa.</summary>
        const uint EmptyChance = 80;

        /// <summary>Semente do layout (sorteada a cada partida).</summary>
        public static int Seed = 12345;

        readonly Dictionary<long, Transform> shown = new Dictionary<long, Transform>();
        readonly HashSet<long> visible = new HashSet<long>();
        readonly List<long> toRemove = new List<long>();
        readonly Stack<Transform> pool = new Stack<Transform>();
        Sprite sprite;
        float scale;

        void Awake()
        {
            sprite = Art.Table;
            scale = TableWorldWidth / sprite.bounds.size.x;
        }

        // ------------------------------------------------------------ layout

        static uint Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)Seed;
                h ^= (uint)x * 0x9E3779B1u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0x85EBCA77u;
                h *= 0xC2B2AE3Du;
                h ^= h >> 16;
                h *= 0x27D4EB2Fu;
                h ^= h >> 15;
                return h;
            }
        }

        /// <summary>Retorna true se a célula (cx, cy) tem uma mesa, e onde ela está.</summary>
        public static bool TableAt(int cx, int cy, out Vector2 pos)
        {
            uint h = Hash(cx, cy);
            pos = default;
            if ((h & 0xFF) < EmptyChance) return false;
            float jx = ((h >> 8) & 0xFF) / 255f - 0.5f;
            float jy = ((h >> 16) & 0xFF) / 255f - 0.5f;
            pos = new Vector2((cx + 0.5f + jx * 0.3f) * CellSize, (cy + 0.5f + jy * 0.3f) * CellSize);
            return pos.sqrMagnitude > SafeRadius * SafeRadius;
        }

        /// <summary>Empurra um círculo (posição, raio) para fora de qualquer mesa.</summary>
        public static Vector2 Resolve(Vector2 p, float radius)
        {
            int cx = Mathf.FloorToInt(p.x / CellSize), cy = Mathf.FloorToInt(p.y / CellSize);
            for (int y = cy - 1; y <= cy + 1; y++)
                for (int x = cx - 1; x <= cx + 1; x++)
                {
                    if (!TableAt(x, y, out var c)) continue;
                    Vector2 d = p - c;
                    float min = TableRadius + radius;
                    float d2 = d.sqrMagnitude;
                    if (d2 >= min * min) continue;
                    float dist = Mathf.Sqrt(d2);
                    p = dist > 0.0001f ? c + d / dist * min : c + Vector2.up * min;
                }
            return p;
        }

        /// <summary>
        /// Desvia a direção de movimento para contornar a mesa à frente
        /// (os inimigos não ficam presos atrás das mesas).
        /// </summary>
        public static Vector2 Steer(Vector2 p, Vector2 dir, float radius)
        {
            const float look = 1.5f;
            int cx = Mathf.FloorToInt(p.x / CellSize), cy = Mathf.FloorToInt(p.y / CellSize);
            for (int y = cy - 1; y <= cy + 1; y++)
                for (int x = cx - 1; x <= cx + 1; x++)
                {
                    if (!TableAt(x, y, out var c)) continue;
                    Vector2 d = p - c;
                    float dist = d.magnitude;
                    float edge = dist - TableRadius - radius;
                    if (edge > look || dist < 0.0001f) continue;
                    Vector2 n = d / dist;
                    float approach = -Vector2.Dot(dir, n);
                    if (approach <= 0f) continue;
                    Vector2 t = new Vector2(-n.y, n.x);
                    if (Vector2.Dot(t, dir) < 0f) t = -t;
                    float k = approach * (1f - Mathf.Clamp01(edge / look));
                    dir = (dir + t * k * 2f).normalized;
                }
            return dir;
        }

        // ------------------------------------------------------------ visual

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 c = cam.transform.position;
            float hh = cam.orthographicSize + 3f, hw = cam.orthographicSize * cam.aspect + 3f;
            int x0 = Mathf.FloorToInt((c.x - hw) / CellSize), x1 = Mathf.FloorToInt((c.x + hw) / CellSize);
            int y0 = Mathf.FloorToInt((c.y - hh) / CellSize), y1 = Mathf.FloorToInt((c.y + hh) / CellSize);

            visible.Clear();
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!TableAt(x, y, out var pos)) continue;
                    long key = ((long)x << 32) ^ (uint)y;
                    visible.Add(key);
                    if (!shown.ContainsKey(key))
                    {
                        var t = Get();
                        t.position = new Vector3(pos.x, pos.y, 0f);
                        shown[key] = t;
                    }
                }

            toRemove.Clear();
            foreach (var kv in shown)
                if (!visible.Contains(kv.Key)) toRemove.Add(kv.Key);
            foreach (var key in toRemove)
            {
                var t = shown[key];
                t.gameObject.SetActive(false);
                pool.Push(t);
                shown.Remove(key);
            }
        }

        Transform Get()
        {
            if (pool.Count > 0)
            {
                var t = pool.Pop();
                t.gameObject.SetActive(true);
                return t;
            }
            var go = new GameObject("Mesa");
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -30;
            return go.transform;
        }
    }
}
