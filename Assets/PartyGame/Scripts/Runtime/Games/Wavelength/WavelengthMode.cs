using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Games.Wavelength
{
    /// <summary>
    /// Everyone is given the same secret number on a 1-10 scale, except the imposter.
    /// Players answer a question at the strength of their number and the group works out
    /// whose answer does not fit.
    /// </summary>
    public class WavelengthMode : ImposterModeBase
    {
        public const string SettingDeviation = "minDeviation";
        public const string SettingImposterKnows = "imposterKnows";

        private const int ScaleMin = 1;
        private const int ScaleMax = 10;

        private readonly ContentRotation<WavelengthQuestion> _rotation = new ContentRotation<WavelengthQuestion>(q => q.Id, 20);

        private WavelengthQuestion _question;
        private int _crewNumber;
        private int _imposterNumber;

        public override GameModeId Id => GameModeId.Wavelength;

        protected override void OnInitialise()
        {
            Scoring = new ImposterScoring
            {
                CorrectAccusation = 100,
                CrewCaughtImposter = 150,
                ImposterSurvived = 300
            };
        }

        public override IReadOnlyList<SettingDefinition> GetSettingDefinitions(ContentService content)
        {
            var imposterCount = SettingDefinition.Stepper(CommonSettingKeys.ImposterCount, "Off-scale players", 1, 1, 3,
                description: "How many players get a different number");
            imposterCount.MaxForPlayerCount = players => Math.Max(1, (players - 1) / 3);

            return new List<SettingDefinition>
            {
                SettingDefinition.CategoryPicker(CommonSettingKeys.Categories, "Question packs",
                    "Pick one, several, or leave empty for all"),
                imposterCount,
                SettingDefinition.Stepper(SettingDeviation, "Minimum gap", 4, 2, 7,
                    description: "How far the odd number sits from everyone else"),
                SettingDefinition.Toggle(SettingImposterKnows, "Tell them they are odd", false,
                    "On: the odd player knows. Off: they have no idea - far funnier"),
                SettingDefinition.Stepper(CommonSettingKeys.Rounds, "Rounds", 3, 1, 10),
                SettingDefinition.Stepper(CommonSettingKeys.DiscussionSeconds, "Discussion time", 90, 30, 300, 15, "s"),
                SettingDefinition.Toggle(CommonSettingKeys.VotingEnabled, "Voting", true),
                SettingDefinition.Choice(CommonSettingKeys.TieBehaviour, "If the vote ties", "noResult", TieOptions())
            };
        }

        public override ValidationResult Validate(GameSession session)
        {
            var baseResult = base.Validate(session);
            if (!baseResult.IsValid) return baseResult;

            var packs = session.Content.ResolveSelection<WavelengthPackData>(
                GameModeId.Wavelength, session.Settings.GetList(CommonSettingKeys.Categories));
            if (packs.SelectMany(p => p.Questions).All(q => q == null || !q.IsValid))
                return ValidationResult.Fail("No wavelength questions are available.");
            return ValidationResult.Ok();
        }

        public override IReadOnlyList<string> BuildRulesSummary(GameSettings settings)
        {
            return new List<string>
            {
                "Everyone secretly gets the same number from 1 to 10 - except one player.",
                "A question appears with a scale, for example 1 = awful and 10 = incredible.",
                "Answer out loud at the strength of your number, never saying the number.",
                "Spot whose answer sits at the wrong point on the scale.",
                settings.GetBool(SettingImposterKnows, false)
                    ? "The odd player knows they are off-scale."
                    : "The odd player has no idea they are off-scale."
            };
        }

        protected override void BuildRound()
        {
            var packs = Session.Content.ResolveSelection<WavelengthPackData>(
                GameModeId.Wavelength, Session.Settings.GetList(CommonSettingKeys.Categories));
            var questions = packs.SelectMany(p => p.Questions).Where(q => q != null && q.IsValid).ToList();

            if (questions.Count == 0)
            {
                Enqueue(new MessageStep
                {
                    Title = "No questions",
                    Body = "The selected packs contain no wavelength questions.",
                    Accent = StepAccent.Danger,
                    ContinueLabel = "Back"
                });
                return;
            }

            _question = _rotation.Next(questions, Session.Random);

            var gap = Math.Max(2, Session.Settings.GetInt(SettingDeviation, 4));
            AssignNumbers(gap);

            var imposters = AssignImposters(Session.Settings.GetInt(CommonSettingKeys.ImposterCount, 1));
            var imposterKnows = Session.Settings.GetBool(SettingImposterKnows, false);

            Enqueue(new MessageStep
            {
                PhaseLabel = Session.RoundLabel,
                Title = "Secret numbers",
                Body = "Everyone gets a number from 1 to 10. Pass the phone around and keep it to yourself.",
                Feature = ScaleMin + " - " + ScaleMax,
                Accent = StepAccent.Primary,
                ContinueLabel = "Start passing"
            });

            var infoSteps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                var isImposter = imposters.Any(i => i.Id == player.Id);
                var number = isImposter ? _imposterNumber : _crewNumber;
                player.SetRoundData(RoundDataKeys.Number, number.ToString(CultureInfo.InvariantCulture));

                var step = new PrivateInfoStep
                {
                    PhaseLabel = "Your number",
                    Title = player.DisplayName,
                    ActorPlayerId = player.Id,
                    Accent = isImposter && imposterKnows ? StepAccent.Danger : StepAccent.Primary,
                    Headline = number.ToString(CultureInfo.InvariantCulture),
                    ContinueLabel = "Hide and pass on",
                    Footnote = "Never say your number out loud."
                };
                step.Lines.Add(new InfoLine("Your number", number + " out of 10", true));
                step.Lines.Add(new InfoLine("1 means", _question.LowLabel));
                step.Lines.Add(new InfoLine("10 means", _question.HighLabel));
                if (isImposter && imposterKnows)
                    step.Lines.Add(new InfoLine("Careful", "Your number is not the same as everyone else's."));

                infoSteps.Add(Enqueue(step));
            }
            ApplySequence(infoSteps);

            Enqueue(new MessageStep
            {
                PhaseLabel = "The question",
                Title = "Answer at your number",
                Feature = _question.Prompt,
                Body = "1 = " + _question.LowLabel + "   |   10 = " + _question.HighLabel,
                Accent = StepAccent.Primary,
                ContinueLabel = "First player"
            });

            var promptSteps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                var step = new MessageStep
                {
                    PhaseLabel = "Answers",
                    Title = player.DisplayName + ", your answer",
                    Feature = _question.Prompt,
                    Body = "Answer out loud as if your number is the right level. Do not say the number.",
                    ActorPlayerId = player.Id,
                    ContinueLabel = "Next player"
                };
                promptSteps.Add(Enqueue(step));
            }
            ApplySequence(promptSteps);

            Enqueue(BuildDiscussion(
                "Who is off the scale?",
                "Compare the answers. Somebody is working from a different number.",
                Session.Settings.GetInt(CommonSettingKeys.DiscussionSeconds, 90),
                "Ask people to rank each other's answers",
                "A too-strong or too-weak answer is the giveaway"));

            BuildVotingPhase("Whose answer did not fit the scale?");
        }

        private void AssignNumbers(int minimumGap)
        {
            _crewNumber = Session.Random.Range(ScaleMin, ScaleMax + 1);

            var candidates = new List<int>();
            for (var n = ScaleMin; n <= ScaleMax; n++)
                if (Math.Abs(n - _crewNumber) >= minimumGap) candidates.Add(n);

            if (candidates.Count == 0)
            {
                // The scale is too small for the requested gap: fall back to the far end.
                _imposterNumber = _crewNumber <= (ScaleMin + ScaleMax) / 2 ? ScaleMax : ScaleMin;
                return;
            }
            _imposterNumber = candidates[Session.Random.Range(0, candidates.Count)];
        }

        protected override void BuildReveal()
        {
            var imposters = Imposters;
            var reveal = new RevealStep
            {
                PhaseLabel = "Reveal",
                Title = "The numbers",
                Headline = imposters.Count == 1
                    ? NameOf(imposters[0].Id) + " was on " + _imposterNumber
                    : JoinNames(imposters) + " were on " + _imposterNumber,
                Accent = ImposterWasCaught ? StepAccent.Success : StepAccent.Danger
            };

            reveal.Entries.Add(new RevealEntry
            {
                Title = "Everyone else had",
                Detail = _crewNumber + " out of 10",
                Accent = StepAccent.Primary,
                Highlight = true
            });
            reveal.Entries.Add(new RevealEntry
            {
                Title = "The odd number was",
                Detail = _imposterNumber + " out of 10",
                Accent = StepAccent.Danger,
                Highlight = true
            });
            if (_question != null)
                reveal.Entries.Add(new RevealEntry
                {
                    Title = _question.Prompt,
                    Detail = "1 = " + _question.LowLabel + " / 10 = " + _question.HighLabel
                });

            AppendVoteEntries(reveal);
            Enqueue(reveal);
        }
    }
}
