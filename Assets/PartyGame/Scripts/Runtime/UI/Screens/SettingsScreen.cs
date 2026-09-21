using System.Collections.Generic;
using PartyGame.Core.Modes;
using PartyGame.Core.Monetisation;
using PartyGame.Core.Persistence;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Screens
{
    /// <summary>App preferences. Everything here is persisted immediately.</summary>
    public class SettingsScreen : ScreenBase
    {
        private UiButton _removeAdsButton;
        private TextMeshProUGUI _adStatus;

        public override string Title => "Settings";

        protected override void Build()
        {
            var content = CreateScrollBody();
            var settings = App.Services.Settings;

            SectionTitle(content, "Sound and feel");

            ToggleSwitch.Create(content, "Sound effects", "Taps, reveals and countdowns",
                settings.Settings.SoundEffects,
                value => settings.Apply(s => s.SoundEffects = value));

            ToggleSwitch.Create(content, "Music", "Background music, when a track is loaded",
                settings.Settings.Music,
                value => settings.Apply(s => s.Music = value));

            ToggleSwitch.Create(content, "Vibration", "A short buzz on reveals and votes",
                settings.Settings.Vibration,
                value => settings.Apply(s => s.Vibration = value));

            SectionTitle(content, "Accessibility");

            OptionPicker.Create(content, "Animation", "Reduce or switch off screen movement",
                new List<SettingOption>
                {
                    new SettingOption(((int)AnimationSpeed.Normal).ToString(), "Normal", "Standard transitions"),
                    new SettingOption(((int)AnimationSpeed.Relaxed).ToString(), "Relaxed", "Slower, gentler transitions"),
                    new SettingOption(((int)AnimationSpeed.Fast).ToString(), "Fast", "Snappier transitions"),
                    new SettingOption(((int)AnimationSpeed.Off).ToString(), "Off", "No animation at all")
                },
                ((int)settings.Settings.Animation).ToString(),
                value =>
                {
                    if (int.TryParse(value, out var parsed))
                        settings.Apply(s => s.Animation = (AnimationSpeed)parsed);
                });

            ToggleSwitch.Create(content, "Hold to reveal secrets",
                "On: press and hold the card. Off: a single tap reveals it",
                settings.Settings.HoldToReveal,
                value => settings.Apply(s => s.HoldToReveal = value));

            SectionTitle(content, "Advertising");

            var card = UIFactory.CreatePaddedCard(content, "RemoveAds", Theme.Surface);
            var title = UIFactory.CreateText(card, "Remove ads", Theme.FontSubheading, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Title");
            UIFactory.FitHeight(title.gameObject);

            _adStatus = UIFactory.CreateText(card, string.Empty, Theme.FontLabel, Theme.TextSecondary,
                TextAlignmentOptions.TopLeft, FontStyles.Normal, "Status");
            UIFactory.FitHeight(_adStatus.gameObject);

            _removeAdsButton = UiButton.Create(card, "Remove ads", ButtonStyle.Primary, BuyRemoveAds,
                Theme.CompactButtonHeight);
            UiButton.Create(card, "Restore purchases", ButtonStyle.Ghost, RestorePurchases, 86f);

            RefreshAdSection();

            SectionTitle(content, "Players");

            UiButton.Create(content, "Clear saved players", ButtonStyle.Ghost, ClearPlayers, Theme.CompactButtonHeight);

            UIFactory.Spacer(content, Theme.SpaceL);
        }

        private void SectionTitle(Transform parent, string text)
        {
            var label = UIFactory.CreateText(parent, text.ToUpperInvariant(), Theme.FontCaption, Theme.TextMuted,
                TextAlignmentOptions.Left, FontStyles.Bold, "Section");
            label.characterSpacing = 6f;
            UIFactory.SetSize(label.gameObject, 60f, 60f);
        }

        private void RefreshAdSection()
        {
            var owned = App.Services.Entitlements.Has(Entitlement.AdFree);
            var storeAvailable = App.Services.Store_Purchases.StoreAvailable;

            if (_adStatus != null)
            {
                _adStatus.text = owned
                    ? "You already own this. Thank you."
                    : storeAvailable
                        ? "One payment removes every advert, forever."
                        : "The store is not connected in this build, so there is nothing to buy yet. " +
                          "No adverts are shown either.";
            }

            if (_removeAdsButton != null)
            {
                _removeAdsButton.Interactable = !owned && storeAvailable;
                _removeAdsButton.SetLabel(owned ? "Ads removed" : "Remove ads");
                _removeAdsButton.SetStyle(owned ? ButtonStyle.Success : ButtonStyle.Primary);
            }
        }

        private void BuyRemoveAds()
        {
            App.Services.Store_Purchases.Buy(StoreProducts.RemoveAds, result =>
            {
                App.Toast(result.Succeeded ? "Ads removed. Thank you." : result.Message, !result.Succeeded);
                RefreshAdSection();
            });
        }

        private void RestorePurchases()
        {
            App.Services.Store_Purchases.Restore(success =>
            {
                App.Toast(success ? "Purchases restored." : "Nothing to restore.", !success);
                RefreshAdSection();
            });
        }

        private void ClearPlayers()
        {
            App.Services.Roster.Clear();
            App.Services.Roster.EnsureMinimum(4);
            App.Services.PersistPlayers();
            App.Toast("Player list reset.", false);
        }
    }

    /// <summary>Version, credits and a short note about the content.</summary>
    public class AboutScreen : ScreenBase
    {
        public override string Title => "About";

        protected override void Build()
        {
            var content = CreateScrollBody();

            var card = UIFactory.CreatePaddedCard(content, "About", Theme.Surface);

            var heading = UIFactory.CreateText(card, "Odd One Out", Theme.FontTitle, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Heading");
            UIFactory.FitHeight(heading.gameObject);

            var version = UIFactory.CreateText(card, "Version " + Application.version, Theme.FontCaption,
                Theme.TextMuted, TextAlignmentOptions.Left, FontStyles.Normal, "Version");
            UIFactory.FitHeight(version.gameObject);

            var body = UIFactory.CreateText(card,
                "A collection of social deduction and bluffing games designed for people in the same room, " +
                "sharing one phone. Everything runs offline: no accounts, no network, and nothing about a " +
                "round is ever saved.",
                Theme.FontBody, Theme.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Normal, "Body");
            UIFactory.FitHeight(body.gameObject);

            var contentCard = UIFactory.CreatePaddedCard(content, "Content", Theme.Surface);
            var contentTitle = UIFactory.CreateText(contentCard, "About the questions", Theme.FontSubheading,
                Theme.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold, "Title");
            UIFactory.FitHeight(contentTitle.gameObject);

            var contentBody = UIFactory.CreateText(contentCard,
                "Debate topics are kept to everyday opinions on purpose - food, habits, films and weather - " +
                "rather than anything personal or political. Packs can be switched on and off per game in the " +
                "setup screen.",
                Theme.FontLabel, Theme.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Normal, "Body");
            UIFactory.FitHeight(contentBody.gameObject);

            UIFactory.Spacer(content, Theme.SpaceL);
        }
    }
}
