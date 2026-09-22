using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PartyGame.Core.Modes
{
    public enum SettingType
    {
        Toggle = 0,
        Stepper = 1,
        Options = 2,
        /// <summary>Multi-select over the content categories a mode supports.</summary>
        Categories = 3
    }

    public sealed class SettingOption
    {
        public string Id { get; }
        public string Label { get; }
        public string Description { get; }

        public SettingOption(string id, string label, string description = "")
        {
            Id = id;
            Label = label;
            Description = description ?? string.Empty;
        }
    }

    /// <summary>
    /// Declarative description of one configurable option. Modes declare these and the
    /// settings screen renders them, so adding a setting never means writing UI.
    /// </summary>
    public sealed class SettingDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SettingType Type { get; set; } = SettingType.Toggle;

        public int Min { get; set; }
        public int Max { get; set; } = 10;
        public int Step { get; set; } = 1;
        public string Suffix { get; set; } = string.Empty;

        public List<SettingOption> Options { get; } = new List<SettingOption>();

        /// <summary>
        /// Overrides how particular stepper values read, so an edge of the range can mean
        /// something ("Any") rather than showing a bare number.
        /// </summary>
        public Dictionary<int, string> ValueLabels { get; } = new Dictionary<int, string>();

        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>Lets a maximum depend on the live player count (e.g. imposters).</summary>
        public Func<int, int> MaxForPlayerCount { get; set; }

        public int ResolveMax(int playerCount)
        {
            return MaxForPlayerCount != null ? Math.Max(Min, MaxForPlayerCount(playerCount)) : Max;
        }

        public static SettingDefinition Toggle(string key, string label, bool defaultValue, string description = "")
        {
            return new SettingDefinition
            {
                Key = key,
                Label = label,
                Description = description,
                Type = SettingType.Toggle,
                DefaultValue = defaultValue ? "1" : "0"
            };
        }

        /// <summary>Formats a value for display, honouring any <see cref="ValueLabels"/> override.</summary>
        public string FormatValue(int value)
        {
            return ValueLabels.TryGetValue(value, out var label) ? label : value + Suffix;
        }

        public static SettingDefinition Stepper(string key, string label, int defaultValue, int min, int max, int step = 1, string suffix = "", string description = "")
        {
            return new SettingDefinition
            {
                Key = key,
                Label = label,
                Description = description,
                Type = SettingType.Stepper,
                Min = min,
                Max = max,
                Step = step,
                Suffix = suffix,
                DefaultValue = defaultValue.ToString(CultureInfo.InvariantCulture)
            };
        }

        public static SettingDefinition Choice(string key, string label, string defaultOptionId, IEnumerable<SettingOption> options, string description = "")
        {
            var definition = new SettingDefinition
            {
                Key = key,
                Label = label,
                Description = description,
                Type = SettingType.Options,
                DefaultValue = defaultOptionId
            };
            if (options != null) definition.Options.AddRange(options);
            return definition;
        }

        public static SettingDefinition CategoryPicker(string key, string label, string description = "")
        {
            return new SettingDefinition
            {
                Key = key,
                Label = label,
                Description = description,
                Type = SettingType.Categories,
                DefaultValue = string.Empty
            };
        }
    }

    /// <summary>
    /// Untyped value bag for one mode's configuration. Modes read it through their own
    /// strongly typed wrapper so gameplay code never juggles magic strings.
    /// </summary>
    public class GameSettings
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<SettingDefinition> _definitions = new List<SettingDefinition>();

        public IReadOnlyList<SettingDefinition> Definitions => _definitions;

        public void Declare(IEnumerable<SettingDefinition> definitions)
        {
            if (definitions == null) return;
            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Key)) continue;
                _definitions.RemoveAll(d => d.Key == definition.Key);
                _definitions.Add(definition);
                if (!_values.ContainsKey(definition.Key)) _values[definition.Key] = definition.DefaultValue;
            }
        }

        public SettingDefinition GetDefinition(string key)
        {
            return _definitions.FirstOrDefault(d => d.Key == key);
        }

        public void ResetToDefaults()
        {
            foreach (var definition in _definitions) _values[definition.Key] = definition.DefaultValue;
        }

        public string GetString(string key, string fallback = "")
        {
            return _values.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;
        }

        public void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _values[key] = value ?? string.Empty;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            var raw = GetString(key, null);
            if (raw == null) return fallback;
            return raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        }

        public void SetBool(string key, bool value)
        {
            SetString(key, value ? "1" : "0");
        }

        public int GetInt(string key, int fallback = 0)
        {
            var raw = GetString(key, null);
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        public void SetInt(string key, int value)
        {
            SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public IReadOnlyList<string> GetList(string key)
        {
            var raw = GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        public void SetList(string key, IEnumerable<string> values)
        {
            SetString(key, values == null ? string.Empty : string.Join(",", values));
        }

        /// <summary>Clamps every stepper into range for the current player count.</summary>
        public void ClampAll(int playerCount)
        {
            foreach (var definition in _definitions)
            {
                if (definition.Type != SettingType.Stepper) continue;
                var max = definition.ResolveMax(playerCount);
                var value = GetInt(definition.Key, definition.Min);
                SetInt(definition.Key, Math.Min(Math.Max(value, definition.Min), Math.Max(definition.Min, max)));
            }
        }

        public GameSettings Clone()
        {
            var clone = new GameSettings();
            clone._definitions.AddRange(_definitions);
            foreach (var kvp in _values) clone._values[kvp.Key] = kvp.Value;
            return clone;
        }
    }

    /// <summary>Setting keys shared by more than one mode.</summary>
    public static class CommonSettingKeys
    {
        public const string Rounds = "rounds";
        public const string Categories = "categories";
        public const string DiscussionSeconds = "discussionSeconds";
        public const string VotingEnabled = "votingEnabled";
        public const string ImposterCount = "imposterCount";
        public const string TieBehaviour = "tieBehaviour";
    }
}
