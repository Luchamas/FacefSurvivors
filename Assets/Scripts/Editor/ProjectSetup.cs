using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FacefSurvivors.EditorTools
{
    /// <summary>
    /// Configura o projeto automaticamente ao abrir no Unity:
    /// cria as cenas MainMenu e Game, adiciona ao Build e ajusta a importação do logo.
    /// Também disponível no menu "FACEF Survivors" do editor.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string MenuScene = "Assets/Scenes/MainMenu.unity";
        public const string GameScene = "Assets/Scenes/Game.unity";
        const string LogoPath = "Assets/Resources/UI/Logo.png";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                bool created = EnsureScenes();
                EnsureBuildSettings();
                FixLogoImport();
                var active = SceneManager.GetActiveScene();
                bool emptyUntitled = string.IsNullOrEmpty(active.path) && active.rootCount <= 2 && !active.isDirty;
                if (created || emptyUntitled) EditorSceneManager.OpenScene(MenuScene);
            };
        }

        [MenuItem("FACEF Survivors/Configurar projeto (cenas e build)")]
        public static void Setup()
        {
            EnsureScenes();
            EnsureBuildSettings();
            FixLogoImport();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MenuScene);
            Debug.Log("FACEF Survivors: projeto configurado. Abra a cena MainMenu e aperte Play!");
        }

        [MenuItem("FACEF Survivors/Abrir menu principal")]
        static void OpenMenu()
        {
            EnsureScenes();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MenuScene);
        }

        [MenuItem("FACEF Survivors/Abrir cena do jogo")]
        static void OpenGame()
        {
            EnsureScenes();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(GameScene);
        }

        /// <summary>Para linha de comando: -executeMethod FacefSurvivors.EditorTools.ProjectSetup.SetupBatch</summary>
        public static void SetupBatch()
        {
            EnsureScenes();
            EnsureBuildSettings();
            FixLogoImport();
            AssetDatabase.SaveAssets();
        }

        static bool EnsureScenes()
        {
            bool created = false;
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!File.Exists(MenuScene))
            {
                CreateScene(MenuScene, "MainMenu", typeof(MainMenuController));
                created = true;
            }
            if (!File.Exists(GameScene))
            {
                CreateScene(GameScene, "Game", typeof(GameController));
                created = true;
            }
            return created;
        }

        static void CreateScene(string path, string rootName, System.Type controller)
        {
            // O Unity não permite abrir cena aditiva com uma cena "Untitled" aberta:
            // nesse caso a nova cena substitui a Untitled (perguntando antes se houver alterações).
            var active = SceneManager.GetActiveScene();
            bool single = string.IsNullOrEmpty(active.path);
            if (single && active.isDirty && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, single ? NewSceneMode.Single : NewSceneMode.Additive);

            var camGo = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(camGo, scene);
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.1f, 0.09f);
            camGo.AddComponent<AudioListener>();

            var go = new GameObject(rootName, controller);
            SceneManager.MoveGameObjectToScene(go, scene);

            EditorSceneManager.SaveScene(scene, path);
            if (!single) EditorSceneManager.CloseScene(scene, true);
        }

        static void EnsureBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(GameScene, true),
            };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != MenuScene && s.path != GameScene) list.Add(s);

            var cur = EditorBuildSettings.scenes;
            bool same = cur.Length == list.Count;
            for (int i = 0; same && i < cur.Length; i++)
                same = cur[i].path == list[i].path && cur[i].enabled == list[i].enabled;
            if (!same) EditorBuildSettings.scenes = list.ToArray();
        }

        public static void ConfigureUiTexture(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
        }

        /// <summary>
        /// Imagens do jogo (Resources/Sprites): Sprite com malha retangular (necessária para o piso repetir),
        /// mipmaps (ficam suaves quando desenhados menores) e sem compressão.
        /// Pixel art (Resources/Sprites/Pixel) fica sem mipmaps e sem suavização.
        /// </summary>
        public static void ConfigureGameSprite(TextureImporter ti, bool repeat, bool pixel)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.mipmapEnabled = !pixel;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = pixel ? FilterMode.Point : FilterMode.Trilinear;
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }

        public static bool IsPixelArt(string path)
        {
            return path.Replace('\\', '/').StartsWith(SpritesFolder + "/Pixel/");
        }

        public static bool IsRepeating(string path)
        {
            return Path.GetFileNameWithoutExtension(path) == "Floor";
        }

        static void FixLogoImport()
        {
            var ti = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
            if (ti != null && (ti.textureType != TextureImporterType.Sprite || ti.mipmapEnabled))
            {
                ConfigureUiTexture(ti);
                ti.SaveAndReimport();
            }

            if (!AssetDatabase.IsValidFolder(SpritesFolder)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var gi = AssetImporter.GetAtPath(path) as TextureImporter;
                if (gi == null) continue;
                var s = new TextureImporterSettings();
                gi.ReadTextureSettings(s);
                if (gi.textureType == TextureImporterType.Sprite && gi.mipmapEnabled != IsPixelArt(path)
                    && s.spriteMeshType == SpriteMeshType.FullRect) continue;
                ConfigureGameSprite(gi, IsRepeating(path), IsPixelArt(path));
                gi.SaveAndReimport();
            }
        }

        public const string SpritesFolder = "Assets/Resources/Sprites";
    }

    /// <summary>Configura a importação das imagens (Resources/UI e Resources/Sprites) e da música (Resources/Audio).</summary>
    public class ImportSettingsPostprocessor : AssetPostprocessor
    {
        /// <summary>Música: tocada direto do disco (streaming), comprimida em Vorbis, sem ocupar a memória toda.</summary>
        void OnPreprocessAudio()
        {
            if (!assetImporter.importSettingsMissing) return;
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/Resources/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            ai.defaultSampleSettings = s;
        }

        void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing) return;
            string path = assetPath.Replace('\\', '/');
            if (path.StartsWith("Assets/Resources/UI/"))
                ProjectSetup.ConfigureUiTexture((TextureImporter)assetImporter);
            else if (path.StartsWith(ProjectSetup.SpritesFolder + "/"))
                ProjectSetup.ConfigureGameSprite((TextureImporter)assetImporter, ProjectSetup.IsRepeating(path),
                    ProjectSetup.IsPixelArt(path));
        }
    }
}
