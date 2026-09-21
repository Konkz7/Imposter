using System;
using System.Collections.Generic;
using PartyGame.Games.DevilsAdvocate;
using PartyGame.Games.DifferentWord;
using PartyGame.Games.Fib;
using PartyGame.Games.SocialDeduction;
using PartyGame.Games.Wavelength;

namespace PartyGame.Core.Modes
{
    /// <summary>
    /// Maps a mode id to its rules implementation. The only place that knows every mode,
    /// so adding a game means adding one line here plus its content assets.
    /// </summary>
    public static class GameModeFactory
    {
        private static readonly Dictionary<GameModeId, Func<IGameMode>> Builders =
            new Dictionary<GameModeId, Func<IGameMode>>
            {
                { GameModeId.DifferentWord, () => new DifferentWordMode() },
                { GameModeId.Fib, () => new FibMode() },
                { GameModeId.Wavelength, () => new WavelengthMode() },
                { GameModeId.DevilsAdvocate, () => new DevilsAdvocateMode() },
                { GameModeId.SocialDeduction, () => new SocialDeductionMode() }
            };

        public static bool IsImplemented(GameModeId id) => Builders.ContainsKey(id);

        public static IGameMode Create(GameModeId id)
        {
            return Builders.TryGetValue(id, out var builder) ? builder() : null;
        }

        public static IEnumerable<GameModeId> ImplementedModes => Builders.Keys;
    }
}
