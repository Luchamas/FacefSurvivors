using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FacefSurvivors
{
    /// <summary>
    /// Tela de opções (áudio, vídeo e jogabilidade). Usada no menu principal e no menu de pausa.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        Action onClose;
        Transform panel;
        Selectable first;
        readonly List<Action> refreshers = new List<Action>();
        float lastSfxPreview;

        public bool IsOpen => gameObject.activeSelf;

        public static OptionsPanel Create(Transform parent, Action onClose)
        {
            var dim = UI.Dim(parent, "Options", 0.6f);
            var op = dim.gameObject.AddComponent<OptionsPanel>();
            op.onClose = onClose;
            op.Build();
            dim.gameObject.SetActive(false);
            return op;
        }

        void Build()
        {
            panel = UI.Panel(transform, "Panel", new Vector2(1100, 920)).transform;

            var title = UI.Text(panel, "OPÇÕES", 64, UI.Gold);
            title.rectTransform.Place(UI.Top, new Vector2(0, -28), new Vector2(900, 84));

            float y = -135f;
            const float step = 70f;

            SliderRow("Volume geral", y, () => GameSettings.MasterVolume, v => GameSettings.MasterVolume = v);
            y -= step;
            SliderRow("Música", y, () => GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            y -= step;
            SliderRow("Efeitos sonoros", y, () => GameSettings.SfxVolume, v =>
            {
                GameSettings.SfxVolume = v;
                if (Time.unscaledTime - lastSfxPreview > 0.15f)
                {
                    lastSfxPreview = Time.unscaledTime;
                    AudioManager.Play(Sfx.Gem);
                }
            });
            y -= step;

            var onOff = new[] { "DESLIGADO", "LIGADO" };
            CycleRow("Tela cheia", y, onOff, () => GameSettings.Fullscreen ? 1 : 0, i =>
            {
                GameSettings.Fullscreen = i == 1;
                GameSettings.ApplyDisplay();
            });
            y -= step;

            var resolutions = GameSettings.GetResolutions();
            var resNames = new string[resolutions.Count];
            for (int i = 0; i < resolutions.Count; i++) resNames[i] = resolutions[i].x + " x " + resolutions[i].y;
            CycleRow("Resolução", y, resNames, () =>
            {
                int idx = GameSettings.ResolutionIndex;
                return idx < 0 || idx >= resNames.Length ? resNames.Length - 1 : idx;
            }, i =>
            {
                GameSettings.ResolutionIndex = i;
                GameSettings.ApplyDisplay();
            });
            y -= step;

            CycleRow("VSync", y, onOff, () => GameSettings.VSync ? 1 : 0, i =>
            {
                GameSettings.VSync = i == 1;
                GameSettings.ApplyDisplay();
            });
            y -= step;
            CycleRow("Números de dano", y, onOff, () => GameSettings.ShowDamageNumbers ? 1 : 0, i => GameSettings.ShowDamageNumbers = i == 1);
            y -= step;
            CycleRow("Tremor de tela", y, onOff, () => GameSettings.ScreenShake ? 1 : 0, i => GameSettings.ScreenShake = i == 1);
            y -= step;
            CycleRow("Mostrar FPS", y, onOff, () => GameSettings.ShowFps ? 1 : 0, i => GameSettings.ShowFps = i == 1);

            var reset = UI.Button(panel, "PADRÃO", new Vector2(360, 84), () =>
            {
                GameSettings.ResetToDefaults();
                Refresh();
            }, 38);
            reset.GetComponent<RectTransform>().Place(UI.Bottom, new Vector2(-200, 36), new Vector2(360, 84));

            var back = UI.Button(panel, "VOLTAR", new Vector2(360, 84), Close, 38);
            back.GetComponent<RectTransform>().Place(UI.Bottom, new Vector2(200, 36), new Vector2(360, 84));
        }

        RectTransform Row(string label, float y)
        {
            var row = UI.Rect(panel, "Row " + label).Place(UI.Top, new Vector2(0, y), new Vector2(980, 62));
            var t = UI.Text(row, label, 34, UI.TextLight, TextAnchor.MiddleLeft);
            t.rectTransform.Place(UI.Left, new Vector2(10, 0), new Vector2(440, 62));
            return row;
        }

        static string Percent(float v)
        {
            return Mathf.RoundToInt(v * 100f) + "%";
        }

        void SliderRow(string label, float y, Func<float> get, Action<float> set)
        {
            var row = Row(label, y);
            var valueText = UI.Text(row, Percent(get()), 30, UI.TextDim, TextAnchor.MiddleRight);
            valueText.rectTransform.Place(UI.Right, new Vector2(-10, 0), new Vector2(100, 62));
            var s = UI.Slider(row, new Vector2(370, 54), get(), v =>
            {
                set(v);
                valueText.text = Percent(v);
                GameSettings.NotifyChanged();
            });
            ((RectTransform)s.transform).Place(UI.Right, new Vector2(-130, 0), new Vector2(370, 54));
            UI.SetVerticalNavigation(s);
            if (first == null) first = s;
            refreshers.Add(() =>
            {
                s.SetValueWithoutNotify(get());
                valueText.text = Percent(get());
            });
        }

        void CycleRow(string label, float y, string[] options, Func<int> get, Action<int> set)
        {
            var row = Row(label, y);
            var img = UI.Image(row, "Selector", Art.UiButton, Color.white);
            img.raycastTarget = true;
            img.rectTransform.Place(UI.Right, new Vector2(-10, 0), new Vector2(490, 56));
            var txt = UI.Text(img.transform, "", 30, UI.TextLight);
            txt.rectTransform.Fill();
            var cs = img.gameObject.AddComponent<CycleSelector>();
            UI.StyleSelectable(cs, img);
            cs.Setup(options, get(), i =>
            {
                set(i);
                GameSettings.NotifyChanged();
            }, txt);
            UI.SetVerticalNavigation(cs);
            if (first == null) first = cs;
            refreshers.Add(() => cs.SetIndexWithoutNotify(get()));
        }

        void Refresh()
        {
            foreach (var r in refreshers) r();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
            if (first != null) UI.Select(first.gameObject);
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            GameSettings.Save();
            gameObject.SetActive(false);
            onClose?.Invoke();
        }
    }
}
