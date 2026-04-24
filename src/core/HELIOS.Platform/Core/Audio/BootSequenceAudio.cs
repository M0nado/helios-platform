using NAudio.Wave;
using System;

namespace HELIOS.Platform.Core.Audio
{
    /// <summary>
    /// Manages audio sequences for system boot and load screens.
    /// Includes startup tone, progress cues, and completion fanfare.
    /// </summary>
    public class BootSequenceAudio : IDisposable
    {
        private readonly int _sampleRate;

        public BootSequenceAudio(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
        }

        /// <summary>
        /// Generate initial startup tone (ascending notes).
        /// </summary>
        public WaveProvider32 GenerateStartupTone()
        {
            // Ascending sequence: C-E-G (major triad)
            float[] frequencies = { 261.63f, 329.63f, 392.00f };
            int noteDurationMs = 150;
            int totalDurationMs = noteDurationMs * frequencies.Length;
            int totalSamples = _sampleRate * totalDurationMs / 1000;
            float[] samples = new float[totalSamples];

            double phase = 0;
            int noteDurationSamples = _sampleRate * noteDurationMs / 1000;

            for (int i = 0; i < totalSamples; i++)
            {
                int noteIndex = i / noteDurationSamples;
                if (noteIndex >= frequencies.Length)
                    noteIndex = frequencies.Length - 1;

                float frequency = frequencies[noteIndex];
                double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;

                // Harmonic-rich tone
                float oscillator = (float)Math.Sin(phase) * 0.8f;
                oscillator += (float)Math.Sin(phase * 2) * 0.15f;
                phase += phaseIncrement;

                // Envelope for each note
                float noteProgress = (i % noteDurationSamples) / (float)noteDurationSamples;
                float envelope = 1.0f;
                
                if (noteProgress < 0.1f)
                    envelope = noteProgress / 0.1f; // Attack
                else if (noteProgress > 0.8f)
                    envelope = (1.0f - noteProgress) / 0.2f; // Release

                samples[i] = oscillator * envelope * 0.5f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate loading progress cue at specific percentage.
        /// </summary>
        public WaveProvider32 GenerateProgressCue(int percentage)
        {
            // Different tone for each checkpoint
            float frequency = percentage switch
            {
                25 => 440f,   // A4
                50 => 523.25f, // C5
                75 => 587.33f, // D5
                100 => 659.25f, // E5
                _ => 440f
            };

            int durationMs = 100;
            int durationSamples = _sampleRate * durationMs / 1000;
            float[] samples = new float[durationSamples];

            double phase = 0;

            for (int i = 0; i < durationSamples; i++)
            {
                double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;
                float oscillator = (float)Math.Sin(phase);
                phase += phaseIncrement;

                // Quick envelope
                float progress = i / (float)durationSamples;
                float envelope = (float)Math.Exp(-progress * 4f);

                samples[i] = oscillator * envelope * 0.4f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate completion fanfare (ascending arpeggio).
        /// </summary>
        public WaveProvider32 GenerateCompletionFanfare()
        {
            // Ascending arpeggio: C-E-G-C (major triad)
            float[] frequencies = { 261.63f, 329.63f, 392.00f, 523.25f };
            int noteDurationMs = 200;
            int totalDurationMs = noteDurationMs * frequencies.Length;
            int totalSamples = _sampleRate * totalDurationMs / 1000;
            float[] samples = new float[totalSamples];

            double phase = 0;
            int noteDurationSamples = _sampleRate * noteDurationMs / 1000;

            for (int i = 0; i < totalSamples; i++)
            {
                int noteIndex = i / noteDurationSamples;
                if (noteIndex >= frequencies.Length)
                    noteIndex = frequencies.Length - 1;

                float frequency = frequencies[noteIndex];
                double phaseIncrement = 2 * Math.PI * frequency / _sampleRate;

                // Rich harmonic content
                float oscillator = (float)Math.Sin(phase) * 0.7f;
                oscillator += (float)Math.Sin(phase * 2) * 0.2f;
                oscillator += (float)Math.Sin(phase * 3) * 0.1f;
                phase += phaseIncrement;

                // Smooth envelope per note
                float noteProgress = (i % noteDurationSamples) / (float)noteDurationSamples;
                float envelope = 1.0f;
                
                if (noteProgress < 0.15f)
                    envelope = noteProgress / 0.15f; // Quick attack
                else if (noteProgress > 0.75f)
                    envelope = (1.0f - noteProgress) / 0.25f; // Smooth release

                samples[i] = oscillator * envelope * 0.5f;
            }

            return new FloatToneProvider(samples, _sampleRate);
        }

        /// <summary>
        /// Generate complete boot sequence (startup + progress + fanfare).
        /// </summary>
        public WaveProvider32 GenerateCompleteBootSequence()
        {
            var startupData = ExtractToneData(GenerateStartupTone());
            var cue25Data = ExtractToneData(GenerateProgressCue(25));
            var cue50Data = ExtractToneData(GenerateProgressCue(50));
            var cue75Data = ExtractToneData(GenerateProgressCue(75));
            var fanfareData = ExtractToneData(GenerateCompletionFanfare());

            // Add silence between elements
            int silenceSamples = _sampleRate / 5; // 200ms silence

            int totalSamples = startupData.Length + silenceSamples +
                              cue25Data.Length + silenceSamples +
                              cue50Data.Length + silenceSamples +
                              cue75Data.Length + silenceSamples +
                              fanfareData.Length;

            float[] combined = new float[totalSamples];
            int offset = 0;

            // Add startup
            Array.Copy(startupData, 0, combined, offset, startupData.Length);
            offset += startupData.Length + silenceSamples;

            // Add progress cues
            Array.Copy(cue25Data, 0, combined, offset, cue25Data.Length);
            offset += cue25Data.Length + silenceSamples;

            Array.Copy(cue50Data, 0, combined, offset, cue50Data.Length);
            offset += cue50Data.Length + silenceSamples;

            Array.Copy(cue75Data, 0, combined, offset, cue75Data.Length);
            offset += cue75Data.Length + silenceSamples;

            // Add fanfare
            Array.Copy(fanfareData, 0, combined, offset, fanfareData.Length);

            return new FloatToneProvider(combined, _sampleRate);
        }

        /// <summary>
        /// Extract float array from WaveProvider32.
        /// </summary>
        private float[] ExtractToneData(WaveProvider32 provider)
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
