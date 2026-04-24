using System;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Core.Audio
{
    /// <summary>
    /// Manages audio effect chains with reverb, EQ, compression, and delay.
    /// Supports real-time parameter control and effect bypass.
    /// </summary>
    public class AudioEffectChain : IDisposable
    {
        private readonly int _sampleRate;
        private readonly List<IAudioEffect> _effects;
        private bool _enabled = true;

        public AudioEffectChain(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
            _effects = new List<IAudioEffect>();
        }

        /// <summary>
        /// Add an effect to the chain.
        /// </summary>
        public void AddEffect(IAudioEffect effect)
        {
            _effects.Add(effect);
        }

        /// <summary>
        /// Remove an effect from the chain.
        /// </summary>
        public void RemoveEffect(IAudioEffect effect)
        {
            _effects.Remove(effect);
        }

        /// <summary>
        /// Process audio through the entire effect chain.
        /// </summary>
        public float[] ProcessAudio(float[] samples)
        {
            if (!_enabled) return samples;

            float[] result = new float[samples.Length];
            Array.Copy(samples, result, samples.Length);

            foreach (var effect in _effects.Where(e => e.Enabled))
            {
                result = effect.Process(result);
            }

            return result;
        }

        /// <summary>
        /// Enable/disable the entire effect chain.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// Create a standard effect chain (reverb -> compression -> EQ).
        /// </summary>
        public static AudioEffectChain CreateStandardChain(int sampleRate = 44100)
        {
            var chain = new AudioEffectChain(sampleRate);
            
            chain.AddEffect(new ReverbEffect(sampleRate) { RoomSize = 0.5f });
            chain.AddEffect(new CompressionEffect(sampleRate) { Threshold = -20f, Ratio = 2f });
            chain.AddEffect(new EQEffect(sampleRate) { LowGain = 0f, MidGain = 2f, HighGain = 2f });
            
            return chain;
        }

        public void Dispose()
        {
            foreach (var effect in _effects)
            {
                (effect as IDisposable)?.Dispose();
            }
            _effects.Clear();
        }
    }

    /// <summary>
    /// Interface for audio effects.
    /// </summary>
    public interface IAudioEffect
    {
        float[] Process(float[] samples);
        bool Enabled { get; set; }
    }

    /// <summary>
    /// Reverb effect implementation.
    /// </summary>
    public class ReverbEffect : IAudioEffect
    {
        private readonly int _sampleRate;
        private readonly List<float[]> _delayBuffers;
        private readonly int[] _delaySamples;

        public float RoomSize { get; set; } = 0.5f;
        public float Damp { get; set; } = 0.5f;
        public float Wet { get; set; } = 0.33f;
        public float Dry { get; set; } = 0.4f;
        public bool Enabled { get; set; } = true;

        public ReverbEffect(int sampleRate)
        {
            _sampleRate = sampleRate;
            _delayBuffers = new List<float[]>();
            
            // Initialize delay times (in samples)
            _delaySamples = new int[]
            {
                (int)(_sampleRate * 0.0297f),
                (int)(_sampleRate * 0.0371f),
                (int)(_sampleRate * 0.0411f),
                (int)(_sampleRate * 0.0437f)
            };

            foreach (var delay in _delaySamples)
            {
                _delayBuffers.Add(new float[delay]);
            }
        }

        public float[] Process(float[] samples)
        {
            float[] result = new float[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                float wetSignal = 0;

                for (int j = 0; j < _delayBuffers.Count; j++)
                {
                    var buffer = _delayBuffers[j];
                    int idx = i % buffer.Length;

                    float delayed = buffer[idx];
                    buffer[idx] = samples[i] + (delayed * Damp);

                    wetSignal += delayed / _delayBuffers.Count;
                }

                result[i] = samples[i] * Dry + wetSignal * Wet;
            }

            return result;
        }
    }

    /// <summary>
    /// Compression effect implementation.
    /// </summary>
    public class CompressionEffect : IAudioEffect
    {
        private readonly int _sampleRate;
        private float _envelope = 0;

        public float Threshold { get; set; } = -20f;
        public float Ratio { get; set; } = 4f;
        public float Attack { get; set; } = 0.005f; // 5ms
        public float Release { get; set; } = 0.05f; // 50ms
        public bool Enabled { get; set; } = true;

        public CompressionEffect(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public float[] Process(float[] samples)
        {
            float[] result = new float[samples.Length];
            float attackSamples = _sampleRate * Attack;
            float releaseSamples = _sampleRate * Release;

            for (int i = 0; i < samples.Length; i++)
            {
                float sampleDB = 20f * (float)Math.Log10(Math.Abs(samples[i]) + 1e-10f);

                float gainReduction = 0;
                if (sampleDB > Threshold)
                {
                    gainReduction = (sampleDB - Threshold) * (1f - 1f / Ratio) * -1f;
                }

                float targetGain = (float)Math.Pow(10f, gainReduction / 20f);
                float isAttack = targetGain < _envelope ? 1f : 0f;
                float timeConstant = isAttack * attackSamples + (1f - isAttack) * releaseSamples;

                if (timeConstant > 0)
                {
                    _envelope += (targetGain - _envelope) / timeConstant;
                }

                result[i] = samples[i] * _envelope;
            }

            return result;
        }
    }

    /// <summary>
    /// EQ effect implementation (3-band).
    /// </summary>
    public class EQEffect : IAudioEffect
    {
        private readonly int _sampleRate;

        public float LowGain { get; set; } = 0f;   // dB
        public float MidGain { get; set; } = 0f;   // dB
        public float HighGain { get; set; } = 0f;  // dB
        public bool Enabled { get; set; } = true;

        public EQEffect(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public float[] Process(float[] samples)
        {
            float[] result = new float[samples.Length];
            Array.Copy(samples, result, samples.Length);

            // Apply low shelf (simplified)
            result = ApplyLowBand(result);

            // Apply mid peak (simplified)
            result = ApplyMidBand(result);

            // Apply high shelf (simplified)
            result = ApplyHighBand(result);

            return result;
        }

        private float[] ApplyLowBand(float[] samples)
        {
            float gain = (float)Math.Pow(10f, LowGain / 20f);
            float[] result = new float[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                result[i] = samples[i] * (1f + (gain - 1f) * (i % 200 < 100 ? 0.5f : 1f));
            }

            return result;
        }

        private float[] ApplyMidBand(float[] samples)
        {
            float gain = (float)Math.Pow(10f, MidGain / 20f);
            float[] result = new float[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                result[i] = samples[i] * gain;
            }

            return result;
        }

        private float[] ApplyHighBand(float[] samples)
        {
            float gain = (float)Math.Pow(10f, HighGain / 20f);
            float[] result = new float[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                result[i] = samples[i] * (1f + (gain - 1f) * (i % 200 >= 100 ? 0.5f : 1f));
            }

            return result;
        }
    }

    /// <summary>
    /// Delay effect implementation.
    /// </summary>
    public class DelayEffect : IAudioEffect
    {
        private readonly int _sampleRate;
        private readonly float[] _delayBuffer;
        private int _writeIndex = 0;

        public float DelayTime { get; set; } = 0.25f; // 250ms
        public float Feedback { get; set; } = 0.6f;
        public float Wet { get; set; } = 0.5f;
        public bool Enabled { get; set; } = true;

        public DelayEffect(int sampleRate)
        {
            _sampleRate = sampleRate;
            _delayBuffer = new float[(int)(sampleRate * 2)]; // 2 second max delay
        }

        public float[] Process(float[] samples)
        {
            float[] result = new float[samples.Length];
            int delaySamples = (int)(_sampleRate * DelayTime);

            for (int i = 0; i < samples.Length; i++)
            {
                int readIndex = (_writeIndex - delaySamples + _delayBuffer.Length) % _delayBuffer.Length;
                float delayedSample = _delayBuffer[readIndex];

                _delayBuffer[_writeIndex] = samples[i] + (delayedSample * Feedback);
                _writeIndex = (_writeIndex + 1) % _delayBuffer.Length;

                result[i] = samples[i] * (1f - Wet) + delayedSample * Wet;
            }

            return result;
        }
    }
}
