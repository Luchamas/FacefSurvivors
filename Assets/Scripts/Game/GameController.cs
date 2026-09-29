using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    public enum GameState { Playing, LevelUp, Paused, GameOver, Victory }

    /// <summary>
    /// Ponto de entrada da cena de jogo. Monta o mundo (câmera, chão, jogador,
    /// sistemas e HUD) e controla os estados da partida.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        /// <summary>Momento (em segundos) em que o chefe final aparece.</summary>
        public const float FinalBossTime = 600f;

        public GameState State { get; private set; } = GameState.Playing;
        public float ElapsedTime;
        public int Kills;

        public Player Player { get; private set; }
        public HUD Hud { get; private set; }
        public CameraFollow Cam { get; private set; }

        int pendingLevelUps;

        void Awake()
        {
            Instance = this;
            GameSettings.EnsureLoaded();
            GameSettings.ApplyDisplayOnce();
            Time.timeScale = 1f;

            var cam = CameraFollow.SetupMainCamera(7f);
            Cam = cam.GetComponent<CameraFollow>();
            if (Cam == null) Cam = cam.gameObject.AddComponent<CameraFollow>();

            new GameObject("Ground").AddComponent<GroundTiler>();
            Obstacles.Seed = Random.Range(1, int.MaxValue); // mesas em lugares diferentes a cada partida
            Create<Obstacles>("Mesas");
            Create<EnemyManager>("Enemies");
            Create<PickupManager>("Pickups");
            Create<ProjectileManager>("Projectiles");
            Create<Effects>("Effects");

            var pgo = new GameObject("Player");
            Player = pgo.AddComponent<Player>();
            Player.Init(GameSession.SelectedCharacter);
            Cam.Target = pgo.transform;

            UI.EnsureEventSystem();
            Hud = new GameObject("HUD").AddComponent<HUD>();
            Hud.Init(this);

            AudioManager.PlayMusic(MusicTrack.Game);
            AudioManager.SetDuck(false);
            Hud.Toast("Sobreviva! Chegue aos 10:00 e derrote o TCC.", UI.TextLight);
        }

        T Create<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Playing:
                    ElapsedTime += Time.deltaTime;
                    if (GameInput.PausePressed) Pause();
                    else if (pendingLevelUps > 0) OpenLevelUp();
                    break;

                case GameState.Paused:
                    if (GameInput.PausePressed || Input.GetKeyDown(KeyCode.JoystickButton1))
                    {
                        if (Hud.OptionsOpen) Hud.CloseOptions();
                        else Resume();
                    }
                    break;
            }
        }

        // ------------------------------------------------------------ level up / baú

        public void QueueLevelUp()
        {
            pendingLevelUps++;
        }

        void OpenLevelUp()
        {
            State = GameState.LevelUp;
            Time.timeScale = 0f;
            AudioManager.Play(Sfx.LevelUp);
            AudioManager.SetDuck(true);
            Hud.ShowLevelUp(UpgradeSystem.Roll(Player, 3), "SUBIU DE NÍVEL!", OnUpgradePicked);
        }

        void OnUpgradePicked(UpgradeOption option)
        {
            UpgradeSystem.Apply(Player, option);
            pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
            Hud.RefreshInventory();
            if (pendingLevelUps > 0)
            {
                AudioManager.Play(Sfx.LevelUp);
                Hud.ShowLevelUp(UpgradeSystem.Roll(Player, 3), "SUBIU DE NÍVEL!", OnUpgradePicked);
                return;
            }
            ResumePlay();
            Effects.Instance.RingPulse(Player.Pos, UI.Gold, 0.5f, 4f, 0.5f);
            Effects.Instance.Burst(Player.Pos, UI.Gold, 14, 4f, 0.12f);
        }

        public void OpenChest()
        {
            AudioManager.Play(Sfx.Chest);
            var rewards = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var o = UpgradeSystem.RandomChestUpgrade(Player);
                UpgradeSystem.Apply(Player, o);
                if (o.Kind == UpgradeKind.Heal) rewards.Add("Coxinha");
                else rewards.Add(o.IsNew ? o.Title + " (novo)" : o.Title + " Nv " + o.NextLevel);
            }
            Hud.RefreshInventory();
            Hud.Toast("BAÚ!  " + string.Join(",  ", rewards), UI.Gold);
            Effects.Instance.RingPulse(Player.Pos, UI.Gold, 0.5f, 6f, 0.7f);
            Effects.Instance.Burst(Player.Pos, UI.Gold, 30, 6f, 0.14f);
        }

        // ------------------------------------------------------------ pausa

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            AudioManager.SetDuck(true);
            AudioManager.Play(Sfx.Click);
            Hud.ShowPause(true);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Hud.ShowPause(false);
            ResumePlay();
        }

        void ResumePlay()
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            AudioManager.SetDuck(false);
        }

        // ------------------------------------------------------------ fim de jogo

        public void OnPlayerDied()
        {
            if (State == GameState.GameOver || State == GameState.Victory) return;
            State = GameState.GameOver;
            AudioManager.PlayMusic(MusicTrack.None);
            AudioManager.Play(Sfx.GameOver);
            Effects.Instance.Burst(Player.Pos, new Color(1f, 0.3f, 0.3f), 40, 6f, 0.15f);
            Cam.Shake(0.3f, 0.4f);
            StartCoroutine(EndSequence(false));
        }

        public void OnBossKilled(EnemyDef def)
        {
            Cam.Shake(0.35f, 0.5f);
            if (def.FinalBoss)
            {
                if (State != GameState.Playing) return;
                State = GameState.Victory;
                AudioManager.PlayMusic(MusicTrack.None);
                AudioManager.Play(Sfx.Victory);
                StartCoroutine(EndSequence(true));
                return;
            }
            if (EnemyManager.Instance.Boss == null) AudioManager.PlayMusic(MusicTrack.Game);
            Hud.Toast("Você passou na " + def.Name + "! Pegue o baú!", UI.Green);
        }

        IEnumerator EndSequence(bool victory)
        {
            AudioManager.SetDuck(false);
            yield return new WaitForSecondsRealtime(victory ? 2f : 1.4f);
            Time.timeScale = 0f;
            Hud.ShowGameOver(victory);
        }

        public void Restart()
        {
            SceneLoader.Load("Game");
        }

        public void QuitToMenu()
        {
            SceneLoader.Load("MainMenu");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
