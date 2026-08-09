using UnityEngine;
using UnityEngine.InputSystem;

namespace RoyalDecisions.Composition
{
    /// <summary>Coalesces Unity pause/focus callbacks and owns Android back navigation.</summary>
    public sealed class ApplicationLifecycleController : MonoBehaviour
    {
        [SerializeField] private GameSceneController gameSceneController;
        [SerializeField] private SettingsController settingsController;
        [SerializeField] private TutorialCoordinator tutorialCoordinator;
        [SerializeField] private bool mainMenuMode;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private ISceneLoader sceneLoader;
        private IApplicationQuitter quitter;
        private bool backgrounded;

        private void Awake()
        {
            sceneLoader ??= new UnitySceneLoader();
            quitter ??= new UnityApplicationQuitter();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleBackRequested();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            SetBackgrounded(paused);
        }

        private void OnApplicationFocus(bool focused)
        {
            SetBackgrounded(!focused);
        }

        public void HandleBackRequested()
        {
            if (mainMenuMode)
            {
                quitter?.Quit();
                return;
            }

            if (tutorialCoordinator != null && tutorialCoordinator.CloseIfOpen())
            {
                return;
            }
            if (settingsController != null && settingsController.CloseIfOpen())
            {
                return;
            }

            gameSceneController?.HandleApplicationInterrupted();
            sceneLoader?.LoadScene(mainMenuSceneName);
        }

        public void SetBackgrounded(bool isBackgrounded)
        {
            if (backgrounded == isBackgrounded)
            {
                return;
            }
            backgrounded = isBackgrounded;
            if (backgrounded)
            {
                gameSceneController?.HandleApplicationInterrupted();
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            GameSceneController gameController,
            bool isMainMenu,
            string menuSceneName = "MainMenu",
            SettingsController settings = null,
            TutorialCoordinator tutorial = null)
        {
            gameSceneController = gameController;
            mainMenuMode = isMainMenu;
            mainMenuSceneName = menuSceneName;
            settingsController = settings;
            tutorialCoordinator = tutorial;
        }

        public void ConfigureForTests(ISceneLoader loader, IApplicationQuitter applicationQuitter)
        {
            sceneLoader = loader;
            quitter = applicationQuitter;
        }
#endif
    }
}
