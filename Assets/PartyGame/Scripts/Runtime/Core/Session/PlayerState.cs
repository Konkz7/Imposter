using System;
using System.Collections.Generic;

namespace PartyGame.Core.Session
{
    /// <summary>
    /// Runtime state for a single participant.
    /// Deliberately plain C# (no UnityEngine types) so that it can be serialised, unit tested
    /// and - later - replicated by a network layer without touching game rules.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        /// <summary>Stable identifier for the whole session. Never re-used.</summary>
        public int Id { get; }

        public string DisplayName { get; set; }

        /// <summary>Cumulative score across the whole game session.</summary>
        public int Score { get; set; }

        /// <summary>Used by elimination based modes. Modes that do not eliminate leave this true.</summary>
        public bool IsAlive { get; set; } = true;

        /// <summary>Role for the current round, e.g. "imposter", "citizen", "investigator".</summary>
        public string RoleId { get; set; } = PlayerRoles.None;

        /// <summary>Seat order index, refreshed whenever the player order changes.</summary>
        public int SeatIndex { get; set; }

        private readonly Dictionary<string, string> _roundData = new Dictionary<string, string>();

        public PlayerState(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        /// <summary>Temporary, round scoped information (secret word, number, written answer...).</summary>
        public void SetRoundData(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _roundData[key] = value ?? string.Empty;
        }

        public string GetRoundData(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            return _roundData.TryGetValue(key, out var value) ? value : fallback;
        }

        public int GetRoundDataInt(string key, int fallback = 0)
        {
            var raw = GetRoundData(key, null);
            return int.TryParse(raw, out var parsed) ? parsed : fallback;
        }

        public bool HasRoundData(string key) => !string.IsNullOrEmpty(key) && _roundData.ContainsKey(key);

        /// <summary>Wipes everything that must not leak into the next round.</summary>
        public void ClearRoundData()
        {
            _roundData.Clear();
            RoleId = PlayerRoles.None;
        }

        public override string ToString()
        {
            return "#" + Id + " " + DisplayName + " (" + Score + ")";
        }
    }

    /// <summary>Role identifiers shared across modes. Modes may add their own.</summary>
    public static class PlayerRoles
    {
        public const string None = "none";
        public const string Crew = "crew";
        public const string Imposter = "imposter";
        public const string Investigator = "investigator";
        public const string Witness = "witness";
    }

    /// <summary>Round data keys shared across modes.</summary>
    public static class RoundDataKeys
    {
        public const string Word = "word";
        public const string Hint = "hint";
        public const string Number = "number";
        public const string WrittenAnswer = "answer";
        public const string Stance = "stance";
        public const string Instruction = "instruction";
        public const string Clue = "clue";
        public const string Guess = "guess";
        public const string Vote = "vote";
    }
}
