using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.Core.Session;

namespace PartyGame.Tests
{
    /// <summary>Everything one automated play-through recorded for assertions.</summary>
    public class Playthrough
    {
        public readonly List<GameStep> Steps = new List<GameStep>();
        public readonly List<RoundSummary> Rounds = new List<RoundSummary>();

        public IEnumerable<T> StepsOfType<T>() where T : GameStep => Steps.OfType<T>();

        public IEnumerable<GameStep> PrivateSteps => Steps.Where(s => s.IsPrivate);
    }

    /// <summary>
    /// Drives a mode end to end with no UI at all. This is what makes the game rules testable:
    /// the mode only ever sees steps in and results out.
    /// </summary>
    public static class ModeTestHarness
    {
        public static ContentService LoadContent()
        {
            var content = ContentService.LoadFromResources();
            Assert.IsTrue(content.HasLibrary,
                "No ContentLibrary in Resources. Run Party Game > Rebuild Content.");
            return content;
        }

        public static GameSession CreateSession(GameModeId modeId, int playerCount, out IGameMode mode,
            Action<GameSettings> configure = null, int seed = 20260921)
        {
            var content = LoadContent();
            var definition = content.GetMode(modeId);
            Assert.IsNotNull(definition, "No GameModeDefinition asset for " + modeId);

            mode = GameModeFactory.Create(modeId);
            Assert.IsNotNull(mode, "No rules implementation for " + modeId);

            var roster = new PlayerRoster();
            var names = new[] { "Alex", "Bea", "Chris", "Dev", "Eli", "Fran", "Gus", "Hana", "Ira", "Jo", "Kit", "Lou" };
            for (var i = 0; i < playerCount; i++) roster.Add(names[i % names.Length] + (i >= names.Length ? i.ToString() : ""));

            var settings = new GameSettings();
            settings.Declare(mode.GetSettingDefinitions(content));
            configure?.Invoke(settings);
            settings.ClampAll(playerCount);

            var scores = new ScoreManager();
            scores.RegisterAll(roster.Players.Select(p => p.Id));

            var session = new GameSession(roster, scores, new SystemRandomProvider(seed), content, definition, settings);
            session.ConfigureRounds(settings.GetInt(CommonSettingKeys.Rounds, 2));

            var validation = mode.Validate(session);
            Assert.IsTrue(validation.IsValid, "Configuration rejected: " + validation.Message);

            mode.Initialise(session);
            return session;
        }

        /// <summary>Plays a single round and returns everything that happened.</summary>
        public static Playthrough PlayRound(GameSession session, IGameMode mode, int maxSteps = 600)
        {
            var log = new Playthrough();
            RunRound(session, mode, log, maxSteps);
            log.Rounds.Add(mode.CompleteRound());
            return log;
        }

        /// <summary>Plays until the mode reports the game is over.</summary>
        public static Playthrough PlayWholeGame(GameSession session, IGameMode mode, int maxRounds = 12)
        {
            var log = new Playthrough();

            for (var round = 0; round < maxRounds; round++)
            {
                RunRound(session, mode, log);
                var summary = mode.CompleteRound();
                log.Rounds.Add(summary);
                if (summary.GameOver) return log;
                session.AdvanceRound();
            }

            Assert.Fail("The game never reported that it was over.");
            return log;
        }

        private static void RunRound(GameSession session, IGameMode mode, Playthrough log, int maxSteps = 600)
        {
            mode.StartRound();

            var guard = 0;
            while (!mode.IsRoundComplete)
            {
                var step = mode.Current;
                if (step == null) break;

                log.Steps.Add(step);
                mode.Submit(Respond(step, session, guard));

                if (++guard > maxSteps) Assert.Fail("Round did not finish within " + maxSteps + " steps.");
            }
        }

        /// <summary>A deterministic stand-in for a player: always answers, always picks the first option.</summary>
        private static StepResult Respond(GameStep step, GameSession session, int counter)
        {
            switch (step)
            {
                case TextInputStep text:
                    var author = step.ActorPlayerId.HasValue ? session.NameOf(step.ActorPlayerId.Value) : "anon";
                    var candidate = author + " answer " + counter.ToString(CultureInfo.InvariantCulture);
                    return StepResult.ForText(text, candidate);

                case ChoiceStep choice:
                    var option = choice.Options.FirstOrDefault(o => o.Enabled);
                    return StepResult.ForChoice(choice, option != null ? option.Id : string.Empty);

                default:
                    return StepResult.Acknowledged(step);
            }
        }
    }
}
