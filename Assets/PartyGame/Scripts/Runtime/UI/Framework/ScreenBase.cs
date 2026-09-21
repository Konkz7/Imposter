using PartyGame.UI.Components;
using PartyGame.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Base for every screen. Handles the canvas group, the standard header and the layout
    /// scaffold, so a screen only describes its own content.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        protected AppController App { get; private set; }

        public CanvasGroup Group { get; private set; }
        public RectTransform Root { get; private set; }

        /// <summary>Container for the screen body, already inside the screen padding.</summary>
        protected RectTransform Body { get; private set; }

        /// <summary>Pinned to the bottom of the screen, above the safe area inset.</summary>
        protected RectTransform Footer { get; private set; }

        private RectTransform _header;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private UiButton _backButton;

        /// <summary>Screen title. Override to change what the header shows.</summary>
        public virtual string Title => string.Empty;
        public virtual string Subtitle => string.Empty;
        public virtual bool ShowBackButton => true;

        /// <summary>Label on the header button. Screens that would lose work say so.</summary>
        public virtual string BackLabel => "Back";

        /// <summary>Screens that display secret information opt in, which blocks every ad.</summary>
        public virtual bool ShowsPrivateInformation => false;

        internal void Construct(AppController app)
        {
            App = app;
            Root = (RectTransform)transform;
            UIFactory.Stretch(Root);
            Group = gameObject.AddComponent<CanvasGroup>();

            BuildScaffold();
            Build();
            RefreshHeader();
        }

        private void BuildScaffold()
        {
            var column = UIFactory.VerticalGroup(transform, "Column", Theme.SpaceS,
                new RectOffset((int)Theme.ScreenPadding, (int)Theme.ScreenPadding,
                    (int)Theme.SpaceM, (int)Theme.SpaceM));
            UIFactory.Stretch((RectTransform)column.transform);
            column.childForceExpandHeight = false;

            _header = BuildHeader(column.transform);

            var bodyHolder = UIFactory.CreateRect("Body", column.transform);
            UIFactory.SetSize(bodyHolder.gameObject, flexibleHeight: 1f, minHeight: 100f);
            Body = bodyHolder;

            var footer = UIFactory.VerticalGroup(column.transform, "Footer", Theme.SpaceXs);
            UIFactory.FitHeight(footer.gameObject);
            Footer = (RectTransform)footer.transform;
        }

        private RectTransform BuildHeader(Transform parent)
        {
            var header = UIFactory.HorizontalGroup(parent, "Header", Theme.SpaceS, null, TextAnchor.MiddleLeft);
            UIFactory.SetSize(header.gameObject, 120f, 120f);

            _backButton = UiButton.Create(header.transform, BackLabel, ButtonStyle.Ghost, OnBackPressed, 96f, null, "Back");
            UIFactory.SetSize(_backButton.gameObject, 96f, 96f, 150f, 150f);

            var column = UIFactory.VerticalGroup(header.transform, "Titles", 0f, null, TextAnchor.MiddleLeft);
            UIFactory.SetSize(column.gameObject, flexibleWidth: 1f);

            _titleText = UIFactory.CreateText(column.transform, string.Empty, Theme.FontHeading, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Title");
            _titleText.enableAutoSizing = true;
            _titleText.fontSizeMin = Theme.FontBody;
            _titleText.fontSizeMax = Theme.FontHeading;
            UIFactory.FitHeight(_titleText.gameObject);

            _subtitleText = UIFactory.CreateText(column.transform, string.Empty, Theme.FontCaption,
                Theme.TextMuted, TextAlignmentOptions.Left, FontStyles.Normal, "Subtitle");
            UIFactory.FitHeight(_subtitleText.gameObject);

            return (RectTransform)header.transform;
        }

        protected void RefreshHeader()
        {
            if (_titleText != null) _titleText.text = Title ?? string.Empty;
            if (_subtitleText != null)
            {
                _subtitleText.text = Subtitle ?? string.Empty;
                _subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(Subtitle));
            }
            if (_backButton != null)
            {
                _backButton.SetLabel(BackLabel);
                _backButton.gameObject.SetActive(ShowBackButton);
            }
            if (_header != null) _header.gameObject.SetActive(ShowBackButton || !string.IsNullOrEmpty(Title));
        }

        /// <summary>Builds the screen content. Called once, right after construction.</summary>
        protected abstract void Build();

        public virtual void OnShown()
        {
        }

        public virtual void OnHidden()
        {
        }

        /// <summary>Return true to swallow the hardware back gesture.</summary>
        public virtual bool HandleBack()
        {
            return false;
        }

        protected virtual void OnBackPressed()
        {
            App.Back();
        }

        /// <summary>Convenience: a scrollable body with the standard spacing.</summary>
        protected RectTransform CreateScrollBody(float spacing = Theme.SpaceS)
        {
            var content = UIFactory.CreateScrollView(Body, "Scroll", spacing,
                new RectOffset(0, 0, (int)Theme.SpaceXs, (int)Theme.SpaceXl), out var scrollRect);
            UIFactory.Stretch((RectTransform)scrollRect.transform);
            return content;
        }

        protected UiButton AddFooterButton(string label, ButtonStyle style, System.Action onClick)
        {
            return UiButton.Create(Footer, label, style, onClick);
        }
    }
}
