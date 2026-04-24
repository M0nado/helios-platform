using NAudio.Wave;
using System;

namespace HELIOS.Platform.Core.Audio
{
    /// <summary>
    /// Generates blade effect sounds for UI interactions.
    /// Includes laser beam sound, glow pulse, and expansion whoosh effects.
    /// </summary>
    public class BladeEffectSoundGenerator : IDisposable
    {
        private readonly int _sampleRate;

        public BladeEffectSoundGenerator(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
        }

        /// <summary>
        /// Generate a laser sound effect (sci-fi laser tone).
        /// </summary>
        public WaveProvider32 GenerateLaserSound()
        {
            int durationMs = 200;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            // Laser: descending frequency sweep from 2000Hz to 400Hz
            double phase = 0;
            float startFreq = 2000f;
            float endFreq = 400f;

            for (int i = 0; i < durationSamples; i++)
            {
                float progress = i / (float)durationSamples;
                
                // Frequency sweep
                float frequency = startFreq + (endFreq - startFreq) * progress;
                double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;

                // Generate square-like wave for laser sound
                float oscillator = (float)Math.Sin(phase) + (float)Math.Sin(phase * 1.5f) * 0.3f;
                phase += phaseIncrement;

                // Envelope: quick attack, exponential release
                float envelope = (float)Math.Exp(-progress * 3f);

                samples[i] = oscillator * envelope * 0.6f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate a glow pulse sound (soft shimmer).
        /// </summary>
        public WaveProvider32 GenerateGlowPulse()
        {
            int durationMs = 150;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            double phase = 0;
            
            // Glow: combination of harmonics for shimmer effect
            float[] harmonics = { 800f, 1200f, 1600f };

            for (int i = 0; i < durationSamples; i++)
            {
                float progress = i / (float)durationSamples;
                float oscillator = 0;

                // Add harmonics
                foreach (float harmonic in harmonics)
                {
                    double phaseIncrement = 2 * Math.PI * harmonic / _sampleRate;
                    oscillator += (float)Math.Sin(phase * (harmonic / 800f)) * (1f / harmonics.Length);
                    phase += phaseIncrement / harmonics.Length;
                }

                // Smooth envelope with early peak then decay
                float envelope = progress < 0.3f 
                    ? progress / 0.3f 
                    : (float)Math.Exp(-(progress - 0.3f) * 4f);

                samples[i] = oscillator * envelope * 0.5f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate an expansion sound (whoosh effect).
        /// </summary>
        public WaveProvider32 GenerateExpansionSound()
        {
            int durationMs = 300;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            double phase = 0;
            
            // Whoosh: rising frequency sweep with noise component
            float startFreq = 300f;
            float endFreq = 1500f;

            Random rand = new Random(42); // Deterministic random

            for (int i = 0; i < durationSamples; i++)
            {
                float progress = i / (float)durationSamples;

                // Frequency sweep
                float frequency = startFreq + (endFreq - startFreq) * (float)Math.Sqrt(progress);
                double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;

                // Sine wave component
                float oscillator = (float)Math.Sin(phase);
                phase += phaseIncrement;

                // Add noise component for whoosh texture
                float noise = (float)(rand.NextDouble() - 0.5) * 0.3f;
                oscillator = oscillator * 0.7f + noise * 0.3f;

                // Envelope: quick attack, linear decay
                float envelope = 1.0f - progress;

                samples[i] = oscillator * envelope * 0.6f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate a complete blade effect (combination of effects).
        /// </summary>
        public WaveProvider32 GenerateCompleteBladeEffect()
        {
            // Sequence: laser -> glow -> expansion
            var laserData = GenerateToneData(GenerateLaserSound());
            var glowData = GenerateToneData(GenerateGlowPulse());
            var expansionData = GenerateToneData(GenerateExpansionSound());

            int totalSamples = laserData.Length + glowData.Length + expansionData.Length;
            float[] combined = new float[totalSamples];

            Array.Copy(laserData, 0, combined, 0, laserData.Length);
            Array.Copy(glowData, 0, combined, laserData.Length, glowData.Length);
            Array.Copy(expansionData, 0, combined, laserData.Length + glowData.Length, expansionData.Length);

            return new FloatToneProvider(combined, _sampleRate);
        }

        /// <summary>
        /// Extract float array from WaveProvider32.
        /// </summary>
        private float[] GenerateToneData(WaveProvider32 provider)
        {
            var list = new System.Collections.Generic.List<float>();
            float[] buffer = new float[1024];
            int count;

            while ((count = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                list.AddRange(buffer[..count]);
            }

            return list.ToArray();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
