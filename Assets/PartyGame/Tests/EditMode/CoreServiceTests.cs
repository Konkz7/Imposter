using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PartyGame.Core.Modes;
using PartyGame.Core.Monetisation;
using PartyGame.Core.Persistence;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Tests
{
    public class ShufflerTests
    {
        [Test]
        public void ShuffleKeepsEveryElement()
        {
            var source = Enumerable.Range(0, 20).ToList();
            var shuffled = Shuffler.Shuffled(source, new SystemRandomProvider(1234));

            Assert.AreEqual(source.Count, shuffled.Count);
            CollectionAssert.AreEquivalent(source, shuffled);
        }

        [Test]
        public void ShuffleActuallyReordersLargeLists()
        {
            var source = Enumerable.Range(0, 50).ToList();
            var shuffled = Shuffler.Shuffled(source, new SystemRandomProvider(99));

            var samePositions = source.Where((value, index) => shuffled[index] == value).Count();
            Assert.Less(samePositions, source.Count / 2, "A shuffle should not leave most items in place.");
        }

        [Test]
        public void PickDistinctNeverRepeatsAndRespectsCount()
        {
            var source = Enumerable.Range(0, 10).ToList();
            var picked = Shuffler.PickDistinct(source, 4, new SystemRandomProvider(7));

            Assert.AreEqual(4, picked.Count);
            Assert.AreEqual(picked.Count, picked.Distinct().Count());
        }

        [Test]
        public void PickDistinctClampsToPoolSize()
        {
            var source = new List<int> { 1, 2 };
            var picked = Shuffler.PickDistinct(source, 9, new SystemRandomProvider(3));
            Assert.AreEqual(2, picked.Count);
        }

        [Test]
        public void EveryPositionIsReachableOverManyShuffles()
        {
            // Guards against an off-by-one in Fisher-Yates that would pin the first element.
            var random = new SystemRandomProvider(555);
            var seenFirst = new HashSet<int>();
            for (var i = 0; i < 200; i++)
                seenFirst.Add(Shuffler.Shuffled(Enumerable.Range(0, 5).ToList(), random)[0]);

            CollectionAssert.AreEquivalent(Enumerable.Range(0, 5), seenFirst);
        }
    }

    public class ScoreManagerTests
    {
        [Test]
        public void AddsAndReadsBackPoints()
        {
            var scores = new ScoreManager();
            scores.AddPoints(1, 100, "test");
            scores.AddPoints(1, 50, "test");
            Assert.AreEqual(150, scores.GetScore(1));
        }

        [Test]
        public void UnknownPlayerScoresZero()
        {
            Assert.AreEqual(0, new ScoreManager().GetScore(42));
        }

        [Test]
        public void LeaderboardOrdersDescendingAndSharesRanksOnTies()
        {
            var scores = new ScoreManager();
            scores.AddPoints(1, 100);
            scores.AddPoints(2, 300);
            scores.AddPoints(3, 100);
            scores.Register(4);

            var board = scores.GetLeaderboard();

            Assert.AreEqual(2, board[0].PlayerId);
            Assert.AreEqual(1, board[0].Rank);
            Assert.AreEqual(2, board[1].Rank);
            Assert.AreEqual(2, board[2].Rank);
            Assert.AreEqual(4, board[3].Rank, "A tie should consume the ranks it spans.");
            Assert.AreEqual(0, board[3].Score);
        }

        [Test]
        public void RoundTotalsOnlyCountTheCurrentRound()
        {
            var scores = new ScoreManager();
            scores.BeginRound();
            scores.AddPoints(1, 100);
            scores.BeginRound();
            scores.AddPoints(1, 40);

            Assert.AreEqual(140, scores.GetScore(1));
            Assert.AreEqual(40, scores.RoundTotalFor(1));
        }

        [Test]
        public void ResetKeepsPlayersButZeroesScores()
        {
            var scores = new ScoreManager();
            scores.AddPoints(1, 100);
            scores.ResetScores();

            Assert.AreEqual(0, scores.GetScore(1));
            Assert.AreEqual(1, scores.GetLeaderboard().Count);
        }
    }

    public class VotingManagerTests
    {
        private static VotingManager Build(TieResolution tie = TieResolution.NoResult, bool allowSelf = false)
        {
            var ids = new[] { 1, 2, 3, 4 };
            return new VotingManager(ids, ids, allowSelf, tie);
        }

        [Test]
        public void SelfVotesAreRejectedByDefault()
        {
            var voting = Build();
            Assert.IsFalse(voting.CastVote(1, 1));
            Assert.IsFalse(voting.HasVoted(1));
        }

        [Test]
        public void SelfVotesAreAllowedWhenConfigured()
        {
            var voting = Build(allowSelf: true);
            Assert.IsTrue(voting.CastVote(1, 1));
        }

        [Test]
        public void CandidateListExcludesTheVoter()
        {
            var voting = Build();
            CollectionAssert.DoesNotContain(voting.CandidatesFor(2), 2);
            Assert.AreEqual(3, voting.CandidatesFor(2).Count);
        }

        [Test]
        public void ClearMajorityWins()
        {
            var voting = Build();
            voting.CastVote(1, 3);
            voting.CastVote(2, 3);
            voting.CastVote(3, 1);
            voting.CastVote(4, 3);

            var outcome = voting.Resolve(new SystemRandomProvider(1));
            Assert.IsTrue(outcome.HasWinner);
            Assert.AreEqual(3, outcome.WinnerId);
            Assert.IsFalse(outcome.WasTie);
        }

        [Test]
        public void TieWithNoResultLeavesNobodyAccused()
        {
            var voting = Build();
            voting.CastVote(1, 2);
            voting.CastVote(2, 1);

            var outcome = voting.Resolve(new SystemRandomProvider(1));
            Assert.IsTrue(outcome.WasTie);
            Assert.IsFalse(outcome.HasWinner);
            Assert.IsFalse(outcome.NeedsRevote);
        }

        [Test]
        public void TieWithRevoteAsksForAnotherBallot()
        {
            var voting = Build(TieResolution.Revote);
            voting.CastVote(1, 2);
            voting.CastVote(2, 1);

            var outcome = voting.Resolve(new SystemRandomProvider(1));
            Assert.IsTrue(outcome.NeedsRevote);
            Assert.AreEqual(2, outcome.TopCandidates.Count);
        }

        [Test]
        public void TieWithRandomAlwaysPicksOneOfTheTiedCandidates()
        {
            var voting = Build(TieResolution.Random);
            voting.CastVote(1, 2);
            voting.CastVote(2, 1);

            var outcome = voting.Resolve(new SystemRandomProvider(5));
            Assert.IsTrue(outcome.HasWinner);
            CollectionAssert.Contains(outcome.TopCandidates, outcome.WinnerId);
        }

        [Test]
        public void NoVotesMeansNoWinner()
        {
            var outcome = Build().Resolve(new SystemRandomProvider(1));
            Assert.IsFalse(outcome.HasWinner);
            Assert.IsFalse(outcome.WasTie);
        }

        [Test]
        public void RevoteIsLimitedToTheTiedCandidates()
        {
            var voting = Build(TieResolution.Revote);
            var revote = voting.CreateRevote(new List<int> { 1, 2 });

            Assert.AreEqual(2, revote.Candidates.Count);
            Assert.IsFalse(revote.CastVote(3, 4));
        }

        [Test]
        public void VotesStayHiddenUntilResolved()
        {
            var voting = Build();
            voting.CastVote(1, 3);
            Assert.AreEqual(3, voting.VoteOf(1));
            Assert.AreEqual(VoteOutcome.NoWinner, voting.VoteOf(2));
            Assert.IsFalse(voting.AllVotesIn);
        }
    }

    public class GameTimerTests
    {
        [Test]
        public void CountsDownAndCompletesOnce()
        {
            var timer = new GameTimer();
            var completions = 0;
            timer.Completed += () => completions++;

            timer.Start(2f);
            timer.Tick(1f);
            Assert.IsFalse(timer.IsComplete);

            timer.Tick(1.5f);
            Assert.IsTrue(timer.IsComplete);
            Assert.AreEqual(0f, timer.Remaining);

            timer.Tick(1f);
            Assert.AreEqual(1, completions, "Completion must fire exactly once.");
        }

        [Test]
        public void PauseStopsTheClock()
        {
            var timer = new GameTimer();
            timer.Start(10f);
            timer.Pause();
            timer.Tick(5f);
            Assert.AreEqual(10f, timer.Remaining);

            timer.Resume();
            timer.Tick(4f);
            Assert.AreEqual(6f, timer.Remaining, 0.001f);
        }

        [Test]
        public void ForceCompleteFinishesImmediately()
        {
            var timer = new GameTimer();
            var completions = 0;
            timer.Completed += () => completions++;

            timer.Start(30f);
            timer.ForceComplete();

            Assert.IsTrue(timer.IsComplete);
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void ZeroDurationCompletesStraightAway()
        {
            var timer = new GameTimer();
            timer.Start(0f);
            Assert.IsTrue(timer.IsComplete);
        }

        [Test]
        public void FormatsAsMinutesAndSeconds()
        {
            Assert.AreEqual("9", GameTimer.Format(8.4f));
            Assert.AreEqual("1:00", GameTimer.Format(60f));
            Assert.AreEqual("2:05", GameTimer.Format(124.2f));
        }
    }

    public class ContentRotationTests
    {
        [Test]
        public void UsesEveryEntryBeforeRepeating()
        {
            var pool = new List<string> { "a", "b", "c", "d" };
            var rotation = new ContentRotation<string>(s => s);
            var random = new SystemRandomProvider(11);

            var picks = Enumerable.Range(0, 4).Select(_ => rotation.Next(pool, random)).ToList();
            CollectionAssert.AreEquivalent(pool, picks);
        }

        [Test]
        public void RecyclesWithoutRepeatingTheMostRecentPick()
        {
            var pool = new List<string> { "a", "b", "c" };
            var rotation = new ContentRotation<string>(s => s, 1);
            var random = new SystemRandomProvider(4);

            string previous = null;
            for (var i = 0; i < 20; i++)
            {
                var next = rotation.Next(pool, random);
                Assert.AreNotEqual(previous, next, "The same entry came up twice in a row.");
                previous = next;
            }
        }

        [Test]
        public void EmptyPoolReturnsNothingRatherThanThrowing()
        {
            var rotation = new ContentRotation<string>(s => s);
            Assert.IsNull(rotation.Next(new List<string>(), new SystemRandomProvider(1)));
        }
    }

    public class PlayerRosterTests
    {
        private static PlayerRoster WithPlayers(params string[] names)
        {
            var roster = new PlayerRoster();
            foreach (var name in names) roster.Add(name);
            return roster;
        }

        [Test]
        public void BlankNamesBecomeNumberedPlaceholders()
        {
            var roster = WithPlayers(null, "  ");
            Assert.AreEqual("Player 1", roster.Players[0].DisplayName);
            Assert.AreEqual("Player 2", roster.Players[1].DisplayName);
        }

        [Test]
        public void RejectsDuplicateNames()
        {
            var roster = WithPlayers("Sam", "sam", "Alex", "Jo");
            var result = roster.Validate(4, 12);
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("more than once", result.Message);
        }

        [Test]
        public void RejectsTooFewAndTooManyPlayers()
        {
            Assert.IsFalse(WithPlayers("A", "B").Validate(4, 12).IsValid);
            var many = new PlayerRoster();
            for (var i = 0; i < 12; i++) many.Add("P" + i);
            Assert.IsFalse(many.Validate(4, 8).IsValid);
        }

        [Test]
        public void AcceptsAValidRoster()
        {
            Assert.IsTrue(WithPlayers("Sam", "Alex", "Jo", "Kim").Validate(4, 12).IsValid);
        }

        [Test]
        public void SeatIndicesStayContiguousAfterRemoval()
        {
            var roster = WithPlayers("A", "B", "C", "D");
            roster.Remove(roster.Players[1].Id);

            for (var i = 0; i < roster.Count; i++)
                Assert.AreEqual(i, roster.Players[i].SeatIndex);
        }

        [Test]
        public void ResetClearsScoresRolesAndRoundData()
        {
            var roster = WithPlayers("A", "B", "C", "D");
            roster.Players[0].Score = 500;
            roster.Players[0].IsAlive = false;
            roster.Players[0].RoleId = PlayerRoles.Imposter;
            roster.Players[0].SetRoundData(RoundDataKeys.Word, "Pizza");

            roster.ResetForNewGame();

            Assert.AreEqual(0, roster.Players[0].Score);
            Assert.IsTrue(roster.Players[0].IsAlive);
            Assert.AreEqual(PlayerRoles.None, roster.Players[0].RoleId);
            Assert.AreEqual(string.Empty, roster.Players[0].GetRoundData(RoundDataKeys.Word));
        }

        [Test]
        public void RespectsTheHardMaximum()
        {
            var roster = new PlayerRoster();
            for (var i = 0; i < PlayerRoster.AbsoluteMaxPlayers + 3; i++) roster.Add("P" + i);
            Assert.AreEqual(PlayerRoster.AbsoluteMaxPlayers, roster.Count);
        }
    }

    public class GameSettingsTests
    {
        private static GameSettings Build()
        {
            var settings = new GameSettings();
            var imposters = SettingDefinition.Stepper(CommonSettingKeys.ImposterCount, "Imposters", 1, 1, 4);
            imposters.MaxForPlayerCount = players => System.Math.Max(1, (players - 1) / 2);
            settings.Declare(new[]
            {
                imposters,
                SettingDefinition.Toggle(CommonSettingKeys.VotingEnabled, "Voting", true),
                SettingDefinition.Stepper(CommonSettingKeys.Rounds, "Rounds", 3, 1, 10)
            });
            return settings;
        }

        [Test]
        public void DefaultsAreAppliedOnDeclaration()
        {
            var settings = Build();
            Assert.AreEqual(1, settings.GetInt(CommonSettingKeys.ImposterCount));
            Assert.IsTrue(settings.GetBool(CommonSettingKeys.VotingEnabled));
            Assert.AreEqual(3, settings.GetInt(CommonSettingKeys.Rounds));
        }

        [Test]
        public void ClampingRespectsThePlayerCount()
        {
            var settings = Build();
            settings.SetInt(CommonSettingKeys.ImposterCount, 4);
            settings.ClampAll(5);

            Assert.AreEqual(2, settings.GetInt(CommonSettingKeys.ImposterCount),
                "Five players allow at most two imposters.");
        }

        [Test]
        public void ListsRoundTrip()
        {
            var settings = Build();
            settings.SetList(CommonSettingKeys.Categories, new[] { "food", "animals" });
            CollectionAssert.AreEqual(new[] { "food", "animals" }, settings.GetList(CommonSettingKeys.Categories));
        }

        [Test]
        public void EmptyListReadsBackAsEmpty()
        {
            var settings = Build();
            settings.SetList(CommonSettingKeys.Categories, new string[0]);
            Assert.AreEqual(0, settings.GetList(CommonSettingKeys.Categories).Count);
        }

        [Test]
        public void SettingsSurviveAStoreRoundTrip()
        {
            var store = new InMemoryStore();
            var written = Build();
            written.SetInt(CommonSettingKeys.Rounds, 7);
            written.SetBool(CommonSettingKeys.VotingEnabled, false);
            new ModeSettingsStore(store).Save(GameModeId.DifferentWord, written);

            var mode = new Games.DifferentWord.DifferentWordMode();
            var read = new ModeSettingsStore(store).Load(mode, new Core.Content.ContentService(null));

            Assert.AreEqual(7, read.GetInt(CommonSettingKeys.Rounds));
            Assert.IsFalse(read.GetBool(CommonSettingKeys.VotingEnabled));
        }
    }

    public class MonetisationTests
    {
        [Test]
        public void EntitlementsPersistThroughTheStore()
        {
            var store = new InMemoryStore();
            new EntitlementService(store).Grant(Entitlement.AdFree);

            Assert.IsTrue(new EntitlementService(store).Has(Entitlement.AdFree));
        }

        [Test]
        public void AdFreeBlocksInterstitials()
        {
            var entitlements = new EntitlementService(new InMemoryStore());
            var policy = new AdPolicy(entitlements) { WarmupGames = 0, CooldownSeconds = 0f };
            policy.NotifyGameCompleted();

            Assert.IsTrue(policy.CanShow(AdPlacement.AfterGame));

            entitlements.Grant(Entitlement.AdFree);
            Assert.IsFalse(policy.CanShow(AdPlacement.AfterGame));
        }

        [Test]
        public void PrivateInformationAlwaysBlocksAds()
        {
            var policy = new AdPolicy(new EntitlementService(new InMemoryStore()))
            {
                WarmupGames = 0,
                CooldownSeconds = 0f,
                PrivateInformationVisible = true
            };
            policy.NotifyGameCompleted();

            Assert.IsFalse(policy.CanShow(AdPlacement.AfterGame));
            Assert.IsFalse(policy.CanShow(AdPlacement.RewardedUnlock));
        }

        [Test]
        public void WarmupSuppressesTheFirstInterstitial()
        {
            var policy = new AdPolicy(new EntitlementService(new InMemoryStore()))
            {
                WarmupGames = 2,
                CooldownSeconds = 0f
            };

            policy.NotifyGameCompleted();
            Assert.IsFalse(policy.CanShow(AdPlacement.AfterGame));

            policy.NotifyGameCompleted();
            Assert.IsTrue(policy.CanShow(AdPlacement.AfterGame));
        }

        [Test]
        public void PurchasingRemoveAdsGrantsTheEntitlement()
        {
            var entitlements = new EntitlementService(new InMemoryStore());
            var coordinator = new PurchaseCoordinator(new StubStore(), entitlements);

            PurchaseResult captured = default;
            coordinator.Buy(StoreProducts.RemoveAds, result => captured = result);

            Assert.IsTrue(captured.Succeeded);
            Assert.IsTrue(entitlements.Has(Entitlement.AdFree));
        }

        [Test]
        public void NullPurchaseServiceReportsTheStoreAsUnavailable()
        {
            var entitlements = new EntitlementService(new InMemoryStore());
            var service = new NullPurchaseService(entitlements);

            PurchaseResult captured = default;
            service.Purchase(StoreProducts.RemoveAds, result => captured = result);

            Assert.IsFalse(service.IsStoreAvailable);
            Assert.AreEqual(PurchaseOutcome.Unavailable, captured.Outcome);
            Assert.IsFalse(entitlements.Has(Entitlement.AdFree));
        }

        [Test]
        public void NullAdServiceStillCallsBackSoFlowNeverStalls()
        {
            var policy = new AdPolicy(new EntitlementService(new InMemoryStore()));
            var service = new NullAdService(policy);

            var closed = false;
            service.ShowInterstitial(AdPlacement.AfterGame, () => closed = true);
            Assert.IsTrue(closed);

            var rewarded = true;
            service.ShowRewarded("bonus", earned => rewarded = earned);
            Assert.IsFalse(rewarded);
        }

        private class StubStore : IPurchaseService
        {
            public bool IsStoreAvailable => true;
            public string GetLocalisedPrice(string productId) => "2.99";

            public void Purchase(string productId, System.Action<PurchaseResult> onComplete)
            {
                onComplete(new PurchaseResult(PurchaseOutcome.Success, productId));
            }

            public void RestorePurchases(System.Action<bool> onComplete) => onComplete(true);
        }
    }

    public class TextUtilityTests
    {
        [Test]
        public void IgnoresCasePunctuationAndLeadingArticles()
        {
            Assert.IsTrue(TextUtility.Matches("The BackRub!", "backrub"));
            Assert.IsTrue(TextUtility.Matches("A  blue   whale", "Blue Whale"));
            Assert.IsFalse(TextUtility.Matches("Pizza", "Burger"));
        }

        [Test]
        public void EmptyStringsNeverMatch()
        {
            Assert.IsFalse(TextUtility.Matches("", ""));
            Assert.IsFalse(TextUtility.Matches("   ", "!!!"));
        }

        [Test]
        public void OrdinalsHandleTheAwkwardCases()
        {
            Assert.AreEqual("1st", TextUtility.Ordinal(1));
            Assert.AreEqual("2nd", TextUtility.Ordinal(2));
            Assert.AreEqual("3rd", TextUtility.Ordinal(3));
            Assert.AreEqual("11th", TextUtility.Ordinal(11));
            Assert.AreEqual("12th", TextUtility.Ordinal(12));
            Assert.AreEqual("21st", TextUtility.Ordinal(21));
        }
    }
}
