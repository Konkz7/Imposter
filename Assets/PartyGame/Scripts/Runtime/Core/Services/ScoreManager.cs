using System.Collections.Generic;
using System.Linq;

namespace PartyGame.Core.Services
{
    /// <summary>A single scoring event, kept so reveal screens can explain where points came from.</summary>
    public readonly struct ScoreAward
    {
        public int PlayerId { get; }
        public int Amount { get; }
        public string Reason { get; }

        public ScoreAward(int playerId, int amount, string reason)
        {
            PlayerId = playerId;
            Amount = amount;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct ScoreEntry
    {
        public int PlayerId { get; }
        public int Score { get; }
        public int Rank { get; }

        public ScoreEntry(int playerId, int score, int rank)
        {
            PlayerId = playerId;
            Score = score;
            Rank = rank;
        }
    }

    /// <summary>
    /// Generic scoring used by every mode. Modes decide the amounts, this only stores them.
    /// </summary>
    public class ScoreManager
    {
        private readonly Dictionary<int, int> _scores = new Dictionary<int, int>();
        private readonly List<ScoreAward> _roundAwards = new List<ScoreAward>();

        public IReadOnlyList<ScoreAward> RoundAwards => _roundAwards;

        public void Register(int playerId)
        {
            if (!_scores.ContainsKey(playerId)) _scores[playerId] = 0;
        }

        public void RegisterAll(IEnumerable<int> playerIds)
        {
            foreach (var id in playerIds) Register(id);
        }

        public void AddPoints(int playerId, int amount, string reason = null)
        {
            if (amount == 0) return;
            Register(playerId);
            _scores[playerId] += amount;
            _roundAwards.Add(new ScoreAward(playerId, amount, reason));
        }

        public void RemovePoints(int playerId, int amount, string reason = null)
        {
            AddPoints(playerId, -amount, reason);
        }

        public int GetScore(int playerId)
        {
            return _scores.TryGetValue(playerId, out var score) ? score : 0;
        }

        /// <summary>Clears the per round award log. Totals are kept.</summary>
        public void BeginRound()
        {
            _roundAwards.Clear();
        }

        public IReadOnlyList<ScoreAward> AwardsFor(int playerId)
        {
            return _roundAwards.Where(a => a.PlayerId == playerId).ToList();
        }

        public int RoundTotalFor(int playerId)
        {
            return _roundAwards.Where(a => a.PlayerId == playerId).Sum(a => a.Amount);
        }

        /// <summary>Descending by score. Equal scores share a rank.</summary>
        public IReadOnlyList<ScoreEntry> GetLeaderboard()
        {
            var ordered = _scores.OrderByDescending(kvp => kvp.Value).ToList();
            var result = new List<ScoreEntry>(ordered.Count);
            var rank = 0;
            var lastScore = int.MinValue;
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].Value != lastScore)
                {
                    rank = i + 1;
                    lastScore = ordered[i].Value;
                }
                result.Add(new ScoreEntry(ordered[i].Key, ordered[i].Value, rank));
            }
            return result;
        }

        public void ResetScores()
        {
            var keys = _scores.Keys.ToList();
            foreach (var key in keys) _scores[key] = 0;
            _roundAwards.Clear();
        }

        public void Clear()
        {
            _scores.Clear();
            _roundAwards.Clear();
        }
    }
}
