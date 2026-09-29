using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FacefSurvivors
{
    /// <summary>
    /// Ponto de entrada da cena do menu: logo, Jogar / Opções / Créditos / Sair,
    /// seleção de personagem e um fundo animado com estudantes passeando.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        class Wanderer
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public Sprite[] Frames;
            public Vector2 Vel;
            public float Phase;
            public float Scale;
            public bool Bouncy;
        }

        Camera cam;
        Transform root;
        GameObject mainPanel, charPanel, creditsPanel;
        OptionsPanel options;
        Button playButton, optionsButton, creditsButton, firstCharCard, creditsBack;
        RectTransform logo;
        readonly List<Wanderer> wanderers = new List<Wanderer>();
        readonly List<KeyValuePair<Image, CharacterDef>> portraits = new List<KeyValuePair<Image, CharacterDef>>();

        void Start()
        {
            GameSettings.EnsureLoaded();
            GameSettings.ApplyDisplayOnce();
            Time.timeScale = 1f;

            cam = CameraFollow.SetupMainCamera(6f);
            new GameObject("Ground").AddComponent<GroundTiler>();
            Obstacles.Seed = Random.Range(1, int.MaxValue);
            new GameObject("Mesas").AddComponent<Obstacles>();
            SpawnWanderers();

            UI.EnsureEventSystem();
            BuildUI();

            AudioManager.PlayMusic(MusicTrack.Menu);
            AudioManager.SetDuck(false);
            ShowMain(playButton);
        }

        // ------------------------------------------------------------ fundo animado

        void SpawnWanderers()
        {
            var students = new[]
            {
                Database.Freshman, Database.Latecomer, Database.Sleepy, Database.Senior, Database.Nerd,
                Database.Freshman, Database.Latecomer,
            };
            float hh = cam.orthographicSize, hw = hh * cam.aspect;
            for (int i = 0; i < 26; i++)
            {
                bool big = Random.value < 0.12f;
                var def = big ? Database.Repeater : students[Random.Range(0, students.Length)];
                var go = new GameObject("Wanderer");
                var sr = go.AddComponent<SpriteRenderer>();
                var frames = def.Walk();
                sr.sprite = frames[0];
                var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>();
                sh.transform.SetParent(go.transform, false);
                sh.transform.localPosition = new Vector3(0f, -0.45f, 0f);
                sh.sprite = Art.Shadow;
                sh.sortingOrder = -50;
                var w = new Wanderer
                {
                    Tr = go.transform,
                    Sr = sr,
                    Frames = frames,
                    Vel = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1.2f),
                    Phase = Random.value * 10f,
                    Scale = big ? 1.8f : 1f,
                    Bouncy = def.Bouncy,
                };
                w.Tr.position = new Vector3(Random.Range(-hw, hw), Random.Range(-hh, hh), 0f);
                wanderers.Add(w);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            cam.transform.position += new Vector3(0.35f, 0.18f, 0f) * dt;
            Vector2 c = cam.transform.position;
            float hh = cam.orthographicSize + 1f, hw = cam.orthographicSize * cam.aspect + 1f;
            float t = Time.time;
            foreach (var w in wanderers)
            {
                Vector3 p = w.Tr.position + (Vector3)(w.Vel * dt);
                if (p.x < c.x - hw) p.x += hw * 2f;
                if (p.x > c.x + hw) p.x -= hw * 2f;
                if (p.y < c.y - hh) p.y += hh * 2f;
                if (p.y > c.y + hh) p.y -= hh * 2f;
                p = Obstacles.Resolve(p, 0.4f);
                w.Tr.position = p;
                float at = t * (w.Bouncy ? 14f : 8f) + w.Phase;
                w.Sr.sprite = w.Frames[(int)(at * 1.1f) % w.Frames.Length];
                w.Tr.localScale = new Vector3(w.Scale, w.Scale, 1f);
                w.Sr.flipX = w.Vel.x < 0f;
            }

            if (logo != null) logo.anchoredPosition = new Vector2(0f, -40f + Mathf.Sin(t * 1.6f) * 8f);

            // personagem correndo no card de seleção
            if (charPanel.activeSelf)
            {
                bool open = (int)(Time.unscaledTime / 0.14f) % 2 == 0;
                foreach (var kv in portraits)
                    kv.Key.sprite = open ? Art.CharacterRun(kv.Value) : Art.CharacterIdle(kv.Value);
            }

            if (GameInput.BackPressed)
            {
                if (options.IsOpen) options.Close();
                else if (charPanel.activeSelf) ShowMain(playButton);
                else if (creditsPanel.activeSelf) ShowMain(creditsButton);
            }
        }

        // ------------------------------------------------------------ UI

        void BuildUI()
        {
            var canvas = UI.CreateCanvas("Menu Canvas", 10);
            root = canvas.transform;

            UI.Image(root, "Darken", null, new Color(0.02f, 0.02f, 0.08f, 0.6f)).rectTransform.Fill();
            UI.Vignette(root, "Vignette", new Color(0f, 0f, 0f, 0.9f));

            BuildMain();
            BuildCharacterSelect();
            BuildCredits();
            options = OptionsPanel.Create(root, () => ShowMain(optionsButton));
        }

        static Sprite LoadLogo()
        {
            var s = Resources.Load<Sprite>("UI/Logo");
            if (s != null) return s;
            var tex = Resources.Load<Texture2D>("UI/Logo");
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        void BuildMain()
        {
            mainPanel = UI.Rect(root, "Main").Fill().gameObject;
            var t = mainPanel.transform;

            var logoSprite = LoadLogo();
            if (logoSprite != null)
            {
                var img = UI.Image(t, "Logo", logoSprite, Color.white);
                img.preserveAspect = true;
                logo = img.rectTransform.Place(UI.Top, new Vector2(0, -40), new Vector2(1000, 422));
            }
            else
            {
                var title = UI.Text(t, "FACEF\nSURVIVORS", 120, UI.TextLight);
                logo = title.rectTransform.Place(UI.Top, new Vector2(0, -40), new Vector2(1200, 380));
            }

            playButton = UI.Button(t, "JOGAR", new Vector2(480, 88), ShowCharacterSelect, 46);
            optionsButton = UI.Button(t, "OPÇÕES", new Vector2(480, 88), () =>
            {
                mainPanel.SetActive(false);
                options.Open();
            }, 46);
            creditsButton = UI.Button(t, "CRÉDITOS", new Vector2(480, 88), ShowCredits, 46);
            var quit = UI.Button(t, "SAIR", new Vector2(480, 88), Quit, 46);
            var buttons = new[] { playButton, optionsButton, creditsButton, quit };
            for (int i = 0; i < buttons.Length; i++)
            {
                ((RectTransform)buttons[i].transform).Place(UI.Center, new Vector2(0, -100 - i * 104), new Vector2(480, 88));
                UI.SetVerticalNavigation(buttons[i]);
            }

            var hint = UI.Text(t, "WASD / Setas: mover    •    Enter: confirmar    •    Esc: voltar / pausar", 24,
                UI.TextDim, TextAnchor.LowerLeft, FontStyle.Normal);
            hint.rectTransform.Place(UI.BottomLeft, new Vector2(24, 18), new Vector2(1100, 34));
            var version = UI.Text(t, "v1.0  •  Feito com Unity", 24, UI.TextDim, TextAnchor.LowerRight, FontStyle.Normal);
            version.rectTransform.Place(UI.BottomRight, new Vector2(-24, 18), new Vector2(500, 34));
        }

        void BuildCharacterSelect()
        {
            charPanel = UI.Rect(root, "CharacterSelect").Fill().gameObject;
            var t = charPanel.transform;

            var title = UI.Text(t, "ESCOLHA SEU PERSONAGEM", 64, UI.Gold);
            title.rectTransform.Place(UI.Top, new Vector2(0, -50), new Vector2(1600, 84));
            var sub = UI.Text(t, "Clique no personagem (ou aperte Enter) para começar", 30, UI.TextDim, TextAnchor.MiddleCenter, FontStyle.Normal);
            sub.rectTransform.Place(UI.Top, new Vector2(0, -134), new Vector2(1600, 40));

            var chars = Database.Characters;
            const float w = 440f, h = 640f, gap = 30f;
            float total = chars.Count * w + (chars.Count - 1) * gap;
            for (int i = 0; i < chars.Count; i++)
            {
                var def = chars[i];
                var card = UI.Button(t, null, new Vector2(w, h), () => StartGame(def));
                ((RectTransform)card.transform).Place(UI.Center, new Vector2(-total / 2f + w / 2f + i * (w + gap), 0), new Vector2(w, h));
                if (i == 0) firstCharCard = card;
                var ct = card.transform;

                var portrait = UI.Image(ct, "Portrait", Art.UiSlot, Color.white);
                portrait.rectTransform.Place(UI.Top, new Vector2(0, -24), new Vector2(340, 300));
                var anim = UI.Icon(portrait.transform, Art.CharacterIdle(def), new Vector2(310, 270));
                portraits.Add(new KeyValuePair<Image, CharacterDef>(anim, def));

                var name = UI.Text(ct, def.Name, 44, UI.TextLight);
                name.rectTransform.Place(UI.Top, new Vector2(0, -338), new Vector2(420, 56));

                var desc = UI.Text(ct, def.Description, 26, UI.TextDim, TextAnchor.UpperCenter, FontStyle.Normal);
                desc.rectTransform.Place(UI.Top, new Vector2(0, -400), new Vector2(390, 90));

                var weapon = Database.GetWeapon(def.StartWeapon);
                var wslot = UI.Image(ct, "WeaponSlot", Art.UiSlot, Color.white);
                wslot.rectTransform.Place(UI.Top, new Vector2(-110, -498), new Vector2(64, 64));
                UI.Icon(wslot.transform, weapon.Icon(), new Vector2(46, 46), weapon.IconRotation);
                var wname = UI.Text(ct, "Arma: " + weapon.Name, 30, UI.TextLight, TextAnchor.MiddleLeft);
                wname.rectTransform.Place(UI.Top, new Vector2(76, -498), new Vector2(280, 64));

                var bonus = UI.Text(ct, def.Bonus, 26, UI.Gold, TextAnchor.MiddleCenter, FontStyle.Normal);
                bonus.rectTransform.Place(UI.Top, new Vector2(0, -574), new Vector2(400, 50));
            }

            var back = UI.Button(t, "VOLTAR", new Vector2(360, 80), () => ShowMain(playButton), 38);
            ((RectTransform)back.transform).Place(UI.Bottom, new Vector2(0, 50), new Vector2(360, 80));
            charPanel.SetActive(false);
        }

        void BuildCredits()
        {
            creditsPanel = UI.Rect(root, "Credits").Fill().gameObject;
            var panel = UI.Panel(creditsPanel.transform, "Panel", new Vector2(1300, 380)).transform;
            var title = UI.Text(panel, "CRÉDITOS", 64, UI.Gold);
            title.rectTransform.Place(UI.Top, new Vector2(0, -34), new Vector2(900, 84));

            var body = UI.Text(panel, "Criado por Lucas Macedo, Otávio, João e Matheus Ferrarezi em Unity",
                34, UI.TextLight, TextAnchor.MiddleCenter, FontStyle.Normal);
            body.rectTransform.Place(UI.Top, new Vector2(0, -150), new Vector2(1200, 60));

            creditsBack = UI.Button(panel, "VOLTAR", new Vector2(360, 80), () => ShowMain(creditsButton), 38);
            ((RectTransform)creditsBack.transform).Place(UI.Bottom, new Vector2(0, 40), new Vector2(360, 80));
            creditsPanel.SetActive(false);
        }

        // ------------------------------------------------------------ navegação

        void ShowMain(Button select)
        {
            charPanel.SetActive(false);
            creditsPanel.SetActive(false);
            mainPanel.SetActive(true);
            UI.Select(select.gameObject);
        }

        void ShowCharacterSelect()
        {
            mainPanel.SetActive(false);
            charPanel.SetActive(true);
            UI.Select(firstCharCard.gameObject);
        }

        void ShowCredits()
        {
            mainPanel.SetActive(false);
            creditsPanel.SetActive(true);
            UI.Select(creditsBack.gameObject);
        }

        void StartGame(CharacterDef def)
        {
            GameSession.SelectedCharacter = def;
            AudioManager.Play(Sfx.Select);
            SceneLoader.Load("Game");
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
