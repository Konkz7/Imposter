using System;
using PartyGame.Core.Audio;
using PartyGame.Core.Modes;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Gameplay
{
    /// <summary>
    /// Base for the view of one <see cref="GameStep"/>. Each view knows how to draw one kind
    /// of step and nothing about any particular game, which is why five games share one screen.
    /// </summary>
    public abstract class StepView : MonoBehaviour
    {
        protected GameStep Step { get; private set; }
        protected Action<StepResult> Complete { get; private set; }

        /// <summary>Content area between the header and the action button.</summary>
        protected RectTransform Content { get; private set; }

        protected RectTransform Actions { get; private set; }

        /// <summary>Used by the gameplay screen to cross-fade between steps.</summary>
        public CanvasGroup Group { get; private set; }

        private TextMeshProUGUI _phaseLabel;
        private TextMeshProUGUI _progressLabel;

        /// <summary>True while genuinely secret information is on screen.</summary>
        public virtual bool IsShowingSecret => false;

        public static T Spawn<T>(Transform parent, GameStep step, Action<StepResult> onComplete,
            Action<T> configure = null) where T : StepView
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            UIFactory.Stretch((RectTransform)go.transform);
            var view = go.AddComponent<T>();
            configure?.Invoke(view);
            view.Initialise(step, onComplete);
            return view;
        }

        private void Initialise(GameStep step, Action<StepResult> onComplete)
        {
            Step = step;
            Complete = onComplete;
            Group = gameObject.AddComponent<CanvasGroup>();

            var column = UIFactory.VerticalGroup(transform, "Column", Theme.SpaceS);
            UIFactory.Stretch((RectTransform)column.transform);
            column.childForceExpandHeight = false;

            var meta = UIFactory.HorizontalGroup(column.transform, "Meta", Theme.SpaceXs, null, TextAnchor.MiddleLeft);
            UIFactory.SetSize(meta.gameObject, 46f, 46f);

            // A private step's accent describes what the card underneath says - danger for an
            // imposter, primary for everyone else - so tinting the header with it would announce
            // the role before the owner has even revealed the card, and would be visible to
            // whoever is still holding the phone. Private steps get a fixed neutral colour.
            _phaseLabel = UIFactory.CreateText(meta.transform, (step.PhaseLabel ?? string.Empty).ToUpperInvariant(),
                Theme.FontCaption, PhaseLabelColour(step),
                TextAlignmentOptions.Left, FontStyles.Bold, "Phase");
            _phaseLabel.characterSpacing = 6f;
            UIFactory.SetSize(_phaseLabel.gameObject, flexibleWidth: 1f);

            _progressLabel = UIFactory.CreateText(meta.transform,
                step.SequenceCount > 1 ? step.SequenceIndex + " / " + step.SequenceCount : string.Empty,
                Theme.FontCaption, Theme.TextMuted, TextAlignmentOptions.Right, FontStyles.Bold, "Progress");
            UIFactory.SetSize(_progressLabel.gameObject, preferredWidth: 160f, minWidth: 160f);

            var contentHolder = UIFactory.CreateRect("Content", column.transform);
            UIFactory.SetSize(contentHolder.gameObject, flexibleHeight: 1f, minHeight: 200f);
            Content = contentHolder;

            var actions = UIFactory.VerticalGroup(column.transform, "Actions", Theme.SpaceXs);
            UIFactory.FitHeight(actions.gameObject);
            Actions = (RectTransform)actions.transform;

            Build();
        }

        /// <summary>
        /// Nothing visible before a secret is revealed may vary with that secret. Public steps
        /// can carry their accent into the header; private ones must look identical for every
        /// player, whatever their card turns out to say.
        /// </summary>
        private static Color PhaseLabelColour(GameStep step)
        {
            if (step.IsPrivate) return Theme.TextSecondary;
            return Theme.ForAccent(step.Accent == StepAccent.Neutral ? StepAccent.Primary : step.Accent);
        }

        protected abstract void Build();

        /// <summary>Standard title + body block used by most views.</summary>
        protected RectTransform BuildHeadingBlock(Transform parent, string title, string body, string feature = null,
            StepAccent accent = StepAccent.Neutral)
        {
            var column = UIFactory.VerticalGroup(parent, "Heading", Theme.SpaceS, null, TextAnchor.UpperCenter);
            UIFactory.FitHeight(column.gameObject);

            if (!string.IsNullOrEmpty(title))
            {
                var titleText = UIFactory.CreateText(column.transform, title, Theme.FontTitle, Theme.TextPrimary,
                    TextAlignmentOptions.Center, FontStyles.Bold, "Title");
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = Theme.FontSubheading;
                titleText.fontSizeMax = Theme.FontTitle;
                UIFactory.FitHeight(titleText.gameObject);
            }

            if (!string.IsNullOrEmpty(feature))
            {
                var card = UIFactory.CreatePaddedCard(column.transform, "Feature",
                    Theme.WithAlpha(Theme.ForAccent(accent == StepAccent.Neutral ? StepAccent.Primary : accent), 0.16f),
                    Theme.SpaceM);
                var featureText = UIFactory.CreateText(card, feature, Theme.FontHeading, Theme.TextPrimary,
                    TextAlignmentOptions.Center, FontStyles.Bold, "FeatureText");
                featureText.enableAutoSizing = true;
                featureText.fontSizeMin = Theme.FontBody;
                featureText.fontSizeMax = Theme.FontHeading;
                UIFactory.FitHeight(featureText.gameObject);
            }

            if (!string.IsNullOrEmpty(body))
            {
                var bodyText = UIFactory.CreateText(column.transform, body, Theme.FontBody, Theme.TextSecondary,
                    TextAlignmentOptions.Center, FontStyles.Normal, "Body");
                UIFactory.FitHeight(bodyText.gameObject);
            }

            return (RectTransform)column.transform;
        }

        protected UiButton BuildPrimaryAction(string label, Action onClick, ButtonStyle style = ButtonStyle.Primary)
        {
            return UiButton.Create(Actions, label, style, onClick);
        }

        protected void Finish(StepResult result)
        {
            var complete = Complete;
            Complete = null;
            complete?.Invoke(result ?? StepResult.Acknowledged(Step));
        }

        protected void FinishAcknowledged()
        {
            UiFeedback.Sound(SoundId.Confirm);
            Finish(StepResult.Acknowledged(Step));
        }
    }

    /// <summary>
    /// The pass-the-phone gate. Shown before any private step so the previous player can hand
    /// the device over with nothing sensitive on screen.
    /// </summary>
    public class HandoffView : StepView
    {
        private string _playerName = "the next player";

        public void SetPlayerName(string name)
        {
            _playerName = string.IsNullOrEmpty(name) ? "the next player" : name;
        }

        protected override void Build()
        {
            var column = UIFactory.VerticalGroup(Content, "Handoff", Theme.SpaceM, null, TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)column.transform);

            var label = UIFactory.CreateText(column.transform, "PASS THE PHONE TO", Theme.FontLabel,
                Theme.TextMuted, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
            label.characterSpacing = 8f;
            UIFactory.FitHeight(label.gameObject);

            var name = UIFactory.CreateFittedText(column.transform, _playerName, Theme.FontDisplay, Theme.FontHeading,
                Theme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold, "Name");
            UIFactory.SetSize(name.gameObject, 220f, 220f);

            var note = UIFactory.CreateText(column.transform,
                "Nobody else should look at the screen from here.",
                Theme.FontLabel, Theme.TextSecondary, TextAlignmentOptions.Center, FontStyles.Normal, "Note");
            UIFactory.FitHeight(note.gameObject);

            BuildPrimaryAction("I am " + _playerName, () =>
            {
                UiFeedback.Haptic();
                FinishAcknowledged();
            });
        }
    }
}
