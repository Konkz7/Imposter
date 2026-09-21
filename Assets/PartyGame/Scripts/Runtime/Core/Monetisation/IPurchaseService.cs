using System;
using UnityEngine;

namespace PartyGame.Core.Monetisation
{
    public enum PurchaseOutcome
    {
        Success = 0,
        Cancelled = 1,
        AlreadyOwned = 2,
        Unavailable = 3,
        Failed = 4
    }

    public readonly struct PurchaseResult
    {
        public PurchaseOutcome Outcome { get; }
        public string ProductId { get; }
        public string Message { get; }

        public PurchaseResult(PurchaseOutcome outcome, string productId, string message = "")
        {
            Outcome = outcome;
            ProductId = productId;
            Message = message ?? string.Empty;
        }

        public bool Succeeded => Outcome == PurchaseOutcome.Success || Outcome == PurchaseOutcome.AlreadyOwned;
    }

    /// <summary>
    /// Store abstraction. A Unity IAP implementation can be dropped in behind this without
    /// gameplay or UI changes - the UI only ever asks for a product and reacts to the result.
    /// </summary>
    public interface IPurchaseService
    {
        /// <summary>False when no store is configured, which is the development default.</summary>
        bool IsStoreAvailable { get; }

        string GetLocalisedPrice(string productId);

        void Purchase(string productId, Action<PurchaseResult> onComplete);

        void RestorePurchases(Action<bool> onComplete);
    }

    /// <summary>
    /// Development implementation with no store attached. It reports the store as
    /// unavailable so the UI can show an honest "not available yet" instead of failing.
    /// </summary>
    public class NullPurchaseService : IPurchaseService
    {
        private readonly IEntitlementService _entitlements;

        public NullPurchaseService(IEntitlementService entitlements)
        {
            _entitlements = entitlements;
        }

        public bool IsStoreAvailable => false;

        public string GetLocalisedPrice(string productId) => "-";

        public void Purchase(string productId, Action<PurchaseResult> onComplete)
        {
            if (StoreProducts.ProductToEntitlement.TryGetValue(productId ?? string.Empty, out var entitlement)
                && _entitlements != null && _entitlements.Has(entitlement))
            {
                onComplete?.Invoke(new PurchaseResult(PurchaseOutcome.AlreadyOwned, productId));
                return;
            }

            Debug.Log("[IAP] Purchase requested for " + productId + " but no store is configured.");
            onComplete?.Invoke(new PurchaseResult(PurchaseOutcome.Unavailable, productId,
                "The store is not connected in this build."));
        }

        public void RestorePurchases(Action<bool> onComplete)
        {
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Turns a completed purchase into an entitlement. Keeping this mapping in one place is
    /// what lets the rest of the app ask "do they own ad free" rather than "did they buy X".
    /// </summary>
    public class PurchaseCoordinator
    {
        private readonly IPurchaseService _purchases;
        private readonly IEntitlementService _entitlements;

        public PurchaseCoordinator(IPurchaseService purchases, IEntitlementService entitlements)
        {
            _purchases = purchases;
            _entitlements = entitlements;
        }

        public bool StoreAvailable => _purchases != null && _purchases.IsStoreAvailable;

        public bool Owns(string productId)
        {
            return StoreProducts.ProductToEntitlement.TryGetValue(productId ?? string.Empty, out var entitlement)
                   && _entitlements != null && _entitlements.Has(entitlement);
        }

        public string PriceOf(string productId)
        {
            return _purchases != null ? _purchases.GetLocalisedPrice(productId) : "-";
        }

        public void Buy(string productId, Action<PurchaseResult> onComplete)
        {
            if (_purchases == null)
            {
                onComplete?.Invoke(new PurchaseResult(PurchaseOutcome.Unavailable, productId));
                return;
            }

            _purchases.Purchase(productId, result =>
            {
                if (result.Succeeded &&
                    StoreProducts.ProductToEntitlement.TryGetValue(productId ?? string.Empty, out var entitlement))
                {
                    _entitlements?.Grant(entitlement);
                }
                onComplete?.Invoke(result);
            });
        }

        public void Restore(Action<bool> onComplete)
        {
            if (_purchases == null)
            {
                onComplete?.Invoke(false);
                return;
            }
            _purchases.RestorePurchases(onComplete);
        }
    }
}
