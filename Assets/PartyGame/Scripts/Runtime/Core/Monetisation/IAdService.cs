using System;
using UnityEngine;

namespace PartyGame.Core.Monetisation
{
    /// <summary>
    /// Where an ad could appear. Deliberately coarse: there is no placement inside a round,
    /// because an ad must never interrupt a player holding secret information.
    /// </summary>
    public enum AdPlacement
    {
        /// <summary>After a whole game finishes and the final scores have been seen.</summary>
        AfterGame = 0,
        /// <summary>Player asked for something in exchange for watching.</summary>
        RewardedUnlock = 1
    }

    public interface IAdService
    {
        bool IsAdFree { get; }
        bool IsRewardedAdAvailable();

        /// <summary>Requests an interstitial. The callback always runs, ad or no ad.</summary>
        void ShowInterstitial(AdPlacement placement, Action onClosed = null);

        /// <summary>Requests a rewarded ad. The callback reports whether the reward was earned.</summary>
        void ShowRewarded(string rewardId, Action<bool> onComplete = null);
    }

    /// <summary>
    /// Decides whether an ad may be shown at all. Every rule that would otherwise be scattered
    /// through gameplay as "if (!removeAds)" lives here instead.
    /// </summary>
    public class AdPolicy
    {
        private readonly IEntitlementService _entitlements;

        /// <summary>Games finished before the first interstitial is considered.</summary>
        public int WarmupGames { get; set; } = 1;

        /// <summary>Minimum seconds between interstitials.</summary>
        public float CooldownSeconds { get; set; } = 180f;

        /// <summary>Raised while any private information is on screen. Hard blocks every ad.</summary>
        public bool PrivateInformationVisible { get; set; }

        private int _gamesCompleted;
        private float _lastShownAt = float.NegativeInfinity;

        public AdPolicy(IEntitlementService entitlements)
        {
            _entitlements = entitlements;
        }

        public bool IsAdFree => _entitlements != null && _entitlements.Has(Entitlement.AdFree);

        public void NotifyGameCompleted()
        {
            _gamesCompleted++;
        }

        public bool CanShow(AdPlacement placement)
        {
            if (IsAdFree) return false;
            if (PrivateInformationVisible) return false;
            if (placement == AdPlacement.RewardedUnlock) return true;
            if (_gamesCompleted < WarmupGames) return false;
            return Time.realtimeSinceStartup - _lastShownAt >= CooldownSeconds;
        }

        public void NotifyShown()
        {
            _lastShownAt = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// The development implementation. It never shows anything and always completes, so the
    /// whole app runs identically with no ad SDK installed.
    /// </summary>
    public class NullAdService : IAdService
    {
        private readonly AdPolicy _policy;
        private readonly bool _logCalls;

        public NullAdService(AdPolicy policy, bool logCalls = false)
        {
            _policy = policy;
            _logCalls = logCalls;
        }

        public bool IsAdFree => _policy != null && _policy.IsAdFree;

        public bool IsRewardedAdAvailable() => false;

        public void ShowInterstitial(AdPlacement placement, Action onClosed = null)
        {
            if (_policy != null && _policy.CanShow(placement))
            {
                _policy.NotifyShown();
                if (_logCalls) Debug.Log("[Ads] Interstitial slot reached: " + placement + " (no provider configured)");
            }
            onClosed?.Invoke();
        }

        public void ShowRewarded(string rewardId, Action<bool> onComplete = null)
        {
            if (_logCalls) Debug.Log("[Ads] Rewarded ad requested for " + rewardId + " (no provider configured)");
            onComplete?.Invoke(false);
        }
    }
}
