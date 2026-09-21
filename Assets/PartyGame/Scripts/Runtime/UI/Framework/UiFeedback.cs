using System;
using System.Collections;
using PartyGame.Core.Audio;
using UnityEngine;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Hooks the UI layer up to audio, haptics and the animation speed setting without making
    /// every widget depend on the app root. The bootstrap wires these once at startup.
    /// </summary>
    public static class UiFeedback
    {
        public static Action<SoundId> PlaySound;
        public static Action Vibrate;

        /// <summary>Multiplier from the settings screen. Zero disables animation entirely.</summary>
        public static float AnimationScale = 1f;

        public static bool AnimationsEnabled => AnimationScale > 0.01f;

        public static void Sound(SoundId id)
        {
            PlaySound?.Invoke(id);
        }

        public static void Haptic()
        {
            Vibrate?.Invoke();
        }

        public static float Duration(float seconds)
        {
            return seconds * AnimationScale;
        }
    }

    /// <summary>Minimal tween helpers. Everything the UI animates goes through these.</summary>
    public static class UiTween
    {
        private static MonoBehaviour _runner;

        public static void SetRunner(MonoBehaviour runner)
        {
            _runner = runner;
        }

        public static Coroutine Run(IEnumerator routine)
        {
            if (_runner == null || routine == null) return null;
            return _runner.StartCoroutine(routine);
        }

        public static void Stop(Coroutine routine)
        {
            if (_runner != null && routine != null) _runner.StopCoroutine(routine);
        }

        public static IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float seconds)
        {
            if (group == null) yield break;
            seconds = UiFeedback.Duration(seconds);
            if (seconds <= 0.001f)
            {
                group.alpha = to;
                yield break;
            }

            var elapsed = 0f;
            group.alpha = from;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                // The target can be destroyed mid-tween when a screen advances quickly.
                if (group == null) yield break;
                group.alpha = Mathf.Lerp(from, to, EaseOut(Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            if (group != null) group.alpha = to;
        }

        public static IEnumerator Slide(RectTransform rect, Vector2 from, Vector2 to, float seconds)
        {
            if (rect == null) yield break;
            seconds = UiFeedback.Duration(seconds);
            if (seconds <= 0.001f)
            {
                rect.anchoredPosition = to;
                yield break;
            }

            var elapsed = 0f;
            rect.anchoredPosition = from;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (rect == null) yield break;
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseOut(Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            if (rect != null) rect.anchoredPosition = to;
        }

        public static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float seconds, bool overshoot = false)
        {
            if (target == null) yield break;
            seconds = UiFeedback.Duration(seconds);
            if (seconds <= 0.001f)
            {
                target.localScale = to;
                yield break;
            }

            var elapsed = 0f;
            target.localScale = from;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (target == null) yield break;
                var t = Mathf.Clamp01(elapsed / seconds);
                target.localScale = Vector3.LerpUnclamped(from, to, overshoot ? EaseBack(t) : EaseOut(t));
                yield return null;
            }
            if (target != null) target.localScale = to;
        }

        public static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float EaseBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
