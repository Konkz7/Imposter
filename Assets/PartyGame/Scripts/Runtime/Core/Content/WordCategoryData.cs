using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    /// <summary>
    /// Two words that are close enough for an imposter to bluff with, but far enough apart that
    /// careful questioning can expose the difference. That tension is the whole game, so a pair
    /// where the words are unrelated is worse content than one where they are near-synonyms.
    /// </summary>
    [Serializable]
    public class WordPair
    {
        [SerializeField] private string crewWord = string.Empty;
        [SerializeField] private string imposterWord = string.Empty;
        [SerializeField] private string hint = string.Empty;
        [SerializeField] private Difficulty difficulty = Difficulty.Medium;

        public string CrewWord => crewWord;
        public string ImposterWord => imposterWord;

        /// <summary>Optional nudge shown when the imposter hint setting asks for one.</summary>
        public string Hint => hint;

        /// <summary>
        /// How hard the pair is to tell apart in conversation, not how obscure the words are.
        /// Easy pairs differ obviously once described; hard pairs survive several rounds.
        /// </summary>
        public Difficulty Difficulty => difficulty;

        public WordPair() { }

        public WordPair(string crewWord, string imposterWord, string hint, Difficulty difficulty = Difficulty.Medium)
        {
            this.crewWord = crewWord;
            this.imposterWord = imposterWord;
            this.hint = hint;
            this.difficulty = difficulty;
        }

        public string PairId => crewWord + "|" + imposterWord;
        public bool IsValid => !string.IsNullOrWhiteSpace(crewWord) && !string.IsNullOrWhiteSpace(imposterWord);
    }

    [CreateAssetMenu(menuName = "Party Game/Content/Word Category", fileName = "WordCategory")]
    public class WordCategoryData : ContentPack
    {
        [SerializeField] private List<WordPair> pairs = new List<WordPair>();

        public IReadOnlyList<WordPair> Pairs => pairs;
        public override int EntryCount => pairs.Count;

        public void SetPairs(IEnumerable<WordPair> newPairs)
        {
            pairs = new List<WordPair>(newPairs);
        }
    }
}
