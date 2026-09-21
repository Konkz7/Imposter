using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    [Serializable]
    public class WordPair
    {
        [SerializeField] private string crewWord = string.Empty;
        [SerializeField] private string imposterWord = string.Empty;
        [SerializeField] private string hint = string.Empty;

        public string CrewWord => crewWord;
        public string ImposterWord => imposterWord;

        /// <summary>Optional nudge shown when the imposter hint setting asks for one.</summary>
        public string Hint => hint;

        public WordPair() { }

        public WordPair(string crewWord, string imposterWord, string hint)
        {
            this.crewWord = crewWord;
            this.imposterWord = imposterWord;
            this.hint = hint;
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
