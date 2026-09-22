using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Services;
using PartyGame.Core.Session;

namespace PartyGame.Games.SocialDeduction
{
    /// <summary>
    /// What the table did last round, kept by the mode so the witness has something real to
    /// have noticed. Only public events go in here: every one of them was on the reveal screen
    /// that everybody just read, so a clue built from this can be checked rather than believed.
    /// </summary>
    public class VoteMemory
    {
        /// <summary>Who each player voted for last round.</summary>
        public Dictionary<int, int> Votes { get; } = new Dictionary<int, int>();

        /// <summary>Who the group removed last round, or <see cref="VoteOutcome.NoWinner"/>.</summary>
        public int AccusedId { get; set; } = VoteOutcome.NoWinner;

        /// <summary>Everyone who has been voted for at least once, across the whole game.</summary>
        public HashSet<int> EverVotedFor { get; } = new HashSet<int>();

        public bool HasVotes => Votes.Count > 0;

        public void Clear()
        {
            Votes.Clear();
            EverVotedFor.Clear();
            AccusedId = VoteOutcome.NoWinner;
        }

        /// <summary>Wipes last round's ballot but keeps what the whole game has shown.</summary>
        private void ClearRound()
        {
            Votes.Clear();
            AccusedId = VoteOutcome.NoWinner;
        }

        public void Record(IEnumerable<int> voters, System.Func<int, int> voteOf, int accusedId)
        {
            ClearRound();
            AccusedId = accusedId;
            if (voters == null || voteOf == null) return;
            foreach (var voter in voters)
            {
                var target = voteOf(voter);
                if (target == VoteOutcome.NoWinner) continue;
                Votes[voter] = target;
                EverVotedFor.Add(target);
            }
        }

        public bool TryVoteOf(int playerId, out int target) => Votes.TryGetValue(playerId, out target);

        public IEnumerable<int> VotersFor(int candidateId)
        {
            return Votes.Where(pair => pair.Value == candidateId).Select(pair => pair.Key);
        }
    }

    /// <summary>
    /// A witness statement and, crucially, every player it is true of. The mode shows the text;
    /// the candidate set is what makes the guarantees testable - a fact is only ever used when
    /// it fits at least two people, so the witness narrows the table without ending the game.
    /// </summary>
    public class WitnessFact
    {
        public string Text { get; }
        public IReadOnlyList<int> Candidates { get; }

        public WitnessFact(string text, IReadOnlyList<int> candidates)
        {
            Text = text;
            Candidates = candidates;
        }
    }

    /// <summary>
    /// Builds the factual half of a clue. Flavour text is authored in a <see cref="CluePackData"/>,
    /// the fact itself is generated here from live round state and is always true - a clue can
    /// never contradict what actually happened.
    ///
    /// Witness facts are drawn only from things the table has seen: who voted for whom last
    /// round, and who the group removed. Nothing here describes the room the players are
    /// actually sitting in, because the app cannot see it.
    /// </summary>
    public class SocialClueFactory
    {
        private readonly IRandomProvider _random;

        public SocialClueFactory(IRandomProvider random)
        {
            _random = random;
        }

        /// <summary>Exactly one of the two named players is a suspect. Always true by construction.</summary>
        public string BuildInvestigatorPair(ClueTemplate template, IReadOnlyList<PlayerState> suspects,
            IReadOnlyList<PlayerState> innocents)
        {
            if (suspects.Count == 0 || innocents.Count == 0) return null;

            var suspect = Shuffler.Pick(suspects, _random);
            var innocent = Shuffler.Pick(innocents, _random);

            var names = new List<string> { suspect.DisplayName, innocent.DisplayName };
            Shuffler.ShuffleInPlace(names, _random);

            var text = template != null && template.IsValid
                ? template.Text
                : "Exactly one of {a} and {b} is involved.";
            return text.Replace("{a}", names[0]).Replace("{b}", names[1]);
        }

        /// <summary>A true, non-identifying observation about one suspect.</summary>
        public string BuildWitnessDetail(ClueTemplate template, IReadOnlyList<PlayerState> suspects,
            IReadOnlyList<PlayerState> everyone, IReadOnlyList<PlayerState> allPlayers, VoteMemory memory)
        {
            var fact = BuildWitnessFact(suspects, everyone, allPlayers, memory);
            if (fact == null) return null;

            var text = template != null && template.IsValid
                ? template.Text
                : "You have been watching the room. {fact}";
            return text.Replace("{fact}", fact.Text);
        }

        /// <summary>
        /// The statement itself, with the players it fits. Exposed so the invariants - true of a
        /// suspect, fits at least two people - can be asserted rather than hoped for.
        /// </summary>
        public WitnessFact BuildWitnessFact(IReadOnlyList<PlayerState> suspects,
            IReadOnlyList<PlayerState> everyone, IReadOnlyList<PlayerState> allPlayers, VoteMemory memory)
        {
            if (suspects == null || suspects.Count == 0 || everyone == null || everyone.Count == 0) return null;

            var suspect = Shuffler.Pick(suspects, _random);
            var facts = BuildBehaviourFacts(suspect, everyone, allPlayers ?? everyone, memory);

            // Round one, or a round nobody voted in: there is no behaviour to have noticed yet,
            // so the witness gets a weaker version of the investigator's pair instead.
            if (facts.Count == 0)
            {
                var pair = BuildPairFact(suspect, everyone);
                if (pair != null) facts.Add(pair);
            }

            return facts.Count == 0 ? null : facts[_random.Range(0, facts.Count)];
        }

        /// <summary>A true but weak statement the whole table hears.</summary>
        public string BuildAnonymousHint(ClueTemplate template, IReadOnlyList<PlayerState> suspects,
            IReadOnlyList<PlayerState> everyone)
        {
            if (suspects.Count == 0 || everyone.Count == 0) return null;

            var suspect = Shuffler.Pick(suspects, _random);
            var others = everyone.Where(p => p.Id != suspect.Id).ToList();
            var groupSize = System.Math.Min(3, everyone.Count);
            var group = new List<PlayerState> { suspect };
            group.AddRange(Shuffler.PickDistinct(others, groupSize - 1, _random));
            Shuffler.ShuffleInPlace(group, _random);

            var fact = "At least one suspect is among " + JoinNames(group.Select(p => p.DisplayName).ToList()) + ".";

            var text = template != null && template.IsValid ? template.Text : "{fact}";
            return text.Replace("{fact}", fact);
        }

        /// <summary>
        /// Everything the witness can have noticed, drawn from last round's ballot. Each fact
        /// carries the group it is true of and is only offered when that group holds at least
        /// two people and stops short of the whole table - otherwise it either names the suspect
        /// outright or says nothing at all.
        /// </summary>
        private List<WitnessFact> BuildBehaviourFacts(PlayerState suspect, IReadOnlyList<PlayerState> everyone,
            IReadOnlyList<PlayerState> allPlayers, VoteMemory memory)
        {
            var facts = new List<WitnessFact>();
            if (memory == null || !memory.HasVotes) return facts;

            var livingIds = everyone.Select(p => p.Id).ToHashSet();

            // Names come from the whole roster: last round's accused has left the game but is
            // still the most memorable thing that happened, so the clue can name them.
            var names = allPlayers.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First().DisplayName);

            // Who a player sided with, when that was not the name the group settled on - the
            // accusation itself gets its own fact below, and saying both is saying one twice.
            if (memory.TryVoteOf(suspect.Id, out var target) && target != memory.AccusedId)
            {
                var together = memory.VotersFor(target).Where(livingIds.Contains).ToList();
                if (Usable(together, everyone.Count))
                {
                    var targetName = names.TryGetValue(target, out var n) ? n : "the accused";
                    facts.Add(new WitnessFact(
                        "They were one of the " + together.Count + " people who voted for " + targetName +
                        " last round.", together));
                }
            }

            // Whether a player backed the accusation that actually removed somebody.
            if (memory.AccusedId != VoteOutcome.NoWinner)
            {
                var backed = memory.VotersFor(memory.AccusedId).Where(livingIds.Contains).ToList();
                var accusedName = names.TryGetValue(memory.AccusedId, out var accused) ? accused : null;

                if (!string.IsNullOrEmpty(accusedName))
                {
                    var votedForAccused = backed.Contains(suspect.Id);
                    var group = votedForAccused
                        ? backed
                        : memory.Votes.Keys.Where(id => livingIds.Contains(id) && !backed.Contains(id)).ToList();

                    if (Usable(group, everyone.Count))
                    {
                        facts.Add(new WitnessFact(votedForAccused
                            ? "They voted to remove " + accusedName + " last round."
                            : "They did not vote to remove " + accusedName + " last round.", group));
                    }
                }
            }

            // Whether the group already had doubts about them.
            var received = memory.VotersFor(suspect.Id).Count();
            var alsoAccused = everyone
                .Where(p => received > 0 ? memory.VotersFor(p.Id).Any() : !memory.VotersFor(p.Id).Any())
                .Select(p => p.Id)
                .ToList();
            if (Usable(alsoAccused, everyone.Count))
            {
                facts.Add(new WitnessFact(received > 0
                    ? "Somebody voted for them last round."
                    : "Nobody voted for them last round.", alsoAccused));
            }

            // Survives a unanimous ballot, where every vote-based fact above covers the whole
            // table and therefore says nothing.
            var everSuspected = memory.EverVotedFor.Contains(suspect.Id);
            var sameHistory = everyone
                .Where(p => memory.EverVotedFor.Contains(p.Id) == everSuspected)
                .Select(p => p.Id)
                .ToList();
            if (Usable(sameHistory, everyone.Count))
            {
                facts.Add(new WitnessFact(everSuspected
                    ? "Somebody has voted for them at some point in this game."
                    : "Nobody has voted for them at any point in this game.", sameHistory));
            }

            return facts;
        }

        /// <summary>
        /// The round one statement: two names, at least one of them involved. Deliberately
        /// weaker than the investigator's "exactly one", and it fits both named players.
        /// </summary>
        private WitnessFact BuildPairFact(PlayerState suspect, IReadOnlyList<PlayerState> everyone)
        {
            var others = everyone.Where(p => p.Id != suspect.Id).ToList();
            if (others.Count == 0) return null;

            var partner = Shuffler.Pick(others, _random);
            var pair = new List<PlayerState> { suspect, partner };
            Shuffler.ShuffleInPlace(pair, _random);

            return new WitnessFact(
                "At least one of " + pair[0].DisplayName + " and " + pair[1].DisplayName + " is involved.",
                pair.Select(p => p.Id).ToList());
        }

        /// <summary>A fact must fit more than one player, and must not fit everybody.</summary>
        private static bool Usable(IReadOnlyCollection<int> group, int tableSize)
        {
            return group.Count >= 2 && group.Count < tableSize;
        }

        private static string JoinNames(IReadOnlyList<string> names)
        {
            if (names.Count == 0) return "nobody";
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
        }
    }
}
