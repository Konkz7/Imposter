using PartyGame.Core.Audio;
using PartyGame.Core.Content;
using PartyGame.Core.Monetisation;
using PartyGame.Core.Persistence;
using PartyGame.Core.Services;
using PartyGame.Core.Session;

namespace PartyGame.Core.App
{
    /// <summary>
    /// Every long-lived service in one object, built once at boot. Screens receive this rather
    /// than reaching for singletons, which keeps them easy to reason about and to replace.
    /// </summary>
    public class AppServices
    {
        public IKeyValueStore Store { get; }
        public SettingsService Settings { get; }
        public IEntitlementService Entitlements { get; }
        public AdPolicy AdPolicy { get; }
        public IAdService Ads { get; }
        public IPurchaseService Purchases { get; }
        public PurchaseCoordinator Store_Purchases { get; }
        public ContentService Content { get; }
        public PlayerRoster Roster { get; }
        public IRandomProvider Random { get; }
        public AudioManager Audio { get; }

        public AppServices(IKeyValueStore store, ContentService content, AudioManager audio, IRandomProvider random = null)
        {
            Store = store ?? new InMemoryStore();
            Settings = new SettingsService(Store);
            Entitlements = new EntitlementService(Store);
            AdPolicy = new AdPolicy(Entitlements);
            Ads = new NullAdService(AdPolicy);
            Purchases = new NullPurchaseService(Entitlements);
            Store_Purchases = new PurchaseCoordinator(Purchases, Entitlements);
            Content = content;
            Roster = new PlayerRoster();
            Random = random ?? new SystemRandomProvider();
            Audio = audio;
        }

        /// <summary>Restores the saved player names, or seeds a sensible default party.</summary>
        public void RestorePlayers(int minimum = 4)
        {
            var saved = Settings.LoadPlayerNames();
            foreach (var name in saved) Roster.Add(name);
            Roster.EnsureMinimum(minimum);
        }

        public void PersistPlayers()
        {
            Settings.SavePlayerNames(System.Linq.Enumerable.Select(Roster.Players, p => p.DisplayName));
        }
    }
}
