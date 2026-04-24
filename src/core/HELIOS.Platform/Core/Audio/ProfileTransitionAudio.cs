using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Audio
{
    /// <summary>
    /// Manages audio transitions for user profile switches.
    /// Provides smooth crossfades between different profile audio themes.
    /// </summary>
    public class ProfileTransitionAudio : IDisposable
    {
        private readonly int _sampleRate;
        private readonly Dictionary<string, (float frequency, float[] samples)> _profileThemes;

        public ProfileTransitionAudio(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
            _profileThemes = new Dictionary<string, (float, float[])>();
            InitializeProfileThemes();
        }

        /// <summary>
        /// Initialize default profile themes.
        /// </summary>
        private void InitializeProfileThemes()
        {
            // Work profile: focused, efficient
            _profileThemes["Work"] = (440f, GenerateProfileTone(440f, "Work"));

            // Creative profile: open, flowing
            _profileThemes["Creative"] = (523.25f, GenerateProfileTone(523.25f, "Creative"));

            // Gaming profile: energetic, playful
            _profileThemes["Gaming"] = (659.25f, GenerateProfileTone(659.25f, "Gaming"));

            // Rest profile: calm, restorative
            _profileThemes["Rest"] = (261.63f, GenerateProfileTone(261.63f, "Rest"));

            // Default profile
            _profileThemes["Default"] = (392f, GenerateProfileTone(392f, "Default"));
        }

        /// <summary>
        /// Generate profile-specific tone.
        /// </summary>
        private float[] GenerateProfileTone(float frequency, string profileName)
        {
            int durationMs = 400;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            double phase = 0;
            int attackMs = 50;
            int releaseMs = 150;
            int attackSamples = _sampleRate * attackMs / 1000;
            int releaseSamples = _sampleRate * releaseMs / 1000;

            for (int i = 0; i < durationSamples; i++)
            {
                double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;
                
                // Generate harmonic-rich tone
                float oscillator = (float)Math.Sin(phase) * 0.7f;
                oscillator += (float)Math.Sin(phase * 2) * 0.2f;
                oscillator += (float)Math.Sin(phase * 3) * 0.1f;
                
                phase += phaseIncrement;

                // ADSR envelope
                float envelope = 1.0f;
                if (i < attackSamples)
                {
                    envelope = i / (float)attackSamples;
                }
                else if (i >= durationSamples - releaseSamples)
                {
                    int releaseIndex = i - (durationSamples - releaseSamples);
                    envelope = 1.0f - (releaseIndex / (float)releaseSamples);
                }

                samples[i] = oscillator * envelope * 0.5f;
            }

            return samples;
        }

        /// <summary>
        /// Generate smooth crossfade between two profiles.
        /// </summary>
        public WaveProvider32 GenerateProfileTransition(string fromProfile, string toProfile)
        {
            if (!_profileThemes.ContainsKey(fromProfile))
                fromProfile = "Default";
            if (!_profileThemes.ContainsKey(toProfile))
                toProfile = "Default";

            int durationMs = 400;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] result = new float[durationSamples];

            var (fromFreq, fromSamples) = _profileThemes[fromProfile];
            var (toFreq, toSamples) = _profileThemes[toProfile];

            for (int i = 0; i < durationSamples; i++)
            {
                float progress = i / (float)durationSamples;

                // Crossfade using sinusoidal interpolation for smoothness
                float fromAmount = (float)Math.Cos(progress * Math.PI / 2);
                float toAmount = (float)Math.Sin(progress * Math.PI / 2);

                // Mix the two profile tones
                float fromSample = i < fromSamples.Length ? fromSamples[i] : 0;
                float toSample = i < toSamples.Length ? toSamples[i] : 0;

                result[i] = (fromSample * fromAmount + toSample * toAmount) * 0.6f;
            }

            return new FloatToneProvider(result, _sampleRate);
        }

        /// <summary>
        /// Register a custom profile theme.
        /// </summary>
        public void RegisterProfileTheme(string profileName, float frequency)
        {
            _profileThemes[profileName] = (frequency, GenerateProfileTone(frequency, profileName));
        }

        /// <summary>
        /// Get notification sound for profile change.
        /// </summary>
        public WaveProvider32 GetProfileChangeNotification()
        {
            int durationMs = 200;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            // Quick ascending arpeggio: G-B-D
            float[] frequencies = { 392f, 493.88f, 587.33f };
            double phase = 0;

            int noteDurationSamples = durationSamples / frequencies.Length;

            for (int i = 0; i < durationSamples; i++)
            {
                int noteIndex = i / noteDurationSamples;
                if (noteIndex >= frequencies.Length)
                    noteIndex = frequencies.Length - 1;

                float frequency = frequencies[noteIndex];
                double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;

                float oscillator = (float)Math.Sin(phase);
                phase += phaseIncrement;

                // Quick envelope per note
                float noteProgress = (i % noteDurationSamples) / (float)noteDurationSamples;
                float envelope = 1.0f - (float)Math.Pow(noteProgress, 0.3f);

                samples[i] = oscillator * envelope * 0.6f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
