using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    [Serializable]
    public class WavelengthQuestion
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(1, 3)] private string prompt = string.Empty;
        [SerializeField] private string lowLabel = "terrible";
        [SerializeField] private string highLabel = "amazing";
        [SerializeField] private Difficulty difficulty = Difficulty.Medium;
        [SerializeField] private List<string> tags = new List<string>();

        public string Id => string.IsNullOrEmpty(id) ? prompt : id;
        public string Prompt => prompt;

        /// <summary>What a 1 means on this scale.</summary>
        public string LowLabel => lowLabel;

        /// <summary>What a 10 means on this scale.</summary>
        public string HighLabel => highLabel;

        public Difficulty Difficulty => difficulty;
        public IReadOnlyList<string> Tags => tags;

        public bool IsValid => !string.IsNullOrWhiteSpace(prompt)
                               && !string.IsNullOrWhiteSpace(lowLabel)
                               && !string.IsNullOrWhiteSpace(highLabel);

        public WavelengthQuestion() { }

        public WavelengthQuestion(string id, string prompt, string lowLabel, string highLabel,
            Difficulty difficulty = Difficulty.Medium, IEnumerable<string> tags = null)
        {
            this.id = id;
            this.prompt = prompt;
            this.lowLabel = lowLabel;
            this.highLabel = highLabel;
            this.difficulty = difficulty;
            this.tags = tags != null ? new List<string>(tags) : new List<string>();
        }
    }

    [CreateAssetMenu(menuName = "Party Game/Content/Wavelength Pack", fileName = "WavelengthPack")]
    public class WavelengthPackData : ContentPack
    {
        [SerializeField] private List<WavelengthQuestion> questions = new List<WavelengthQuestion>();

        public IReadOnlyList<WavelengthQuestion> Questions => questions;
        public override int EntryCount => questions.Count;

        public void SetQuestions(IEnumerable<WavelengthQuestion> newQuestions)
        {
            questions = new List<WavelengthQuestion>(newQuestions);
        }
    }
}
