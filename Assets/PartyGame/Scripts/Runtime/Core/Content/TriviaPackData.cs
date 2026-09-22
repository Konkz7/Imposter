using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    /// <summary>
    /// A question with one true answer that players will try to bury under invented ones.
    ///
    /// The bar for a good entry is not "is this true" but "can somebody invent a lie that
    /// sounds just as plausible". A question whose answer is obvious makes bluffing pointless.
    /// </summary>
    [Serializable]
    public class TriviaQuestion
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(1, 3)] private string question = string.Empty;
        [SerializeField] private string correctAnswer = string.Empty;
        [SerializeField] private Difficulty difficulty = Difficulty.Medium;
        [SerializeField] private string subcategory = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private ContentSource sourceInfo = new ContentSource();

        public string Id => string.IsNullOrEmpty(id) ? question : id;
        public string Question => question;
        public string CorrectAnswer => correctAnswer;
        public Difficulty Difficulty => difficulty;
        public string Subcategory => subcategory;
        public IReadOnlyList<string> Tags => tags;
        public ContentSource SourceInfo => sourceInfo;

        public bool IsValid => !string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(correctAnswer);

        public TriviaQuestion() { }

        public TriviaQuestion(string id, string question, string correctAnswer,
            Difficulty difficulty = Difficulty.Medium, string subcategory = "",
            IEnumerable<string> tags = null, ContentSource source = null)
        {
            this.id = id;
            this.question = question;
            this.correctAnswer = correctAnswer;
            this.difficulty = difficulty;
            this.subcategory = subcategory;
            this.tags = tags != null ? new List<string>(tags) : new List<string>();
            sourceInfo = source ?? new ContentSource();
        }
    }

    [CreateAssetMenu(menuName = "Party Game/Content/Trivia Pack", fileName = "TriviaPack")]
    public class TriviaPackData : ContentPack
    {
        [SerializeField] private List<TriviaQuestion> questions = new List<TriviaQuestion>();

        public IReadOnlyList<TriviaQuestion> Questions => questions;
        public override int EntryCount => questions.Count;

        public void SetQuestions(IEnumerable<TriviaQuestion> newQuestions)
        {
            questions = new List<TriviaQuestion>(newQuestions);
        }
    }
}
