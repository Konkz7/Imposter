using System;
using System.Collections.Generic;
using System.Linq;

namespace PartyGame.Core.Persistence
{
    /// <summary>How much motion the app is allowed to use. Doubles as the accessibility control.</summary>
    public enum AnimationSpeed
    {
        Off = 0,
        Relaxed = 1,
        Normal = 2,
        Fast = 3
    }

    /// <summary>
    /// App wide preferences. Persisted locally and exposed as a single object so no screen
    /// has to know which keys exist.
    /// </summary>
    public class AppSettings
    {
        public bool SoundEffects { get; set; } = true;
        public bool Music { get; set; } = true;
        public bool Vibration { get; set; } = true;
        public AnimationSpeed Animation { get; set; } = AnimationSpeed.Normal;
        public bool HoldToReveal { get; set; } = true;

        /// <summary>Multiplier applied to every UI transition.</summary>
        public float AnimationMultiplier
        {
            get
            {
                switch (Animation)
                {
                    case AnimationSpeed.Off: return 0f;
                    case AnimationSpeed.Relaxed: return 1.5f;
                    case AnimationSpeed.Fast: return 0.6f;
                    default: return 1f;
                }
            }
        }

        public AppSettings Clone()
        {
            return new AppSettings
            {
                SoundEffects = SoundEffects,
                Music = Music,
                Vibration = Vibration,
                Animation = Animation,
                HoldToReveal = HoldToReveal
            };
        }
    }

    public class SettingsService
    {
        private const string KeySound = "settings.sfx";
        private const string KeyMusic = "settings.music";
        private const string KeyVibration = "settings.vibration";
        private const string KeyAnimation = "settings.animation";
        private const string KeyHoldToReveal = "settings.holdToReveal";
        private const string KeyPlayerNames = "players.names";

        private readonly IKeyValueStore _store;

        public AppSettings Settings { get; private set; } = new AppSettings();

        public event Action<AppSettings> Changed;

        public SettingsService(IKeyValueStore store)
        {
            _store = store ?? new InMemoryStore();
            Load();
        }

        public void Load()
        {
            Settings = new AppSettings
            {
                SoundEffects = _store.GetBool(KeySound, true),
                Music = _store.GetBool(KeyMusic, true),
                Vibration = _store.GetBool(KeyVibration, true),
                HoldToReveal = _store.GetBool(KeyHoldToReveal, true),
                Animation = (AnimationSpeed)Math.Min(3, Math.Max(0, _store.GetInt(KeyAnimation, (int)AnimationSpeed.Normal)))
            };
        }

        public void Save()
        {
            _store.SetBool(KeySound, Settings.SoundEffects);
            _store.SetBool(KeyMusic, Settings.Music);
            _store.SetBool(KeyVibration, Settings.Vibration);
            _store.SetBool(KeyHoldToReveal, Settings.HoldToReveal);
            _store.SetInt(KeyAnimation, (int)Settings.Animation);
            _store.Save();
            Changed?.Invoke(Settings);
        }

        public void Apply(Action<AppSettings> mutate)
        {
            if (mutate == null) return;
            mutate(Settings);
            Save();
        }

        /// <summary>
        /// Remembers the player names between sessions. Nothing about a round is ever stored:
        /// roles, words and votes stay in memory only.
        /// </summary>
        public void SavePlayerNames(IEnumerable<string> names)
        {
            var cleaned = (names ?? Enumerable.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim().Replace("|", " "))
                .Take(16)
                .ToList();
            _store.SetString(KeyPlayerNames, string.Join("|", cleaned));
            _store.Save();
        }

        public IReadOnlyList<string> LoadPlayerNames()
        {
            var raw = _store.GetString(KeyPlayerNames, string.Empty);
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
    }
}
