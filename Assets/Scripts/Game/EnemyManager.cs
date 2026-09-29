using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FacefSurvivors
{
    /// <summary>
    /// Cria, move e remove inimigos. Mantém uma grade espacial para que armas e
    /// separação entre inimigos encontrem vizinhos rapidamente.
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        public readonly List<Enemy> Active = new List<Enemy>(512);
        readonly Stack<Enemy> pool = new Stack<Enemy>();

        /// <summary>Chefe vivo no momento (para a barra de vida do HUD).</summary>
        public Enemy Boss { get; private set; }

        // ------------------------------------------------------------ grade espacial
        const int GridN = 48;
        const float Cell = 1.5f;
        const float MaxRadius = 1.6f;
        readonly int[] cellStart = new int[GridN * GridN];
        readonly int[] cellCount = new int[GridN * GridN];
        readonly int[] cellFill = new int[GridN * GridN];
        Enemy[] sorted = new Enemy[512];
        int[] enemyCell = new int[512];
        Vector2 gridOrigin;

        // ------------------------------------------------------------ ondas
        class Wave
        {
            public float Interval;
            public int Batch, Min, Max;
            public EnemyDef[] Types;
            public float[] Weights;
        }

        class TimedEvent
        {
            public float Time;
            public Action Run;
            public bool Done;
        }

        Wave[] waves;
        List<TimedEvent> events;
        float spawnTimer = 0.5f;
        readonly List<Enemy> tmp = new List<Enemy>(256);

        void Awake()
        {
            Instance = this;
            BuildWaves();
            BuildEvents();
        }

        static Wave W(float interval, int batch, int min, int max, params object[] typesAndWeights)
        {
            int n = typesAndWeights.Length / 2;
            var w = new Wave { Interval = interval, Batch = batch, Min = min, Max = max, Types = new EnemyDef[n], Weights = new float[n] };
            for (int i = 0; i < n; i++)
            {
                w.Types[i] = (EnemyDef)typesAndWeights[i * 2];
                w.Weights[i] = Convert.ToSingle(typesAndWeights[i * 2 + 1]);
            }
            return w;
        }

        void BuildWaves()
        {
            waves = new[]
            {
                /* 0 min */ W(0.85f, 1, 10, 50, Database.Freshman, 3f, Database.Latecomer, 1f),
                /* 1 min */ W(0.70f, 2, 20, 80, Database.Freshman, 2f, Database.Latecomer, 2f, Database.Sleepy, 1f),
                /* 2 min */ W(0.65f, 2, 30, 110, Database.Latecomer, 2f, Database.Sleepy, 3f, Database.Freshman, 1f),
                /* 3 min */ W(0.60f, 2, 40, 140, Database.Sleepy, 2f, Database.Senior, 2f, Database.Latecomer, 1f),
                /* 4 min */ W(0.55f, 3, 50, 170, Database.Senior, 3f, Database.Nerd, 2f, Database.Sleepy, 1f),
                /* 5 min */ W(0.50f, 3, 60, 200, Database.Nerd, 3f, Database.Senior, 2f, Database.Repeater, 0.3f),
                /* 6 min */ W(0.45f, 3, 70, 230, Database.Senior, 2f, Database.Nerd, 2f, Database.Sleepy, 2f, Database.Repeater, 0.5f),
                /* 7 min */ W(0.42f, 4, 80, 260, Database.Nerd, 3f, Database.Latecomer, 3f, Database.Repeater, 0.6f),
                /* 8 min */ W(0.38f, 4, 90, 290, Database.Senior, 3f, Database.Nerd, 2f, Database.Repeater, 0.8f),
                /* 9 min+ */ W(0.35f, 4, 100, 320, Database.Senior, 2f, Database.Nerd, 2f, Database.Sleepy, 2f, Database.Latecomer, 2f, Database.Repeater, 1f),
            };
        }

        void BuildEvents()
        {
            // super ondas (revoadas e cercos) acontecem na "Hora do Intervalo"
            events = new List<TimedEvent>
            {
                new TimedEvent { Time = 60f, Run = () => Swarm(Database.Latecomer, 26, "Todo mundo correndo para a cantina!") },
                new TimedEvent { Time = 150f, Run = () => Ring(Database.Sleepy, 32, "Você foi cercado! Abra caminho!") },
                new TimedEvent { Time = 210f, Run = () => Swarm(Database.Latecomer, 34, "Todo mundo correndo para a cantina!") },
                new TimedEvent { Time = 270f, Run = () => Ring(Database.Senior, 36, "Os veteranos cercaram você!") },
                new TimedEvent { Time = 300f, Run = () => SpawnBoss(Database.BossQuiz, "Uma PROVA SURPRESA apareceu!") },
                new TimedEvent { Time = 390f, Run = () => Swarm(Database.Nerd, 30, "Os nerds correram para a biblioteca!") },
                new TimedEvent { Time = 450f, Run = () => Ring(Database.Senior, 44, "Os veteranos cercaram você!") },
                new TimedEvent { Time = 510f, Run = () => Swarm(Database.Latecomer, 50, "A faculdade inteira saiu das salas!") },
                new TimedEvent { Time = 540f, Run = () => Ring(Database.Repeater, 10, "Os repetentes chegaram!") },
                new TimedEvent { Time = GameController.FinalBossTime, Run = () => SpawnBoss(Database.BossThesis, "O TCC chegou! Derrote-o para se formar!") },
            };
        }

        // ------------------------------------------------------------ loop principal

        void Update()
        {
            var gc = GameController.Instance;
            var player = Player.Instance;
            if (gc == null || player == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f || gc.State != GameState.Playing) return;

            float time = gc.ElapsedTime;
            foreach (var ev in events)
            {
                if (!ev.Done && time >= ev.Time)
                {
                    ev.Done = true;
                    ev.Run();
                }
            }
            Spawning(time, dt);
            Simulate(dt, player);
            RebuildGrid(player.Pos);
        }

        void Spawning(float time, float dt)
        {
            int minute = Mathf.Clamp((int)(time / 60f), 0, waves.Length - 1);
            var w = waves[minute];
            spawnTimer -= dt;
            if (spawnTimer > 0f) return;
            bool below = Active.Count < w.Min;
            spawnTimer = below ? w.Interval * 0.3f : w.Interval;
            if (Active.Count >= w.Max) return;
            int batch = below ? w.Batch + 2 : w.Batch;
            for (int i = 0; i < batch; i++) Spawn(Pick(w), SpawnPoint());
        }

        static EnemyDef Pick(Wave w)
        {
            float total = 0f;
            foreach (var x in w.Weights) total += x;
            float r = Random.value * total;
            for (int i = 0; i < w.Types.Length; i++)
            {
                r -= w.Weights[i];
                if (r <= 0f) return w.Types[i];
            }
            return w.Types[w.Types.Length - 1];
        }

        static float HpScale(float time)
        {
            return 1f + 0.12f * (time / 60f);
        }

        void Simulate(float dt, Player player)
        {
            Vector2 pp = player.Pos;
            float pr = player.Radius;
            var cam = Camera.main;
            float halfH = cam != null ? cam.orthographicSize : 7f;
            float halfW = cam != null ? halfH * cam.aspect : 12f;
            float far = (Mathf.Max(halfW, halfH) + 4f) * 1.5f;
            float far2 = far * far;
            float t = Time.time;

            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var e = Active[i];
                Vector2 dir;
                float spd = e.Speed;

                if (e.FixedDir != Vector2.zero)
                {
                    dir = e.FixedDir;
                    e.Life -= dt;
                    if (e.Life <= 0f)
                    {
                        Remove(e);
                        continue;
                    }
                }
                else
                {
                    Vector2 to = pp - e.Pos;
                    float d = to.magnitude;
                    dir = d > 0.0001f ? to / d : Vector2.zero;
                    if (e.Def.Boss) BossLogic(e, ref dir, ref spd, dt);
                }

                dir = Obstacles.Steer(e.Pos, dir, e.Radius);
                Vector2 sep = Separation(e);
                e.Pos += (dir * spd + e.Knock + sep) * dt;
                e.Pos = Obstacles.Resolve(e.Pos, e.Radius);
                e.Knock *= Mathf.Exp(-9f * dt);

                // dano por contato
                float rr = e.Radius + pr;
                if ((pp - e.Pos).sqrMagnitude < rr * rr) player.TakeDamage(e.Damage);

                // muito longe: reaparece perto da borda da tela
                if (e.FixedDir == Vector2.zero && !e.Def.Boss && (e.Pos - pp).sqrMagnitude > far2)
                    e.Pos = SpawnPoint();

                // animação: quadros de andar (estudantes) ou "respiração" (chefes); quem está atrasado saltita
                float at = t * (e.Def.Bouncy ? 14f : 9f) + e.AnimOffset;
                if (e.Frames != null && e.Frames.Length > 0)
                    e.Sr.sprite = e.Frames[(int)(at * 1.1f) % e.Frames.Length];
                else
                {
                    float sin = Mathf.Sin(at);
                    e.Tr.localScale = new Vector3(e.Def.Scale * (1f - sin * 0.05f), e.Def.Scale * (1f + sin * 0.07f), 1f);
                }
                float yOff = e.Def.Bouncy ? Mathf.Sin(at * 0.5f) * 0.08f : 0f;
                e.Tr.position = new Vector3(e.Pos.x, e.Pos.y + yOff, 0f);
                if (Mathf.Abs(dir.x) > 0.05f) e.Sr.flipX = dir.x < 0f;

                if (e.Flash > 0f)
                {
                    e.Flash -= dt;
                    e.Sr.color = e.Def.Boss ? new Color(1f, 0.72f, 0.72f) : new Color(1f, 0.4f, 0.4f, e.Def.Tint.a);
                }
                else if (e.Telegraph > 0f)
                    e.Sr.color = Color.Lerp(e.Def.Tint, new Color(1f, 0.25f, 0.25f), 0.5f + 0.5f * Mathf.Sin(t * 30f));
                else
                    e.Sr.color = e.Def.Tint;
            }
        }

        Vector2 Separation(Enemy e)
        {
            int cx = Mathf.FloorToInt((e.Pos.x - gridOrigin.x) / Cell);
            int cy = Mathf.FloorToInt((e.Pos.y - gridOrigin.y) / Cell);
            Vector2 push = Vector2.zero;
            for (int y = cy - 1; y <= cy + 1; y++)
            {
                if (y < 0 || y >= GridN) continue;
                for (int x = cx - 1; x <= cx + 1; x++)
                {
                    if (x < 0 || x >= GridN) continue;
                    int c = y * GridN + x;
                    int end = cellStart[c] + cellCount[c];
                    for (int k = cellStart[c]; k < end; k++)
                    {
                        var o = sorted[k];
                        if (o == e || !o.Alive) continue;
                        Vector2 d = e.Pos - o.Pos;
                        float min = (e.Radius + o.Radius) * 0.85f;
                        float d2 = d.sqrMagnitude;
                        if (d2 >= min * min) continue;
                        if (d2 < 0.00001f)
                        {
                            push += Random.insideUnitCircle;
                            continue;
                        }
                        float dist = Mathf.Sqrt(d2);
                        float w = (min - dist) / min * (o.Mass / (e.Mass + o.Mass)) * 2f;
                        push += d / dist * w;
                    }
                }
            }
            return Vector2.ClampMagnitude(push * 2.5f, 4f);
        }

        void RebuildGrid(Vector2 center)
        {
            gridOrigin = center - new Vector2(GridN * Cell * 0.5f, GridN * Cell * 0.5f);
            int n = Active.Count;
            if (sorted.Length < n)
            {
                sorted = new Enemy[n * 2];
                enemyCell = new int[n * 2];
            }
            Array.Clear(cellCount, 0, cellCount.Length);
            for (int i = 0; i < n; i++)
            {
                int c = CellOf(Active[i].Pos);
                enemyCell[i] = c;
                if (c >= 0) cellCount[c]++;
            }
            int sum = 0;
            for (int c = 0; c < cellCount.Length; c++)
            {
                cellStart[c] = sum;
                cellFill[c] = sum;
                sum += cellCount[c];
            }
            for (int i = 0; i < n; i++)
            {
                int c = enemyCell[i];
                if (c >= 0) sorted[cellFill[c]++] = Active[i];
            }
        }

        int CellOf(Vector2 p)
        {
            int x = Mathf.FloorToInt((p.x - gridOrigin.x) / Cell);
            int y = Mathf.FloorToInt((p.y - gridOrigin.y) / Cell);
            if (x < 0 || y < 0 || x >= GridN || y >= GridN) return -1;
            return y * GridN + x;
        }

        // ------------------------------------------------------------ consultas

        /// <summary>Inimigos cujo corpo encosta no círculo (centro, raio).</summary>
        public void Query(Vector2 center, float radius, List<Enemy> results)
        {
            results.Clear();
            float reach = radius + MaxRadius;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reach - gridOrigin.x) / Cell));
            int x1 = Mathf.Min(GridN - 1, Mathf.FloorToInt((center.x + reach - gridOrigin.x) / Cell));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reach - gridOrigin.y) / Cell));
            int y1 = Mathf.Min(GridN - 1, Mathf.FloorToInt((center.y + reach - gridOrigin.y) / Cell));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int c = y * GridN + x;
                    int end = cellStart[c] + cellCount[c];
                    for (int k = cellStart[c]; k < end; k++)
                    {
                        var e = sorted[k];
                        if (!e.Alive) continue;
                        float rr = radius + e.Radius;
                        if ((e.Pos - center).sqrMagnitude <= rr * rr) results.Add(e);
                    }
                }
        }

        /// <summary>Inimigos que encostam no retângulo (centro, meia-largura, meia-altura).</summary>
        public void QueryBox(Vector2 center, Vector2 half, List<Enemy> results)
        {
            results.Clear();
            float reachX = half.x + MaxRadius, reachY = half.y + MaxRadius;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reachX - gridOrigin.x) / Cell));
            int x1 = Mathf.Min(GridN - 1, Mathf.FloorToInt((center.x + reachX - gridOrigin.x) / Cell));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reachY - gridOrigin.y) / Cell));
            int y1 = Mathf.Min(GridN - 1, Mathf.FloorToInt((center.y + reachY - gridOrigin.y) / Cell));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int c = y * GridN + x;
                    int end = cellStart[c] + cellCount[c];
                    for (int k = cellStart[c]; k < end; k++)
                    {
                        var e = sorted[k];
                        if (!e.Alive) continue;
                        float dx = Mathf.Max(Mathf.Abs(e.Pos.x - center.x) - half.x, 0f);
                        float dy = Mathf.Max(Mathf.Abs(e.Pos.y - center.y) - half.y, 0f);
                        if (dx * dx + dy * dy <= e.Radius * e.Radius) results.Add(e);
                    }
                }
        }

        public Enemy Nearest(Vector2 p, float maxDist)
        {
            Enemy best = null;
            float bestD = maxDist * maxDist;
            foreach (var e in Active)
            {
                float d = (e.Pos - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = e;
                }
            }
            return best;
        }

        /// <summary>Até <paramref name="count"/> inimigos distintos e visíveis na tela.</summary>
        public void RandomVisible(int count, List<Enemy> results)
        {
            results.Clear();
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 c = cam.transform.position;
            float hh = cam.orthographicSize, hw = hh * cam.aspect;
            tmp.Clear();
            foreach (var e in Active)
                if (Mathf.Abs(e.Pos.x - c.x) < hw && Mathf.Abs(e.Pos.y - c.y) < hh) tmp.Add(e);
            for (int i = 0; i < count && tmp.Count > 0; i++)
            {
                int k = Random.Range(0, tmp.Count);
                results.Add(tmp[k]);
                tmp[k] = tmp[tmp.Count - 1];
                tmp.RemoveAt(tmp.Count - 1);
            }
        }

        // ------------------------------------------------------------ criação / dano / morte

        Enemy CreateEnemy()
        {
            var e = new Enemy();
            e.Go = new GameObject("Enemy");
            e.Tr = e.Go.transform;
            e.Tr.SetParent(transform, false);
            e.Sr = e.Go.AddComponent<SpriteRenderer>();
            e.Sr.sortingOrder = 0;
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(e.Tr, false);
            sh.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            sh.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var shr = sh.AddComponent<SpriteRenderer>();
            shr.sprite = Art.Shadow;
            shr.sortingOrder = -50;
            return e;
        }

        public Enemy Spawn(EnemyDef def, Vector2 pos)
        {
            var e = pool.Count > 0 ? pool.Pop() : CreateEnemy();
            float hpScale = def.Boss ? 1f : HpScale(GameController.Instance != null ? GameController.Instance.ElapsedTime : 0f);
            e.Def = def;
            e.Pos = pos;
            e.Knock = Vector2.zero;
            e.FixedDir = Vector2.zero;
            e.Life = 0f;
            e.MaxHp = e.Hp = def.Hp * hpScale;
            e.Speed = def.Speed * (def.Boss ? 1f : Random.Range(0.92f, 1.08f));
            e.Damage = def.Damage;
            e.Radius = def.Radius;
            e.Mass = def.Boss ? 60f : def.Radius * def.Radius * 6f;
            e.Alive = true;
            e.Flash = 0f;
            e.AnimOffset = Random.value * 10f;
            e.ChargeTimer = 4f;
            e.ChargeTime = 0f;
            e.Telegraph = 0f;
            e.SummonTimer = 6f;
            e.ResetHitTimes();
            e.Frames = def.Walk != null ? def.Walk() : null;
            e.Sr.sprite = e.Frames != null && e.Frames.Length > 0 ? e.Frames[0] : def.Sprite();
            e.Sr.color = def.Tint;
            e.Tr.localScale = Vector3.one * def.Scale;
            e.Tr.position = pos;
            e.Go.SetActive(true);
            e.Index = Active.Count;
            Active.Add(e);
            if (def.Boss) Boss = e;
            return e;
        }

        /// <summary>Aplica dano. Retorna o dano efetivamente causado (para estatísticas).</summary>
        public float Damage(Enemy e, float dmg, Vector2 dir, float knock)
        {
            if (!e.Alive) return 0f;
            float dealt = Mathf.Min(dmg, e.Hp);
            e.Hp -= dmg;
            e.Flash = e.Def.Boss ? 0.05f : 0.1f;
            if (knock > 0f) e.Knock += dir * knock * (1f - e.Def.KnockResist);
            if (Effects.Instance != null) Effects.Instance.DamageNumber(e.Pos + new Vector2(0f, e.Radius * e.Def.Scale * 0.6f + 0.2f), dmg);
            AudioManager.Play(Sfx.Hit, 0.45f, 0.15f);
            if (e.Hp <= 0f) Kill(e);
            return dealt;
        }

        void Kill(Enemy e)
        {
            var gc = GameController.Instance;
            gc.Kills++;
            var fx = Effects.Instance;
            fx.Burst(e.Pos, e.Def.DeathColor, e.Def.Boss ? 50 : 6, e.Def.Boss ? 7f : 3f, e.Def.Boss ? 0.22f : 0.1f);
            AudioManager.Play(Sfx.EnemyDie, 0.5f, 0.2f);

            var pk = PickupManager.Instance;
            if (e.Def.Xp > 0) pk.SpawnGem(e.Pos, e.Def.Xp);
            if (e.Def.Boss)
            {
                if (!e.Def.FinalBoss) pk.Spawn(PickupType.Chest, e.Pos);
                Remove(e);
                gc.OnBossKilled(e.Def);
                return;
            }

            float r = Random.value;
            Vector2 jitter = Random.insideUnitCircle * 0.3f;
            if (r < (e.Def == Database.Repeater ? 0.12f : 0.008f)) pk.Spawn(PickupType.Coxinha, e.Pos + jitter);
            else if (r > 0.9965f) pk.Spawn(PickupType.Magnet, e.Pos + jitter);
            Remove(e);
        }

        void Remove(Enemy e)
        {
            if (!e.Alive) return;
            e.Alive = false;
            e.Go.SetActive(false);
            int last = Active.Count - 1;
            var moved = Active[last];
            Active[e.Index] = moved;
            moved.Index = e.Index;
            Active.RemoveAt(last);
            pool.Push(e);
            if (Boss == e)
            {
                Boss = null;
                foreach (var o in Active)
                    if (o.Def.Boss) { Boss = o; break; }
            }
        }

        // ------------------------------------------------------------ eventos

        public Vector2 SpawnPoint()
        {
            var cam = Camera.main;
            Vector2 c = cam != null ? (Vector2)cam.transform.position : Vector2.zero;
            float hh = (cam != null ? cam.orthographicSize : 7f) + 1.2f;
            float hw = (cam != null ? cam.orthographicSize * cam.aspect : 12f) + 1.2f;
            float v = Random.value * 4f * (hw + hh);
            if (v < 2f * hw) return c + new Vector2(Random.Range(-hw, hw), hh);
            v -= 2f * hw;
            if (v < 2f * hw) return c + new Vector2(Random.Range(-hw, hw), -hh);
            v -= 2f * hw;
            if (v < 2f * hh) return c + new Vector2(-hw, Random.Range(-hh, hh));
            return c + new Vector2(hw, Random.Range(-hh, hh));
        }

        void ViewHalf(out float hw, out float hh)
        {
            var cam = Camera.main;
            hh = cam != null ? cam.orthographicSize : 7f;
            hw = cam != null ? hh * cam.aspect : 12f;
        }

        void Swarm(EnemyDef def, int count, string hint)
        {
            var p = Player.Instance.Pos;
            ViewHalf(out float hw, out float hh);
            Vector2 dir = Random.value < 0.5f ? (Random.value < 0.5f ? Vector2.right : Vector2.left)
                                              : (Random.value < 0.5f ? Vector2.up : Vector2.down);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float dist = (dir.x != 0f ? hw : hh) + 1.5f;
            float span = (dir.x != 0f ? hh : hw) * 0.9f;
            Vector2 start = p - dir * dist;
            for (int i = 0; i < count; i++)
            {
                var e = Spawn(def, start + perp * Random.Range(-span, span) - dir * Random.Range(0f, 3f));
                e.FixedDir = dir;
                e.Speed *= 1.35f;
                e.Life = (dist * 2f + 6f) / e.Speed + 2f;
            }
            Recess(hint);
        }

        void Ring(EnemyDef def, int count, string hint)
        {
            var p = Player.Instance.Pos;
            ViewHalf(out float hw, out float hh);
            float r = Mathf.Lerp(hh, hw, 0.5f) + 1f;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                Spawn(def, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            Recess(hint);
        }

        /// <summary>Super onda: toca o sinal e mostra "Hora do Intervalo!" com uma dica do que vem aí.</summary>
        static void Recess(string hint)
        {
            AudioManager.Play(Sfx.Bell);
            GameController.Instance.Hud.Banner("Hora do Intervalo!", hint);
        }

        void SpawnBoss(EnemyDef def, string message)
        {
            Spawn(def, SpawnPoint());
            AudioManager.Play(Sfx.BossAlarm);
            AudioManager.PlayMusic(MusicTrack.Boss);
            GameController.Instance.Hud.Toast(message, UI.Red);
            GameController.Instance.Cam.Shake(0.25f, 0.6f);
        }

        void BossLogic(Enemy e, ref Vector2 dir, ref float spd, float dt)
        {
            if (e.Telegraph > 0f)
            {
                e.Telegraph -= dt;
                spd *= 0.15f;
                if (e.Telegraph <= 0f)
                {
                    e.ChargeTime = 0.85f;
                    e.ChargeDir = dir;
                }
                return;
            }
            if (e.ChargeTime > 0f)
            {
                e.ChargeTime -= dt;
                dir = e.ChargeDir;
                spd *= 3.2f;
                return;
            }
            e.ChargeTimer -= dt;
            if (e.ChargeTimer <= 0f)
            {
                e.ChargeTimer = e.Def.FinalBoss ? 4.5f : 6f;
                e.Telegraph = 0.75f;
            }
            if (e.Def.FinalBoss)
            {
                e.SummonTimer -= dt;
                if (e.SummonTimer <= 0f)
                {
                    e.SummonTimer = 9f;
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2f;
                        Spawn(Database.Senior, e.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.6f);
                    }
                    Effects.Instance.RingPulse(e.Pos, new Color(0.75f, 0.4f, 1f), 1f, 6f, 0.5f);
                }
            }
        }
    }
}
