using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    public enum PickupType { Gem, Coxinha, Magnet, Chest }

    public class Pickup
    {
        public PickupType Type;
        public int Value;
        public Vector2 Pos;
        public GameObject Go;
        public Transform Tr;
        public SpriteRenderer Sr;
        public bool Attracted;
        public float Speed;
        public float Phase;
        public int Index;
    }

    /// <summary>Gemas de XP e itens no chão (coxinha, ímã, baú).</summary>
    public class PickupManager : MonoBehaviour
    {
        public static PickupManager Instance { get; private set; }

        const int MaxGems = 350;
        readonly List<Pickup> active = new List<Pickup>(512);
        readonly Stack<Pickup> pool = new Stack<Pickup>();
        int gemCount;

        void Awake()
        {
            Instance = this;
        }

        static int Tier(int value)
        {
            return value >= 10 ? 2 : value >= 3 ? 1 : 0;
        }

        public void SpawnGem(Vector2 pos, int value)
        {
            if (gemCount >= MaxGems && Player.Instance != null)
            {
                // muitas gemas no chão: soma o valor na gema mais distante
                Pickup target = null;
                float best = -1f;
                var pp = Player.Instance.Pos;
                foreach (var p in active)
                {
                    if (p.Type != PickupType.Gem || p.Attracted) continue;
                    float d = (p.Pos - pp).sqrMagnitude;
                    if (d > best)
                    {
                        best = d;
                        target = p;
                    }
                }
                if (target != null)
                {
                    target.Value += value;
                    target.Sr.sprite = Art.Gem(Tier(target.Value));
                    return;
                }
            }
            var g = Get(PickupType.Gem, pos);
            g.Value = value;
            g.Sr.sprite = Art.Gem(Tier(value));
            gemCount++;
        }

        public void Spawn(PickupType type, Vector2 pos)
        {
            var p = Get(type, pos);
            switch (type)
            {
                case PickupType.Coxinha: p.Sr.sprite = Art.Coxinha; break;
                case PickupType.Magnet: p.Sr.sprite = Art.Magnet; break;
                case PickupType.Chest:
                    p.Sr.sprite = Art.Chest;
                    p.Tr.localScale = Vector3.one * 1.4f;
                    break;
            }
        }

        Pickup Create()
        {
            var p = new Pickup();
            p.Go = new GameObject("Pickup");
            p.Tr = p.Go.transform;
            p.Tr.SetParent(transform, false);
            p.Sr = p.Go.AddComponent<SpriteRenderer>();
            return p;
        }

        Pickup Get(PickupType type, Vector2 pos)
        {
            var p = pool.Count > 0 ? pool.Pop() : Create();
            pos = Obstacles.Resolve(pos, 0.3f); // nunca dentro de uma mesa
            p.Type = type;
            p.Pos = pos;
            p.Value = 0;
            p.Attracted = false;
            p.Speed = 0f;
            p.Phase = Random.value * 6f;
            p.Tr.position = pos;
            p.Tr.localScale = Vector3.one;
            p.Sr.sortingOrder = type == PickupType.Gem ? -20 : -15;
            p.Go.SetActive(true);
            p.Index = active.Count;
            active.Add(p);
            return p;
        }

        void Release(Pickup p)
        {
            p.Go.SetActive(false);
            int last = active.Count - 1;
            var moved = active[last];
            active[p.Index] = moved;
            moved.Index = p.Index;
            active.RemoveAt(last);
            pool.Push(p);
        }

        public void AttractAllGems()
        {
            foreach (var p in active)
            {
                if (p.Type == PickupType.Gem && !p.Attracted)
                {
                    p.Attracted = true;
                    p.Speed = 3f;
                }
            }
        }

        void Update()
        {
            var gc = GameController.Instance;
            var player = Player.Instance;
            if (gc == null || player == null || gc.State != GameState.Playing) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 pp = player.Pos;
            float mag2 = player.Stats.Magnet * player.Stats.Magnet;
            float t = Time.time;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                Vector2 to = pp - p.Pos;
                float d2 = to.sqrMagnitude;
                if (!p.Attracted)
                {
                    // gemas são puxadas pelo ímã; itens precisam de contato
                    float range2 = p.Type == PickupType.Gem ? mag2 : 0.8f;
                    if (d2 < range2)
                    {
                        p.Attracted = true;
                        p.Speed = p.Type == PickupType.Gem ? -2.5f : 2f;
                    }
                }
                if (p.Attracted)
                {
                    float d = Mathf.Sqrt(d2);
                    if (d < 0.35f)
                    {
                        Collect(p);
                        continue;
                    }
                    p.Speed += 28f * dt;
                    p.Pos += to / d * Mathf.Min(p.Speed * dt, d);
                }
                float bob = p.Type == PickupType.Gem ? 0f : Mathf.Sin(t * 4f + p.Phase) * 0.08f;
                p.Tr.position = new Vector3(p.Pos.x, p.Pos.y + bob, 0f);
            }
        }

        void Collect(Pickup p)
        {
            var player = Player.Instance;
            switch (p.Type)
            {
                case PickupType.Gem:
                    gemCount--;
                    player.AddXp(p.Value);
                    AudioManager.Play(Sfx.Gem, 0.45f, 0.12f);
                    break;
                case PickupType.Coxinha:
                    player.Heal(30f);
                    AudioManager.Play(Sfx.Heal);
                    Effects.Instance.Burst(p.Pos, new Color(0.9f, 0.6f, 0.25f), 8, 2f, 0.1f);
                    break;
                case PickupType.Magnet:
                    AttractAllGems();
                    AudioManager.Play(Sfx.Magnet);
                    Effects.Instance.RingPulse(p.Pos, new Color(0.4f, 0.7f, 1f), 0.5f, 12f, 0.6f);
                    break;
                case PickupType.Chest:
                    GameController.Instance.OpenChest();
                    break;
            }
            Release(p);
        }
    }
}
