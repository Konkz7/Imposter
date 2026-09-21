using PartyGame.Core.Modes;
using UnityEngine;

namespace PartyGame.UI.Design
{
    /// <summary>
    /// The whole visual language in one asset: colours, type scale, spacing and radii.
    /// Nothing else in the project declares a colour or a font size.
    /// </summary>
    public static class Theme
    {
        // ------------------------------------------------------------------ colours
        public static readonly Color Background = Hex("#12101C");
        public static readonly Color BackgroundTop = Hex("#1B1430");
        public static readonly Color Surface = Hex("#1E1B2E");
        public static readonly Color SurfaceRaised = Hex("#2A2540");
        public static readonly Color SurfaceSunken = Hex("#171426");
        public static readonly Color Outline = Hex("#3A3358");

        public static readonly Color Primary = Hex("#7C5CFF");
        public static readonly Color PrimaryDeep = Hex("#5B3FD9");
        public static readonly Color Secondary = Hex("#FF4D8D");
        public static readonly Color Success = Hex("#2ED47A");
        public static readonly Color Warning = Hex("#FFB020");
        public static readonly Color Danger = Hex("#FF5A5F");

        public static readonly Color TextPrimary = Hex("#F6F4FF");
        public static readonly Color TextSecondary = Hex("#B4AED2");
        public static readonly Color TextMuted = Hex("#7E7799");
        public static readonly Color TextOnAccent = Hex("#12101C");

        /// <summary>Card accents, referenced by index from content assets.</summary>
        public static readonly Color[] Accents =
        {
            Hex("#7C5CFF"),
            Hex("#FF4D8D"),
            Hex("#2ED47A"),
            Hex("#FFB020"),
            Hex("#38BDF8"),
            Hex("#F472B6"),
            Hex("#A78BFA"),
            Hex("#34D399")
        };

        public static Color Accent(int index)
        {
            if (Accents.Length == 0) return Primary;
            var i = ((index % Accents.Length) + Accents.Length) % Accents.Length;
            return Accents[i];
        }

        public static Color ForAccent(StepAccent accent)
        {
            switch (accent)
            {
                case StepAccent.Primary: return Primary;
                case StepAccent.Danger: return Danger;
                case StepAccent.Success: return Success;
                case StepAccent.Warning: return Warning;
                default: return Outline;
            }
        }

        /// <summary>Readable foreground for a filled surface of the given colour.</summary>
        public static Color OnColour(Color background)
        {
            var luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance > 0.62f ? TextOnAccent : TextPrimary;
        }

        public static Color WithAlpha(Color colour, float alpha)
        {
            colour.a = alpha;
            return colour;
        }

        // ------------------------------------------------------------------ type scale
        // Sizes are in reference pixels for a 1080 x 1920 canvas.
        public const float FontDisplay = 96f;
        public const float FontTitle = 62f;
        public const float FontHeading = 46f;
        public const float FontSubheading = 38f;
        public const float FontBody = 34f;
        public const float FontLabel = 28f;
        public const float FontCaption = 24f;

        // ------------------------------------------------------------------ spacing
        public const float SpaceXs = 8f;
        public const float SpaceS = 16f;
        public const float SpaceM = 24f;
        public const float SpaceL = 36f;
        public const float SpaceXl = 56f;

        public const float ScreenPadding = 44f;

        // ------------------------------------------------------------------ shape
        public const int RadiusSmall = 16;
        public const int RadiusMedium = 28;
        public const int RadiusLarge = 40;
        public const int RadiusPill = 64;

        /// <summary>Minimum comfortable touch target on a phone.</summary>
        public const float TouchTarget = 132f;
        public const float ButtonHeight = 140f;
        public const float CompactButtonHeight = 104f;

        // ------------------------------------------------------------------ motion
        public const float FastTransition = 0.16f;
        public const float NormalTransition = 0.26f;
        public const float SlowTransition = 0.4f;

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var colour) ? colour : Color.magenta;
        }
    }
}
