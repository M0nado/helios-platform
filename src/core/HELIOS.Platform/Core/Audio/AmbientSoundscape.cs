using NAudio.Wave;
using System;

namespace HELIOS.Platform.Core.Audio
{
    /// <summary>
    /// Manages dynamic ambient soundscape with time-of-day variations.
    /// Provides subtle background audio with environmental textures.
    /// </summary>
    public class AmbientSoundscape : IDisposable
    {
        private readonly int _sampleRate;
        private readonly float _targetVolume = -27f; // -24 to -30dB range

        public enum TimeOfDay
        {
            Morning,
            Afternoon,
            Evening,
            Night
        }

        public AmbientSoundscape(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
        }

        /// <summary>
        /// Generate ambient soundscape for specified time of day.
        /// </summary>
        public WaveProvider32 GenerateAmbientLoop(TimeOfDay timeOfDay, int durationSeconds = 10)
        {
            int durationSamples = _sampleRate * durationSeconds;
            float[] samples = new float[durationSamples];

            var ambientFreqs = GetAmbientFrequencies(timeOfDay);
            double phase = 0;

            for (int i = 0; i < durationSamples; i++)
            {
                float oscillator = 0;
                float progress = i / (float)durationSamples;

                // Layer multiple ambient frequencies
                for (int j = 0; j < ambientFreqs.Length; j++)
                {
                    float frequency = ambientFreqs[j];
                    double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;
                    
                    // Add slight modulation for natural variation
                    float modulation = (float)Math.Sin(phase / 100f) * 0.05f;
                    frequency += modulation;

                    oscillator += (float)Math.Sin(phase) / ambientFreqs.Length;
                    phase += phaseIncrement;
                }

                // Apply soft envelope to prevent artifacts
                float envelope = GetEnvelopeForTimeOfDay(timeOfDay, progress);

                samples[i] = oscillator * envelope * 0.3f;
            }

            // Ensure loop compatibility - fade edges
            int fadeSamples = _sampleRate / 100; // 10ms fade
            for (int i = 0; i < fadeSamples; i++)
            {
                float fadeAmount = i / (float)fadeSamples;
                samples[i] *= fadeAmount;
                
                int endIdx = durationSamples - 1 - i;
                samples[endIdx] *= fadeAmount;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate wind effect component.
        /// </summary>
        public WaveProvider32 GenerateWindEffect()
        {
            int durationMs = 2000;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            Random rand = new Random(123);
            float windFrequency = 40f; // Low frequency for wind
            double phase = 0;

            for (int i = 0; i < durationSamples; i++)
            {
                float progress = i / (float)durationSamples;

                // Low frequency oscillation for wind movement
                double phaseIncrement = 2 * Math.PI * windFrequency / _sampleRate;
                float wind = (float)Math.Sin(phase) * 0.5f;
                phase += phaseIncrement;

                // Brown noise simulation (random walk)
                float noise = (float)(rand.NextDouble() - 0.5) * 0.6f;
                
                // Combine and apply envelope
                float envelope = progress < 0.1f 
                    ? progress / 0.1f 
                    : progress > 0.9f 
                        ? (1f - progress) / 0.1f 
                        : 1f;

                samples[i] = (wind * 0.4f + noise * 0.6f) * envelope * 0.2f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate energy hum component (subtle electronic drone).
        /// </summary>
        public WaveProvider32 GenerateEnergyHum()
        {
            int durationMs = 3000;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            // Power grid hum frequencies (50Hz fundamental with harmonics)
            float[] humFreqs = { 50f, 100f, 150f };
            double phase = 0;

            for (int i = 0; i < durationSamples; i++)
            {
                float oscillator = 0;
                
                foreach (float freq in humFreqs)
                {
                    double phaseIncrement = 2 * Math.PI * freq / _sampleRate;
                    oscillator += (float)Math.Sin(phase * (freq / 50f)) / humFreqs.Length;
                    phase += phaseIncrement / humFreqs.Length;
                }

                // Very gentle envelope - almost static
                float envelope = (float)Math.Sin(i / (float)durationSamples * Math.PI) * 0.1f + 0.9f;

                samples[i] = oscillator * envelope * 0.15f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Get ambient frequencies for time of day.
        /// </summary>
        private float[] GetAmbientFrequencies(TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                TimeOfDay.Morning => new[] { 73.42f, 82.41f, 110.00f, 146.83f }, // Bright, active
                TimeOfDay.Afternoon => new[] { 82.41f, 110.00f, 146.83f, 196.00f }, // Energetic
                TimeOfDay.Evening => new[] { 65.41f, 82.41f, 110.00f, 130.81f }, // Mellower
                TimeOfDay.Night => new[] { 55.00f, 73.42f, 82.41f, 110.00f }, // Deep, dark
                _ => new[] { 82.41f, 110.00f, 146.83f }
            };
        }

        /// <summary>
        /// Get envelope variation for time of day.
        /// </summary>
        private float GetEnvelopeForTimeOfDay(TimeOfDay timeOfDay, float progress)
        {
            return timeOfDay switch
            {
                TimeOfDay.Morning => (float)Math.Sin(progress * Math.PI) * 0.8f + 0.2f,
                TimeOfDay.Afternoon => 0.8f, // Consistent
                TimeOfDay.Evening => (float)Math.Cos(progress * Math.PI * 2) * 0.2f + 0.8f,
                TimeOfDay.Night => (float)Math.Sin(progress * Math.PI * 0.5) * 0.3f + 0.7f,
                _ => 0.75f
            };
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
