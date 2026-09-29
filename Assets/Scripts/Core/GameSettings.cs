using System;
using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>
    /// Opções do jogador (volume, vídeo, gameplay). Persistidas em PlayerPrefs.
    /// </summary>
    public static class GameSettings
    {
        public static float MasterVolume = 0.8f;
        public static float MusicVolume = 0.6f;
        public static float SfxVolume = 0.8f;
        public static bool Fullscreen = true;
        public static bool VSync = true;
        public static bool ShowDamageNumbers = true;
        public static bool ScreenShake = true;
        public static bool ShowFps = false;
        /// <summary>Índice em <see cref="GetResolutions"/>; -1 = maior disponível.</summary>
        public static int ResolutionIndex = -1;

        /// <summary>Disparado sempre que alguma opção muda.</summary>
        public static event Action Changed;

        static bool loaded;
        static bool displayApplied;

        public static void EnsureLoaded()
        {
            if (!loaded) Load();
        }

        public static void Load()
        {
            loaded = true;
            MasterVolume = PlayerPrefs.GetFloat("opt_master", 0.8f);
            MusicVolume = PlayerPrefs.GetFloat("opt_music", 0.6f);
            SfxVolume = PlayerPrefs.GetFloat("opt_sfx", 0.8f);
            Fullscreen = PlayerPrefs.GetInt("opt_fullscreen", 1) == 1;
            VSync = PlayerPrefs.GetInt("opt_vsync", 1) == 1;
            ShowDamageNumbers = PlayerPrefs.GetInt("opt_dmgnum", 1) == 1;
            ScreenShake = PlayerPrefs.GetInt("opt_shake", 1) == 1;
            ShowFps = PlayerPrefs.GetInt("opt_fps", 0) == 1;
            ResolutionIndex = PlayerPrefs.GetInt("opt_res", -1);
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("opt_master", MasterVolume);
            PlayerPrefs.SetFloat("opt_music", MusicVolume);
            PlayerPrefs.SetFloat("opt_sfx", SfxVolume);
            PlayerPrefs.SetInt("opt_fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("opt_vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("opt_dmgnum", ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt("opt_shake", ScreenShake ? 1 : 0);
            PlayerPrefs.SetInt("opt_fps", ShowFps ? 1 : 0);
            PlayerPrefs.SetInt("opt_res", ResolutionIndex);
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            MasterVolume = 0.8f;
            MusicVolume = 0.6f;
            SfxVolume = 0.8f;
            Fullscreen = true;
            VSync = true;
            ShowDamageNumbers = true;
            ScreenShake = true;
            ShowFps = false;
            ResolutionIndex = -1;
            ApplyDisplay();
            NotifyChanged();
        }

        public static void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>Resoluções únicas (sem repetir por taxa de atualização), da menor para a maior.</summary>
        public static List<Vector2Int> GetResolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x >= 800 && !list.Contains(v)) list.Add(v);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height));
            return list;
        }

        public static Vector2Int CurrentResolution
        {
            get
            {
                var res = GetResolutions();
                int i = ResolutionIndex < 0 || ResolutionIndex >= res.Count ? res.Count - 1 : ResolutionIndex;
                return res[i];
            }
        }

        /// <summary>Aplica as opções de vídeo apenas na primeira chamada (ao abrir o jogo).</summary>
        public static void ApplyDisplayOnce()
        {
            if (displayApplied) return;
            displayApplied = true;
            ApplyDisplay();
        }

        public static void ApplyDisplay()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : 144;
#if !UNITY_EDITOR
            var r = CurrentResolution;
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.width != r.x || Screen.height != r.y || Screen.fullScreenMode != mode)
                Screen.SetResolution(r.x, r.y, mode);
#endif
        }
    }
}
