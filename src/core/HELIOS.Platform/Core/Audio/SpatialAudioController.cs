using NAudio.Wave;
using System;

namespace HELIOS.Platform.Core.Audio
{
    /// <summary>
    /// Manages spatial audio positioning for UI elements.
    /// Provides 3D audio panning and distance attenuation.
    /// </summary>
    public class SpatialAudioController : IDisposable
    {
        private readonly int _sampleRate;
        private const float MaxDistance = 1000f; // Virtual distance units

        public SpatialAudioController(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
        }

        /// <summary>
        /// Apply spatial positioning to audio based on screen coordinates.
        /// </summary>
        public float[] ApplySpatialPositioning(float[] samples, float screenX, float screenY, 
                                              float screenWidth, float screenHeight, float distance = 0f)
        {
            float[] result = new float[samples.Length];
            Array.Copy(samples, result, samples.Length);

            // Convert screen position to pan amount (-1.0 to 1.0)
            float panAmount = (screenX / screenWidth) * 2f - 1f;
            panAmount = Math.Max(-1f, Math.Min(1f, panAmount)); // Clamp

            // Convert screen Y to height (vertical information - subtle)
            float heightAmount = (screenY / screenHeight) * 2f - 1f;
            heightAmount = Math.Max(-1f, Math.Min(1f, heightAmount));

            // Apply panning
            result = ApplyPanning(result, panAmount);

            // Apply distance attenuation if distance provided
            if (distance > 0)
            {
                result = ApplyDistanceAttenuation(result, distance);
            }

            // Apply subtle HRTF-like filtering based on position
            result = ApplyHRTFFiltering(result, panAmount, heightAmount);

            return result;
        }

        /// <summary>
        /// Apply L/R panning to audio.
        /// </summary>
        private float[] ApplyPanning(float[] samples, float panAmount)
        {
            float[] result = new float[samples.Length];
            
            // Calculate left and right gains using equal-power panning
            float angle = (panAmount + 1f) / 2f * (float)Math.PI / 2f; // 0 to π/2
            float leftGain = (float)Math.Cos(angle);
            float rightGain = (float)Math.Sin(angle);

            for (int i = 0; i < samples.Length; i++)
            {
                // For mono samples, apply panning amplitude modulation
                // (In stereo, would apply differently to L and R channels)
                result[i] = samples[i] * (Math.Abs(panAmount) < 0.1f ? 1f : (panAmount > 0 ? rightGain : leftGain));
            }

            return result;
        }

        /// <summary>
        /// Apply distance-based attenuation (inverse distance law).
        /// </summary>
        private float[] ApplyDistanceAttenuation(float[] samples, float distance)
        {
            float[] result = new float[samples.Length];
            
            // Distance attenuation: 1/r squared
            float gain = 1f / (1f + (distance / 100f) * (distance / 100f));
            gain = Math.Max(0.1f, gain); // Minimum gain to avoid silence

            for (int i = 0; i < samples.Length; i++)
            {
                result[i] = samples[i] * gain;
            }

            return result;
        }

        /// <summary>
        /// Apply HRTF-like filtering for spatial perception.
        /// Simplified version using amplitude and frequency shaping.
        /// </summary>
        private float[] ApplyHRTFFiltering(float[] samples, float panAmount, float heightAmount)
        {
            float[] result = new float[samples.Length];
            
            // HRTF simulation: frequency response varies with azimuth and elevation
            // Side positions (high panAmount absolute value) get more high-frequency emphasis
            // Above ear level (positive heightAmount) gets slight treble emphasis

            float highFreqEmphasis = Math.Abs(panAmount) * 0.3f + Math.Max(0, heightAmount) * 0.2f;
            
            // Simple first-order highpass-like effect
            float previousSample = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float highPass = samples[i] - previousSample;
                result[i] = samples[i] * (1f - highFreqEmphasis * 0.2f) + 
                           highPass * (highFreqEmphasis * 0.3f);
                previousSample = samples[i];
            }

            return result;
        }

        /// <summary>
        /// Apply reverb effect for room simulation.
        /// </summary>
        public float[] ApplyReverb(float[] samples, float roomSize, float damp)
        {
            float[] result = new float[samples.Length];
            Array.Copy(samples, result, samples.Length);

            // Simplified reverb using multiple delays
            int[] delaySamples = new int[]
            {
                (int)(_sampleRate * 0.005f * (0.5f + roomSize * 0.5f)), // 5ms base
                (int)(_sampleRate * 0.011f * (0.5f + roomSize * 0.5f)), // 11ms
                (int)(_sampleRate * 0.027f * (0.5f + roomSize * 0.5f)), // 27ms
                (int)(_sampleRate * 0.051f * (0.5f + roomSize * 0.5f))  // 51ms
            };

            float[] delayBuffers = new float[samples.Length];

            for (int delayIdx = 0; delayIdx < delaySamples.Length; delayIdx++)
            {
                int delaySamples_val = delaySamples[delayIdx];
                float[] buffer = new float[delaySamples_val];

                for (int i = 0; i < samples.Length; i++)
                {
                    int bufferIdx = i % delaySamples_val;
                    float delayedSample = buffer[bufferIdx];
                    
                    // Apply damping
                    buffer[bufferIdx] = samples[i] + delayedSample * (1f - damp);
                    
                    result[i] += delayedSample * 0.3f / delaySamples.Length;
                }
            }

            return result;
        }

        /// <summary>
        /// Create a stereo version with spatial positioning.
        /// </summary>
        public (float[] left, float[] right) CreateStereoFromMono(float[] monoSamples, float panAmount)
        {
            float[] left = new float[monoSamples.Length];
            float[] right = new float[monoSamples.Length];

            // Equal-power panning
            float angle = (panAmount + 1f) / 2f * (float)Math.PI / 2f;
            float leftGain = (float)Math.Cos(angle);
            float rightGain = (float)Math.Sin(angle);

            for (int i = 0; i < monoSamples.Length; i++)
            {
                left[i] = monoSamples[i] * leftGain;
                right[i] = monoSamples[i] * rightGain;
            }

            return (left, right);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
