using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Util;
using UnityEditor;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Checks the generated content library for the mistakes that are easy to make at this
    /// volume and impossible to spot by eye: duplicates, near duplicates, empty fields, text too
    /// long for the card it has to fit on, and current content that has gone past its review date.
    ///
    /// Reports counts rather than failing the build, because most findings are judgement calls.
    /// </summary>
    public static class ContentValidator
    {
        /// <summary>Roughly what fits on a phone card before the text has to shrink.</summary>
        private const int MaxQuestionLength = 120;
        private const int MaxStatementLength = 90;
        private const int MaxPromptLength = 70;
        private const int MaxWordLength = 24;

        private static readonly List<string> Problems = new List<string>();
        private static readonly List<string> Notes = new List<string>();

        [MenuItem("Party Game/Validate Content", false, 11)]
        public static void Validate()
        {
            Problems.Clear();
            Notes.Clear();

            var library = AssetDatabase.LoadAssetAtPath<ContentLibrary>(
                "Assets/Resources/PartyGame/ContentLibrary.asset");
            if (library == null)
            {
                Debug.LogError("[Content] No ContentLibrary found. Run Party Game > Rebuild Content.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            ValidateWords(library);
            ValidateTrivia(library);
            ValidateWavelength(library);
            ValidateDebate(library);
            ValidateClues(library);
            ValidateFreshness(library);

            foreach (var note in Notes) Debug.Log("[Content] " + note);
            foreach (var problem in Problems) Debug.LogError("[Content] " + problem);

            var total = library.WordCategories.Sum(p => p.EntryCount)
                        + library.TriviaPacks.Sum(p => p.EntryCount)
                        + library.WavelengthPacks.Sum(p => p.EntryCount)
                        + library.DebatePacks.Sum(p => p.EntryCount)
                        + library.CluePacks.Sum(p => p.EntryCount);

            Debug.Log("[Content] " + total + " entries checked. " + Problems.Count + " problem(s).");
            if (Application.isBatchMode) EditorApplication.Exit(Problems.Count == 0 ? 0 : 1);
        }

        private static void ValidateWords(ContentLibrary library)
        {
            var seenPairs = new Dictionary<string, string>();
            var total = 0;

            foreach (var pack in library.WordCategories.Where(p => p != null))
            {
                var withinPack = new HashSet<string>();

                foreach (var pair in pack.Pairs)
                {
                    total++;

                    if (!pair.IsValid)
                    {
                        Problems.Add(pack.Id + ": a pair is missing one of its words.");
                        continue;
                    }

                    if (TextUtility.Matches(pair.CrewWord, pair.ImposterWord))
                        Problems.Add(pack.Id + ": \"" + pair.CrewWord + "\" is paired with itself.");

                    if (pair.CrewWord.Length > MaxWordLength || pair.ImposterWord.Length > MaxWordLength)
                        Notes.Add(pack.Id + ": \"" + pair.CrewWord + " / " + pair.ImposterWord + "\" is long for a card.");

                    // Order does not matter for a duplicate: A/B and B/A are the same round.
                    var a = TextUtility.Normalise(pair.CrewWord);
                    var b = TextUtility.Normalise(pair.ImposterWord);
                    var key = string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;

                    if (!withinPack.Add(key))
                        Problems.Add(pack.Id + ": duplicate pair \"" + pair.CrewWord + " / " + pair.ImposterWord + "\".");
                    else if (seenPairs.TryGetValue(key, out var otherPack))
                        Notes.Add("\"" + pair.CrewWord + " / " + pair.ImposterWord + "\" appears in both " +
                                  otherPack + " and " + pack.Id + ".");
                    else
                        seenPairs[key] = pack.Id;
                }

                if (pack.EntryCount < 20)
                    Notes.Add(pack.Id + " has only " + pack.EntryCount + " pairs, which will repeat quickly.");
            }

            Notes.Add("Word pairs: " + total + " across " + library.WordCategories.Count + " categories.");
        }

        private static void ValidateTrivia(ContentLibrary library)
        {
            var seenQuestions = new HashSet<string>();
            var seenIds = new HashSet<string>();
            var unverified = 0;
            var total = 0;

            foreach (var pack in library.TriviaPacks.Where(p => p != null))
            {
                foreach (var question in pack.Questions)
                {
                    total++;

                    if (!question.IsValid)
                    {
                        Problems.Add(pack.Id + ": a question is missing its text or answer.");
                        continue;
                    }

                    if (!seenIds.Add(question.Id))
                        Problems.Add("Duplicate question id \"" + question.Id + "\" in " + pack.Id + ".");

                    if (!seenQuestions.Add(TextUtility.Normalise(question.Question)))
                        Problems.Add(pack.Id + ": duplicate question \"" + question.Question + "\".");

                    if (question.Question.Length > MaxQuestionLength)
                        Notes.Add(pack.Id + ": question is " + question.Question.Length + " characters, which will shrink on a phone.");

                    if (question.CorrectAnswer.Length > 45)
                        Notes.Add(pack.Id + ": answer \"" + question.CorrectAnswer + "\" is long for a choice button.");

                    if (!question.Question.TrimEnd().EndsWith("?", StringComparison.Ordinal))
                        Notes.Add(pack.Id + ": \"" + question.Question + "\" does not read as a question.");

                    var source = question.SourceInfo;
                    if (source == null || !source.HasSource)
                        Problems.Add(pack.Id + ": \"" + question.Question + "\" has no source reference.");
                    else if (string.IsNullOrEmpty(source.VerifiedDate))
                        unverified++;

                    if (pack.Freshness == Freshness.Current &&
                        (source == null || string.IsNullOrEmpty(source.VerifiedDate)))
                        Problems.Add(pack.Id + " is a current pack, so \"" + question.Question +
                                     "\" must carry a verification date.");
                }
            }

            Notes.Add("Trivia: " + total + " questions, " + unverified +
                      " carrying a source link but no verification date (evergreen, written from general knowledge).");
        }

        private static void ValidateWavelength(ContentLibrary library)
        {
            var seen = new HashSet<string>();
            var total = 0;

            foreach (var pack in library.WavelengthPacks.Where(p => p != null))
            {
                foreach (var question in pack.Questions)
                {
                    total++;

                    if (!question.IsValid)
                    {
                        Problems.Add(pack.Id + ": a prompt is missing its text or one end of its scale.");
                        continue;
                    }

                    if (!seen.Add(TextUtility.Normalise(question.Prompt)))
                        Problems.Add(pack.Id + ": duplicate prompt \"" + question.Prompt + "\".");

                    if (TextUtility.Matches(question.LowLabel, question.HighLabel))
                        Problems.Add(pack.Id + ": \"" + question.Prompt + "\" has the same label at both ends.");

                    if (question.Prompt.Length > MaxPromptLength)
                        Notes.Add(pack.Id + ": prompt \"" + question.Prompt + "\" is long for the header.");
                }
            }

            Notes.Add("Wavelength: " + total + " prompts.");
        }

        private static void ValidateDebate(ContentLibrary library)
        {
            var seen = new HashSet<string>();
            var total = 0;

            foreach (var pack in library.DebatePacks.Where(p => p != null))
            {
                foreach (var statement in pack.Statements)
                {
                    total++;

                    if (!statement.IsValid)
                    {
                        Problems.Add(pack.Id + ": an empty statement.");
                        continue;
                    }

                    if (!seen.Add(TextUtility.Normalise(statement.Statement)))
                        Problems.Add(pack.Id + ": duplicate statement \"" + statement.Statement + "\".");

                    if (statement.Statement.Length > MaxStatementLength)
                        Notes.Add(pack.Id + ": statement is " + statement.Statement.Length + " characters, which is long to read aloud.");

                    if (!statement.Statement.TrimEnd().EndsWith(".", StringComparison.Ordinal))
                        Notes.Add(pack.Id + ": \"" + statement.Statement + "\" is not written as a flat assertion.");
                }
            }

            Notes.Add("Debate: " + total + " statements.");
        }

        private static void ValidateClues(ContentLibrary library)
        {
            var seen = new HashSet<string>();
            var total = 0;

            foreach (var pack in library.CluePacks.Where(p => p != null))
            {
                var kinds = new Dictionary<ClueKind, int>();

                foreach (var clue in pack.Clues)
                {
                    total++;

                    if (!clue.IsValid)
                    {
                        Problems.Add(pack.Id + ": an empty clue template.");
                        continue;
                    }

                    if (!seen.Add(TextUtility.Normalise(clue.Text)))
                        Problems.Add(pack.Id + ": duplicate clue \"" + clue.Text + "\".");

                    kinds[clue.Kind] = kinds.TryGetValue(clue.Kind, out var count) ? count + 1 : 1;

                    // The generated half of a clue has to have somewhere to go.
                    if (clue.Kind == ClueKind.InvestigatorPair &&
                        (!clue.Text.Contains("{a}") || !clue.Text.Contains("{b}")))
                        Problems.Add(pack.Id + ": investigator clue \"" + clue.Text + "\" is missing an {a} or {b} token.");

                    if ((clue.Kind == ClueKind.WitnessDetail || clue.Kind == ClueKind.AnonymousHint) &&
                        !clue.Text.Contains("{fact}"))
                        Problems.Add(pack.Id + ": \"" + clue.Text + "\" is missing its {fact} token.");

                    if (clue.Kind == ClueKind.Scene && clue.Text.Contains("{"))
                        Problems.Add(pack.Id + ": scene clue \"" + clue.Text + "\" contains a token it cannot fill.");
                }

                foreach (ClueKind kind in Enum.GetValues(typeof(ClueKind)))
                    if (!kinds.ContainsKey(kind))
                        Problems.Add(pack.Id + " has no " + kind + " clues, so that phase will fall back to a default.");
            }

            Notes.Add("Clues: " + total + " templates across " + library.CluePacks.Count + " scenarios.");
        }

        private static void ValidateFreshness(ContentLibrary library)
        {
            var today = DateTime.Today;
            var packs = library.TriviaPacks.Cast<ContentPack>()
                .Concat(library.WordCategories)
                .Concat(library.WavelengthPacks)
                .Concat(library.DebatePacks)
                .Concat(library.CluePacks)
                .Where(p => p != null);

            foreach (var pack in packs)
            {
                if (pack.Freshness != Freshness.Current) continue;

                if (string.IsNullOrEmpty(pack.ReviewBy))
                    Problems.Add(pack.Id + " is marked current but has no review date, so it will never be rechecked.");
                else if (pack.NeedsReview(today))
                    Problems.Add(pack.Id + " passed its review date of " + pack.ReviewBy +
                                 ". Recheck the facts or move it to evergreen.");
                else
                    Notes.Add(pack.Id + " is current content, due for review by " + pack.ReviewBy + ".");
            }
        }
    }
}
