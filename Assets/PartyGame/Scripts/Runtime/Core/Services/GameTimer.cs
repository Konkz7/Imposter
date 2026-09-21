using System;

namespace PartyGame.Core.Services
{
    /// <summary>
    /// Reusable countdown. Pure C# and driven by explicit ticks, so modes never
    /// implement their own timing and tests can fast forward without waiting.
    /// </summary>
    public class GameTimer
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsComplete { get; private set; }

        public float Elapsed => Math.Max(0f, Duration - Remaining);
        public float NormalisedRemaining => Duration <= 0f ? 0f : Math.Max(0f, Remaining / Duration);

        public event Action Completed;
        public event Action<float> Ticked;

        public void Start(float durationSeconds)
        {
            Duration = Math.Max(0f, durationSeconds);
            Remaining = Duration;
            IsRunning = Duration > 0f;
            IsPaused = false;
            IsComplete = Duration <= 0f;
            if (IsComplete) Completed?.Invoke();
        }

        public void Pause()
        {
            if (IsRunning) IsPaused = true;
        }

        public void Resume()
        {
            if (IsRunning) IsPaused = false;
        }

        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>Finishes immediately and raises <see cref="Completed"/> once.</summary>
        public void ForceComplete()
        {
            if (IsComplete) return;
            Remaining = 0f;
            IsRunning = false;
            IsComplete = true;
            Ticked?.Invoke(0f);
            Completed?.Invoke();
        }

        public void AddTime(float seconds)
        {
            if (!IsRunning) return;
            Duration += Math.Max(0f, seconds);
            Remaining += Math.Max(0f, seconds);
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsRunning || IsPaused || IsComplete) return;
            Remaining -= Math.Max(0f, deltaSeconds);
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                IsRunning = false;
                IsComplete = true;
                Ticked?.Invoke(0f);
                Completed?.Invoke();
                return;
            }
            Ticked?.Invoke(Remaining);
        }

        public void Reset()
        {
            Stop();
            IsComplete = false;
            Remaining = Duration;
        }

        public static string Format(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            var total = (int)Math.Ceiling(seconds);
            var minutes = total / 60;
            var secs = total % 60;
            return minutes > 0 ? minutes + ":" + secs.ToString("00") : secs.ToString();
        }
    }
}
