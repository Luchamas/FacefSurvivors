using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FacefSurvivors
{
    /// <summary>
    /// Interface durante a partida: barra de XP, tempo, inventário, chefe, avisos,
    /// e as telas de level up, pausa, opções e fim de jogo.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        GameController gc;
        Transform root;

        Image xpFill;
        Text levelText, timerText, killsText, fpsText;
        readonly Image[] weaponIcons = new Image[Player.MaxWeapons];
        readonly Text[] weaponLevels = new Text[Player.MaxWeapons];
        readonly Image[] passiveIcons = new Image[Player.MaxPassives];
        readonly Text[] passiveLevels = new Text[Player.MaxPassives];

        GameObject bossRoot;
        Image bossFill;
        Text bossName;

        Text toastText;
        float toastTime;
        readonly Queue<KeyValuePair<string, Color>> toasts = new Queue<KeyValuePair<string, Color>>();

        Text bannerTitle, bannerSub;
        float bannerTime;
        const float BannerDuration = 3.5f;

        Image damageFlash;
        float flashAlpha;
        float fpsSmooth = 60f;

        int shownSeconds = -1, shownKills = -1, shownLevel = -1;

        // menus
        GameObject levelUpRoot;
        RectTransform levelUpPanel, levelUpList;
        Text levelUpTitle;
        float levelUpOpenedAt;
        List<UpgradeOption> currentOptions;
        Action<UpgradeOption> onPick;
        readonly List<Button> levelUpButtons = new List<Button>();

        GameObject pauseRoot;
        Button pauseFirst;
        Text pauseInfo;
        OptionsPanel options;

        public bool OptionsOpen => options != null && options.IsOpen;

        public void Init(GameController controller)
        {
            gc = controller;
            var canvas = UI.CreateCanvas("HUD Canvas", 10);
            canvas.transform.SetParent(transform, false);
            root = canvas.transform;

            BuildTopBar();
            BuildInventory();
            BuildBossBar();

            damageFlash = UI.Vignette(root, "DamageFlash", new Color(1f, 0f, 0f, 0f));

            toastText = UI.Text(root, "", 42, UI.Gold);
            toastText.rectTransform.Place(UI.Center, new Vector2(0, 290), new Vector2(1500, 120));
            toastText.color = new Color(1, 1, 1, 0);
            var thick = toastText.gameObject.AddComponent<Outline>(); // contorno mais grosso
            thick.effectColor = new Color(0.01f, 0.01f, 0.05f, 0.95f);
            thick.effectDistance = new Vector2(3f, 3f);

            // aviso grande das super ondas ("Hora do Intervalo!")
            bannerTitle = UI.Text(root, "", 104, UI.Gold);
            bannerTitle.rectTransform.Place(UI.Center, new Vector2(0, 215), new Vector2(1700, 140));
            bannerTitle.color = new Color(1, 1, 1, 0);
            var bannerOutline = bannerTitle.gameObject.AddComponent<Outline>();
            bannerOutline.effectColor = new Color(0.01f, 0.01f, 0.05f, 0.95f);
            bannerOutline.effectDistance = new Vector2(5f, 5f);
            bannerSub = UI.Text(root, "", 38, UI.TextLight);
            bannerSub.rectTransform.Place(UI.Center, new Vector2(0, 140), new Vector2(1500, 54));
            bannerSub.color = new Color(1, 1, 1, 0);

            fpsText = UI.Text(root, "", 24, Color.white, TextAnchor.LowerLeft, FontStyle.Normal);
            fpsText.rectTransform.Place(UI.BottomLeft, new Vector2(14, 10), new Vector2(300, 34));

            BuildLevelUp();
            BuildPause();
            options = OptionsPanel.Create(root, () =>
            {
                pauseRoot.SetActive(true);
                UI.Select(pauseFirst.gameObject);
            });

            RefreshInventory();
        }

        // ------------------------------------------------------------ construção

        void BuildTopBar()
        {
            var xpBg = UI.Image(root, "XpBar", Art.UiBar, Color.white);
            var rt = xpBg.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(8, -46);
            rt.offsetMax = new Vector2(-8, -6);

            xpFill = UI.Image(xpBg.transform, "Fill", Art.UiBarFill("Blue"), Color.white);
            xpFill.rectTransform.Fill(BarInset);
            SetFill(xpFill, 0f);

            levelText = UI.Text(xpBg.transform, "NV 1", 28, Color.white, TextAnchor.MiddleRight);
            levelText.rectTransform.Fill();
            levelText.rectTransform.offsetMax = new Vector2(-18, 0);

            timerText = UI.Text(root, "00:00", 64, Color.white);
            timerText.rectTransform.Place(UI.Top, new Vector2(0, -52), new Vector2(400, 80));

            killsText = UI.Text(root, "Abates: 0", 32, Color.white, TextAnchor.MiddleRight);
            killsText.rectTransform.Place(UI.TopRight, new Vector2(-22, -56), new Vector2(400, 44));
        }

        void BuildInventory()
        {
            for (int i = 0; i < Player.MaxWeapons; i++)
                MakeSlot(new Vector2(12 + i * 62, -54), out weaponIcons[i], out weaponLevels[i]);
            for (int i = 0; i < Player.MaxPassives; i++)
                MakeSlot(new Vector2(12 + i * 62, -116), out passiveIcons[i], out passiveLevels[i]);
        }

        void MakeSlot(Vector2 pos, out Image icon, out Text level)
        {
            var slot = UI.Image(root, "Slot", Art.UiSlot, Color.white);
            slot.rectTransform.Place(UI.TopLeft, pos, new Vector2(58, 58));
            icon = UI.Icon(slot.transform, null, new Vector2(42, 42));
            icon.enabled = false;
            level = UI.Text(slot.transform, "", 20, UI.Gold, TextAnchor.LowerRight);
            level.rectTransform.Fill(4);
        }

        const float BarInset = 7f;

        /// <summary>
        /// Barra de progresso: o preenchimento (9-slice, pontas arredondadas) estica até a fração
        /// <paramref name="k"/> da área interna. Menor que as duas pontas, ele some.
        /// </summary>
        static void SetFill(Image fill, float k)
        {
            var rt = fill.rectTransform;
            float inner = ((RectTransform)rt.parent).rect.width - 2f * BarInset;
            rt.anchorMax = new Vector2(k, 1f);
            rt.offsetMax = new Vector2(BarInset - 2f * BarInset * k, -BarInset);
            fill.enabled = k * inner >= 20f;
        }

        void BuildBossBar()
        {
            var r = UI.Rect(root, "BossBar").Place(UI.Bottom, new Vector2(0, 30), new Vector2(900, 84));
            bossRoot = r.gameObject;
            bossName = UI.Text(r, "", 34, UI.Red);
            bossName.rectTransform.Place(UI.Top, Vector2.zero, new Vector2(900, 40));
            var bg = UI.Image(r, "Bg", Art.UiBar, Color.white);
            bg.rectTransform.Place(UI.Bottom, Vector2.zero, new Vector2(900, 40));
            bossFill = UI.Image(bg.transform, "Fill", Art.UiBarFill("Red"), Color.white);
            bossFill.rectTransform.Fill(BarInset);
            bossRoot.SetActive(false);
        }

        void BuildLevelUp()
        {
            levelUpRoot = UI.Dim(root, "LevelUp", 0.6f).gameObject;
            levelUpPanel = UI.Panel(levelUpRoot.transform, "Panel", new Vector2(960, 800)).rectTransform;
            levelUpTitle = UI.Text(levelUpPanel, "SUBIU DE NÍVEL!", 60, UI.Gold);
            levelUpTitle.rectTransform.Place(UI.Top, new Vector2(0, -26), new Vector2(900, 80));
            var sub = UI.Text(levelUpPanel, "Escolha uma melhoria  (mouse, setas + Enter ou teclas 1-4)", 26, UI.TextDim, TextAnchor.MiddleCenter, FontStyle.Normal);
            sub.rectTransform.Place(UI.Top, new Vector2(0, -104), new Vector2(900, 40));
            levelUpList = UI.Rect(levelUpPanel, "List").Place(UI.Top, new Vector2(0, -160), new Vector2(880, 620));
            levelUpRoot.SetActive(false);
        }

        void BuildPause()
        {
            pauseRoot = UI.Dim(root, "Pause", 0.6f).gameObject;
            var panel = UI.Panel(pauseRoot.transform, "Panel", new Vector2(640, 620)).transform;
            var title = UI.Text(panel, "PAUSADO", 64, UI.Gold);
            title.rectTransform.Place(UI.Top, new Vector2(0, -34), new Vector2(600, 84));

            pauseFirst = UI.Button(panel, "CONTINUAR", new Vector2(460, 88), () => gc.Resume());
            ((RectTransform)pauseFirst.transform).Place(UI.Top, new Vector2(0, -150), new Vector2(460, 88));
            var opt = UI.Button(panel, "OPÇÕES", new Vector2(460, 88), () =>
            {
                pauseRoot.SetActive(false);
                options.Open();
            });
            ((RectTransform)opt.transform).Place(UI.Top, new Vector2(0, -256), new Vector2(460, 88));
            var menu = UI.Button(panel, "MENU PRINCIPAL", new Vector2(460, 88), () => gc.QuitToMenu());
            ((RectTransform)menu.transform).Place(UI.Top, new Vector2(0, -362), new Vector2(460, 88));
            foreach (var b in new[] { pauseFirst, opt, menu }) UI.SetVerticalNavigation(b);

            pauseInfo = UI.Text(panel, "", 26, UI.TextDim, TextAnchor.MiddleCenter, FontStyle.Normal);
            pauseInfo.rectTransform.Place(UI.Bottom, new Vector2(0, 34), new Vector2(580, 90));
            pauseRoot.SetActive(false);
        }

        // ------------------------------------------------------------ API

        public void Toast(string message, Color? color = null)
        {
            toasts.Enqueue(new KeyValuePair<string, Color>(message, color ?? UI.Gold));
        }

        /// <summary>Aviso grande no meio da tela, com uma linha menor embaixo.</summary>
        public void Banner(string title, string subtitle = "")
        {
            bannerTitle.text = title;
            bannerSub.text = subtitle;
            bannerTime = BannerDuration;
        }

        public void FlashDamage()
        {
            flashAlpha = 0.5f;
        }

        public void RefreshInventory()
        {
            var p = Player.Instance;
            if (p == null) return;
            for (int i = 0; i < weaponIcons.Length; i++)
            {
                bool has = i < p.Weapons.Count;
                weaponIcons[i].enabled = has;
                weaponLevels[i].text = "";
                if (!has) continue;
                var w = p.Weapons[i];
                weaponIcons[i].sprite = w.Def.Icon();
                weaponIcons[i].rectTransform.localEulerAngles = new Vector3(0, 0, w.Def.IconRotation);
                weaponLevels[i].text = w.IsMaxed ? "MAX" : w.Level.ToString();
            }
            for (int i = 0; i < passiveIcons.Length; i++)
            {
                bool has = i < p.Passives.Count;
                passiveIcons[i].enabled = has;
                passiveLevels[i].text = "";
                if (!has) continue;
                var pa = p.Passives[i];
                passiveIcons[i].sprite = pa.Def.Icon();
                passiveLevels[i].text = pa.IsMaxed ? "MAX" : pa.Level.ToString();
            }
        }

        public void ShowLevelUp(List<UpgradeOption> opts, string title, Action<UpgradeOption> pick)
        {
            currentOptions = opts;
            onPick = pick;
            levelUpTitle.text = title;
            foreach (Transform c in levelUpList)
            {
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }
            levelUpButtons.Clear();

            const float cardH = 136f, gap = 14f;
            for (int i = 0; i < opts.Count; i++)
            {
                var o = opts[i];
                int idx = i;
                var b = UI.Button(levelUpList, null, new Vector2(880, cardH), () => Pick(idx));
                ((RectTransform)b.transform).Place(UI.Top, new Vector2(0, -i * (cardH + gap)), new Vector2(880, cardH));

                var slot = UI.Image(b.transform, "Slot", Art.UiSlot, Color.white);
                slot.rectTransform.Place(UI.Left, new Vector2(18, 0), new Vector2(100, 100));
                UI.Icon(slot.transform, o.Icon, new Vector2(76, 76), o.IconRotation);

                var name = UI.Text(b.transform, o.Title, 38, UI.TextLight, TextAnchor.UpperLeft);
                name.rectTransform.Place(UI.TopLeft, new Vector2(140, -14), new Vector2(520, 50));

                string tag = o.Kind == UpgradeKind.Heal ? "" : o.IsNew ? "NOVO!" : "Nível " + o.NextLevel;
                var tagText = UI.Text(b.transform, tag, 30, o.IsNew ? UI.Gold : new Color(0.55f, 0.8f, 1f), TextAnchor.UpperRight);
                tagText.rectTransform.Place(UI.TopRight, new Vector2(-24, -16), new Vector2(240, 44));

                var desc = UI.Text(b.transform, o.Description, 28, UI.TextDim, TextAnchor.UpperLeft, FontStyle.Normal);
                desc.rectTransform.Place(UI.TopLeft, new Vector2(140, -66), new Vector2(710, 64));

                var key = UI.Text(b.transform, (i + 1).ToString(), 22, new Color(1, 1, 1, 0.35f), TextAnchor.LowerRight);
                key.rectTransform.Place(UI.BottomRight, new Vector2(-14, 8), new Vector2(40, 30));

                UI.SetVerticalNavigation(b);
                levelUpButtons.Add(b);
            }
            levelUpPanel.sizeDelta = new Vector2(960, 172 + opts.Count * (cardH + gap));
            levelUpRoot.SetActive(true);
            levelUpRoot.transform.SetAsLastSibling();
            levelUpOpenedAt = Time.unscaledTime;
            UI.Select(levelUpButtons[0].gameObject);
        }

        void Pick(int i)
        {
            if (!levelUpRoot.activeSelf || currentOptions == null || i >= currentOptions.Count) return;
            // pequena espera para evitar escolher sem querer
            if (Time.unscaledTime - levelUpOpenedAt < 0.35f) return;
            levelUpRoot.SetActive(false);
            var cb = onPick;
            var choice = currentOptions[i];
            currentOptions = null;
            cb?.Invoke(choice);
        }

        public void ShowPause(bool show)
        {
            if (show)
            {
                var p = Player.Instance;
                pauseInfo.text = p.Character.Name + "  •  Nível " + p.Level + "\nVida " + Mathf.CeilToInt(p.Hp) + "/" + Mathf.CeilToInt(p.Stats.MaxHp);
                pauseRoot.SetActive(true);
                pauseRoot.transform.SetAsLastSibling();
                UI.Select(pauseFirst.gameObject);
            }
            else
            {
                pauseRoot.SetActive(false);
                if (options.IsOpen) options.gameObject.SetActive(false);
            }
        }

        public void CloseOptions()
        {
            options.Close();
        }

        public void ShowGameOver(bool victory)
        {
            var dim = UI.Dim(root, "GameOver", 0.75f);
            var group = dim.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var panel = UI.Panel(dim.transform, "Panel", new Vector2(1100, 920)).transform;

            var title = UI.Text(panel, victory ? "APROVADO!" : "REPROVADO!", 96, victory ? UI.Gold : UI.Red);
            title.rectTransform.Place(UI.Top, new Vector2(0, -30), new Vector2(1000, 120));
            var sub = UI.Text(panel, victory
                    ? "Você derrotou o TCC e se formou na FACEF!"
                    : "Os estudantes venceram desta vez... tente de novo!",
                32, UI.TextLight, TextAnchor.MiddleCenter, FontStyle.Normal);
            sub.rectTransform.Place(UI.Top, new Vector2(0, -150), new Vector2(1000, 44));

            var p = Player.Instance;
            int secs = (int)gc.ElapsedTime;
            var stats = UI.Text(panel, string.Format("Tempo  {0:00}:{1:00}       Nível  {2}       Abates  {3}",
                secs / 60, secs % 60, p.Level, gc.Kills), 36, UI.Gold);
            stats.rectTransform.Place(UI.Top, new Vector2(0, -214), new Vector2(1000, 50));

            var header = UI.Text(panel, "Dano causado por arma", 30, UI.TextDim, TextAnchor.MiddleCenter, FontStyle.Normal);
            header.rectTransform.Place(UI.Top, new Vector2(0, -288), new Vector2(1000, 40));

            float y = -340f;
            foreach (var w in p.Weapons)
            {
                var row = UI.Rect(panel, "Row").Place(UI.Top, new Vector2(0, y), new Vector2(760, 58));
                var slot = UI.Image(row, "Slot", Art.UiSlot, Color.white);
                slot.rectTransform.Place(UI.Left, Vector2.zero, new Vector2(56, 56));
                UI.Icon(slot.transform, w.Def.Icon(), new Vector2(42, 42), w.Def.IconRotation);
                var n = UI.Text(row, w.Def.Name + "  <size=24><color=#aab4dc>Nv " + w.Level + "</color></size>", 32, UI.TextLight, TextAnchor.MiddleLeft);
                n.rectTransform.Place(UI.Left, new Vector2(76, 0), new Vector2(420, 56));
                var d = UI.Text(row, Mathf.RoundToInt(w.TotalDamage).ToString("N0"), 32, Color.white, TextAnchor.MiddleRight);
                d.rectTransform.Place(UI.Right, Vector2.zero, new Vector2(260, 56));
                y -= 64f;
            }

            var again = UI.Button(panel, "JOGAR NOVAMENTE", new Vector2(440, 90), () => gc.Restart(), 38);
            ((RectTransform)again.transform).Place(UI.Bottom, new Vector2(-240, 40), new Vector2(440, 90));
            var menu = UI.Button(panel, "MENU PRINCIPAL", new Vector2(440, 90), () => gc.QuitToMenu(), 38);
            ((RectTransform)menu.transform).Place(UI.Bottom, new Vector2(240, 40), new Vector2(440, 90));

            dim.transform.SetAsLastSibling();
            StartCoroutine(FadeIn(group, again.gameObject));
        }

        IEnumerator FadeIn(CanvasGroup group, GameObject select)
        {
            group.interactable = false;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                group.alpha = t;
                yield return null;
            }
            group.alpha = 1f;
            group.interactable = true;
            UI.Select(select);
        }

        // ------------------------------------------------------------ atualização

        void Update()
        {
            var p = Player.Instance;
            if (p == null) return;

            SetFill(xpFill, p.XpToNext > 0f ? Mathf.Clamp01(p.Xp / p.XpToNext) : 0f);
            if (shownLevel != p.Level)
            {
                shownLevel = p.Level;
                levelText.text = "NV " + p.Level;
            }
            int secs = (int)gc.ElapsedTime;
            if (secs != shownSeconds)
            {
                shownSeconds = secs;
                timerText.text = string.Format("{0:00}:{1:00}", secs / 60, secs % 60);
            }
            if (gc.Kills != shownKills)
            {
                shownKills = gc.Kills;
                killsText.text = "Abates: " + gc.Kills;
            }

            var boss = EnemyManager.Instance != null ? EnemyManager.Instance.Boss : null;
            bool showBoss = boss != null && gc.State != GameState.GameOver && gc.State != GameState.Victory;
            if (bossRoot.activeSelf != showBoss) bossRoot.SetActive(showBoss);
            if (showBoss)
            {
                SetFill(bossFill, Mathf.Clamp01(boss.Hp / boss.MaxHp));
                bossName.text = boss.Def.Name;
            }

            UpdateToast();
            UpdateBanner();

            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 2f);
            damageFlash.color = new Color(1f, 0f, 0f, flashAlpha);

            if (GameSettings.ShowFps)
            {
                float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                fpsSmooth = Mathf.Lerp(fpsSmooth, 1f / dt, 0.05f);
                fpsText.text = Mathf.RoundToInt(fpsSmooth) + " FPS";
            }
            else if (fpsText.text.Length > 0) fpsText.text = "";

            // atalhos numéricos no level up
            if (levelUpRoot.activeSelf && currentOptions != null)
            {
                for (int i = 0; i < currentOptions.Count && i < 4; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    {
                        Pick(i);
                        break;
                    }
                }
            }
        }

        void UpdateToast()
        {
            if (toastTime <= 0f)
            {
                if (toasts.Count == 0) return;
                var next = toasts.Dequeue();
                toastText.text = next.Key;
                toastText.color = next.Value;
                toastTime = 3f;
            }
            toastTime -= Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(toastTime / 0.5f) * Mathf.Clamp01((3f - toastTime) / 0.2f);
            var c = toastText.color;
            toastText.color = new Color(c.r, c.g, c.b, a);
            float s = 1f + Mathf.Clamp01((0.25f - (3f - toastTime)) / 0.25f) * 0.25f;
            toastText.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Entra grande, balança como o sinal tocando e some. Usa o tempo do jogo (congela na pausa).</summary>
        void UpdateBanner()
        {
            if (bannerTime <= 0f) return;
            bannerTime = Mathf.Max(0f, bannerTime - Time.deltaTime);
            float age = BannerDuration - bannerTime;
            float a = Mathf.Clamp01(bannerTime / 0.6f) * Mathf.Clamp01(age / 0.12f);
            bannerTitle.color = new Color(UI.Gold.r, UI.Gold.g, UI.Gold.b, a);
            bannerSub.color = new Color(UI.TextLight.r, UI.TextLight.g, UI.TextLight.b, a);
            float s = 1f + Mathf.Clamp01(1f - age / 0.25f) * 0.6f;
            var rt = bannerTitle.rectTransform;
            rt.localScale = new Vector3(s, s, 1f);
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(age * 20f) * 4f * Mathf.Clamp01(1f - age / 1.4f));
        }
    }
}
