using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Modes
{
    public enum GameModeId
    {
        None = 0,
        DifferentWord = 1,
        Fib = 2,
        Wavelength = 3,
        DevilsAdvocate = 4,
        SocialDeduction = 5
    }

    /// <summary>
    /// Presentation metadata for a game mode: everything the game selection card needs.
    /// Rules live in the matching <see cref="IGameMode"/> implementation, not here.
    /// </summary>
    [CreateAssetMenu(menuName = "Party Game/Game Mode", fileName = "GameMode")]
    public class GameModeDefinition : ScriptableObject
    {
        [SerializeField] private GameModeId modeId = GameModeId.None;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string tagline = string.Empty;
        [SerializeField, TextArea(2, 5)] private string description = string.Empty;
        [SerializeField] private string glyph = "?";
        [SerializeField, Range(0, 7)] private int accentIndex;
        [SerializeField] private int minPlayers = 4;
        [SerializeField] private int maxPlayers = 12;
        [SerializeField] private int minutesPerRound = 5;
        [SerializeField] private bool available = true;
        [SerializeField] private List<string> howToPlay = new List<string>();

        public GameModeId ModeId => modeId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? modeId.ToString() : displayName;
        public string Tagline => tagline;
        public string Description => description;
        public string Glyph => string.IsNullOrEmpty(glyph) ? "?" : glyph;
        public int AccentIndex => accentIndex;
        public int MinPlayers => Mathf.Max(3, minPlayers);
        public int MaxPlayers => Mathf.Max(MinPlayers, maxPlayers);
        public int MinutesPerRound => Mathf.Max(1, minutesPerRound);
        public bool Available => available;
        public IReadOnlyList<string> HowToPlay => howToPlay;

        public string PlayerRangeLabel => MinPlayers + "-" + MaxPlayers + " players";
        public string DurationLabel => "~" + MinutesPerRound + " min per round";

        public void Configure(GameModeId id, string name, string modeTagline, string modeDescription,
            string modeGlyph, int accent, int min, int max, int minutes, IEnumerable<string> steps)
        {
            modeId = id;
            displayName = name;
            tagline = modeTagline;
            description = modeDescription;
            glyph = modeGlyph;
            accentIndex = accent;
            minPlayers = min;
            maxPlayers = max;
            minutesPerRound = minutes;
            available = true;
            howToPlay = new List<string>(steps);
        }
    }
}
