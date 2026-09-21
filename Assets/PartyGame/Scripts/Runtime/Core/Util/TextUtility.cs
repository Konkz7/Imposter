using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PartyGame.Core.Util
{
    /// <summary>Shared text helpers. Kept here so answer matching behaves the same everywhere.</summary>
    public static class TextUtility
    {
        /// <summary>
        /// Loose comparison key for written answers: case, punctuation, spacing and a leading
        /// article are all ignored, so "The BackRub!" and "backrub" count as the same answer.
        /// </summary>
        public static string Normalise(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (var c in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) builder.Append(c);
                else if (char.IsWhiteSpace(c) && builder.Length > 0 && builder[builder.Length - 1] != ' ') builder.Append(' ');
            }

            var result = builder.ToString().Trim();
            foreach (var article in new[] { "the ", "a ", "an " })
            {
                if (result.StartsWith(article, StringComparison.Ordinal))
                {
                    result = result.Substring(article.Length);
                    break;
                }
            }
            return result;
        }

        public static bool Matches(string a, string b)
        {
            var na = Normalise(a);
            return na.Length > 0 && na == Normalise(b);
        }

        /// <summary>Title cases a written answer so the answer list looks consistent.</summary>
        public static string TidyAnswer(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var trimmed = value.Trim();
            while (trimmed.Contains("  ")) trimmed = trimmed.Replace("  ", " ");
            if (trimmed.Length > 1 && trimmed.ToUpperInvariant() == trimmed)
                trimmed = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
            return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        public static string Initial(string name)
        {
            var trimmed = (name ?? string.Empty).Trim();
            return trimmed.Length == 0 ? "?" : trimmed.Substring(0, 1).ToUpperInvariant();
        }

        public static string Ordinal(int number)
        {
            if (number <= 0) return number.ToString(CultureInfo.InvariantCulture);
            var lastTwo = number % 100;
            if (lastTwo >= 11 && lastTwo <= 13) return number + "th";
            switch (number % 10)
            {
                case 1: return number + "st";
                case 2: return number + "nd";
                case 3: return number + "rd";
                default: return number + "th";
            }
        }

        public static string Pluralise(int count, string singular, string plural = null)
        {
            return count + " " + (count == 1 ? singular : plural ?? singular + "s");
        }

        public static bool AnyMatch(string candidate, System.Collections.Generic.IEnumerable<string> values)
        {
            var normalised = Normalise(candidate);
            return normalised.Length > 0 && values != null && values.Any(v => Normalise(v) == normalised);
        }
    }
}
