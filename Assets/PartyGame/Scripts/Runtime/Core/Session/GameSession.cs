using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;

namespace PartyGame.Core.Session
{
    /// <summary>
    /// Everything one play-through of one game mode needs. Passed to the mode on start,
    /// which means the mode never reaches out to singletons or scene objects.
    /// </summary>
    public class GameSession
    {
        public PlayerRoster Roster { get; }
        public ScoreManager Scores { get; }
        public IRandomProvider Random { get; }
        public ContentService Content { get; }
        public GameModeDefinition Definition { get; }
        public GameSettings Settings { get; }

        /// <summary>1-based.</summary>
        public int RoundNumber { get; private set; } = 1;

        public int TotalRounds { get; private set; } = 3;

        public bool IsFinalRound => RoundNumber >= TotalRounds;

        public GameModeId ModeId => Definition != null ? Definition.ModeId : GameModeId.None;

        public GameSession(PlayerRoster roster, ScoreManager scores, IRandomProvider random,
            ContentService content, GameModeDefinition definition, GameSettings settings)
        {
            Roster = roster;
            Scores = scores;
            Random = random;
            Content = content;
            Definition = definition;
            Settings = settings;
        }

        public void ConfigureRounds(int totalRounds)
        {
            TotalRounds = totalRounds < 1 ? 1 : totalRounds;
            RoundNumber = 1;
        }

        public void AdvanceRound()
        {
            RoundNumber++;
        }

        public IReadOnlyList<PlayerState> Players => Roster.Players;

        public IReadOnlyList<PlayerState> ActivePlayers => Roster.AlivePlayers.ToList();

        public IReadOnlyList<int> ActivePlayerIds => Roster.AlivePlayers.Select(p => p.Id).ToList();

        public string NameOf(int playerId) => Roster.GetName(playerId);

        public PlayerState PlayerOf(int playerId) => Roster.Get(playerId);

        /// <summary>Clears secret round state from everyone. Called between rounds.</summary>
        public void ClearRoundData()
        {
            Roster.ClearRoundData();
        }

        public string RoundLabel => "Round " + RoundNumber + " of " + TotalRounds;
    }
}
