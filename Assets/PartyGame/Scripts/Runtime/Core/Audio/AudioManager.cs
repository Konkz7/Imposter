using System;
using System.Collections.Generic;
using PartyGame.Core.Persistence;
using UnityEngine;

namespace PartyGame.Core.Audio
{
    /// <summary>Every sound the app can make. UI code names an intent, never a clip.</summary>
    public enum SoundId
    {
        Tap = 0,
        Confirm = 1,
        Back = 2,
        Error = 3,
        Reveal = 4,
        Hide = 5,
        CountdownTick = 6,
        TimeUp = 7,
        Vote = 8,
        Correct = 9,
        Wrong = 10,
        RoundComplete = 11,
        Fanfare = 12
    }

    /// <summary>
    /// Owns every AudioSource in the app. Sound effects are generated as short tones at
    /// startup so the project ships with working audio and no binary assets; assigning real
    /// clips later is a drop-in replacement through <see cref="OverrideClip"/>.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private readonly Dictionary<SoundId, AudioClip> _clips = new Dictionary<SoundId, AudioClip>();

        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private SettingsService _settings;

        public void Initialise(SettingsService settings)
        {
            _settings = settings;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume = 0.5f;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 0.25f;

            BuildDefaultClips();

            if (_settings != null)
            {
                _settings.Changed += OnSettingsChanged;
                OnSettingsChanged(_settings.Settings);
            }
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.Changed -= OnSettingsChanged;
        }

        private void OnSettingsChanged(AppSettings settings)
        {
            if (settings == null) return;
            if (_musicSource != null)
            {
                _musicSource.mute = !settings.Music;
                if (!settings.Music && _musicSource.isPlaying) _musicSource.Pause();
                else if (settings.Music && _musicSource.clip != null && !_musicSource.isPlaying) _musicSource.UnPause();
            }
        }

        public void Play(SoundId id)
        {
            if (_settings != null && !_settings.Settings.SoundEffects) return;
            if (_sfxSource == null) return;
            if (!_clips.TryGetValue(id, out var clip) || clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        /// <summary>Short haptic pulse, respecting the vibration setting.</summary>
        public void Vibrate()
        {
            if (_settings != null && !_settings.Settings.Vibration) return;
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }

        /// <summary>Replaces a generated placeholder with a real clip.</summary>
        public void OverrideClip(SoundId id, AudioClip clip)
        {
            if (clip != null) _clips[id] = clip;
        }

        public void SetMusic(AudioClip clip)
        {
            if (_musicSource == null) return;
            _musicSource.clip = clip;
            if (clip != null && _settings != null && _settings.Settings.Music) _musicSource.Play();
        }

        // ------------------------------------------------------------ generated placeholders

        private void BuildDefaultClips()
        {
            _clips[SoundId.Tap] = Blip("sfx-tap", 660f, 0.05f, 0.35f);
            _clips[SoundId.Confirm] = Sweep("sfx-confirm", 520f, 780f, 0.12f, 0.4f);
            _clips[SoundId.Back] = Sweep("sfx-back", 520f, 340f, 0.1f, 0.3f);
            _clips[SoundId.Error] = Sweep("sfx-error", 300f, 180f, 0.22f, 0.45f);
            _clips[SoundId.Reveal] = Sweep("sfx-reveal", 400f, 900f, 0.25f, 0.4f);
            _clips[SoundId.Hide] = Sweep("sfx-hide", 700f, 300f, 0.18f, 0.32f);
            _clips[SoundId.CountdownTick] = Blip("sfx-tick", 880f, 0.04f, 0.25f);
            _clips[SoundId.TimeUp] = Chord("sfx-timeup", new[] { 392f, 330f, 262f }, 0.5f, 0.4f);
            _clips[SoundId.Vote] = Blip("sfx-vote", 494f, 0.08f, 0.35f);
            _clips[SoundId.Correct] = Chord("sfx-correct", new[] { 523f, 659f, 784f }, 0.4f, 0.35f);
            _clips[SoundId.Wrong] = Chord("sfx-wrong", new[] { 311f, 233f }, 0.4f, 0.35f);
            _clips[SoundId.RoundComplete] = Chord("sfx-round", new[] { 523f, 698f }, 0.35f, 0.3f);
            _clips[SoundId.Fanfare] = Arpeggio("sfx-fanfare", new[] { 523f, 659f, 784f, 1047f }, 0.13f, 0.38f);
        }

        private static AudioClip Blip(string clipName, float frequency, float duration, float volume)
        {
            return Render(clipName, duration, (t, normalised) =>
                Mathf.Sin(2f * Mathf.PI * frequency * t) * Envelope(normalised, 0.15f, 0.6f) * volume);
        }

        private static AudioClip Sweep(string clipName, float from, float to, float duration, float volume)
        {
            var phase = 0f;
            return Render(clipName, duration, (t, normalised) =>
            {
                var frequency = Mathf.Lerp(from, to, normalised);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                return Mathf.Sin(phase) * Envelope(normalised, 0.1f, 0.5f) * volume;
            });
        }

        private static AudioClip Chord(string clipName, float[] frequencies, float duration, float volume)
        {
            return Render(clipName, duration, (t, normalised) =>
            {
                var sum = 0f;
                foreach (var frequency in frequencies) sum += Mathf.Sin(2f * Mathf.PI * frequency * t);
                return sum / frequencies.Length * Envelope(normalised, 0.06f, 0.55f) * volume;
            });
        }

        private static AudioClip Arpeggio(string clipName, float[] frequencies, float noteDuration, float volume)
        {
            var total = noteDuration * frequencies.Length;
            return Render(clipName, total, (t, normalised) =>
            {
                var index = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, frequencies.Length - 1);
                var localNormalised = (t - index * noteDuration) / noteDuration;
                return Mathf.Sin(2f * Mathf.PI * frequencies[index] * t) *
                       Envelope(localNormalised, 0.08f, 0.5f) * volume;
            });
        }

        /// <summary>Simple attack/decay shape so the generated tones do not click.</summary>
        private static float Envelope(float normalised, float attack, float release)
        {
            if (normalised < attack) return normalised / Mathf.Max(0.0001f, attack);
            if (normalised > 1f - release) return Mathf.Max(0f, (1f - normalised) / Mathf.Max(0.0001f, release));
            return 1f;
        }

        private static AudioClip Render(string clipName, float duration, Func<float, float, float> sample)
        {
            var count = Mathf.Max(16, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sample(t, i / (float)count), -1f, 1f);
            }

            var clip = AudioClip.Create(clipName, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
