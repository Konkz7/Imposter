using PartyGame.Core.Modes;
using PartyGame.Core.Session;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using PartyGame.UI.Gameplay;
using UnityEngine;

namespace PartyGame.UI.Screens
{
    /// <summary>
    /// Drives a whole game. It asks the mode for the next step, renders it with the matching
    /// view and feeds the result back. It contains no rules of its own, which is what lets all
    /// five games share this one screen.
    /// </summary>
    public class GameplayScreen : ScreenBase
    {
        private GameSession _session;
        private IGameMode _mode;
        private RectTransform _stage;
        private StepView _currentView;
        private GameStep _currentStep;
        private int? _phoneHolder;
        private float _quitArmedUntil;

        public override string Title => _session != null && _session.Definition != null
            ? _session.Definition.DisplayName
            : "Game";

        public override string Subtitle => _session != null ? _session.RoundLabel : string.Empty;

        public override string BackLabel => "Quit";

        public override bool ShowsPrivateInformation =>
            _currentStep != null && _currentStep.IsPrivate;

        public void Configure(GameSession session, IGameMode mode)
        {
            _session = session;
            _mode = mode;
        }

        protected override void Build()
        {
            _stage = UIFactory.CreateRect("Stage", Body);
            UIFactory.Stretch(_stage);

            if (_session == null || _mode == null)
            {
                var message = UIFactory.CreateText(_stage, "This game could not be started.", Theme.FontBody,
                    Theme.TextMuted, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Normal, "Error");
                UIFactory.Stretch(message.rectTransform);
                return;
            }

            StartRound();
        }

        private void StartRound()
        {
            _phoneHolder = null;
            _mode.StartRound();
            RefreshHeader();
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var step = _mode.Current;
            if (step == null)
            {
                FinishRound();
                return;
            }

            // A private step for somebody new always goes behind the hand-over gate first.
            var needsHandoff = step.IsPrivate && step.ActorPlayerId.HasValue && _phoneHolder != step.ActorPlayerId;
            if (needsHandoff)
            {
                ShowHandoff(step);
                return;
            }

            ShowStep(step);
        }

        private void ShowHandoff(GameStep step)
        {
            ClearStage();
            _currentStep = step;
            App.SetPrivateInformationVisible(false);

            var name = step.ActorPlayerId.HasValue ? _session.NameOf(step.ActorPlayerId.Value) : string.Empty;
            _currentView = StepView.Spawn<HandoffView>(_stage, step, _ =>
            {
                _phoneHolder = step.ActorPlayerId;
                ShowStep(step);
            }, view => view.SetPlayerName(name));
        }

        private void ShowStep(GameStep step)
        {
            ClearStage();
            _currentStep = step;
            App.SetPrivateInformationVisible(step.IsPrivate);

            switch (step.Kind)
            {
                case StepKind.PrivateInfo:
                    var hold = App.Services.Settings.Settings.HoldToReveal;
                    _currentView = StepView.Spawn<PrivateInfoStepView>(_stage, step, OnStepComplete,
                        view => view.SetHoldToReveal(hold));
                    break;
                case StepKind.TextInput:
                    _currentView = StepView.Spawn<TextInputStepView>(_stage, step, OnStepComplete);
                    break;
                case StepKind.Choice:
                    _currentView = StepView.Spawn<ChoiceStepView>(_stage, step, OnStepComplete);
                    break;
                case StepKind.Discussion:
                    _currentView = StepView.Spawn<DiscussionStepView>(_stage, step, OnStepComplete);
                    break;
                case StepKind.Reveal:
                    _currentView = StepView.Spawn<RevealStepView>(_stage, step, OnStepComplete);
                    break;
                case StepKind.Scoreboard:
                    _currentView = StepView.Spawn<ScoreboardStepView>(_stage, step, OnStepComplete);
                    break;
                default:
                    _currentView = StepView.Spawn<MessageStepView>(_stage, step, OnStepComplete);
                    break;
            }

            if (UiFeedback.AnimationsEnabled && _currentView != null)
                UiTween.Run(UiTween.FadeCanvas(_currentView.Group, 0f, 1f, Theme.FastTransition));
        }

        private void OnStepComplete(StepResult result)
        {
            var step = _currentStep;

            // The phone only stays with a player while consecutive steps are theirs; anything
            // public hands it back to the table and re-arms the gate.
            _phoneHolder = step != null && step.IsPrivate ? step.ActorPlayerId : null;

            _mode.Submit(result);

            if (_mode.IsRoundComplete)
            {
                FinishRound();
                return;
            }

            ShowCurrent();
        }

        private void FinishRound()
        {
            ClearStage();
            _currentStep = null;
            App.SetPrivateInformationVisible(false);

            var summary = _mode.CompleteRound();
            if (summary != null && summary.GameOver)
            {
                App.ShowResults(_session, summary);
                return;
            }

            _session.AdvanceRound();
            StartRound();
        }

        private void ClearStage()
        {
            if (_currentView != null) Destroy(_currentView.gameObject);
            _currentView = null;
            if (_stage == null) return;
            foreach (Transform child in _stage) Destroy(child.gameObject);
        }

        public override bool HandleBack()
        {
            OnBackPressed();
            return true;
        }

        protected override void OnBackPressed()
        {
            // Leaving mid-game throws the round away, so it takes two presses.
            if (Time.unscaledTime <= _quitArmedUntil)
            {
                App.EndSession();
                App.GoHome();
                return;
            }

            _quitArmedUntil = Time.unscaledTime + 3f;
            App.Toast("Press back again to leave this game.");
        }

        public override void OnHidden()
        {
            App.SetPrivateInformationVisible(false);
        }
    }
}
