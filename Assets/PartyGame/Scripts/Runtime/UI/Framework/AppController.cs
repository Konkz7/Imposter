using System;
using PartyGame.Core.App;
using PartyGame.Core.Audio;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Persistence;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.UI.Components;
using PartyGame.UI.Screens;
using UnityEngine;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// The composition root and the only navigator. Screens ask it to go somewhere; it owns
    /// the services, the screen stack and the lifetime of a game session.
    /// </summary>
    public class AppController : MonoBehaviour
    {
        public static AppController Instance { get; private set; }

        public AppServices Services { get; private set; }
        public ModeSettingsStore ModeSettings { get; private set; }
        public ScreenStack Screens { get; private set; }
        public UIRoot UI { get; private set; }

        private ToastLayer _toasts;

        /// <summary>The game currently being played, or null between games.</summary>
        public GameSession ActiveSession { get; private set; }
        public IGameMode ActiveMode { get; private set; }

        public static AppController Bootstrap()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("Party Game App");
            DontDestroyOnLoad(go);
            var controller = go.AddComponent<AppController>();
            controller.Initialise();
            return controller;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Initialise()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var audio = gameObject.AddComponent<AudioManager>();
            var store = new PlayerPrefsStore();
            Services = new AppServices(store, ContentService.LoadFromResources(), audio);
            audio.Initialise(Services.Settings);

            ModeSettings = new ModeSettingsStore(store);

            UiTween.SetRunner(this);
            UiFeedback.PlaySound = id => Services.Audio.Play(id);
            UiFeedback.Vibrate = () => Services.Audio.Vibrate();
            UiFeedback.AnimationScale = Services.Settings.Settings.AnimationMultiplier;
            Services.Settings.Changed += s => UiFeedback.AnimationScale = s.AnimationMultiplier;

            Services.RestorePlayers();

            UI = UIRoot.Create(transform);
            _toasts = ToastLayer.Create(UI.OverlayLayer);
            Screens = new ScreenStack(UI.ScreenLayer, this);
            Screens.Reset<MainMenuScreen>();
        }

        private void Update()
        {
            // Android back / Escape. The screen gets first refusal so a mid-round screen can
            // ask for confirmation instead of dropping out of a game.
            if (WasBackPressed())
            {
                var current = Screens.Current;
                if (current != null && current.HandleBack()) return;
                Back();
            }
        }

        private static bool WasBackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>Keeps the ad policy aware of what is on screen. Ads never run over secrets.</summary>
        public void NotifyScreenChanged()
        {
            if (Services?.AdPolicy != null)
                Services.AdPolicy.PrivateInformationVisible = Screens.AnyPrivateInformationVisible;
        }

        public void SetPrivateInformationVisible(bool visible)
        {
            if (Services?.AdPolicy != null) Services.AdPolicy.PrivateInformationVisible = visible;
        }

        // ------------------------------------------------------------------ navigation

        public void Back()
        {
            if (!Screens.Pop()) UiFeedback.Sound(SoundId.Error);
            else UiFeedback.Sound(SoundId.Back);
        }

        public void GoHome()
        {
            EndSession();
            Screens.Reset<MainMenuScreen>();
        }

        public void ShowGameSelect() => Screens.Push<GameSelectScreen>();

        public void ShowPlayers() => Screens.Push<PlayerSetupScreen>();

        public void ShowSettings() => Screens.Push<SettingsScreen>();

        public void ShowAbout() => Screens.Push<AboutScreen>();

        public void ShowHelpIndex() => Screens.Push<HelpIndexScreen>();

        public void ShowGameSetup(GameModeDefinition definition)
        {
            if (definition == null) return;
            Screens.Push<GameSetupScreen>(screen => screen.Configure(definition));
        }

        public void ShowHowToPlay(GameModeDefinition definition, GameSettings settings, Action onStart)
        {
            Screens.Push<HowToPlayScreen>(screen => screen.Configure(definition, settings, onStart));
        }

        public void Toast(string message, bool isError = true)
        {
            _toasts?.Show(message, isError);
        }

        // ------------------------------------------------------------------ game session

        /// <summary>
        /// Validates and starts a game. Returns false with a toast if the configuration is
        /// not playable, so a bad setting can never drop the player into a broken round.
        /// </summary>
        public bool StartGame(GameModeDefinition definition, GameSettings settings)
        {
            if (definition == null)
            {
                Toast("That game is not available.");
                return false;
            }

            var mode = GameModeFactory.Create(definition.ModeId);
            if (mode == null)
            {
                Toast(definition.DisplayName + " is not playable yet.");
                return false;
            }

            var scores = new ScoreManager();
            scores.RegisterAll(System.Linq.Enumerable.Select(Services.Roster.Players, p => p.Id));

            var session = new GameSession(Services.Roster, scores, Services.Random, Services.Content, definition, settings);
            session.ConfigureRounds(settings.GetInt(CommonSettingKeys.Rounds, 3));

            var validation = mode.Validate(session);
            if (!validation.IsValid)
            {
                Toast(validation.Message);
                return false;
            }

            Services.Roster.ResetForNewGame();
            scores.ResetScores();
            mode.Initialise(session);

            ActiveSession = session;
            ActiveMode = mode;

            Screens.Push<GameplayScreen>(screen => screen.Configure(session, mode));
            return true;
        }

        public void ShowResults(GameSession session, RoundSummary summary)
        {
            var scored = ActiveMode == null || ActiveMode.UsesScoring;
            Screens.Replace<ResultsScreen>(screen => screen.Configure(session, summary, scored));
            Services.AdPolicy.NotifyGameCompleted();
        }

        /// <summary>Restarts the same mode with the same settings and a fresh scoreboard.</summary>
        public void PlayAgain()
        {
            if (ActiveSession == null)
            {
                GoHome();
                return;
            }

            var definition = ActiveSession.Definition;
            var settings = ActiveSession.Settings;
            EndSession();
            Screens.PopToRoot();
            StartGame(definition, settings);
        }

        /// <summary>Leaves the current game, showing an interstitial if the policy allows one.</summary>
        public void FinishAndGoHome()
        {
            Services.Ads.ShowInterstitial(Core.Monetisation.AdPlacement.AfterGame, GoHome);
        }

        public void EndSession()
        {
            if (ActiveSession != null) ActiveSession.ClearRoundData();
            ActiveSession = null;
            ActiveMode = null;
            SetPrivateInformationVisible(false);
        }

        private void OnApplicationPause(bool paused)
        {
            // Backgrounding the app while a secret is on screen must not leave it visible.
            if (paused) Services?.Settings?.Save();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
