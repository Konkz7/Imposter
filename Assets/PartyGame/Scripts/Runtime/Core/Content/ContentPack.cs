using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    /// <summary>
    /// Base for every authored content asset. Adding content means creating or editing an
    /// asset, never editing gameplay code.
    /// </summary>
    public abstract class ContentPack : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(1, 3)] private string description = string.Empty;
        [SerializeField] private string glyph = "*";
        [SerializeField, Range(0, 7)] private int accentIndex;
        [SerializeField] private bool enabled = true;
        [SerializeField] private bool premium;
        [SerializeField] private Freshness freshness = Freshness.Evergreen;
        [SerializeField] private string reviewBy = string.Empty;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public string Glyph => string.IsNullOrEmpty(glyph) ? "*" : glyph;
        public int AccentIndex => accentIndex;

        /// <summary>Disabled packs are hidden everywhere, which is how content gets retired safely.</summary>
        public bool Enabled => enabled;

        /// <summary>Reserved for a future paid category pack. Never gates anything today.</summary>
        public bool Premium => premium;

        /// <summary>
        /// Whether this pack is tied to a moment in time. Current packs carry a review date so
        /// they cannot quietly turn into permanent content once they are out of date.
        /// </summary>
        public Freshness Freshness => freshness;

        public string ReviewBy => reviewBy;

        public bool NeedsReview(System.DateTime today)
        {
            return System.DateTime.TryParse(reviewBy, out var due) && today.Date > due.Date;
        }

        /// <summary>Number of usable entries. Used to hide empty packs from the pickers.</summary>
        public abstract int EntryCount { get; }

        public void Configure(string newId, string newDisplayName, string newDescription, string newGlyph,
            int newAccent, Freshness newFreshness = Freshness.Evergreen, string newReviewBy = "")
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            glyph = newGlyph;
            accentIndex = newAccent;
            enabled = true;
            freshness = newFreshness;
            reviewBy = newReviewBy;
        }
    }
}
