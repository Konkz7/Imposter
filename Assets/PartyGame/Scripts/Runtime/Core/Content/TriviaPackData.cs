using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    [Serializable]
    public class TriviaQuestion
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(1, 3)] private string question = string.Empty;
        [SerializeField] private string correctAnswer = string.Empty;
        [SerializeField, Range(1, 3)] private int difficulty = 1;

        public string Id => string.IsNullOrEmpty(id) ? question : id;
        public string Question => question;
        public string CorrectAnswer => correctAnswer;
        public int Difficulty => difficulty;
        public bool IsValid => !string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(correctAnswer);

        public TriviaQuestion() { }

        public TriviaQuestion(string id, string question, string correctAnswer, int difficulty)
        {
            this.id = id;
            this.question = question;
            this.correctAnswer = correctAnswer;
            this.difficulty = difficulty;
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
