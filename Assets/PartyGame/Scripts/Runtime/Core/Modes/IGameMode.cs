using System.Collections.Generic;
using PartyGame.Core.Content;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Core.Modes
{
    /// <summary>What happened at the end of a round, and whether the game should stop.</summary>
    public class RoundSummary
    {
        public int RoundNumber { get; set; }
        public bool GameOver { get; set; }
        public string GameOverReason { get; set; } = string.Empty;

        /// <summary>
        /// Who won, for modes that end in a result rather than a ranking. Empty for scored
        /// modes, where the leaderboard is the outcome.
        /// </summary>
        public string OutcomeHeadline { get; set; } = string.Empty;
    }

    /// <summary>
    /// A game mode is a rules engine: it turns session state into a stream of steps and
    /// consumes the results. It never touches Unity objects, so it is fully unit testable
    /// and could be driven by a network layer instead of a local UI.
    /// </summary>
    public interface IGameMode
    {
        GameModeId Id { get; }

        /// <summary>
        /// False for modes decided by winning or losing rather than by points. A scored mode
        /// shows a running leaderboard; an unscored one must not, because a per-round points
        /// change can give away who is secretly on which side.
        /// </summary>
        bool UsesScoring { get; }

        /// <summary>Options this mode exposes on the pre-game settings screen.</summary>
        IReadOnlyList<SettingDefinition> GetSettingDefinitions(ContentService content);

        /// <summary>Rejects nonsensical configurations before a round can start.</summary>
        ValidationResult Validate(GameSession session);

        void Initialise(GameSession session);

        /// <summary>Builds the step list for the current round.</summary>
        void StartRound();

        GameStep Current { get; }

        bool IsRoundComplete { get; }

        /// <summary>Feeds a finished step back in and advances.</summary>
        void Submit(StepResult result);

        RoundSummary CompleteRound();

        /// <summary>Short rules bullets for the how-to-play screen.</summary>
        IReadOnlyList<string> BuildRulesSummary(GameSettings settings);
    }
}
