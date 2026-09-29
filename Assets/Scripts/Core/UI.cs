using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FacefSurvivors
{
    /// <summary>
    /// Fábrica de elementos de interface (uGUI) com o visual do jogo.
    /// Toda a UI é montada por código, então não há prefabs para configurar.
    /// </summary>
    public static class UI
    {
        public static readonly Color Gold = new Color(1f, 0.84f, 0.35f);
        public static readonly Color TextLight = new Color(0.93f, 0.94f, 1f);
        public static readonly Color TextDim = new Color(0.68f, 0.72f, 0.86f);
        public static readonly Color Red = new Color(1f, 0.36f, 0.36f);
        public static readonly Color Green = new Color(0.45f, 0.95f, 0.5f);
        public static readonly Color XpBlue = new Color(0.3f, 0.62f, 1f);

        /// <summary>Momento até o qual o som de "hover" fica mudo (evita som ao abrir menus).</summary>
        public static float MuteHoverUntil;

        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.AddComponent<SelectionKeeper>();
        }

        /// <summary>Seleciona um elemento (para navegação por teclado/controle).</summary>
        public static void Select(GameObject go)
        {
            if (EventSystem.current == null || go == null) return;
            MuteHoverUntil = Time.unscaledTime + 0.1f;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(go);
        }

        // ------------------------------------------------------------ Layout

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 Left = new Vector2(0f, 0.5f);
        public static readonly Vector2 Right = new Vector2(1f, 0.5f);

        // ------------------------------------------------------------ Elementos

        public static Image Image(Transform parent, string name, Sprite sprite, Color color)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Ícone mantendo a proporção da imagem.</summary>
        public static Image Icon(Transform parent, Sprite sprite, Vector2 size, float rotation = 0f)
        {
            var img = Image(parent, "Icon", sprite, Color.white);
            img.preserveAspect = true;
            img.rectTransform.Place(Center, Vector2.zero, size);
            img.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
            return img;
        }

        public static Image Panel(Transform parent, string name, Vector2 size)
        {
            var img = Image(parent, name, Art.UiPanel, Color.white);
            img.raycastTarget = true;
            img.rectTransform.Place(Center, Vector2.zero, size);
            return img;
        }

        /// <summary>Fundo escuro de tela cheia (bloqueia cliques no que está atrás). Sem sprite: só a cor.</summary>
        public static Image Dim(Transform parent, string name, float alpha)
        {
            var img = Image(parent, name, null, new Color(0.02f, 0.02f, 0.06f, alpha));
            img.raycastTarget = true;
            img.rectTransform.Fill();
            return img;
        }

        /// <summary>
        /// Vinheta de tela cheia (bordas na cor <paramref name="color"/>). A imagem passa das bordas da tela
        /// para o degradê chegar suave aos cantos.
        /// </summary>
        public static Image Vignette(Transform parent, string name, Color color)
        {
            var img = Image(parent, name, Art.Vignette, color);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(-0.34f, -0.34f);
            rt.anchorMax = new Vector2(1.34f, 1.34f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        public static Text Text(Transform parent, string content, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold, bool outline = true)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.01f, 0.01f, 0.05f, 0.95f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static void StyleSelectable(Selectable s, Image img)
        {
            s.targetGraphic = img;
            s.transition = Selectable.Transition.SpriteSwap;
            s.spriteState = new SpriteState
            {
                highlightedSprite = Art.UiButtonHi,
                selectedSprite = Art.UiButtonHi,
                pressedSprite = Art.UiButtonPressed,
                disabledSprite = Art.UiButton,
            };
            if (s.GetComponent<UIFx>() == null) s.gameObject.AddComponent<UIFx>();
        }

        public static Button Button(Transform parent, string label, Vector2 size, UnityAction onClick, int fontSize = 40)
        {
            var img = Image(parent, "Button " + label, Art.UiButton, Color.white);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            var b = img.gameObject.AddComponent<Button>();
            StyleSelectable(b, img);
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(img.transform, label, fontSize, TextLight);
                t.rectTransform.Fill();
            }
            b.onClick.AddListener(() => AudioManager.Play(Sfx.Click));
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        public static Slider Slider(Transform parent, Vector2 size, float value, UnityAction<float> onChange)
        {
            var root = Rect(parent, "Slider");
            root.sizeDelta = size;

            var bg = Image(root, "Background", Art.UiBar, Color.white);
            bg.raycastTarget = true;
            bg.rectTransform.Fill();
            bg.rectTransform.offsetMin = new Vector2(0, size.y * 0.25f);
            bg.rectTransform.offsetMax = new Vector2(0, -size.y * 0.25f);

            var fillArea = Rect(root, "Fill Area").Fill();
            fillArea.offsetMin = new Vector2(8, size.y * 0.25f + 7);
            fillArea.offsetMax = new Vector2(-8, -size.y * 0.25f - 7);
            var fill = Image(fillArea, "Fill", Art.UiBarFill("White"), Gold);
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect(root, "Handle Area").Fill();
            handleArea.offsetMin = new Vector2(14, 0);
            handleArea.offsetMax = new Vector2(-14, 0);
            var handle = Image(handleArea, "Handle", Art.UiSliderHandle, Color.white);
            handle.preserveAspect = true;
            handle.rectTransform.sizeDelta = new Vector2(34, 4);

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = value;
            var colors = s.colors;
            colors.normalColor = new Color(0.62f, 0.62f, 0.7f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            s.colors = colors;
            s.onValueChanged.AddListener(onChange);
            root.gameObject.AddComponent<UIFx>();
            return s;
        }

        public static void SetVerticalNavigation(Selectable s)
        {
            var nav = s.navigation;
            nav.mode = Navigation.Mode.Vertical;
            s.navigation = nav;
        }
    }

    /// <summary>
    /// Efeito de seleção: o mouse passa a "selecionar" o botão (igual ao teclado),
    /// toca um som e aumenta levemente o elemento selecionado.
    /// </summary>
    public class UIFx : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        bool selected;
        Selectable selectable;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable != null && selectable.IsInteractable() && EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            if (Time.unscaledTime > UI.MuteHoverUntil) AudioManager.Play(Sfx.Hover, 0.8f);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
        }

        void OnDisable()
        {
            selected = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float target = selected ? 1.05f : 1f;
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>
    /// Impede que a navegação por teclado se perca quando o jogador clica no fundo da tela.
    /// </summary>
    public class SelectionKeeper : MonoBehaviour
    {
        GameObject last;

        void Update()
        {
            var es = EventSystem.current;
            if (es == null) return;
            var cur = es.currentSelectedGameObject;
            if (cur != null && cur.activeInHierarchy)
            {
                last = cur;
                return;
            }
            if (last != null && last.activeInHierarchy)
            {
                UI.MuteHoverUntil = Time.unscaledTime + 0.1f;
                es.SetSelectedGameObject(last);
            }
        }
    }

    /// <summary>
    /// Seletor "&lt; valor &gt;" controlado por setas esquerda/direita ou clique.
    /// Usado para opções liga/desliga e resolução.
    /// </summary>
    public class CycleSelector : Selectable, IPointerClickHandler, ISubmitHandler
    {
        string[] options = new string[0];
        int index;
        Action<int> onChanged;
        Text valueText;

        public int Index => index;

        public void Setup(string[] opts, int startIndex, Action<int> changed, Text text)
        {
            options = opts;
            index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, opts.Length - 1));
            onChanged = changed;
            valueText = text;
            Refresh();
        }

        public void SetIndexWithoutNotify(int i)
        {
            index = Mathf.Clamp(i, 0, Mathf.Max(0, options.Length - 1));
            Refresh();
        }

        void Step(int d)
        {
            if (options.Length == 0) return;
            index = (index + d + options.Length) % options.Length;
            Refresh();
            AudioManager.Play(Sfx.Click, 0.7f);
            onChanged?.Invoke(index);
        }

        void Refresh()
        {
            if (valueText != null && options.Length > 0) valueText.text = "<   " + options[index] + "   >";
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left) { Step(-1); eventData.Use(); }
            else if (eventData.moveDir == MoveDirection.Right) { Step(1); eventData.Use(); }
            else base.OnMove(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out var local);
            float mid = (0.5f - rt.pivot.x) * rt.rect.width;
            Step(local.x < mid ? -1 : 1);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Step(1);
        }
    }

    /// <summary>Troca de cena com fade para preto.</summary>
    public class SceneLoader : MonoBehaviour
    {
        static SceneLoader instance;
        Image overlay;
        bool busy;

        public static void Load(string scene)
        {
            if (instance == null)
            {
                var canvas = UI.CreateCanvas("SceneFader", 1000);
                DontDestroyOnLoad(canvas.gameObject);
                instance = canvas.gameObject.AddComponent<SceneLoader>();
                instance.overlay = UI.Image(canvas.transform, "Fade", null, new Color(0, 0, 0, 0));
                instance.overlay.rectTransform.Fill();
            }
            if (!instance.busy) instance.StartCoroutine(instance.Run(scene));
        }

        IEnumerator Run(string scene)
        {
            busy = true;
            overlay.raycastTarget = true;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
            {
                overlay.color = new Color(0, 0, 0, t);
                yield return null;
            }
            overlay.color = Color.black;
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.35f)
            {
                overlay.color = new Color(0, 0, 0, t);
                yield return null;
            }
            overlay.color = new Color(0, 0, 0, 0);
            overlay.raycastTarget = false;
            busy = false;
        }
    }

    /// <summary>Leitura de controles (teclado e controle via Input Manager).</summary>
    public static class GameInput
    {
        public static Vector2 Move
        {
            get
            {
                var v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public static bool PausePressed =>
            Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.JoystickButton7);

        public static bool BackPressed =>
            Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
    }
}
