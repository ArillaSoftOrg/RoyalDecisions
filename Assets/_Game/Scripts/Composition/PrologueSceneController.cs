using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Starts the prologue automatically when Prologue.unity loads, and loads the Game scene
    /// exactly once when it finishes — whether it completes naturally or is skipped.
    /// </summary>
    /// <remarks>
    /// Deliberately thin, mirroring how <see cref="BootstrapController"/> stays thin around the
    /// intro/loading controllers: this owns scene navigation only. Slide content, animation, and
    /// the exactly-once completion guarantee all stay inside
    /// <see cref="PrologueSequenceController"/> — this component only decides what happens once that
    /// callback fires, and fails open to Game if the sequence itself is missing.
    /// </remarks>
    public sealed class PrologueSceneController : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string gameSceneName = "Game";

        [Header("Wiring")]
        [Tooltip("Optional. A missing sequence loads Game immediately instead of blocking the player.")]
        [SerializeField] private PrologueSequenceController prologueSequence;

        private ISceneLoader sceneLoader;
        private ISettingsStore settingsStore;
        private bool hasLoadedGame;

        /// <summary>Injection seam for tests, which must never touch persistent data.</summary>
        public void Configure(
            ISceneLoader loader, PrologueSequenceController prologue, ISettingsStore settings = null)
        {
            sceneLoader = loader;
            prologueSequence = prologue;
            settingsStore = settings;
        }

        private void Start()
        {
            BeginPrologueSequence();
        }

        /// <summary>
        /// Applies the current reduced-motion and audio settings and starts the prologue, or loads
        /// Game immediately if no sequence is wired. Public (rather than folded into
        /// <see cref="Start"/>) so a test can drive it directly after <see cref="Configure"/> without
        /// waiting on Unity's own lifecycle. Safe to call more than once: the underlying
        /// <see cref="PrologueSequenceController.Play"/> and <see cref="LoadGameOnce"/> guards mean
        /// only the first call's sequence ever actually plays, and Game only ever loads once.
        /// </summary>
        public void BeginPrologueSequence()
        {
            sceneLoader ??= new UnitySceneLoader();

            if (prologueSequence == null)
            {
                LoadGameOnce();
                return;
            }

            ApplySettings();
            prologueSequence.Play(LoadGameOnce);
        }

        /// <summary>
        /// Reads the same persisted settings <c>BootstrapController</c>/<c>GameSceneController</c>
        /// apply, through the same file-backed store — Prologue is reached mid-session, after
        /// Bootstrap has already run once, so it reads its own fresh copy rather than sharing a live
        /// instance across scenes. Applies both the reduced-motion accessibility setting and the
        /// audio volume/mute settings, so the prologue's ambient/accent audio respects master
        /// volume, music volume, and mute exactly like every other scene's — Reduced Motion itself
        /// never touches audio, only <see cref="PrologueSequenceController.SetReducedMotion"/>.
        /// </summary>
        private void ApplySettings()
        {
            if (settingsStore == null)
            {
                SavePaths paths = SavePaths.ForPersistentData();
                settingsStore = new SettingsServiceStore(
                    new SettingsSaveService(new SystemFileSystem(), paths));
            }

            // Never fails: unreadable preferences resolve to defaults, same as everywhere else this
            // store is used.
            GameSettings settings = settingsStore.Load();
            prologueSequence.SetReducedMotion(settings.ReducedMotion);
            prologueSequence.ApplyAudioSettings(
                settings.MasterVolume, settings.MusicVolume, settings.SfxVolume, settings.MasterMuted);
        }

        private void LoadGameOnce()
        {
            if (hasLoadedGame)
            {
                return;
            }

            hasLoadedGame = true;
            sceneLoader?.LoadScene(gameSceneName);
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by scene setup and tests.</summary>
        public void SetAuthoringReferences(PrologueSequenceController prologue)
        {
            prologueSequence = prologue;
        }
#endif
    }
}
