using System;
using System.Collections.Generic;
using System.Media;

namespace MonadoBlade.GUI.Systems
{
    /// <summary>
    /// Unified audio controller managing all sound effects.
    /// Handles kanji tones, blade effects, and loading sequences with centralized audio management.
    /// </summary>
    public class AudioController : IDisposable
    {
        private Dictionary<string, SoundPlayer> _soundCache = new Dictionary<string, SoundPlayer>();
        private float _masterVolume = 0.8f;
        private bool _audioEnabled = true;
        private Queue<AudioJob> _audioQueue = new Queue<AudioJob>();

        public event Action<string> OnAudioPlayed;

        public float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = Math.Clamp(value, 0, 1);
        }

        public bool AudioEnabled
        {
            get => _audioEnabled;
            set => _audioEnabled = value;
        }

        public AudioController()
        {
            PreloadSounds();
        }

        /// <summary>
        /// Pre-load all common sound effects for instant playback.
        /// </summary>
        private void PreloadSounds()
        {
            try
            {
                // Kanji tone sounds (harmonic frequencies)
                RegisterSound("kanji_power", "tone_power.wav");
                RegisterSound("kanji_sword", "tone_sword.wav");
                RegisterSound("kanji_light", "tone_light.wav");
                RegisterSound("kanji_flow", "tone_flow.wav");
                RegisterSound("kanji_soul", "tone_soul.wav");
                RegisterSound("kanji_machine", "tone_machine.wav");

                // Blade effect sounds
                RegisterSound("blade_hover", "blade_hover.wav");
                RegisterSound("blade_charge", "blade_charge.wav");
                RegisterSound("blade_release", "blade_release.wav");
                RegisterSound("blade_impact", "blade_impact.wav");

                // Loading sequence sounds
                RegisterSound("load_start", "load_start.wav");
                RegisterSound("load_complete", "load_complete.wav");
                RegisterSound("load_step", "load_step.wav");

                // General effects
                RegisterSound("glow_increase", "glow_increase.wav");
                RegisterSound("success", "success.wav");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// Register a sound file in the cache.
        /// </summary>
        private void RegisterSound(string name, string soundPath)
        {
            try
            {
                _soundCache[name] = new SoundPlayer(soundPath);
            }
            catch
            {
                // Gracefully fail if sound unavailable
                System.Diagnostics.Debug.WriteLine($"Failed to load sound: {soundPath}");
            }
        }

        /// <summary>
        /// Play kanji tone based on frequency.
        /// </summary>
        public void PlayKanjiTone(int frequency, int durationMs = 200)
        {
            if (!_audioEnabled)
                return;

            // Map frequency to kanji type and play
            string soundKey = GetSoundKeyForFrequency(frequency);
            PlaySound(soundKey);
            OnAudioPlayed?.Invoke(soundKey);
        }

        /// <summary>
        /// Play kanji interaction sound by kanji ID.
        /// </summary>
        public void PlayKanjiSound(string kanjiId)
        {
            if (!_audioEnabled)
                return;

            string soundKey = $"kanji_{kanjiId}";
            PlaySound(soundKey);
            OnAudioPlayed?.Invoke(soundKey);
        }

        /// <summary>
        /// Play blade effect sound.
        /// </summary>
        public void PlayBladeEffect(string effectType)
        {
            if (!_audioEnabled)
                return;

            // Validate effect type
            if (!effectType.StartsWith("blade_"))
                effectType = $"blade_{effectType}";

            PlaySound(effectType);
            OnAudioPlayed?.Invoke(effectType);
        }

        /// <summary>
        /// Play loading sequence sound.
        /// </summary>
        public void PlayLoadingSound(string phase)
        {
            if (!_audioEnabled)
                return;

            string soundKey = $"load_{phase}";
            PlaySound(soundKey);
            OnAudioPlayed?.Invoke(soundKey);
        }

        /// <summary>
        /// Play general effect sound.
        /// </summary>
        public void PlayEffectSound(string effectName)
        {
            if (!_audioEnabled)
                return;

            PlaySound(effectName);
            OnAudioPlayed?.Invoke(effectName);
        }

        /// <summary>
        /// Internal method to play a sound by key.
        /// </summary>
        private void PlaySound(string soundKey)
        {
            if (!_audioEnabled || string.IsNullOrEmpty(soundKey))
                return;

            if (_soundCache.ContainsKey(soundKey))
            {
                try
                {
                    _soundCache[soundKey].Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error playing sound {soundKey}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Stop all audio playback.
        /// </summary>
        public void StopAllAudio()
        {
            // Note: System.Media.SoundPlayer doesn't have a built-in stop method
            // This is a placeholder for potential future audio system upgrades
            _audioQueue.Clear();
        }

        /// <summary>
        /// Queue audio to play sequentially.
        /// </summary>
        public void QueueAudio(string soundKey, float delay = 0)
        {
            _audioQueue.Enqueue(new AudioJob { SoundKey = soundKey, Delay = delay });
        }

        /// <summary>
        /// Get sound key for a given frequency.
        /// </summary>
        private string GetSoundKeyForFrequency(int frequency)
        {
            return frequency switch
            {
                BladeConstants.FREQ_POWER => "kanji_power",
                BladeConstants.FREQ_SWORD => "kanji_sword",
                BladeConstants.FREQ_LIGHT => "kanji_light",
                BladeConstants.FREQ_FLOW => "kanji_flow",
                BladeConstants.FREQ_SOUL => "kanji_soul",
                BladeConstants.FREQ_MACHINE => "kanji_machine",
                _ => "kanji_power" // Default fallback
            };
        }

        /// <summary>
        /// Set master volume level (0.0 to 1.0).
        /// </summary>
        public void SetVolume(float volume)
        {
            MasterVolume = Math.Clamp(volume, 0, 1);
        }

        /// <summary>
        /// Mute all audio.
        /// </summary>
        public void Mute()
        {
            _audioEnabled = false;
        }

        /// <summary>
        /// Unmute all audio.
        /// </summary>
        public void Unmute()
        {
            _audioEnabled = true;
        }

        /// <summary>
        /// Check if audio is currently enabled.
        /// </summary>
        public bool IsAudioEnabled()
        {
            return _audioEnabled;
        }

        /// <summary>
        /// Dispose and cleanup all audio resources.
        /// </summary>
        public void Dispose()
        {
            StopAllAudio();
            foreach (var sound in _soundCache.Values)
            {
                sound?.Dispose();
            }
            _soundCache.Clear();
            _audioQueue.Clear();
        }
    }

    /// <summary>
    /// Audio job for queue management.
    /// </summary>
    internal class AudioJob
    {
        public string SoundKey { get; set; }
        public float Delay { get; set; }
    }
}
