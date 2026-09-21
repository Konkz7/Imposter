using System.Collections.Generic;
using System.Linq;

namespace PartyGame.Core.Services
{
    /// <summary>How a mode wants ties handled. Chosen per mode, not hard coded in the voting rules.</summary>
    public enum TieResolution
    {
        /// <summary>Nobody is eliminated / accused.</summary>
        NoResult = 0,
        /// <summary>The caller should run the vote again between the tied candidates.</summary>
        Revote = 1,
        /// <summary>One of the tied candidates is chosen at random.</summary>
        Random = 2
    }

    public readonly struct VoteTally
    {
        public int CandidateId { get; }
        public int Votes { get; }

        public VoteTally(int candidateId, int votes)
        {
            CandidateId = candidateId;
            Votes = votes;
        }
    }

    public class VoteOutcome
    {
        public IReadOnlyList<VoteTally> Tallies { get; }
        public IReadOnlyList<int> TopCandidates { get; }
        public int WinnerId { get; }
        public bool HasWinner => WinnerId != NoWinner;
        public bool WasTie { get; }
        public bool NeedsRevote { get; }

        public const int NoWinner = -1;

        public VoteOutcome(IReadOnlyList<VoteTally> tallies, IReadOnlyList<int> topCandidates, int winnerId, bool wasTie, bool needsRevote)
        {
            Tallies = tallies;
            TopCandidates = topCandidates;
            WinnerId = winnerId;
            WasTie = wasTie;
            NeedsRevote = needsRevote;
        }
    }

    /// <summary>
    /// Reusable secret ballot. Votes stay hidden until <see cref="Resolve"/> is called,
    /// which is what lets every mode share one pass-and-play voting flow.
    /// </summary>
    public class VotingManager
    {
        private readonly List<int> _voters = new List<int>();
        private readonly List<int> _candidates = new List<int>();
        private readonly Dictionary<int, int> _votes = new Dictionary<int, int>();

        public bool AllowSelfVote { get; }
        public TieResolution TieBehaviour { get; }

        public IReadOnlyList<int> Voters => _voters;
        public IReadOnlyList<int> Candidates => _candidates;
        public IReadOnlyDictionary<int, int> Votes => _votes;

        public VotingManager(IEnumerable<int> voters, IEnumerable<int> candidates, bool allowSelfVote, TieResolution tieBehaviour)
        {
            if (voters != null) _voters.AddRange(voters);
            if (candidates != null) _candidates.AddRange(candidates);
            AllowSelfVote = allowSelfVote;
            TieBehaviour = tieBehaviour;
        }

        public bool CanVoteFor(int voterId, int candidateId)
        {
            if (!_voters.Contains(voterId)) return false;
            if (!_candidates.Contains(candidateId)) return false;
            if (!AllowSelfVote && voterId == candidateId) return false;
            return true;
        }

        public IReadOnlyList<int> CandidatesFor(int voterId)
        {
            return _candidates.Where(c => CanVoteFor(voterId, c)).ToList();
        }

        public bool CastVote(int voterId, int candidateId)
        {
            if (!CanVoteFor(voterId, candidateId)) return false;
            _votes[voterId] = candidateId;
            return true;
        }

        public bool HasVoted(int voterId) => _votes.ContainsKey(voterId);

        public bool AllVotesIn => _voters.All(HasVoted);

        public int VotesCast => _votes.Count;

        /// <summary>Who voted for a given candidate. Only call after the ballot closes.</summary>
        public IReadOnlyList<int> VotersFor(int candidateId)
        {
            return _votes.Where(kvp => kvp.Value == candidateId).Select(kvp => kvp.Key).ToList();
        }

        public int VoteOf(int voterId)
        {
            return _votes.TryGetValue(voterId, out var target) ? target : VoteOutcome.NoWinner;
        }

        public VoteOutcome Resolve(IRandomProvider random)
        {
            var tallies = _candidates
                .Select(c => new VoteTally(c, _votes.Count(kvp => kvp.Value == c)))
                .OrderByDescending(t => t.Votes)
                .ToList();

            if (tallies.Count == 0 || tallies[0].Votes == 0)
                return new VoteOutcome(tallies, new List<int>(), VoteOutcome.NoWinner, false, false);

            var best = tallies[0].Votes;
            var top = tallies.Where(t => t.Votes == best).Select(t => t.CandidateId).ToList();

            if (top.Count == 1)
                return new VoteOutcome(tallies, top, top[0], false, false);

            switch (TieBehaviour)
            {
                case TieResolution.Random:
                    var picked = top[random != null ? random.Range(0, top.Count) : 0];
                    return new VoteOutcome(tallies, top, picked, true, false);
                case TieResolution.Revote:
                    return new VoteOutcome(tallies, top, VoteOutcome.NoWinner, true, true);
                default:
                    return new VoteOutcome(tallies, top, VoteOutcome.NoWinner, true, false);
            }
        }

        /// <summary>Starts a fresh ballot, optionally narrowed to the tied candidates.</summary>
        public VotingManager CreateRevote(IReadOnlyList<int> candidates)
        {
            return new VotingManager(_voters, candidates ?? _candidates, AllowSelfVote, TieResolution.Random);
        }

        public void Clear()
        {
            _votes.Clear();
        }
    }
}
