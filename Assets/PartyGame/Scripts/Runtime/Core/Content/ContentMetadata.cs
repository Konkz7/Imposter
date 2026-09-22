using System;
using UnityEngine;

namespace PartyGame.Core.Content
{
    /// <summary>
    /// How long an entry stays true.
    ///
    /// Evergreen content - history, science, animals, geography - can sit in the game forever.
    /// Current content is tied to a moment and has to be rechecked, which is what
    /// <see cref="ContentSource.ReviewBy"/> is for: it stops "current" quietly becoming permanent.
    /// </summary>
    public enum Freshness
    {
        Evergreen = 0,
        Current = 1
    }

    public enum Difficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    /// <summary>
    /// Where a factual claim came from and when it was last checked.
    ///
    /// Only the underlying fact is taken from the source - every question is written from
    /// scratch - so this exists to make a claim re-checkable, not to reproduce anything.
    /// </summary>
    [Serializable]
    public class ContentSource
    {
        [SerializeField] private string source = string.Empty;
        [SerializeField] private string sourceUrl = string.Empty;
        [SerializeField] private string verifiedDate = string.Empty;
        [SerializeField] private string reviewBy = string.Empty;

        /// <summary>Human readable origin, e.g. "Wikipedia" or "Guinness World Records".</summary>
        public string Source => source;

        public string SourceUrl => sourceUrl;

        /// <summary>ISO date the claim was last checked, e.g. 2026-09-22.</summary>
        public string VerifiedDate => verifiedDate;

        /// <summary>ISO date after which this needs re-checking. Empty means evergreen.</summary>
        public string ReviewBy => reviewBy;

        public bool HasSource => !string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(sourceUrl);

        public ContentSource() { }

        public ContentSource(string source, string sourceUrl, string verifiedDate, string reviewBy = "")
        {
            this.source = source;
            this.sourceUrl = sourceUrl;
            this.verifiedDate = verifiedDate;
            this.reviewBy = reviewBy;
        }

        /// <summary>True once the review date has passed, so tooling can flag it.</summary>
        public bool NeedsReview(DateTime today)
        {
            return DateTime.TryParse(reviewBy, out var due) && today.Date > due.Date;
        }
    }
}
