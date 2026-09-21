using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Games.DevilsAdvocate
{
    /// <summary>
    /// A debate statement appears. Everyone privately picks a side, then one player is
    /// secretly told to argue the opposite of whatever they actually chose. The group has
    /// to find the person whose reasoning does not match their real opinion.
    /// </summary>
    public class DevilsAdvocateMode : ImposterModeBase
    {
        private const string StancePrefix = "stance-";
        public const string StanceAgree = "agree";
        public const string StanceDisagree = "disagree";

        private readonly ContentRotation<DebateStatement> _rotation = new ContentRotation<DebateStatement>(s => s.Id, 20);
        private DebateStatement _statement;

        public override GameModeId Id => GameModeId.DevilsAdvocate;

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
            var advocates = SettingDefinition.Stepper(CommonSettingKeys.ImposterCount, "Devil's advocates", 1, 1, 2,
                description: "How many players argue against themselves");
            advocates.MaxForPlayerCount = players => Math.Max(1, (players - 1) / 3);

            return new List<SettingDefinition>
            {
                SettingDefinition.CategoryPicker(CommonSettingKeys.Categories, "Statement packs",
                    "Pick one, several, or leave empty for all"),
                advocates,
                SettingDefinition.Stepper(CommonSettingKeys.Rounds, "Rounds", 3, 1, 10),
                SettingDefinition.Stepper(CommonSettingKeys.DiscussionSeconds, "Debate time", 150, 60, 420, 30, "s"),
                SettingDefinition.Toggle(CommonSettingKeys.VotingEnabled, "Voting", true),
                SettingDefinition.Choice(CommonSettingKeys.TieBehaviour, "If the vote ties", "noResult", TieOptions())
            };
        }

        public override ValidationResult Validate(GameSession session)
        {
            var baseResult = base.Validate(session);
            if (!baseResult.IsValid) return baseResult;

            var packs = session.Content.ResolveSelection<DebatePackData>(
                GameModeId.DevilsAdvocate, session.Settings.GetList(CommonSettingKeys.Categories));
            if (packs.SelectMany(p => p.Statements).All(s => s == null || !s.IsValid))
                return ValidationResult.Fail("No debate statements are available.");
            return ValidationResult.Ok();
        }

        public override IReadOnlyList<string> BuildRulesSummary(GameSettings settings)
        {
            return new List<string>
            {
                "A statement appears. Everyone privately picks agree or disagree.",
                "One player is secretly told to argue the opposite of what they picked.",
                "Everyone gives a short, genuine sounding reason for their side.",
                "The advocate wins by sounding sincere, not by hiding.",
                "Discuss, then vote for whoever is arguing against their real opinion."
            };
        }

        protected override void BuildRound()
        {
            var packs = Session.Content.ResolveSelection<DebatePackData>(
                GameModeId.DevilsAdvocate, Session.Settings.GetList(CommonSettingKeys.Categories));
            var statements = packs.SelectMany(p => p.Statements).Where(s => s != null && s.IsValid).ToList();

            if (statements.Count == 0)
            {
                Enqueue(new MessageStep
                {
                    Title = "No statements",
                    Body = "The selected packs contain no debate statements.",
                    Accent = StepAccent.Danger,
                    ContinueLabel = "Back"
                });
                return;
            }

            _statement = _rotation.Next(statements, Session.Random);
            AssignImposters(Session.Settings.GetInt(CommonSettingKeys.ImposterCount, 1));

            Enqueue(new MessageStep
            {
                PhaseLabel = Session.RoundLabel,
                Title = "Tonight's statement",
                Feature = _statement.Statement,
                Body = "Pass the phone around. Everyone privately decides where they really stand.",
                Accent = StepAccent.Primary,
                ContinueLabel = "Start passing"
            });

            var steps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                var step = new ChoiceStep
                {
                    PhaseLabel = "Your real view",
                    Title = player.DisplayName,
                    Body = _statement.Statement,
                    Prompt = "Where do you actually stand?",
                    ActorPlayerId = player.Id,
                    Accent = StepAccent.Primary,
                    ContinueLabel = "Confirm",
                    Id = StancePrefix + player.Id.ToString(CultureInfo.InvariantCulture)
                };
                step.Options.Add(new ChoiceOption(StanceAgree, "I agree", "The statement is true") { Accent = StepAccent.Success });
                step.Options.Add(new ChoiceOption(StanceDisagree, "I disagree", "The statement is wrong") { Accent = StepAccent.Danger });
                steps.Add(Enqueue(step));
            }
            ApplySequence(steps);

            Enqueue(new MessageStep
            {
                PhaseLabel = "Debate",
                Title = "Make your case",
                Feature = _statement.Statement,
                Body = "Go round the group. Everyone gives a short reason for the side they are arguing.",
                Accent = StepAccent.Primary,
                ContinueLabel = "First speaker"
            });

            var promptSteps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                promptSteps.Add(Enqueue(new MessageStep
                {
                    PhaseLabel = "Debate",
                    Title = player.DisplayName + ", make your case",
                    Feature = _statement.Statement,
                    Body = "Give one clear reason for your side. Sound like you mean it.",
                    ActorPlayerId = player.Id,
                    ContinueLabel = "Next speaker"
                }));
            }
            ApplySequence(promptSteps);

            Enqueue(BuildDiscussion(
                "Open debate",
                "Push back on each other. Who is arguing a side they do not believe?",
                Session.Settings.GetInt(CommonSettingKeys.DiscussionSeconds, 150),
                "Ask people why they hold their view",
                "A real opinion usually has a personal example behind it"));

            BuildVotingPhase("Who is arguing against what they actually think?");
        }

        protected override void OnStepHandled(GameStep step, StepResult result)
        {
            if (step == null || !step.Id.StartsWith(StancePrefix, StringComparison.Ordinal)) return;
            if (!step.ActorPlayerId.HasValue) return;

            var player = Session.PlayerOf(step.ActorPlayerId.Value);
            if (player == null) return;

            var stance = result.OptionId == StanceDisagree ? StanceDisagree : StanceAgree;
            player.SetRoundData(RoundDataKeys.Stance, stance);

            var isAdvocate = player.RoleId == PlayerRoles.Imposter;
            var argueFor = isAdvocate
                ? (stance == StanceAgree ? StanceDisagree : StanceAgree)
                : stance;
            player.SetRoundData(RoundDataKeys.Instruction, argueFor);

            // The instruction can only be written once the player has picked a side, so it is
            // slotted in immediately after their own choice - still on their turn with the phone.
            var brief = new PrivateInfoStep
            {
                PhaseLabel = "Your brief",
                Title = player.DisplayName,
                ActorPlayerId = player.Id,
                Accent = isAdvocate ? StepAccent.Danger : StepAccent.Primary,
                Headline = isAdvocate ? "ARGUE THE OPPOSITE" : "ARGUE YOUR SIDE",
                ContinueLabel = "Hide and pass on"
            };
            brief.Lines.Add(new InfoLine("You really think", stance == StanceAgree ? "Agree" : "Disagree"));
            brief.Lines.Add(new InfoLine("Out loud you must argue",
                argueFor == StanceAgree ? "FOR the statement" : "AGAINST the statement", true));
            brief.Lines.Add(new InfoLine("Statement", _statement.Statement));
            brief.Footnote = isAdvocate
                ? "Win by sounding convincing, not by staying quiet."
                : "Argue honestly. Someone in the group is not.";

            InsertNext(brief);
        }

        protected override void BuildReveal()
        {
            var advocates = Imposters;
            var reveal = new RevealStep
            {
                PhaseLabel = "Reveal",
                Title = "Who was playing devil's advocate?",
                Headline = advocates.Count == 1
                    ? NameOf(advocates[0].Id)
                    : JoinNames(advocates),
                Accent = ImposterWasCaught ? StepAccent.Success : StepAccent.Danger
            };

            foreach (var player in Session.ActivePlayers.OrderBy(p => p.SeatIndex))
            {
                var real = player.GetRoundData(RoundDataKeys.Stance) == StanceDisagree ? "Disagreed" : "Agreed";
                var argued = player.GetRoundData(RoundDataKeys.Instruction) == StanceDisagree ? "argued against" : "argued for";
                var isAdvocate = player.RoleId == PlayerRoles.Imposter;
                reveal.Entries.Add(new RevealEntry
                {
                    Title = player.DisplayName + (isAdvocate ? "  (advocate)" : string.Empty),
                    Detail = "Really " + real.ToLowerInvariant() + ", " + argued + " it",
                    Accent = isAdvocate ? StepAccent.Danger : StepAccent.Neutral,
                    Highlight = isAdvocate
                });
            }

            AppendVoteEntries(reveal);
            Enqueue(reveal);
        }
    }
}
