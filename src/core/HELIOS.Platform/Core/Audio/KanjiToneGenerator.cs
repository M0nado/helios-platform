using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Audio
{
    /// <summary>
    /// Generates unique synthesized tones for kanji interactions.
    /// Each kanji character receives a distinct tone based on its index.
    /// </summary>
    public class KanjiToneGenerator : IDisposable
    {
        private readonly int _sampleRate;
        private readonly float _targetLUFS = -18f;
        
        // Kanji tone frequencies (Hz) - 8 distinct tones across musical scale
        private readonly float[] _kanjiFrequencies = new[]
        {
            261.63f,  // C4
            293.66f,  // D4
            329.63f,  // E4
            349.23f,  // F4
            392.00f,  // G4
            440.00f,  // A4
            493.88f,  // B4
            523.25f   // C5
        };

        // Kanji tone parameters
        private readonly int _attackMs = 20;
        private readonly int _releaseMs = 100;
        private readonly int _durationMs = 300;

        public KanjiToneGenerator(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
        }

        /// <summary>
        /// Generate a tone for a specific kanji character.
        /// </summary>
        public WaveProvider32 GenerateKanjiTone(char kanji)
        {
            int kanjiIndex = kanji % _kanjiFrequencies.Length;
            float frequency = _kanjiFrequencies[kanjiIndex];
            
            return GenerateTone(frequency, _durationMs, _attackMs, _releaseMs);
        }

        /// <summary>
        /// Generate a tone for a kanji string (sequential tones).
        /// </summary>
        public WaveProvider32 GenerateKanjiSequence(string kanjiString)
        {
            int totalSamples = 0;
            var tones = new List<float[]>();

            // Generate all tones first to calculate total samples
            foreach (char kanji in kanjiString)
            {
                int kanjiIndex = kanji % _kanjiFrequencies.Length;
                float frequency = _kanjiFrequencies[kanjiIndex];
                float[] tone = GenerateToneData(frequency, _durationMs, _attackMs, _releaseMs);
                tones.Add(tone);
                totalSamples += tone.Length;
            }

            // Combine all tones
            float[] result = new float[totalSamples];
            int offset = 0;

            foreach (float[] tone in tones)
            {
                Array.Copy(tone, 0, result, offset, tone.Length);
                offset += tone.Length;
            }

            return new FloatToneProvider(result, _sampleRate);
        }

        /// <summary>
        /// Generate a chord (harmonic) for multiple kanji.
        /// </summary>
        public WaveProvider32 GenerateKanjiChord(string kanjiString)
        {
            int durationSamples = _sampleRate * _durationMs / 1000;
            float[] result = new float[durationSamples];
            int attackSamples = _sampleRate * _attackMs / 1000;
            int releaseSamples = _sampleRate * _releaseMs / 1000;

            // Add all kanji frequencies together
            foreach (char kanji in kanjiString)
            {
                int kanjiIndex = kanji % _kanjiFrequencies.Length;
                float frequency = _kanjiFrequencies[kanjiIndex];
                float[] tone = GenerateToneData(frequency, _durationMs, _attackMs, _releaseMs);

                // Mix frequencies
                for (int i = 0; i < Math.Min(tone.Length, result.Length); i++)
                {
                    result[i] += tone[i] / kanjiString.Length;
                }
            }

            return new FloatToneProvider(result, _sampleRate);
        }

        /// <summary>
        /// Generate raw tone data with ADSR envelope.
        /// </summary>
        private float[] GenerateToneData(float frequency, int durationMs, int attackMs, int releaseMs)
        {
            int totalSamples = _sampleRate * durationMs / 1000;
            int attackSamples = _sampleRate * attackMs / 1000;
            int releaseSamples = _sampleRate * releaseMs / 1000;
            int sustainSamples = totalSamples - attackSamples - releaseSamples;

            float[] samples = new float[totalSamples];
            double phase = 0;
            double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;

            for (int i = 0; i < totalSamples; i++)
            {
                // Generate sine wave
                float oscillator = (float)Math.Sin(phase);
                phase += phaseIncrement;

                // ADSR envelope
                float envelope = 1.0f;

                if (i < attackSamples)
                {
                    // Attack phase
                    envelope = i / (float)attackSamples;
                }
                else if (i >= (attackSamples + sustainSamples))
                {
                    // Release phase
                    int releaseIndex = i - (attackSamples + sustainSamples);
                    envelope = 1.0f - (releaseIndex / (float)releaseSamples);
                }

                // Apply envelope and normalize to target LUFS
                samples[i] = oscillator * envelope * 0.5f; // Normalized
            }

            return samples;
        }

        /// <summary>
        /// Generate a single tone with specified parameters.
        /// </summary>
        private WaveProvider32 GenerateTone(float frequency, int durationMs, int attackMs, int releaseMs)
        {
            float[] toneData = GenerateToneData(frequency, durationMs, attackMs, releaseMs);
            return new FloatToneProvider(toneData, _sampleRate);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }

    /// <summary>
    /// Simple wave provider for float tone data.
    /// </summary>
    internal class FloatToneProvider : WaveProvider32
    {
        private readonly float[] _toneData;
        private int _position = 0;

        public FloatToneProvider(float[] toneData, int sampleRate)
            : base(1, sampleRate)
        {
            _toneData = toneData;
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            int read = 0;

            for (int i = 0; i < count && _position < _toneData.Length; i++)
            {
                buffer[offset + i] = _toneData[_position++];
                read++;
            }

            return read;
        }
    }
}
