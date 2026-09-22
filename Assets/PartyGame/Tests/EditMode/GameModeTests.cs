using System.Linq;
using NUnit.Framework;
using PartyGame.Core.Modes;
using PartyGame.Core.Session;
using PartyGame.Games.DevilsAdvocate;
using PartyGame.Games.DifferentWord;

namespace PartyGame.Tests
{
    public class DifferentWordModeTests
    {
        [Test]
        public void EverybodyGetsExactlyOnePrivateCard()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            var cards = log.StepsOfType<PrivateInfoStep>().ToList();
            Assert.AreEqual(6, cards.Count);
            CollectionAssert.AreEquivalent(
                session.Players.Select(p => (int?)p.Id),
                cards.Select(c => c.ActorPlayerId));
        }

        [Test]
        public void TheImposterCountMatchesTheSetting()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 8, out var mode,
                s =>
                {
                    s.SetInt(CommonSettingKeys.ImposterCount, 2);
                    s.SetInt(CommonSettingKeys.Rounds, 1);
                });
            ModeTestHarness.PlayRound(session, mode);

            Assert.AreEqual(2, session.Players.Count(p => p.RoleId == PlayerRoles.Imposter));
        }

        [Test]
        public void EveryoneButTheImposterSharesOneWord()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            ModeTestHarness.PlayRound(session, mode);

            var crewWords = session.Players
                .Where(p => p.RoleId != PlayerRoles.Imposter)
                .Select(p => p.GetRoundData(RoundDataKeys.Word))
                .Distinct()
                .ToList();

            Assert.AreEqual(1, crewWords.Count, "The group must all share one word.");

            var imposterWord = session.Players.First(p => p.RoleId == PlayerRoles.Imposter)
                .GetRoundData(RoundDataKeys.Word);
            Assert.AreNotEqual(crewWords[0], imposterWord);
        }

        [Test]
        public void TheNoHintSettingGivesTheImposterNoWordAtAll()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 6, out var mode,
                s =>
                {
                    s.SetString(DifferentWordMode.SettingHint, ImposterHint.Nothing);
                    s.SetInt(CommonSettingKeys.Rounds, 1);
                });
            ModeTestHarness.PlayRound(session, mode);

            var imposter = session.Players.First(p => p.RoleId == PlayerRoles.Imposter);
            Assert.AreEqual(string.Empty, imposter.GetRoundData(RoundDataKeys.Word));
        }

        [Test]
        public void TheClueSettingAddsAWrittenHintForTheImposter()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 6, out var mode,
                s =>
                {
                    s.SetString(DifferentWordMode.SettingHint, ImposterHint.SimilarWordAndClue);
                    s.SetInt(CommonSettingKeys.Rounds, 1);
                });
            var log = ModeTestHarness.PlayRound(session, mode);

            var imposter = session.Players.First(p => p.RoleId == PlayerRoles.Imposter);
            var card = log.StepsOfType<PrivateInfoStep>().First(s => s.ActorPlayerId == imposter.Id);

            Assert.IsTrue(card.Lines.Any(l => l.Label == "Clue"), "Expected a clue line on the imposter card.");
            Assert.IsNotEmpty(imposter.GetRoundData(RoundDataKeys.Hint));
        }

        [Test]
        public void EveryPrivateStepBelongsToExactlyOnePlayer()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 7, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            foreach (var step in log.PrivateSteps)
                Assert.IsTrue(step.ActorPlayerId.HasValue,
                    "A private step with no owner would be shown to the whole table.");
        }

        [Test]
        public void TooManyImpostersIsRejectedBeforeTheGameStarts()
        {
            var content = ModeTestHarness.LoadContent();
            var mode = GameModeFactory.Create(GameModeId.DifferentWord);

            var roster = new PlayerRoster();
            for (var i = 0; i < 4; i++) roster.Add("P" + i);

            var settings = new GameSettings();
            settings.Declare(mode.GetSettingDefinitions(content));
            settings.SetInt(CommonSettingKeys.ImposterCount, 3);

            var session = new GameSession(roster, new Core.Services.ScoreManager(),
                new Core.Services.SystemRandomProvider(1), content, content.GetMode(GameModeId.DifferentWord), settings);

            var result = mode.Validate(session);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void ARoundAlwaysEndsWithARevealAndAScoreboard()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            Assert.AreEqual(StepKind.Scoreboard, log.Steps.Last().Kind);
            Assert.IsTrue(log.Steps.Any(s => s.Kind == StepKind.Reveal));
        }

        [Test]
        public void PointsAreAwardedForCatchingTheImposter()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            ModeTestHarness.PlayRound(session, mode);

            var total = session.Players.Sum(p => session.Scores.GetScore(p.Id));
            Assert.Greater(total, 0, "Somebody should have scored during a completed round.");
        }

        [Test]
        public void VotingCanBeTurnedOffEntirely()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 5, out var mode,
                s =>
                {
                    s.SetBool(CommonSettingKeys.VotingEnabled, false);
                    s.SetInt(CommonSettingKeys.Rounds, 1);
                });
            var log = ModeTestHarness.PlayRound(session, mode);

            Assert.IsFalse(log.Steps.Any(s => s.PhaseLabel == "Voting"));
            Assert.AreEqual(StepKind.Scoreboard, log.Steps.Last().Kind);
        }

        [Test]
        public void MultipleRoundsRunToCompletion()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DifferentWord, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 3));
            var log = ModeTestHarness.PlayWholeGame(session, mode);

            Assert.AreEqual(3, log.Rounds.Count);
            Assert.IsTrue(log.Rounds.Last().GameOver);
        }
    }

    public class FibModeTests
    {
        [Test]
        public void EverybodyWritesAnAnswerAndThenGuesses()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Fib, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            Assert.AreEqual(5, log.StepsOfType<TextInputStep>().Count());
            Assert.AreEqual(5, log.StepsOfType<ChoiceStep>().Count());
        }

        [Test]
        public void NobodyIsOfferedTheirOwnLie()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Fib, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            foreach (var guess in log.StepsOfType<ChoiceStep>())
            {
                var author = session.PlayerOf(guess.ActorPlayerId ?? -1);
                if (author == null) continue;
                var own = author.GetRoundData(RoundDataKeys.WrittenAnswer);
                if (string.IsNullOrEmpty(own)) continue;

                CollectionAssert.DoesNotContain(guess.Options.Select(o => o.Label), own,
                    author.DisplayName + " was shown their own answer.");
            }
        }

        [Test]
        public void TheAnswerListHoldsOneEntryPerPlayerPlusTheTruth()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Fib, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            var guess = log.StepsOfType<ChoiceStep>().First();
            // Each player sees everything except their own lie.
            Assert.AreEqual(6, guess.Options.Count);
        }

        [Test]
        public void EveryWritingAndGuessingStepIsPrivate()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Fib, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            Assert.IsTrue(log.StepsOfType<TextInputStep>().All(s => s.IsPrivate && s.ActorPlayerId.HasValue));
            Assert.IsTrue(log.StepsOfType<ChoiceStep>().All(s => s.IsPrivate && s.ActorPlayerId.HasValue));
        }

        [Test]
        public void GuessingCorrectlyScoresPoints()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Fib, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            ModeTestHarness.PlayRound(session, mode);

            Assert.Greater(session.Players.Sum(p => session.Scores.GetScore(p.Id)), 0);
        }

        [Test]
        public void TheRealAnswerIsAlwaysOnTheList()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Fib, 4, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            var reveal = log.StepsOfType<RevealStep>().First();
            var guess = log.StepsOfType<ChoiceStep>().First();

            Assert.IsTrue(guess.Options.Any(o => o.Label == reveal.Headline),
                "The genuine answer must appear among the options.");
        }

        [Test]
        public void MultipleRoundsRunToCompletion()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Fib, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 3));
            var log = ModeTestHarness.PlayWholeGame(session, mode);
            Assert.AreEqual(3, log.Rounds.Count);
        }
    }

    public class WavelengthModeTests
    {
        [Test]
        public void EverybodyExceptTheOddPlayerSharesANumber()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Wavelength, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            ModeTestHarness.PlayRound(session, mode);

            var crewNumbers = session.Players
                .Where(p => p.RoleId != PlayerRoles.Imposter)
                .Select(p => p.GetRoundDataInt(RoundDataKeys.Number))
                .Distinct()
                .ToList();

            Assert.AreEqual(1, crewNumbers.Count);
        }

        [Test]
        public void TheOddNumberRespectsTheMinimumGap()
        {
            for (var seed = 1; seed <= 12; seed++)
            {
                var session = ModeTestHarness.CreateSession(GameModeId.Wavelength, 6, out var mode,
                    s =>
                    {
                        s.SetInt(Games.Wavelength.WavelengthMode.SettingDeviation, 4);
                        s.SetInt(CommonSettingKeys.Rounds, 1);
                    }, seed);
                ModeTestHarness.PlayRound(session, mode);

                var crew = session.Players.First(p => p.RoleId != PlayerRoles.Imposter)
                    .GetRoundDataInt(RoundDataKeys.Number);
                var imposter = session.Players.First(p => p.RoleId == PlayerRoles.Imposter)
                    .GetRoundDataInt(RoundDataKeys.Number);

                Assert.GreaterOrEqual(System.Math.Abs(crew - imposter), 4,
                    "Seed " + seed + " produced a gap that was too small.");
            }
        }

        [Test]
        public void EveryNumberStaysInsideTheOneToTenScale()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Wavelength, 8, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            ModeTestHarness.PlayRound(session, mode);

            foreach (var player in session.Players)
            {
                var number = player.GetRoundDataInt(RoundDataKeys.Number);
                Assert.GreaterOrEqual(number, 1);
                Assert.LessOrEqual(number, 10);
            }
        }

        [Test]
        public void TheAnyGapSettingAllowsAnyOtherNumberIncludingAdjacent()
        {
            var seenGaps = new System.Collections.Generic.HashSet<int>();

            for (var seed = 1; seed <= 40; seed++)
            {
                var session = ModeTestHarness.CreateSession(GameModeId.Wavelength, 5, out var mode,
                    s =>
                    {
                        s.SetInt(Games.Wavelength.WavelengthMode.SettingDeviation,
                            Games.Wavelength.WavelengthMode.MinimumGapForAny);
                        s.SetInt(CommonSettingKeys.Rounds, 1);
                    }, seed);
                ModeTestHarness.PlayRound(session, mode);

                var crew = session.Players.First(p => p.RoleId != PlayerRoles.Imposter)
                    .GetRoundDataInt(RoundDataKeys.Number);
                var odd = session.Players.First(p => p.RoleId == PlayerRoles.Imposter)
                    .GetRoundDataInt(RoundDataKeys.Number);

                Assert.AreNotEqual(crew, odd, "The odd number must still differ from everyone else.");
                seenGaps.Add(System.Math.Abs(crew - odd));
            }

            Assert.Contains(1, seenGaps.ToList(),
                "With no minimum gap the odd number should sometimes land right next door.");
        }

        [Test]
        public void TheOddPlayerIsNotToldUnlessTheSettingSaysSo()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.Wavelength, 6, out var mode,
                s =>
                {
                    s.SetBool(Games.Wavelength.WavelengthMode.SettingImposterKnows, false);
                    s.SetInt(CommonSettingKeys.Rounds, 1);
                });
            var log = ModeTestHarness.PlayRound(session, mode);

            var imposter = session.Players.First(p => p.RoleId == PlayerRoles.Imposter);
            var card = log.StepsOfType<PrivateInfoStep>().First(s => s.ActorPlayerId == imposter.Id);

            Assert.IsFalse(card.Lines.Any(l => l.Label == "Careful"),
                "The odd player should not be warned when the setting is off.");
        }
    }

    public class DevilsAdvocateModeTests
    {
        [Test]
        public void EverybodyPicksASideAndThenReceivesABrief()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DevilsAdvocate, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            ModeTestHarness.PlayRound(session, mode);

            foreach (var player in session.Players)
            {
                Assert.IsNotEmpty(player.GetRoundData(RoundDataKeys.Stance));
                Assert.IsNotEmpty(player.GetRoundData(RoundDataKeys.Instruction));
            }
        }

        [Test]
        public void TheAdvocateArguesTheOppositeAndNobodyElseDoes()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DevilsAdvocate, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            ModeTestHarness.PlayRound(session, mode);

            foreach (var player in session.Players)
            {
                var stance = player.GetRoundData(RoundDataKeys.Stance);
                var instruction = player.GetRoundData(RoundDataKeys.Instruction);

                if (player.RoleId == PlayerRoles.Imposter)
                    Assert.AreNotEqual(stance, instruction, player.DisplayName + " should argue the opposite.");
                else
                    Assert.AreEqual(stance, instruction, player.DisplayName + " should argue honestly.");
            }
        }

        [Test]
        public void TheBriefArrivesImmediatelyAfterThePlayersOwnChoice()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DevilsAdvocate, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            for (var i = 0; i < log.Steps.Count - 1; i++)
            {
                if (!(log.Steps[i] is ChoiceStep choice) || choice.PhaseLabel != "Your real view") continue;

                var next = log.Steps[i + 1];
                Assert.IsInstanceOf<PrivateInfoStep>(next);
                Assert.AreEqual(choice.ActorPlayerId, next.ActorPlayerId,
                    "The brief must stay with the player who just chose, so the phone is not passed twice.");
            }
        }

        [Test]
        public void TheRevealShowsEveryPlayersRealStance()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.DevilsAdvocate, 5, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            var reveal = log.StepsOfType<RevealStep>().First();
            foreach (var player in session.Players)
                Assert.IsTrue(reveal.Entries.Any(e => e.Title.StartsWith(player.DisplayName)),
                    player.DisplayName + " is missing from the reveal.");
        }
    }

    public class SocialDeductionModeTests
    {
        [Test]
        public void RolesAreDealtOnceAndKeptForTheWholeGame()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.SocialDeduction, 7, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 4));

            ModeTestHarness.PlayRound(session, mode);
            var firstRound = session.Players.ToDictionary(p => p.Id, p => p.RoleId);

            session.AdvanceRound();
            ModeTestHarness.PlayRound(session, mode);

            foreach (var player in session.Players)
                Assert.AreEqual(firstRound[player.Id], player.RoleId,
                    player.DisplayName + " changed role between rounds.");
        }

        [Test]
        public void RoleCardsAreOnlyHandedOutInTheFirstRound()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.SocialDeduction, 7, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 4));

            var first = ModeTestHarness.PlayRound(session, mode);
            Assert.AreEqual(7, first.StepsOfType<PrivateInfoStep>().Count(s => s.PhaseLabel == "Your role"));

            session.AdvanceRound();
            var second = ModeTestHarness.PlayRound(session, mode);
            Assert.AreEqual(0, second.StepsOfType<PrivateInfoStep>().Count(s => s.PhaseLabel == "Your role"));
        }

        [Test]
        public void TheInvestigatorClueNamesExactlyOneSuspect()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.SocialDeduction, 7, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 2));
            var log = ModeTestHarness.PlayRound(session, mode);

            var clue = log.StepsOfType<PrivateInfoStep>().FirstOrDefault(s => s.PhaseLabel == "Investigator clue");
            Assert.IsNotNull(clue, "The investigator should receive a clue.");

            var text = clue.Lines.First().Value;
            var suspects = session.Players.Where(p => p.RoleId == PlayerRoles.Imposter).ToList();
            var named = session.Players.Count(p => text.Contains(p.DisplayName));
            var namedSuspects = suspects.Count(p => text.Contains(p.DisplayName));

            Assert.AreEqual(2, named, "The clue should name exactly two players.");
            Assert.AreEqual(1, namedSuspects, "Exactly one of the two named players must be a suspect.");
        }

        [Test]
        public void AnAccusationRemovesSomebodyFromTheGame()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.SocialDeduction, 7, out var mode,
                s =>
                {
                    s.SetString(CommonSettingKeys.TieBehaviour, "random");
                    s.SetInt(CommonSettingKeys.Rounds, 4);
                });
            ModeTestHarness.PlayRound(session, mode);

            Assert.AreEqual(1, session.Players.Count(p => !p.IsAlive));
        }

        [Test]
        public void EliminatedPlayersStopVoting()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.SocialDeduction, 7, out var mode,
                s =>
                {
                    s.SetString(CommonSettingKeys.TieBehaviour, "random");
                    s.SetInt(CommonSettingKeys.Rounds, 4);
                });
            ModeTestHarness.PlayRound(session, mode);
            session.AdvanceRound();
            var second = ModeTestHarness.PlayRound(session, mode);

            var ballots = second.StepsOfType<ChoiceStep>().Where(s => s.PhaseLabel == "Accusation").ToList();
            Assert.AreEqual(6, ballots.Count, "Only living players vote.");

            var eliminated = session.Players.First(p => !p.IsAlive);
            Assert.IsFalse(ballots.Any(b => b.ActorPlayerId == eliminated.Id));
        }

        [Test]
        public void NobodyScoresAndNoLeaderboardIsShown()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.SocialDeduction, 7, out var mode,
                s =>
                {
                    s.SetString(CommonSettingKeys.TieBehaviour, "random");
                    s.SetInt(CommonSettingKeys.Rounds, 3);
                });
            var log = ModeTestHarness.PlayWholeGame(session, mode);

            Assert.IsFalse(mode.UsesScoring, "The Suspects is won or lost, not scored.");
            Assert.IsEmpty(log.StepsOfType<ScoreboardStep>(),
                "A scoreboard would show a surviving suspect collecting points.");
            foreach (var player in session.Players)
                Assert.AreEqual(0, session.Scores.GetScore(player.Id),
                    player.DisplayName + " scored in a mode that has no points.");
        }

        [Test]
        public void TheGameEndsWhenOneSideWins()
        {
            var session = ModeTestHarness.CreateSession(GameModeId.SocialDeduction, 5, out var mode,
                s =>
                {
                    s.SetString(CommonSettingKeys.TieBehaviour, "random");
                    s.SetInt(CommonSettingKeys.ImposterCount, 1);
                    s.SetInt(CommonSettingKeys.Rounds, 4);
                });
            var log = ModeTestHarness.PlayWholeGame(session, mode);

            Assert.IsTrue(log.Rounds.Last().GameOver);
        }

        [Test]
        public void TooManySpecialRolesIsRejected()
        {
            var content = ModeTestHarness.LoadContent();
            var mode = GameModeFactory.Create(GameModeId.SocialDeduction);

            var roster = new PlayerRoster();
            for (var i = 0; i < 3; i++) roster.Add("P" + i);

            var settings = new GameSettings();
            settings.Declare(mode.GetSettingDefinitions(content));
            settings.SetInt(CommonSettingKeys.ImposterCount, 1);

            var session = new GameSession(roster, new Core.Services.ScoreManager(),
                new Core.Services.SystemRandomProvider(1), content,
                content.GetMode(GameModeId.SocialDeduction), settings);

            Assert.IsFalse(mode.Validate(session).IsValid);
        }
    }

    public class EveryModeTests
    {
        private static readonly GameModeId[] AllModes =
        {
            GameModeId.DifferentWord, GameModeId.Fib, GameModeId.Wavelength,
            GameModeId.DevilsAdvocate, GameModeId.SocialDeduction
        };

        [Test]
        public void EveryModeExceptTheSuspectsPlaysWithThreePeople(
            [ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var content = ModeTestHarness.LoadContent();
            var definition = content.GetMode(modeId);

            if (modeId == GameModeId.SocialDeduction)
            {
                Assert.Greater(definition.MinPlayers, 3,
                    "The Suspects needs room to hide special roles, so it keeps a higher minimum.");
                return;
            }

            Assert.AreEqual(3, definition.MinPlayers, modeId + " should be playable with three.");

            var session = ModeTestHarness.CreateSession(modeId, 3, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            Assert.IsTrue(log.Steps.Any(s => s.Kind == StepKind.Reveal),
                modeId + " did not finish a round with three players.");
        }

        [Test]
        public void ASuggestedSizeNeverBlocksASmallerTable(
            [ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var definition = ModeTestHarness.LoadContent().GetMode(modeId);

            Assert.GreaterOrEqual(definition.RecommendedPlayers, definition.MinPlayers);
            Assert.IsTrue(definition.SupportsPlayerCount(definition.MinPlayers),
                modeId + " refuses its own minimum.");
            Assert.IsTrue(definition.IsBelowRecommended(definition.MinPlayers) || !definition.HasRecommendation,
                modeId + " should flag its minimum as below the suggested size.");
        }

        [Test]
        public void EveryModeHasContentAndADefinition([ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var content = ModeTestHarness.LoadContent();
            Assert.IsNotNull(content.GetMode(modeId), "Missing definition asset for " + modeId);
            Assert.Greater(content.PacksFor(modeId).Count, 0, "No content packs for " + modeId);
        }

        [Test]
        public void EveryModePlaysARoundWithSixPlayers([ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var session = ModeTestHarness.CreateSession(modeId, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            Assert.Greater(log.Steps.Count, 3);
            Assert.IsTrue(log.Steps.Any(s => s.Kind == StepKind.Reveal), modeId + " never revealed anything.");
        }

        [Test]
        public void EveryModeOffersRulesText([ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var content = ModeTestHarness.LoadContent();
            var mode = GameModeFactory.Create(modeId);
            var settings = new GameSettings();
            settings.Declare(mode.GetSettingDefinitions(content));

            var rules = mode.BuildRulesSummary(settings);
            Assert.IsNotNull(rules);
            Assert.Greater(rules.Count, 2, modeId + " needs a usable how-to-play summary.");
        }

        [Test]
        public void EveryModeWorksAcrossItsWholePlayerRange([ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var content = ModeTestHarness.LoadContent();
            var definition = content.GetMode(modeId);

            for (var players = definition.MinPlayers; players <= definition.MaxPlayers; players++)
            {
                var session = ModeTestHarness.CreateSession(modeId, players, out var mode,
                    s => s.SetInt(CommonSettingKeys.Rounds, 1));
                var log = ModeTestHarness.PlayRound(session, mode);
                Assert.Greater(log.Steps.Count, 3, modeId + " failed with " + players + " players.");
            }
        }

        [Test]
        public void NoPrivateStepIsEverAddressedToTheWholeTable(
            [ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var session = ModeTestHarness.CreateSession(modeId, 7, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 1));
            var log = ModeTestHarness.PlayRound(session, mode);

            foreach (var step in log.PrivateSteps)
                Assert.IsTrue(step.ActorPlayerId.HasValue, modeId + " produced an unowned private step.");
        }

        [Test]
        public void ClearingARoundLeavesNoSecretsBehind([ValueSource(nameof(AllModes))] GameModeId modeId)
        {
            var session = ModeTestHarness.CreateSession(modeId, 6, out var mode,
                s => s.SetInt(CommonSettingKeys.Rounds, 2));

            ModeTestHarness.PlayRound(session, mode);

            var keys = new[]
            {
                RoundDataKeys.Word, RoundDataKeys.Number, RoundDataKeys.WrittenAnswer,
                RoundDataKeys.Stance, RoundDataKeys.Instruction, RoundDataKeys.Clue,
                RoundDataKeys.Guess, RoundDataKeys.Vote, RoundDataKeys.Hint
            };

            Assert.IsTrue(session.Players.Any(p => keys.Any(p.HasRoundData)),
                modeId + " never recorded any round state to begin with.");

            session.ClearRoundData();

            foreach (var player in session.Players)
            {
                Assert.AreEqual(PlayerRoles.None, player.RoleId, player.DisplayName + " kept a role.");
                foreach (var key in keys)
                    Assert.IsFalse(player.HasRoundData(key),
                        player.DisplayName + " kept secret data under " + key + ".");
            }
        }
    }
}
