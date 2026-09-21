using PartyGame.Core.Content;
using PartyGame.Core.Modes;

namespace PartyGame.Core.Persistence
{
    /// <summary>
    /// Remembers each mode's configuration between sessions. Only preferences are stored -
    /// nothing about an in-progress round ever reaches disk.
    /// </summary>
    public class ModeSettingsStore
    {
        private readonly IKeyValueStore _store;

        public ModeSettingsStore(IKeyValueStore store)
        {
            _store = store ?? new InMemoryStore();
        }

        /// <summary>Builds a settings bag for a mode, filled with saved values where they exist.</summary>
        public GameSettings Load(IGameMode mode, ContentService content)
        {
            var settings = new GameSettings();
            if (mode == null) return settings;

            settings.Declare(mode.GetSettingDefinitions(content));
            foreach (var definition in settings.Definitions)
            {
                var key = KeyFor(mode.Id, definition.Key);
                if (_store.Has(key)) settings.SetString(definition.Key, _store.GetString(key, definition.DefaultValue));
            }
            return settings;
        }

        public void Save(GameModeId modeId, GameSettings settings)
        {
            if (settings == null) return;
            foreach (var definition in settings.Definitions)
                _store.SetString(KeyFor(modeId, definition.Key), settings.GetString(definition.Key, definition.DefaultValue));
            _store.Save();
        }

        private static string KeyFor(GameModeId modeId, string settingKey)
        {
            return "mode." + (int)modeId + "." + settingKey;
        }
    }
}
