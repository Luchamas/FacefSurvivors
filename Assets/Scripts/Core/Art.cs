using System.Collections.Generic;
using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>
    /// Todos os gráficos do jogo. São imagens prontas em Assets/Resources (Kenney, Devicon, OpenGameArt e as
    /// artes do jogo); aqui só se define o tamanho no mundo, o pivô e as bordas (9-slice) de cada uma.
    /// Nada é desenhado por código.
    /// </summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> made = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite[]> frames = new Dictionary<string, Sprite[]>();

        // ================================================================ Carregamento

        /// <summary>Carrega um sprite de Assets/Resources (com plano B caso a imagem não esteja importada como Sprite).</summary>
        public static Sprite Load(string path)
        {
            if (loaded.TryGetValue(path, out var s) && s != null) return s;
            s = Resources.Load<Sprite>(path);
            if (s == null)
            {
                var tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            if (s == null) Debug.LogError("Imagem não encontrada em Resources: " + path);
            loaded[path] = s;
            return s;
        }

        /// <summary>
        /// A imagem inteira como sprite com <paramref name="units"/> de largura no mundo
        /// (ou de altura, se <paramref name="byHeight"/>).
        /// </summary>
        static Sprite Sized(string path, float units, bool byHeight = false, Vector2? pivot = null)
        {
            string key = path + (byHeight ? "|h" : "|w") + units;
            if (made.TryGetValue(key, out var s) && s != null) return s;
            var src = Load(path);
            if (src == null) return null;
            var tex = src.texture;
            float ppu = (byHeight ? tex.height : tex.width) / units;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot ?? new Vector2(0.5f, 0.5f), ppu, 0,
                SpriteMeshType.FullRect);
            s.name = key;
            made[key] = s;
            return s;
        }

        /// <summary>Sprite 9-slice. <paramref name="border"/> em pixels da imagem (esquerda, baixo, direita, cima).</summary>
        static Sprite Sliced(string path, Vector4 border, float ppu)
        {
            string key = path + "|9|" + ppu;
            if (made.TryGetValue(key, out var s) && s != null) return s;
            var src = Load(path);
            if (src == null) return null;
            var tex = src.texture;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0,
                SpriteMeshType.FullRect, border);
            s.name = key;
            made[key] = s;
            return s;
        }

        // ================================================================ Cenário e jogador

        public static Sprite CharacterIdle(CharacterDef c) => Load(c.IdleSprite);
        public static Sprite CharacterRun(CharacterDef c) => Load(c.RunSprite);

        /// <summary>Piso: 3 x 2 ladrilhos, repetível.</summary>
        public static Sprite Floor => Load("Sprites/Floor");

        /// <summary>Mesa redonda com 6 cadeiras (vista de cima, sombra embutida).</summary>
        public static Sprite Table => Load("Sprites/Table");

        // ================================================================ Linguagens (armas e itens passivos, Devicon)

        /// <summary>Logo de uma linguagem de programação, de Resources/Sprites/Languages.</summary>
        public static Sprite Language(string name) => Load("Sprites/Languages/" + name);

        /// <summary>O mesmo logo com 0,7 unidade de largura, para projéteis e objetos no mundo.</summary>
        public static Sprite LanguageInWorld(string name) => Sized("Sprites/Languages/" + name, 0.7f);

        // ================================================================ Estudantes (Kenney Toon Characters)

        /// <summary>Altura do quadro inteiro; os pés ficam na borda de baixo, 0,5 unidade abaixo do centro.</summary>
        const float StudentHeight = 1.3f;

        /// <summary>
        /// Quadros de uma animação ("walk0", "walk1"... ou "run0"...) de um personagem em
        /// Resources/Sprites/Students/&lt;personagem&gt;. O desenho olha para a direita.
        /// </summary>
        public static Sprite[] Student(string character, string pose = "walk")
        {
            string key = character + "/" + pose;
            if (frames.TryGetValue(key, out var list)) return list;
            var result = new List<Sprite>();
            var pivot = new Vector2(0.5f, 0.5f / StudentHeight);
            for (int i = 0; Resources.Load<Texture2D>("Sprites/Students/" + key + i) != null; i++)
                result.Add(Sized("Sprites/Students/" + key + i, StudentHeight, true, pivot));
            if (result.Count == 0) Debug.LogError("Animação não encontrada: Sprites/Students/" + key);
            list = result.ToArray();
            frames[key] = list;
            return list;
        }

        // ================================================================ Chefes (Kenney Generic Items)

        /// <summary>Folha de prova (em escala 1 tem ~1,15 unidade de altura).</summary>
        public static Sprite PaperBoss => Sized("Sprites/Bosses/Prova", 1.15f, true);

        /// <summary>O TCC: um livro grosso.</summary>
        public static Sprite ThesisBoss => Sized("Sprites/Bosses/TCC", 1.15f, true);

        // ================================================================ Coletáveis

        public static Sprite Gem(int tier)
        {
            switch (tier)
            {
                case 0: return Sized("Sprites/Items/GemBlue", 0.55f);
                case 1: return Sized("Sprites/Items/GemGreen", 0.55f);
                default: return Sized("Sprites/Items/GemRed", 0.55f);
            }
        }

        /// <summary>Coxinha (pixel art de "Foodies", por maruki).</summary>
        public static Sprite Coxinha => Sized("Sprites/Pixel/Coxinha", 0.6f);

        public static Sprite Chest => Sized("Sprites/Items/Chest", 0.75f);

        public static Sprite Magnet => Sized("Sprites/Items/Magnet", 0.5f);

        // ================================================================ Efeitos (Kenney Particle Pack e Light Masks)

        /// <summary>Ponto de luz suave, 1 unidade (partículas e brilho).</summary>
        public static Sprite Dot => Sized("Sprites/Effects/Dot", 1f);

        /// <summary>Disco de bordas suaves com 1 unidade de diâmetro (auras).</summary>
        public static Sprite Disc => Sized("Sprites/Effects/Disc", 1f);

        /// <summary>Anel com 1 unidade de diâmetro.</summary>
        public static Sprite Ring => Sized("Sprites/Effects/Ring", 1f);

        /// <summary>Sombra oval de 1 x 0,5 unidade.</summary>
        public static Sprite Shadow => Sized("Sprites/Effects/Shadow", 1f);

        /// <summary>Arco do golpe do Haskell, 2 x 1 unidades.</summary>
        public static Sprite Slash => Sized("Sprites/Effects/Slash", 2f);

        /// <summary>Raio com 8 unidades de altura, pivô embaixo (onde ele cai).</summary>
        public static Sprite Lightning(int variant) =>
            Sized("Sprites/Effects/Lightning" + (variant % 2), 8f, true, new Vector2(0.5f, 0f));

        // ================================================================ UI (Kenney UI Pack e UI Pack Adventure)

        const string Ui = "UI/Kenney/";

        public static Sprite UiPanel => Sliced(Ui + "Panel", new Vector4(16, 16, 16, 16), 50f);

        public static Sprite UiSlot => Sliced(Ui + "Slot", new Vector4(12, 12, 12, 12), 100f);

        /// <summary>Fundo das barras (XP, chefe e sliders).</summary>
        public static Sprite UiBar => UiSlot;

        public static Sprite UiButton => Sliced(Ui + "Button", new Vector4(12, 16, 12, 12), 75f);

        public static Sprite UiButtonHi => Sliced(Ui + "ButtonHi", new Vector4(12, 16, 12, 12), 75f);

        public static Sprite UiButtonPressed => Sliced(Ui + "ButtonPressed", new Vector4(12, 12, 12, 12), 75f);

        public static Sprite UiSliderHandle => Load(Ui + "SliderHandle");

        /// <summary>Preenchimento das barras: "Blue", "Red" ou "White" (para tingir).</summary>
        public static Sprite UiBarFill(string color) => Sliced(Ui + "Bar" + color, new Vector4(8, 0, 8, 0), 60f);

        /// <summary>Barrinha de vida no mundo: "White" (fundo, para tingir) ou "Red"; 16 px = 0,13 unidade.</summary>
        public static Sprite WorldBar(string color) => Sliced(Ui + "BarSmall" + color, new Vector4(7, 0, 7, 0), 16f / 0.13f);

        /// <summary>Bordas opacas e centro transparente; tingir de preto (menu) ou vermelho (dano).</summary>
        public static Sprite Vignette => Load(Ui + "Vignette");
    }
}
