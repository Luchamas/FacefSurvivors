using System;
using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    public enum Sfx
    {
        Click, Hover, Select, Shoot, Throw, Whip, Hit, EnemyDie, Gem, LevelUp,
        Hurt, Heal, Lightning, Chest, BossAlarm, GameOver, Victory, Magnet, Bell
    }

    /// <summary>Momentos com música. Hoje todos tocam a mesma trilha (Resources/Audio/Soundtrack).</summary>
    public enum MusicTrack { None, Menu, Game, Boss }

    /// <summary>
    /// Toca efeitos sonoros e a trilha sonora. Os efeitos são sintetizados em código (estilo chiptune);
    /// a música é o arquivo Resources/Audio/Soundtrack, em loop.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SR = 44100;
        static AudioManager instance;

        AudioSource musicSource;
        AudioSource[] sfxSources;
        int nextSfx;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlay = new Dictionary<Sfx, float>();
        float duck = 1f, duckTarget = 1f;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AudioManager");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<AudioManager>();
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            GameSettings.EnsureLoaded();

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            sfxSources = new AudioSource[16];
            for (int i = 0; i < sfxSources.Length; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
            }
            BuildSfx();
        }

        void Update()
        {
            duck = Mathf.MoveTowards(duck, duckTarget, Time.unscaledDeltaTime * 2f);
            musicSource.volume = GameSettings.MasterVolume * GameSettings.MusicVolume * 0.6f * duck;
        }

        // ------------------------------------------------------------------ API

        public static void Play(Sfx s, float volume = 1f, float pitchVariation = 0f)
        {
            var am = Instance;
            float now = Time.unscaledTime;
            if (am.lastPlay.TryGetValue(s, out var last) && now - last < MinInterval(s)) return;
            am.lastPlay[s] = now;

            var src = am.sfxSources[am.nextSfx];
            am.nextSfx = (am.nextSfx + 1) % am.sfxSources.Length;
            src.clip = am.clips[s];
            src.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            src.volume = Mathf.Clamp01(volume * GameSettings.MasterVolume * GameSettings.SfxVolume);
            src.Play();
        }

        const string SoundtrackPath = "Audio/Soundtrack";

        /// <summary>
        /// Liga (qualquer faixa) ou desliga (<see cref="MusicTrack.None"/>) a trilha. Como todas as faixas
        /// usam a mesma música, trocar de uma para outra não a recomeça.
        /// </summary>
        public static void PlayMusic(MusicTrack track)
        {
            var am = Instance;
            if (track == MusicTrack.None)
            {
                am.musicSource.Stop();
                return;
            }
            if (am.musicSource.isPlaying) return;
            if (am.musicSource.clip == null)
            {
                am.musicSource.clip = Resources.Load<AudioClip>(SoundtrackPath);
                if (am.musicSource.clip == null)
                {
                    Debug.LogError("Trilha sonora não encontrada em Resources: " + SoundtrackPath);
                    return;
                }
            }
            am.musicSource.Play();
        }

        /// <summary>Abaixa a música (menus de pausa / level up).</summary>
        public static void SetDuck(bool ducked)
        {
            Instance.duckTarget = ducked ? 0.35f : 1f;
        }

        static float MinInterval(Sfx s)
        {
            switch (s)
            {
                case Sfx.Hit: return 0.045f;
                case Sfx.Gem: return 0.03f;
                case Sfx.EnemyDie: return 0.05f;
                case Sfx.Shoot: return 0.05f;
                case Sfx.Throw: return 0.06f;
                default: return 0.02f;
            }
        }

        // ------------------------------------------------------------ Síntese

        static uint rng = 22222;

        static float Noise()
        {
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            return (rng & 0xFFFFFF) / (float)0xFFFFFF * 2f - 1f;
        }

        const int Sine = 0, Square = 1, Tri = 2, Saw = 3, Noiz = 4;

        static float Osc(int wave, double phase, float duty)
        {
            float p = (float)(phase - Math.Floor(phase));
            switch (wave)
            {
                case Sine: return Mathf.Sin(p * 2f * Mathf.PI);
                case Square: return p < duty ? 0.8f : -0.8f;
                case Tri: return 4f * Mathf.Abs(p - 0.5f) - 1f;
                case Saw: return (2f * p - 1f) * 0.8f;
                default: return Noise();
            }
        }

        /// <summary>Soma uma nota (com varredura exponencial de f0 a f1) no buffer.</summary>
        static void AddTone(float[] buf, float start, float dur, float f0, float f1, int wave, float vol,
            float duty = 0.5f, float attack = 0.003f, float release = 0.02f, float decay = 0f, float vibrato = 0f)
        {
            int s0 = (int)(start * SR);
            int n = (int)(dur * SR);
            double phase = 0;
            float ratio = f1 / f0;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float t = i / (float)SR;
                float u = i / (float)n;
                float f = ratio == 1f ? f0 : f0 * Mathf.Pow(ratio, u);
                if (vibrato > 0f) f *= 1f + vibrato * Mathf.Sin(t * 37.7f);
                phase += f / SR;
                float s = Osc(wave, phase, duty);
                float env = Mathf.Min(1f, t / attack) * Mathf.Clamp01((dur - t) / release);
                if (decay > 0f) env *= Mathf.Exp(-t * decay);
                buf[idx] += s * env * vol;
            }
        }

        static AudioClip ToClip(string name, float[] buf)
        {
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(buf[i]);
            var clip = AudioClip.Create(name, buf.Length, 1, SR, false);
            clip.SetData(buf, 0);
            return clip;
        }

        static AudioClip Make(string name, float dur, Action<float[]> fill)
        {
            var b = new float[Mathf.CeilToInt(dur * SR)];
            fill(b);
            return ToClip(name, b);
        }

        static void Arp(float[] b, float[] freqs, float noteDur, int wave, float vol, float lastDur = 0f, float duty = 0.5f)
        {
            for (int i = 0; i < freqs.Length; i++)
            {
                float d = (i == freqs.Length - 1 && lastDur > 0f) ? lastDur : noteDur;
                AddTone(b, i * noteDur, d, freqs[i], freqs[i], wave, vol, duty, 0.003f, 0.03f);
            }
        }

        void BuildSfx()
        {
            clips[Sfx.Click] = Make("click", 0.07f, b => AddTone(b, 0, 0.07f, 900, 600, Square, 0.35f, 0.5f, 0.002f, 0.02f, 30f));
            clips[Sfx.Hover] = Make("hover", 0.04f, b => AddTone(b, 0, 0.04f, 1400, 1500, Square, 0.12f, 0.25f, 0.002f, 0.015f));
            clips[Sfx.Select] = Make("select", 0.3f, b => Arp(b, new[] { 660f, 880f, 1320f }, 0.07f, Square, 0.25f, 0.16f, 0.25f));
            clips[Sfx.Shoot] = Make("shoot", 0.09f, b => AddTone(b, 0, 0.09f, 1100, 450, Square, 0.2f, 0.25f, 0.002f, 0.03f, 25f));
            clips[Sfx.Throw] = Make("throw", 0.16f, b => AddTone(b, 0, 0.16f, 280, 620, Tri, 0.45f, 0.5f, 0.005f, 0.04f));
            clips[Sfx.Whip] = Make("whip", 0.18f, b =>
            {
                AddTone(b, 0, 0.18f, 1, 1, Noiz, 0.3f, decay: 16f);
                AddTone(b, 0, 0.12f, 700, 200, Sine, 0.25f, decay: 20f);
            });
            clips[Sfx.Hit] = Make("hit", 0.08f, b =>
            {
                AddTone(b, 0, 0.07f, 1, 1, Noiz, 0.2f, decay: 40f);
                AddTone(b, 0, 0.06f, 240, 90, Square, 0.2f, 0.5f, decay: 30f);
            });
            clips[Sfx.EnemyDie] = Make("die", 0.16f, b =>
            {
                AddTone(b, 0, 0.15f, 520, 70, Square, 0.2f, 0.5f, 0.002f, 0.03f);
                AddTone(b, 0, 0.1f, 1, 1, Noiz, 0.12f, decay: 25f);
            });
            clips[Sfx.Gem] = Make("gem", 0.12f, b =>
            {
                AddTone(b, 0, 0.05f, 1250, 1600, Square, 0.14f, 0.25f);
                AddTone(b, 0.045f, 0.07f, 1900, 2300, Square, 0.12f, 0.25f, decay: 20f);
            });
            clips[Sfx.LevelUp] = Make("levelup", 0.7f, b =>
            {
                Arp(b, new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.075f, Square, 0.2f, 0.4f, 0.25f);
                Arp(b, new[] { 261.6f, 329.6f, 392f, 523.25f, 659.25f }, 0.075f, Tri, 0.3f, 0.4f);
            });
            clips[Sfx.Hurt] = Make("hurt", 0.25f, b =>
            {
                AddTone(b, 0, 0.22f, 300, 90, Square, 0.3f, 0.5f, 0.002f, 0.05f);
                AddTone(b, 0, 0.2f, 1, 1, Noiz, 0.15f, decay: 12f);
            });
            clips[Sfx.Heal] = Make("heal", 0.35f, b => Arp(b, new[] { 660f, 880f, 1100f, 1320f }, 0.07f, Sine, 0.35f, 0.14f));
            clips[Sfx.Lightning] = Make("lightning", 0.45f, b =>
            {
                AddTone(b, 0, 0.45f, 1, 1, Noiz, 0.45f, decay: 7f);
                AddTone(b, 0, 0.4f, 110, 40, Sine, 0.5f, decay: 6f);
            });
            clips[Sfx.Chest] = Make("chest", 1.0f, b =>
            {
                Arp(b, new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f, Square, 0.18f, 0.55f, 0.25f);
                AddTone(b, 0.45f, 0.5f, 523.25f, 523.25f, Tri, 0.25f, release: 0.2f);
                AddTone(b, 0.45f, 0.5f, 659.25f, 659.25f, Tri, 0.2f, release: 0.2f);
            });
            clips[Sfx.BossAlarm] = Make("alarm", 0.9f, b =>
            {
                for (int i = 0; i < 4; i++)
                    AddTone(b, i * 0.22f, 0.18f, i % 2 == 0 ? 440f : 330f, i % 2 == 0 ? 440f : 330f, Square, 0.28f, 0.5f);
            });
            clips[Sfx.GameOver] = Make("gameover", 1.7f, b =>
            {
                float[] notes = { 523.25f, 493.9f, 466.2f, 440f, 392f };
                for (int i = 0; i < notes.Length; i++)
                {
                    float d = i == notes.Length - 1 ? 0.8f : 0.22f;
                    AddTone(b, i * 0.24f, d, notes[i], notes[i] * (i == notes.Length - 1 ? 0.97f : 1f), Square, 0.2f, 0.5f, 0.005f, 0.1f, vibrato: 0.01f);
                    AddTone(b, i * 0.24f, d, notes[i] / 2f, notes[i] / 2f, Tri, 0.25f, 0.5f, 0.005f, 0.1f);
                }
            });
            clips[Sfx.Victory] = Make("victory", 2.0f, b =>
            {
                float[] notes = { 523.25f, 523.25f, 523.25f, 698.5f, 783.99f, 698.5f, 783.99f };
                float[] durs = { 0.14f, 0.14f, 0.14f, 0.45f, 0.25f, 0.14f, 0.8f };
                float t = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    AddTone(b, t, durs[i], notes[i], notes[i], Square, 0.2f, 0.25f, 0.005f, 0.05f);
                    AddTone(b, t, durs[i], notes[i] / 2f, notes[i] / 2f, Tri, 0.25f, 0.5f, 0.005f, 0.05f);
                    t += durs[i] + 0.02f;
                }
            });
            clips[Sfx.Magnet] = Make("magnet", 0.5f, b => AddTone(b, 0, 0.5f, 300, 1600, Sine, 0.35f, 0.5f, 0.01f, 0.1f, vibrato: 0.05f));
            clips[Sfx.Bell] = Make("bell", 1.8f, b =>
            {
                // sinal da escola: o martelo da campainha bate ~22 vezes por segundo
                for (float t = 0f; t < 1.3f; t += 0.045f)
                {
                    AddTone(b, t, 0.3f, 1318.5f, 1318.5f, Square, 0.09f, 0.5f, 0.001f, 0.05f, decay: 22f);
                    AddTone(b, t, 0.3f, 1975.5f, 1975.5f, Sine, 0.07f, 0.5f, 0.001f, 0.05f, decay: 28f);
                }
                AddTone(b, 1.3f, 0.5f, 1318.5f, 1318.5f, Sine, 0.12f, 0.5f, 0.001f, 0.2f, decay: 8f);
            });
        }
    }
}
