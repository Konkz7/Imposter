using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Games.SocialDeduction
{
    /// <summary>
    /// A lighter social deduction round: secret roles, a handful of guaranteed-true clues,
    /// a debate and one elimination per round. Roles persist for the whole game, so the
    /// table builds up information instead of resetting every round.
    /// </summary>
    public class SocialDeductionMode : ImposterModeBase
    {
        public const string SettingInvestigator = "useInvestigator";
        public const string SettingWitness = "useWitness";

        private readonly Dictionary<int, string> _roles = new Dictionary<int, string>();
        private readonly ContentRotation<ClueTemplate> _sceneRotation = new ContentRotation<ClueTemplate>(c => c.Id, 10);
        private readonly ContentRotation<ClueTemplate> _hintRotation = new ContentRotation<ClueTemplate>(c => c.Id, 10);

        private SocialClueFactory _clues;
        private bool _rolesDealt;
        private bool _gameOver;
        private string _gameOverReason = string.Empty;

        public override GameModeId Id => GameModeId.SocialDeduction;

        protected override void OnInitialise()
        {
            Scoring = new ImposterScoring
            {
                CorrectAccusation = 100,
                CrewCaughtImposter = 200,
                ImposterSurvived = 250
            };
            _clues = new SocialClueFactory(Session.Random);
            _roles.Clear();
            _rolesDealt = false;
            _gameOver = false;
        }

        public override IReadOnlyList<SettingDefinition> GetSettingDefinitions(ContentService content)
        {
            var imposters = SettingDefinition.Stepper(CommonSettingKeys.ImposterCount, "Suspects", 1, 1, 3,
                description: "How many players are secretly involved");
            imposters.MaxForPlayerCount = players => Math.Max(1, (players - 1) / 3);

            return new List<SettingDefinition>
            {
                SettingDefinition.CategoryPicker(CommonSettingKeys.Categories, "Clue packs",
                    "Pick one, several, or leave empty for all"),
                imposters,
                SettingDefinition.Toggle(SettingInvestigator, "Investigator", true,
                    "One player privately learns that exactly one of two people is involved"),
                SettingDefinition.Toggle(SettingWitness, "Witness", true,
                    "One player privately gets a true detail about a suspect"),
                SettingDefinition.Stepper(CommonSettingKeys.Rounds, "Maximum rounds", 4, 2, 8),
                SettingDefinition.Stepper(CommonSettingKeys.DiscussionSeconds, "Discussion time", 180, 60, 420, 30, "s"),
                SettingDefinition.Choice(CommonSettingKeys.TieBehaviour, "If the vote ties", "noResult", TieOptions())
            };
        }

        public override ValidationResult Validate(GameSession session)
        {
            var baseResult = base.Validate(session);
            if (!baseResult.IsValid) return baseResult;

            var players = session.Roster.Count;
            var imposters = session.Settings.GetInt(CommonSettingKeys.ImposterCount, 1);
            var specials = (session.Settings.GetBool(SettingInvestigator, true) ? 1 : 0)
                           + (session.Settings.GetBool(SettingWitness, true) ? 1 : 0);

            if (imposters + specials >= players)
                return ValidationResult.Fail("Too many special roles for " + players + " players. Remove one.");
            if (imposters > (players - 1) / 2)
                return ValidationResult.Fail("At most " + Math.Max(1, (players - 1) / 2) + " suspects with " + players + " players.");

            return ValidationResult.Ok();
        }

        public override IReadOnlyList<string> BuildRulesSummary(GameSettings settings)
        {
            return new List<string>
            {
                "Everyone gets a secret role once, at the start of the game.",
                "Each round the table hears a scene and a weak public clue.",
                "The investigator and the witness privately receive a true, narrow clue.",
                "Debate, then vote. Whoever the group accuses is out of the game.",
                "The group wins by removing every suspect before the suspects reach half the table."
            };
        }

        public override RoundSummary CompleteRound()
        {
            return new RoundSummary
            {
                RoundNumber = Session.RoundNumber,
                GameOver = _gameOver || Session.IsFinalRound,
                GameOverReason = _gameOver ? _gameOverReason : string.Empty
            };
        }

        protected override void BuildRound()
        {
            if (!_rolesDealt) DealRoles();
            ApplyStoredRoles();

            var packs = Session.Content.ResolveSelection<CluePackData>(
                GameModeId.SocialDeduction, Session.Settings.GetList(CommonSettingKeys.Categories));
            var templates = packs.SelectMany(p => p.Clues).Where(c => c != null && c.IsValid).ToList();

            var scenes = templates.Where(c => c.Kind == ClueKind.Scene).ToList();
            var pairs = templates.Where(c => c.Kind == ClueKind.InvestigatorPair).ToList();
            var details = templates.Where(c => c.Kind == ClueKind.WitnessDetail).ToList();
            var hints = templates.Where(c => c.Kind == ClueKind.AnonymousHint).ToList();

            var alive = Session.ActivePlayers.OrderBy(p => p.SeatIndex).ToList();
            var suspects = alive.Where(p => p.RoleId == PlayerRoles.Imposter).ToList();
            var innocents = alive.Where(p => p.RoleId != PlayerRoles.Imposter).ToList();

            if (Session.RoundNumber == 1)
            {
                Enqueue(new MessageStep
                {
                    PhaseLabel = Session.RoundLabel,
                    Title = "Secret roles",
                    Body = "Pass the phone around. Everyone finds out who they are - once, for the whole game.",
                    Feature = Session.Roster.Count + " at the table",
                    Accent = StepAccent.Primary,
                    ContinueLabel = "Start passing"
                });

                var roleSteps = new List<GameStep>();
                foreach (var player in TurnOrder()) roleSteps.Add(Enqueue(BuildRoleCard(player, suspects)));
                ApplySequence(roleSteps);
            }

            var scene = _sceneRotation.Next(scenes, Session.Random);
            Enqueue(new MessageStep
            {
                PhaseLabel = "Round " + Session.RoundNumber + " - the scene",
                Title = "What the group knows",
                Feature = scene != null ? scene.Text : "Something happened again. Nobody is quite sure what.",
                Body = "Everyone can read this. It sets the scene - it does not name anyone.",
                Accent = StepAccent.Neutral,
                ContinueLabel = "Continue"
            });

            if (Session.Settings.GetBool(SettingInvestigator, true))
            {
                var investigator = alive.FirstOrDefault(p => p.RoleId == PlayerRoles.Investigator);
                var text = _clues.BuildInvestigatorPair(Shuffler.Pick(pairs, Session.Random), suspects, innocents);
                if (investigator != null && !string.IsNullOrEmpty(text))
                    Enqueue(BuildPrivateClue(investigator, "Investigator", text,
                        "This is always true. Use it without giving yourself away."));
            }

            if (Session.Settings.GetBool(SettingWitness, true))
            {
                var witness = alive.FirstOrDefault(p => p.RoleId == PlayerRoles.Witness);
                var text = _clues.BuildWitnessDetail(Shuffler.Pick(details, Session.Random), suspects, alive);
                if (witness != null && !string.IsNullOrEmpty(text))
                    Enqueue(BuildPrivateClue(witness, "Witness", text,
                        "Everything you saw is true, but it may fit more than one person."));
            }

            var publicHint = _clues.BuildAnonymousHint(_hintRotation.Next(hints, Session.Random), suspects, alive);
            if (!string.IsNullOrEmpty(publicHint))
            {
                Enqueue(new MessageStep
                {
                    PhaseLabel = "Shared clue",
                    Title = "Everyone hears this",
                    Feature = publicHint,
                    Body = "This clue is true, and everyone has it.",
                    Accent = StepAccent.Warning,
                    ContinueLabel = "Start the debate"
                });
            }

            Enqueue(BuildDiscussion(
                "Work it out",
                "Share what you are willing to share. Somebody is lying about what they know.",
                Session.Settings.GetInt(CommonSettingKeys.DiscussionSeconds, 180),
                "Special roles: revealing yourself is powerful but dangerous",
                "Suspects: claiming a role is a valid bluff"));

            BuildVotingPhase("Who does the group accuse?", "Accusation");
        }

        private void DealRoles()
        {
            var players = Session.Players.ToList();
            var pool = Shuffler.Shuffled(players, Session.Random);
            var cursor = 0;

            var imposterCount = Math.Min(
                Session.Settings.GetInt(CommonSettingKeys.ImposterCount, 1),
                Math.Max(1, (players.Count - 1) / 2));

            _roles.Clear();
            for (var i = 0; i < imposterCount && cursor < pool.Count; i++, cursor++)
                _roles[pool[cursor].Id] = PlayerRoles.Imposter;

            if (Session.Settings.GetBool(SettingInvestigator, true) && cursor < pool.Count)
                _roles[pool[cursor++].Id] = PlayerRoles.Investigator;

            if (Session.Settings.GetBool(SettingWitness, true) && cursor < pool.Count)
                _roles[pool[cursor++].Id] = PlayerRoles.Witness;

            foreach (var player in players)
                if (!_roles.ContainsKey(player.Id)) _roles[player.Id] = PlayerRoles.Crew;

            _rolesDealt = true;
        }

        /// <summary>Round data is wiped between rounds, so the dealt roles are re-applied here.</summary>
        private void ApplyStoredRoles()
        {
            foreach (var player in Session.Players)
                player.RoleId = _roles.TryGetValue(player.Id, out var role) ? role : PlayerRoles.Crew;
        }

        private PrivateInfoStep BuildRoleCard(PlayerState player, IReadOnlyList<PlayerState> suspects)
        {
            var role = player.RoleId;
            var step = new PrivateInfoStep
            {
                PhaseLabel = "Your role",
                Title = player.DisplayName,
                ActorPlayerId = player.Id,
                ContinueLabel = "Hide and pass on"
            };

            switch (role)
            {
                case PlayerRoles.Imposter:
                    step.Headline = "YOU ARE A SUSPECT";
                    step.Accent = StepAccent.Danger;
                    step.Lines.Add(new InfoLine("Your goal", "Survive the accusations", true));
                    if (suspects.Count > 1)
                        step.Lines.Add(new InfoLine("Working with", string.Join(", ",
                            suspects.Where(s => s.Id != player.Id).Select(s => s.DisplayName))));
                    else
                        step.Lines.Add(new InfoLine("You are", "on your own"));
                    step.Footnote = "Blend in. Claiming a role is allowed.";
                    break;
                case PlayerRoles.Investigator:
                    step.Headline = "INVESTIGATOR";
                    step.Accent = StepAccent.Success;
                    step.Lines.Add(new InfoLine("Each round", "You get a true clue about two players", true));
                    step.Footnote = "Reveal yourself carefully - or not at all.";
                    break;
                case PlayerRoles.Witness:
                    step.Headline = "WITNESS";
                    step.Accent = StepAccent.Success;
                    step.Lines.Add(new InfoLine("Each round", "You saw something true about a suspect", true));
                    step.Footnote = "What you saw is true but it may fit several people.";
                    break;
                default:
                    step.Headline = "YOU ARE CLEAN";
                    step.Accent = StepAccent.Primary;
                    step.Lines.Add(new InfoLine("Your goal", "Work out who is involved", true));
                    step.Footnote = "You have no special information. Listen carefully.";
                    break;
            }
            return step;
        }

        private PrivateInfoStep BuildPrivateClue(PlayerState player, string roleLabel, string clueText, string footnote)
        {
            player.SetRoundData(RoundDataKeys.Clue, clueText);
            var step = new PrivateInfoStep
            {
                PhaseLabel = roleLabel + " clue",
                Title = player.DisplayName,
                ActorPlayerId = player.Id,
                Accent = StepAccent.Success,
                Headline = roleLabel.ToUpperInvariant(),
                ContinueLabel = "Hide and pass on",
                Footnote = footnote
            };
            step.Lines.Add(new InfoLine("You know", clueText, true));
            return step;
        }

        protected override void ApplyScoring()
        {
            base.ApplyScoring();

            if (Outcome == null || !Outcome.HasWinner) return;
            var accused = Session.PlayerOf(Outcome.WinnerId);
            if (accused == null) return;
            accused.IsAlive = false;
        }

        protected override void BuildReveal()
        {
            var reveal = new RevealStep
            {
                PhaseLabel = "Reveal",
                Title = "The accusation",
                Accent = StepAccent.Neutral
            };

            if (Outcome != null && Outcome.HasWinner)
            {
                var accused = Session.PlayerOf(Outcome.WinnerId);
                var wasSuspect = accused != null && accused.RoleId == PlayerRoles.Imposter;
                reveal.Headline = (accused != null ? accused.DisplayName : "Nobody") +
                                  (wasSuspect ? " was involved" : " was clean");
                reveal.Accent = wasSuspect ? StepAccent.Success : StepAccent.Danger;
                reveal.Entries.Add(new RevealEntry
                {
                    Title = "Removed from the game",
                    Detail = accused != null ? accused.DisplayName + " - " + DescribeRole(accused.RoleId) : "-",
                    Accent = wasSuspect ? StepAccent.Success : StepAccent.Danger,
                    Highlight = true
                });
            }
            else
            {
                reveal.Headline = "Nobody was accused";
                reveal.Entries.Add(new RevealEntry { Title = "No elimination", Detail = "The group could not agree." });
            }

            EvaluateEndConditions(reveal);
            AppendVoteEntries(reveal);

            if (_gameOver)
            {
                reveal.Entries.Add(new RevealEntry
                {
                    Title = "All roles",
                    Detail = string.Join("   |   ", Session.Players.Select(p => p.DisplayName + ": " + DescribeRole(p.RoleId))),
                    Accent = StepAccent.Neutral
                });
            }

            Enqueue(reveal);
        }

        private void EvaluateEndConditions(RevealStep reveal)
        {
            var alive = Session.ActivePlayers.ToList();
            var aliveSuspects = alive.Count(p => p.RoleId == PlayerRoles.Imposter);
            var aliveClean = alive.Count - aliveSuspects;

            if (aliveSuspects == 0)
            {
                _gameOver = true;
                _gameOverReason = "The group removed everyone who was involved.";
                foreach (var player in Session.Players.Where(p => p.RoleId != PlayerRoles.Imposter))
                    Session.Scores.AddPoints(player.Id, 200, "The group won");
                reveal.Entries.Add(new RevealEntry
                {
                    Title = "The group wins",
                    Detail = _gameOverReason,
                    Accent = StepAccent.Success,
                    Highlight = true
                });
                return;
            }

            if (aliveSuspects >= aliveClean)
            {
                _gameOver = true;
                _gameOverReason = "The suspects now match the rest of the table.";
                foreach (var player in Session.Players.Where(p => p.RoleId == PlayerRoles.Imposter))
                    Session.Scores.AddPoints(player.Id, 300, "The suspects won");
                reveal.Entries.Add(new RevealEntry
                {
                    Title = "The suspects win",
                    Detail = _gameOverReason,
                    Accent = StepAccent.Danger,
                    Highlight = true
                });
                return;
            }

            reveal.Entries.Add(new RevealEntry
            {
                Title = "Still at the table",
                Detail = TextUtility.Pluralise(aliveSuspects, "suspect") + " among " + alive.Count + " players",
                Accent = StepAccent.Warning
            });
        }

        private static string DescribeRole(string roleId)
        {
            switch (roleId)
            {
                case PlayerRoles.Imposter: return "suspect";
                case PlayerRoles.Investigator: return "investigator";
                case PlayerRoles.Witness: return "witness";
                default: return "clean";
            }
        }
    }
}
