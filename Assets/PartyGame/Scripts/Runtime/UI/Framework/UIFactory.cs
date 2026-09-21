using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PartyGame.UI.Design;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Builds every UI element in the app from the design system. Screens describe what they
    /// need; this decides how it looks, so restyling the app happens in one place.
    /// </summary>
    public static class UIFactory
    {
        private static TMP_FontAsset _font;

        /// <summary>
        /// The project font. Falls back to a runtime-generated asset from the built-in font if
        /// the TextMeshPro essentials have not been imported, so text is never invisible.
        /// </summary>
        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_font != null) return _font;

                try
                {
                    _font = TMP_Settings.defaultFontAsset;
                }
                catch (System.Exception)
                {
                    _font = null;
                }

                if (_font == null)
                {
                    var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (builtin == null) builtin = Font.CreateDynamicFontFromOSFont("Arial", 48);
                    if (builtin != null) _font = TMP_FontAsset.CreateFontAsset(builtin);
                }

                return _font;
            }
        }

        // ----------------------------------------------------------------- containers

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        public static RectTransform CreateBox(string name, Transform parent, float height)
        {
            var rect = CreateRect(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Color colour, int radius = Theme.RadiusMedium)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIGraphics.RoundedRect(radius);
            image.type = Image.Type.Sliced;
            image.color = colour;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        /// <summary>
        /// A decorative border drawn over its parent. It opts out of layout so that adding a
        /// layout group to the parent later cannot turn the border into a content row.
        /// </summary>
        public static Image CreateOutline(string name, Transform parent, Color colour, int radius, int thickness = 3)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIGraphics.RoundedOutline(radius, thickness);
            image.type = Image.Type.Sliced;
            image.color = colour;
            image.raycastTarget = false;
            image.pixelsPerUnitMultiplier = 1f;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return image;
        }

        /// <summary>Card surface with a subtle outline. The base of most composite widgets.</summary>
        public static RectTransform CreateCard(string name, Transform parent, Color? fill = null,
            int radius = Theme.RadiusLarge)
        {
            var panel = CreatePanel(name, parent, fill ?? Theme.SurfaceRaised, radius);
            var outline = CreateOutline("Outline", panel.transform, Theme.WithAlpha(Theme.Outline, 0.85f), radius);
            Stretch(outline.rectTransform);
            return panel.rectTransform;
        }

        /// <summary>
        /// Card that grows to fit whatever is added to it. The workhorse for text blocks,
        /// summaries and reveal panels.
        /// </summary>
        public static RectTransform CreatePaddedCard(Transform parent, string name, Color? fill = null,
            float padding = Theme.SpaceM, float spacing = Theme.SpaceXs, int radius = Theme.RadiusLarge)
        {
            var card = CreateCard(name, parent, fill, radius);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            var pad = (int)padding;
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            FitHeight(card.gameObject);
            return card;
        }

        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        // ----------------------------------------------------------------- layout

        public static VerticalLayoutGroup VerticalGroup(Transform parent, string name, float spacing,
            RectOffset padding = null, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            var rect = CreateRect(name, parent);
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childAlignment = alignment;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        /// <summary>
        /// A row. Children keep their own widths and the leftover space goes to whichever child
        /// declares a flexible width - forcing equal expansion instead would stretch fixed-size
        /// badges and icons out of shape.
        /// </summary>
        public static HorizontalLayoutGroup HorizontalGroup(Transform parent, string name, float spacing,
            RectOffset padding = null, TextAnchor alignment = TextAnchor.MiddleCenter, bool evenColumns = false)
        {
            var rect = CreateRect(name, parent);
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childAlignment = alignment;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = evenColumns;
            group.childForceExpandHeight = false;
            return group;
        }

        public static LayoutElement SetSize(GameObject target, float preferredHeight = -1f, float minHeight = -1f,
            float preferredWidth = -1f, float minWidth = -1f, float flexibleHeight = -1f, float flexibleWidth = -1f)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
            if (minHeight >= 0f) element.minHeight = minHeight;
            if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
            if (minWidth >= 0f) element.minWidth = minWidth;
            if (flexibleHeight >= 0f) element.flexibleHeight = flexibleHeight;
            if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
            return element;
        }

        /// <summary>
        /// Fixed or flexible gap. Two flexible spacers with different weights are how a screen
        /// balances a block at the top against a block at the bottom.
        /// </summary>
        public static RectTransform Spacer(Transform parent, float height, bool flexible = false, float weight = 1f)
        {
            var rect = CreateRect("Spacer", parent);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            if (flexible) element.flexibleHeight = Mathf.Max(0.01f, weight);
            return rect;
        }

        public static ContentSizeFitter FitHeight(GameObject target)
        {
            var fitter = target.GetComponent<ContentSizeFitter>() ?? target.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            return fitter;
        }

        // ----------------------------------------------------------------- text

        public static TextMeshProUGUI CreateText(Transform parent, string content, float size, Color colour,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, FontStyles style = FontStyles.Normal,
            string name = "Text")
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = DefaultFont;
            text.text = content ?? string.Empty;
            text.fontSize = size;
            text.color = colour;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.lineSpacing = -4f;
            return text;
        }

        /// <summary>Text that shrinks to fit instead of overflowing. Used for long words and names.</summary>
        public static TextMeshProUGUI CreateFittedText(Transform parent, string content, float maxSize, float minSize,
            Color colour, TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Bold, string name = "Text")
        {
            var text = CreateText(parent, content, maxSize, colour, alignment, style, name);
            text.enableAutoSizing = true;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            return text;
        }

        // ----------------------------------------------------------------- scrolling

        /// <summary>Vertical scroll view whose content grows with its children.</summary>
        public static RectTransform CreateScrollView(Transform parent, string name, float spacing,
            RectOffset padding, out ScrollRect scrollRect)
        {
            var viewportRect = CreateRect(name, parent);
            scrollRect = viewportRect.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.08f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 40f;

            var viewport = CreateRect("Viewport", viewportRect);
            var mask = viewport.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            Stretch(viewport);

            var content = VerticalGroup(viewport, "Content", spacing, padding);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            FitHeight(content.gameObject);

            scrollRect.viewport = viewport;
            scrollRect.content = contentRect;
            return contentRect;
        }

        // ----------------------------------------------------------------- input

        public static TMP_InputField CreateInputField(Transform parent, string placeholder, int characterLimit)
        {
            var background = CreatePanel("InputField", parent, Theme.SurfaceSunken, Theme.RadiusMedium);
            var outline = CreateOutline("Outline", background.transform, Theme.Outline, Theme.RadiusMedium);
            Stretch(outline.rectTransform);

            var textArea = CreateRect("TextArea", background.transform);
            Stretch(textArea);
            textArea.offsetMin = new Vector2(Theme.SpaceM, Theme.SpaceXs);
            textArea.offsetMax = new Vector2(-Theme.SpaceM, -Theme.SpaceXs);
            textArea.gameObject.AddComponent<RectMask2D>();

            var placeholderText = CreateText(textArea, placeholder, Theme.FontBody, Theme.TextMuted,
                TextAlignmentOptions.Left, FontStyles.Italic, "Placeholder");
            Stretch(placeholderText.rectTransform);

            var inputText = CreateText(textArea, string.Empty, Theme.FontBody, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Normal, "Text");
            Stretch(inputText.rectTransform);
            inputText.raycastTarget = true;

            var field = background.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = textArea;
            field.textComponent = inputText;
            field.placeholder = placeholderText;
            field.characterLimit = characterLimit;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.customCaretColor = true;
            field.caretColor = Theme.Primary;
            field.caretWidth = 4;
            field.selectionColor = Theme.WithAlpha(Theme.Primary, 0.35f);
            field.targetGraphic = background;
            field.transition = Selectable.Transition.None;
            field.richText = false;
            return field;
        }
    }
}
