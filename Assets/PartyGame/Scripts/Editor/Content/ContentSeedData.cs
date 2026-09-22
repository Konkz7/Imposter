using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;

namespace PartyGame.EditorTools
{
    /// <summary>Shape of one authored pack before it becomes a ScriptableObject.</summary>
    public class PackSeed<T>
    {
        public string Id;
        public string Name;
        public string Description;
        public string Glyph;
        public int Accent;
        public Freshness Freshness;
        public string ReviewBy;
        public List<T> Entries;

        public PackSeed(string id, string name, string description, string glyph, int accent,
            IEnumerable<T> entries, Freshness freshness = Freshness.Evergreen, string reviewBy = "")
        {
            Id = id;
            Name = name;
            Description = description;
            Glyph = glyph;
            Accent = accent;
            Freshness = freshness;
            ReviewBy = reviewBy;
            Entries = new List<T>(entries);
        }
    }

    /// <summary>
    /// The authored content library, kept as plain data so the whole thing can be regenerated
    /// into assets in one pass. After generation the .asset files are the source of truth and
    /// can be edited in the inspector like any other content.
    ///
    /// Split across partial files by content type; the lists below are only the assembly point.
    /// </summary>
    public static partial class ContentSeedData
    {
        /// <summary>Date the current-affairs pack was last checked against live sources.</summary>
        public const string VerifiedOn = "2026-09-22";

        public static List<PackSeed<WordPair>> WordCategories => BuildWordCategories();
        public static List<PackSeed<TriviaQuestion>> TriviaPacks => BuildTriviaPacks();
        public static List<PackSeed<WavelengthQuestion>> WavelengthPacks => BuildWavelengthPacks();
        public static List<PackSeed<DebateStatement>> DebatePacks => BuildDebatePacks();
        public static List<PackSeed<ClueTemplate>> CluePacks => BuildCluePacks();

        // ---------------------------------------------------------------- authoring shorthands
        // Terse by design: the value of this file is in the breadth of the data, and long
        // constructor calls would bury it.

        private static WordPair E(string crew, string imposter, string hint = "")
        {
            return new WordPair(crew, imposter, hint, Difficulty.Easy);
        }

        private static WordPair M(string crew, string imposter, string hint = "")
        {
            return new WordPair(crew, imposter, hint, Difficulty.Medium);
        }

        private static WordPair H(string crew, string imposter, string hint = "")
        {
            return new WordPair(crew, imposter, hint, Difficulty.Hard);
        }

        private static TriviaQuestion Q(string id, string question, string answer, Difficulty difficulty,
            string subcategory, string tags, ContentSource source = null)
        {
            return new TriviaQuestion(id, question, answer, difficulty, subcategory, Split(tags), source);
        }

        private static WavelengthQuestion W(string id, string prompt, string low, string high,
            Difficulty difficulty = Difficulty.Medium, string tags = "")
        {
            return new WavelengthQuestion(id, prompt, low, high, difficulty, Split(tags));
        }

        private static DebateStatement D(string id, string statement, string subcategory, string tags = "")
        {
            return new DebateStatement(id, statement, subcategory, Split(tags));
        }

        private static ClueTemplate C(string id, ClueKind kind, string text)
        {
            return new ClueTemplate(id, kind, text);
        }

        /// <summary>
        /// A place the claim can be checked, with no verification date attached.
        ///
        /// Used for evergreen questions written from general knowledge: the link says where to
        /// confirm it, and the empty date is an honest admission that nobody has re-checked it
        /// against the source. Tooling reports how many entries are in this state.
        /// </summary>
        private static ContentSource Ref(string article)
        {
            return new ContentSource("Wikipedia", "https://en.wikipedia.org/wiki/" + article, string.Empty);
        }

        /// <summary>A claim checked against a live source on <see cref="VerifiedOn"/>.</summary>
        private static ContentSource Wiki(string article, string reviewBy = "")
        {
            return new ContentSource("Wikipedia", "https://en.wikipedia.org/wiki/" + article, VerifiedOn, reviewBy);
        }

        private static ContentSource Src(string name, string url, string reviewBy = "")
        {
            return new ContentSource(name, url, VerifiedOn, reviewBy);
        }

        private static List<string> Split(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return new List<string>();
            return tags.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        }
    }
}
