using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Persistence
{
    /// <summary>
    /// Local persistence abstraction. Everything the app saves goes through this, so swapping
    /// PlayerPrefs for a file or a cloud save later is a one class change.
    /// </summary>
    public interface IKeyValueStore
    {
        bool Has(string key);
        string GetString(string key, string fallback = "");
        void SetString(string key, string value);
        int GetInt(string key, int fallback = 0);
        void SetInt(string key, int value);
        bool GetBool(string key, bool fallback = false);
        void SetBool(string key, bool value);
        void Delete(string key);
        void Save();
    }

    /// <summary>Default store. PlayerPrefs is fine for settings and a single entitlement flag.</summary>
    public class PlayerPrefsStore : IKeyValueStore
    {
        private const string Prefix = "partygame.";

        public bool Has(string key) => PlayerPrefs.HasKey(Prefix + key);

        public string GetString(string key, string fallback = "") => PlayerPrefs.GetString(Prefix + key, fallback);

        public void SetString(string key, string value) => PlayerPrefs.SetString(Prefix + key, value ?? string.Empty);

        public int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(Prefix + key, fallback);

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(Prefix + key, value);

        public bool GetBool(string key, bool fallback = false) => PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0;

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);

        public void Delete(string key) => PlayerPrefs.DeleteKey(Prefix + key);

        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>Non persistent store used by tests and by the editor play-mode sandbox.</summary>
    public class InMemoryStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public bool Has(string key) => _values.ContainsKey(key);

        public string GetString(string key, string fallback = "")
        {
            return _values.TryGetValue(key, out var value) ? value : fallback;
        }

        public void SetString(string key, string value) => _values[key] = value ?? string.Empty;

        public int GetInt(string key, int fallback = 0)
        {
            return int.TryParse(GetString(key, null), out var parsed) ? parsed : fallback;
        }

        public void SetInt(string key, int value) => SetString(key, value.ToString());

        public bool GetBool(string key, bool fallback = false) => GetInt(key, fallback ? 1 : 0) != 0;

        public void SetBool(string key, bool value) => SetInt(key, value ? 1 : 0);

        public void Delete(string key) => _values.Remove(key);

        public void Save()
        {
        }
    }
}
