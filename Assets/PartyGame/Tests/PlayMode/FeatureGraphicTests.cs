using System.Collections;
using NUnit.Framework;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PartyGame.Tests.PlayMode
{
    /// <summary>
    /// Renders Google Play's 1024x500 feature graphic.
    ///
    /// It is built from the app's own theme and fonts rather than drawn in an image editor, so
    /// it cannot drift away from what the app actually looks like, and a colour change in Theme
    /// regenerates it. Play rejects anything that is not exactly 1024x500, which is why the
    /// capture session is opened at that size instead of the phone size the other tests use.
    /// </summary>
    public class FeatureGraphicTests
    {
        private AppController _app;

        [SetUp]
        public void SetUp()
        {
            _app = AppController.Bootstrap();
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
        public IEnumerator CaptureFeatureGraphic()
        {
            ScreenshotCapture.BeginSession(_app.UI.Canvas, 1024, 500);

            // The app's screens are portrait by construction, so the graphic gets its own
            // layout built directly on the canvas instead.
            foreach (Transform child in _app.UI.Canvas.transform)
                child.gameObject.SetActive(false);

            var root = UIFactory.CreateRect("Feature Graphic", _app.UI.Canvas.transform);
            UIFactory.Stretch(root);

            var background = root.gameObject.AddComponent<Image>();
            background.sprite = UIGraphics.VerticalGradient(Theme.BackgroundTop, Theme.Background);
            background.type = Image.Type.Sliced;
            background.color = Color.white;

            BuildMark(root);
            BuildWords(root);

            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return null;

            ScreenshotCapture.Shot("feature-graphic-1024x500");

            var written = System.IO.Path.Combine(ScreenshotCapture.OutputFolder,
                "feature-graphic-1024x500.png");
            Assert.IsTrue(System.IO.File.Exists(written), "The feature graphic was not written.");
        }

        /// <summary>The icon's three dots, oversized and bled off the left edge.</summary>
        private static void BuildMark(RectTransform parent)
        {
            var colours = new[] { Theme.Primary, Theme.Primary, Theme.Secondary };
            for (var i = 0; i < 3; i++)
            {
                var dot = UIFactory.CreateRect("Dot" + i, parent);
                var image = dot.gameObject.AddComponent<Image>();
                image.sprite = UIGraphics.Circle(128);
                image.color = i == 2 ? colours[i] : Theme.WithAlpha(colours[i], 0.9f);

                var size = i == 2 ? 96f : 82f;
                dot.anchorMin = dot.anchorMax = new Vector2(0f, 0.5f);
                dot.pivot = new Vector2(0.5f, 0.5f);
                dot.sizeDelta = new Vector2(size, size);
                dot.anchoredPosition = new Vector2(96f + i * 94f, i == 2 ? -4f : 0f);
            }
        }

        private static void BuildWords(RectTransform parent)
        {
            var column = UIFactory.CreateRect("Words", parent);
            column.anchorMin = new Vector2(0f, 0f);
            column.anchorMax = new Vector2(1f, 1f);
            column.offsetMin = new Vector2(362f, 80f);
            column.offsetMax = new Vector2(-56f, -80f);

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Every line is sized to fit the column on one row except the tagline, which is
            // allowed two. Wrapping a title mid-phrase is what made the first attempt unusable.
            var title = UIFactory.CreateText(column, "ODD ONE OUT", 68f, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Title");
            title.characterSpacing = 2f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.SetSize(title.gameObject, 82f, 82f);

            var tagline = UIFactory.CreateText(column,
                "Five party games for one phone." + "\n" + "Somebody is lying.",
                30f, Theme.TextSecondary, TextAlignmentOptions.Left, FontStyles.Normal, "Tagline");
            UIFactory.SetSize(tagline.gameObject, 84f, 84f);

            var offline = UIFactory.CreateText(column, "NO INTERNET   ·   NO ACCOUNTS   ·   3-12 PLAYERS",
                21f, Theme.TextMuted, TextAlignmentOptions.Left, FontStyles.Bold, "Offline");
            offline.characterSpacing = 3f;
            offline.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.SetSize(offline.gameObject, 30f, 30f);
        }
    }
}
