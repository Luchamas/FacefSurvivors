using UnityEngine;
using UnityEngine.SceneManagement;

namespace FacefSurvivors
{
    /// <summary>
    /// Rede de segurança: se o Play for apertado numa cena vazia (sem menu nem jogo),
    /// abre o menu principal automaticamente.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Object.FindAnyObjectByType<MainMenuController>() != null) return;
            if (Object.FindAnyObjectByType<GameController>() != null) return;
            if (SceneManager.GetActiveScene().rootCount > 3) return; // cena do usuário: não interfere
            new GameObject("MainMenu").AddComponent<MainMenuController>();
        }
    }
}
