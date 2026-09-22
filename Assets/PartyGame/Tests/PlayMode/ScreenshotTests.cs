using System.Collections;
using System.Linq;
using NUnit.Framework;
using PartyGame.Core.Modes;
using PartyGame.UI.Components;
using PartyGame.UI.Framework;
using PartyGame.UI.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace PartyGame.Tests.PlayMode
{
    /// <summary>
    /// Not an assertion suite: this walks the app and writes PNGs of every important screen at
    /// phone resolution, so the layout can actually be looked at rather than guessed about.
    /// Run it with the graphics device enabled.
    /// </summary>
    [Category("Screenshots")]
    public class ScreenshotTests
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
            ScreenshotCapture.EndSession();
            if (_app != null) Object.DestroyImmediate(_app.gameObject);
            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null) Object.DestroyImmediate(eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator CaptureEveryScreen()
        {
            // Everything is built against a real phone rect from here on, so the captures show
            // exactly the layout a 1080x1920 device would get.
            ScreenshotCapture.BeginSession(_app.UI.Canvas);
            _app.Screens.Reset<UI.Screens.MainMenuScreen>();
            yield return Settle();
            Shot("01-main-menu");

            _app.ShowGameSelect();
            yield return Settle();
            Shot("02-game-select");

            _app.Back();
            yield return Settle();
            _app.ShowPlayers();
            yield return Settle();
            Shot("03-players");

            _app.Back();
            yield return Settle();

            var differentWord = _app.Services.Content.GetMode(GameModeId.DifferentWord);
            _app.ShowGameSetup(differentWord);
            yield return Settle();
            Shot("04-game-setup");

            _app.ShowHowToPlay(differentWord, null, null);
            yield return Settle();
            Shot("05-how-to-play");

            _app.Back();
            yield return Settle();
            _app.Back();
            yield return Settle();

            _app.ShowSettings();
            yield return Settle();
            Shot("06-settings");
            _app.Back();
            yield return Settle();

            yield return CaptureGameplay();
        }

        /// <summary>
        /// The copy shown to a table that is below a game's suggested size, and the wavelength
        /// gap setting at its lowest value. Both are strings worth actually looking at.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureSmallTableAndGapSetting()
        {
            ScreenshotCapture.BeginSession(_app.UI.Canvas);

            _app.Services.Roster.Clear();
            foreach (var name in new[] { "Alex", "Bea", "Chris" }) _app.Services.Roster.Add(name);

            _app.Screens.Reset<UI.Screens.MainMenuScreen>();
            yield return Settle();

            _app.ShowGameSelect();
            yield return Settle();
            Shot("20-three-players-select");

            _app.ShowGameSetup(_app.Services.Content.GetMode(GameModeId.Wavelength));
            yield return Settle();
            Shot("21-wavelength-setup");
        }

        /// <summary>
        /// The Suspects ends in a win or a loss rather than a ranking, so its results screen
        /// takes a different shape from every other game's. Worth a look, not just an assert.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureSuspectsOutcome()
        {
            ScreenshotCapture.BeginSession(_app.UI.Canvas);
            _app.Screens.Reset<UI.Screens.MainMenuScreen>();
            yield return Settle();

            var definition = _app.Services.Content.GetMode(GameModeId.SocialDeduction);
            var mode = GameModeFactory.Create(GameModeId.SocialDeduction);
            var settings = new GameSettings();
            settings.Declare(mode.GetSettingDefinitions(_app.Services.Content));
            settings.SetInt(CommonSettingKeys.Rounds, 2);
            settings.SetInt(CommonSettingKeys.DiscussionSeconds, 1);
            settings.SetString(CommonSettingKeys.TieBehaviour, "random");
            settings.ClampAll(_app.Services.Roster.Count);

            Assert.IsTrue(_app.StartGame(definition, settings));
            yield return Settle();

            var shotReveal = false;
            for (var i = 0; i < 900; i++)
            {
                if (!shotReveal && Object.FindAnyObjectByType<RevealStepView>() != null)
                {
                    yield return Settle();
                    Shot("15-suspects-reveal");
                    shotReveal = true;
                }

                if (_app.Screens.Current is UI.Screens.ResultsScreen)
                {
                    yield return Settle();
                    Shot("16-suspects-outcome");
                    yield break;
                }

                Assert.IsNull(Object.FindAnyObjectByType<ScoreboardStepView>(),
                    "The Suspects must never show a scoreboard.");
                yield return Tap();
            }

            Assert.Fail("The Suspects never reached its results screen.");
        }

        private IEnumerator CaptureGameplay()
        {
            var definition = _app.Services.Content.GetMode(GameModeId.DifferentWord);
            var mode = GameModeFactory.Create(GameModeId.DifferentWord);
            var settings = new GameSettings();
            settings.Declare(mode.GetSettingDefinitions(_app.Services.Content));
            settings.SetInt(CommonSettingKeys.Rounds, 1);
            settings.ClampAll(_app.Services.Roster.Count);

            Assert.IsTrue(_app.StartGame(definition, settings));
            yield return Settle();
            Shot("07-round-intro");

            var captured = 0;
            for (var i = 0; i < 400 && captured < 6; i++)
            {
                var privateView = Object.FindAnyObjectByType<PrivateInfoStepView>();
                if (privateView != null && !privateView.HasBeenRevealed)
                {
                    Shot("08-handoff-secret-covered");
                    privateView.OnPointerDown(null);
                    yield return Settle();
                    Shot("09-secret-revealed");
                    privateView.OnPointerUp(null);
                    yield return Settle();
                    captured++;
                }

                if (Object.FindAnyObjectByType<DiscussionStepView>() != null && captured < 2)
                {
                    Shot("10-discussion");
                    captured = Mathf.Max(captured, 2);
                }

                if (Object.FindAnyObjectByType<ChoiceStepView>() != null && captured < 3)
                {
                    Shot("11-voting");
                    captured = Mathf.Max(captured, 3);
                }

                if (Object.FindAnyObjectByType<RevealStepView>() != null && captured < 4)
                {
                    Shot("12-reveal");
                    captured = Mathf.Max(captured, 4);
                }

                if (Object.FindAnyObjectByType<ScoreboardStepView>() != null && captured < 5)
                {
                    Shot("13-scores");
                    captured = Mathf.Max(captured, 5);
                }

                if (_app.Screens.Current is UI.Screens.ResultsScreen)
                {
                    yield return Settle();
                    Shot("14-results");
                    yield break;
                }

                yield return Tap();
            }

            for (var i = 0; i < 400; i++)
            {
                if (_app.Screens.Current is UI.Screens.ResultsScreen)
                {
                    yield return Settle();
                    Shot("14-results");
                    yield break;
                }
                yield return Tap();
            }
        }

        private void Shot(string name)
        {
            ScreenshotCapture.Shot(name);
        }

        private IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(0.45f);
            yield return null;
        }

        private IEnumerator Tap()
        {
            var secret = Object.FindAnyObjectByType<PrivateInfoStepView>();
            if (secret != null && !secret.HasBeenRevealed)
            {
                secret.OnPointerDown(null);
                secret.OnPointerUp(null);
                yield return null;
            }

            var field = Object.FindObjectsByType<TMP_InputField>()
                .FirstOrDefault(f => f.isActiveAndEnabled);
            if (field != null && string.IsNullOrEmpty(field.text)) field.text = "A likely answer";

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
                yield return new WaitForSecondsRealtime(0.2f);
                yield break;
            }

            confirm.Button.onClick.Invoke();
            yield return null;
        }

        private UiButton ActionButton()
        {
            return Object.FindObjectsByType<UiButton>()
                .Where(b => b.isActiveAndEnabled && b.Interactable)
                .Where(b => b.transform.parent != null && b.transform.parent.name == "Actions")
                .LastOrDefault();
        }
    }
}
