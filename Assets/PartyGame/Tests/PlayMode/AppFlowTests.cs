using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartyGame.Core.Modes;
using PartyGame.UI.Components;
using PartyGame.UI.Framework;
using PartyGame.UI.Gameplay;
using PartyGame.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace PartyGame.Tests.PlayMode
{
    /// <summary>
    /// Drives the real UI, not the rules: every screen is built, every game is played from the
    /// first tap to the results screen through the same buttons a player would press.
    /// </summary>
    public class AppFlowTests
    {
        private AppController _app;

        [SetUp]
        public void SetUp()
        {
            _app = AppController.Bootstrap();
            _app.Services.Roster.Clear();
            foreach (var name in new[] { "Alex", "Bea", "Chris", "Dev", "Eli", "Fran" })
                _app.Services.Roster.Add(name);
        }

        [TearDown]
        public void TearDown()
        {
            if (_app != null) Object.DestroyImmediate(_app.gameObject);
            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null) Object.DestroyImmediate(eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator AppBootsIntoTheMainMenu()
        {
            yield return null;

            Assert.IsNotNull(_app.UI, "The canvas was never created.");
            Assert.IsNotNull(_app.Services.Content, "Content service missing.");
            Assert.IsTrue(_app.Services.Content.HasLibrary, "No content library was loaded at boot.");
            Assert.IsInstanceOf<MainMenuScreen>(_app.Screens.Current);
        }

        [UnityTest]
        public IEnumerator EveryTopLevelScreenBuilds()
        {
            yield return null;

            _app.ShowGameSelect();
            yield return null;
            Assert.IsInstanceOf<GameSelectScreen>(_app.Screens.Current);

            _app.Back();
            yield return null;

            _app.ShowPlayers();
            yield return null;
            Assert.IsInstanceOf<PlayerSetupScreen>(_app.Screens.Current);
            _app.Back();
            yield return null;

            _app.ShowSettings();
            yield return null;
            Assert.IsInstanceOf<SettingsScreen>(_app.Screens.Current);
            _app.Back();
            yield return null;

            _app.ShowAbout();
            yield return null;
            Assert.IsInstanceOf<AboutScreen>(_app.Screens.Current);
            _app.Back();
            yield return null;

            _app.ShowHelpIndex();
            yield return null;
            Assert.IsInstanceOf<HelpIndexScreen>(_app.Screens.Current);
            _app.Back();
            yield return null;

            Assert.IsInstanceOf<MainMenuScreen>(_app.Screens.Current);
        }

        [UnityTest]
        public IEnumerator EveryGameHasASetupScreenAndRulesScreen()
        {
            yield return null;

            foreach (var definition in _app.Services.Content.GameModes.ToList())
            {
                _app.ShowGameSetup(definition);
                yield return null;
                Assert.IsInstanceOf<GameSetupScreen>(_app.Screens.Current, definition.DisplayName + " setup failed.");

                _app.ShowHowToPlay(definition, null, null);
                yield return null;
                Assert.IsInstanceOf<HowToPlayScreen>(_app.Screens.Current, definition.DisplayName + " rules failed.");

                _app.Back();
                yield return null;
                _app.Back();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DifferentWordPlaysThroughTheUi()
        {
            yield return PlayThrough(GameModeId.DifferentWord);
        }

        [UnityTest]
        public IEnumerator FibPlaysThroughTheUi()
        {
            yield return PlayThrough(GameModeId.Fib);
        }

        [UnityTest]
        public IEnumerator WavelengthPlaysThroughTheUi()
        {
            yield return PlayThrough(GameModeId.Wavelength);
        }

        [UnityTest]
        public IEnumerator DevilsAdvocatePlaysThroughTheUi()
        {
            yield return PlayThrough(GameModeId.DevilsAdvocate);
        }

        [UnityTest]
        public IEnumerator SocialDeductionPlaysThroughTheUi()
        {
            yield return PlayThrough(GameModeId.SocialDeduction);
        }

        /// <summary>
        /// The chrome around a covered card must not vary with what the card says. A header
        /// tinted by the step accent used to turn red for the imposter, announcing the role
        /// before anybody had revealed anything - and while the previous player was still
        /// holding the phone.
        /// </summary>
        [UnityTest]
        public IEnumerator CoveredCardsLookIdenticalWhateverTheSecretSays()
        {
            yield return null;
            yield return StartGame(GameModeId.DifferentWord, 1);

            var coveredColours = new HashSet<Color>();
            var handoffColours = new HashSet<Color>();
            var cardsSeen = 0;

            for (var i = 0; i < 400; i++)
            {
                if (_app.Screens.Current is ResultsScreen) break;

                var handoff = Object.FindAnyObjectByType<HandoffView>();
                if (handoff != null) Collect(handoff, handoffColours);

                var secret = Object.FindAnyObjectByType<PrivateInfoStepView>();
                if (secret != null && !secret.HasBeenRevealed)
                {
                    Collect(secret, coveredColours);
                    cardsSeen++;
                }

                yield return Tap();
            }

            Assert.GreaterOrEqual(cardsSeen, 6, "Expected one covered card per player.");
            Assert.AreEqual(1, coveredColours.Count,
                "Covered cards are not all the same colour, so the header gives the role away.");
            Assert.AreEqual(1, handoffColours.Count,
                "Hand-over screens are not all the same colour, so the role leaks before the pass.");
        }

        /// <summary>Records the header colour of whichever step view is on screen.</summary>
        private static void Collect(StepView view, HashSet<Color> into)
        {
            var label = view.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.name == "Phase");
            if (label != null) into.Add(label.color);
        }

        [UnityTest]
        public IEnumerator SecretsAreCoveredUntilThePlayerRevealsThem()
        {
            yield return null;
            yield return StartGame(GameModeId.DifferentWord, 1);

            // Walk forward until the first private card appears.
            for (var i = 0; i < 40; i++)
            {
                var view = Object.FindAnyObjectByType<PrivateInfoStepView>();
                if (view != null)
                {
                    Assert.IsFalse(view.IsShowingSecret,
                        "A secret card was visible before anybody asked to see it.");
                    yield break;
                }
                yield return Tap(revealSecrets: false);
            }

            Assert.Fail("No private information screen appeared.");
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// With no store wired up there are no ads and nothing to buy, so an "Advertising"
        /// heading over a permanently disabled purchase button would describe a build that does
        /// not exist - and contradict a listing that declares neither ads nor purchases.
        /// </summary>
        [UnityTest]
        public IEnumerator SettingsHidesAdvertisingWhenThereIsNoStore()
        {
            yield return null;
            Assert.IsFalse(_app.Services.Store_Purchases.StoreAvailable,
                "This test describes a build with no store connected.");

            _app.ShowSettings();
            yield return null;
            yield return null;

            var texts = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)
                .Where(t => t.isActiveAndEnabled)
                .Select(t => t.text)
                .ToList();

            Assert.IsNotEmpty(texts, "The settings screen did not build.");
            foreach (var banned in new[] { "ADVERTISING", "Remove ads", "Restore purchases" })
                Assert.IsFalse(texts.Any(t => t != null && t.Contains(banned)),
                    "Settings still offers \"" + banned + "\" with no store connected.");
        }

        private IEnumerator PlayThrough(GameModeId modeId)
        {
            yield return null;
            yield return StartGame(modeId, 2);

            for (var i = 0; i < 900; i++)
            {
                if (_app.Screens.Current is ResultsScreen) yield break;
                yield return Tap();
            }

            Assert.Fail(modeId + " never reached the results screen.");
        }

        private IEnumerator StartGame(GameModeId modeId, int rounds)
        {
            var definition = _app.Services.Content.GetMode(modeId);
            Assert.IsNotNull(definition, "No definition for " + modeId);

            var mode = GameModeFactory.Create(modeId);
            var settings = new GameSettings();
            settings.Declare(mode.GetSettingDefinitions(_app.Services.Content));
            settings.SetInt(CommonSettingKeys.Rounds, rounds);
            settings.SetInt(CommonSettingKeys.DiscussionSeconds, 1);
            settings.ClampAll(_app.Services.Roster.Count);

            Assert.IsTrue(_app.StartGame(definition, settings), modeId + " refused to start.");
            yield return null;
            Assert.IsInstanceOf<GameplayScreen>(_app.Screens.Current);
        }

        /// <summary>
        /// One simulated interaction: fill in any text field, pick an option if one is needed,
        /// then press whichever action button the screen is offering.
        /// </summary>
        private IEnumerator Tap(bool revealSecrets = true)
        {
            // A covered secret card keeps its action button disabled until somebody looks at it,
            // which is exactly the behaviour being exercised here.
            if (revealSecrets)
            {
                var secret = Object.FindAnyObjectByType<PrivateInfoStepView>();
                if (secret != null && !secret.HasBeenRevealed)
                {
                    secret.OnPointerDown(null);
                    secret.OnPointerUp(null);
                    yield return null;
                }
            }

            var field = Object.FindObjectsByType<TMP_InputField>()
                .FirstOrDefault(f => f.isActiveAndEnabled);
            if (field != null && string.IsNullOrEmpty(field.text))
                field.text = "Answer " + Random.Range(1000, 999999);

            var confirm = ActionButton();
            if (confirm == null || !confirm.Interactable)
            {
                var card = Object.FindObjectsByType<SelectionCard>()
                    .FirstOrDefault(c => c.isActiveAndEnabled && c.Interactable && !c.Selected);
                if (card != null)
                {
                    card.Button.onClick.Invoke();
                    yield return null;
                    confirm = ActionButton();
                }
            }

            if (confirm == null)
            {
                // A timed step may still be counting down; give it real time to finish.
                yield return new WaitForSecondsRealtime(0.2f);
                yield break;
            }

            confirm.Button.onClick.Invoke();
            yield return null;
        }

        /// <summary>The step's own action button, never the header's back button.</summary>
        private UiButton ActionButton()
        {
            return Object.FindObjectsByType<UiButton>()
                .Where(b => b.isActiveAndEnabled && b.Interactable)
                .Where(b => b.transform.parent != null && b.transform.parent.name == "Actions")
                .LastOrDefault();
        }
    }
}
