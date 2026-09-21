using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Persistence;

namespace PartyGame.Core.Monetisation
{
    /// <summary>Things a player can own. One place, so nothing invents its own string.</summary>
    public static class Entitlement
    {
        public const string AdFree = "ad_free";
        public const string PremiumCategories = "premium_categories";
    }

    /// <summary>Store product ids. Mapped to entitlements by <see cref="EntitlementService"/>.</summary>
    public static class StoreProducts
    {
        public const string RemoveAds = "remove_ads";

        public static readonly IReadOnlyDictionary<string, string> ProductToEntitlement =
            new Dictionary<string, string>
            {
                { RemoveAds, Entitlement.AdFree }
            };
    }

    public interface IEntitlementService
    {
        bool Has(string entitlementId);
        void Grant(string entitlementId);
        void Revoke(string entitlementId);
        event Action Changed;
    }

    /// <summary>
    /// The single source of truth for what the player owns. Gameplay never checks a purchase
    /// flag directly: the ad and content systems ask this instead.
    /// </summary>
    public class EntitlementService : IEntitlementService
    {
        private const string KeyPrefix = "entitlement.";

        private readonly IKeyValueStore _store;
        private readonly HashSet<string> _granted = new HashSet<string>(StringComparer.Ordinal);

        public event Action Changed;

        public EntitlementService(IKeyValueStore store)
        {
            _store = store ?? new InMemoryStore();
            foreach (var id in new[] { Entitlement.AdFree, Entitlement.PremiumCategories })
                if (_store.GetBool(KeyPrefix + id, false)) _granted.Add(id);
        }

        public bool Has(string entitlementId)
        {
            return !string.IsNullOrEmpty(entitlementId) && _granted.Contains(entitlementId);
        }

        public void Grant(string entitlementId)
        {
            if (string.IsNullOrEmpty(entitlementId) || !_granted.Add(entitlementId)) return;
            _store.SetBool(KeyPrefix + entitlementId, true);
            _store.Save();
            Changed?.Invoke();
        }

        public void Revoke(string entitlementId)
        {
            if (string.IsNullOrEmpty(entitlementId) || !_granted.Remove(entitlementId)) return;
            _store.SetBool(KeyPrefix + entitlementId, false);
            _store.Save();
            Changed?.Invoke();
        }

        public IReadOnlyList<string> All => _granted.ToList();
    }
}
