using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Services;
using PartyGame.Core.Util;

namespace PartyGame.Core.Session
{
    /// <summary>
    /// Owns the list of participants for the whole app, so players carry over between games
    /// and every mode reads from one source of truth.
    /// </summary>
    public class PlayerRoster
    {
        public const int AbsoluteMinPlayers = 3;
        public const int AbsoluteMaxPlayers = 16;

        private readonly List<PlayerState> _players = new List<PlayerState>();
        private int _nextId = 1;

        public IReadOnlyList<PlayerState> Players => _players;
        public int Count => _players.Count;

        public event Action Changed;

        public PlayerState Add(string displayName)
        {
            if (_players.Count >= AbsoluteMaxPlayers) return null;
            var player = new PlayerState(_nextId++, SanitiseName(displayName, _players.Count + 1))
            {
                SeatIndex = _players.Count
            };
            _players.Add(player);
            ReindexSeats();
            Changed?.Invoke();
            return player;
        }

        public bool Remove(int playerId)
        {
            var index = _players.FindIndex(p => p.Id == playerId);
            if (index < 0) return false;
            _players.RemoveAt(index);
            ReindexSeats();
            Changed?.Invoke();
            return true;
        }

        public void Rename(int playerId, string displayName)
        {
            var player = Get(playerId);
            if (player == null) return;
            player.DisplayName = SanitiseName(displayName, player.SeatIndex + 1);
            Changed?.Invoke();
        }

        public PlayerState Get(int playerId)
        {
            return _players.FirstOrDefault(p => p.Id == playerId);
        }

        public string GetName(int playerId)
        {
            var player = Get(playerId);
            return player != null ? player.DisplayName : "Unknown";
        }

        public void Shuffle(IRandomProvider random)
        {
            Shuffler.ShuffleInPlace(_players, random);
            ReindexSeats();
            Changed?.Invoke();
        }

        public void Clear()
        {
            _players.Clear();
            Changed?.Invoke();
        }

        /// <summary>Resets scores, roles, liveness and any round scratch data.</summary>
        public void ResetForNewGame()
        {
            foreach (var player in _players)
            {
                player.Score = 0;
                player.IsAlive = true;
                player.ClearRoundData();
            }
            Changed?.Invoke();
        }

        public void ClearRoundData()
        {
            foreach (var player in _players) player.ClearRoundData();
        }

        public IEnumerable<PlayerState> AlivePlayers => _players.Where(p => p.IsAlive);

        /// <summary>Ensures the roster is usable: unique, non empty names within the allowed range.</summary>
        public ValidationResult Validate(int minPlayers, int maxPlayers)
        {
            if (_players.Count < minPlayers)
                return ValidationResult.Fail("Add at least " + minPlayers + " players to start.");
            if (_players.Count > maxPlayers)
                return ValidationResult.Fail("This game supports up to " + maxPlayers + " players.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var player in _players)
            {
                if (string.IsNullOrWhiteSpace(player.DisplayName))
                    return ValidationResult.Fail("Every player needs a name.");
                if (!seen.Add(player.DisplayName.Trim()))
                    return ValidationResult.Fail(player.DisplayName + " is used more than once.");
            }
            return ValidationResult.Ok();
        }

        /// <summary>Tops the roster up with placeholder names so the app is never unusable.</summary>
        public void EnsureMinimum(int minPlayers)
        {
            while (_players.Count < minPlayers) Add(null);
        }

        public void RaiseChanged()
        {
            Changed?.Invoke();
        }

        private void ReindexSeats()
        {
            for (var i = 0; i < _players.Count; i++) _players[i].SeatIndex = i;
        }

        private static string SanitiseName(string name, int ordinal)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Player " + ordinal;
            name = name.Trim();
            return name.Length > 14 ? name.Substring(0, 14) : name;
        }
    }
}
