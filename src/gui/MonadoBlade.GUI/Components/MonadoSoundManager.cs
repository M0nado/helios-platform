using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Media;

namespace MonadoBlade.GUI
{
    /// <summary>
    /// Sound effects manager for Monado Blade.
    /// Handles kanji interactions, blade effects, loading sequences with spatial audio.
    /// </summary>
    public class MonadoSoundManager : IDisposable
    {
        private Dictionary<string, SoundPlayer> _soundCache = new Dictionary<string, SoundPlayer>();
        private float _masterVolume = 0.8f;
        private bool _soundEnabled = true;

        public float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = Math.Clamp(value, 0, 1);
        }

        public bool SoundEnabled
        {
            get => _soundEnabled;
            set => _soundEnabled = value;
        }

        public MonadoSoundManager()
        {
            // Pre-load critical sounds
            PreloadSounds();
        }

        private void PreloadSounds()
        {
            // These would be actual audio files in production
            // For now, we use system sounds as placeholders
            try
            {
                // Kanji interaction sounds (harmonic tones)
                RegisterSound("kanji_power", "tone_power.wav");       // Power (A4 440Hz)
                RegisterSound("kanji_sword", "tone_sword.wav");       // Sword (B4 494Hz)
                RegisterSound("kanji_light", "tone_light.wav");       // Light (C5 523Hz)
                RegisterSound("kanji_flow", "tone_flow.wav");         // Flow (D5 587Hz)
                RegisterSound("kanji_soul", "tone_soul.wav");         // Soul (E5 659Hz)
                RegisterSound("kanji_machine", "tone_machine.wav");   // Machine (F#5 740Hz)

                // Blade effects
                RegisterSound("blade_hover", "blade_hover.wav");
                RegisterSound("blade_charge", "blade_charge.wav");
                RegisterSound("blade_release", "blade_release.wav");

                // Loading sequence
                RegisterSound("load_start", "load_start.wav");
                RegisterSound("load_complete", "load_complete.wav");

                // Kanji glow intensify
                RegisterSound("glow_increase", "glow_increase.wav");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sound initialization error: {ex.Message}");
            }
        }

        private void RegisterSound(string name, string soundPath)
        {
            try
            {
                _soundCache[name] = new SoundPlayer(soundPath);
            }
            catch { /* Gracefully fail if sound unavailable */ }
        }

        public void PlayKanjiSound(string kanjiType)
        {
            if (!_soundEnabled) return;

            string soundKey = $"kanji_{kanjiType}";
            if (_soundCache.ContainsKey(soundKey))
            {
                try
                {
                    _soundCache[soundKey].Play();
                }
                catch { /* Silently fail */ }
            }
        }

        public void PlayBladeSound(string effect)
        {
            if (!_soundEnabled) return;

            string soundKey = $"blade_{effect}";
            if (_soundCache.ContainsKey(soundKey))
            {
                try
                {
                    _soundCache[soundKey].Play();
                }
                catch { /* Silently fail */ }
            }
        }

        public void PlayLoadingSound(string phase)
        {
            if (!_soundEnabled) return;

            string soundKey = $"load_{phase}";
            if (_soundCache.ContainsKey(soundKey))
            {
                try
                {
                    _soundCache[soundKey].Play();
                }
                catch { /* Silently fail */ }
            }
        }

        public void PlayGlowSound()
        {
            if (!_soundEnabled) return;
            PlaySound("glow_increase");
        }

        private void PlaySound(string soundKey)
        {
            if (_soundCache.ContainsKey(soundKey))
            {
                try
                {
                    _soundCache[soundKey].Play();
                }
                catch { /* Silently fail */ }
            }
        }

        public void Dispose()
        {
            foreach (var sound in _soundCache.Values)
            {
                sound?.Dispose();
            }
            _soundCache.Clear();
        }
    }

    /// <summary>
    /// Kanji glow effect manager - Synchronizes kanji glows with blade/wheel.
    /// </summary>
    public class KanjiGlowSystem
    {
        private Dictionary<string, KanjiGlowConfig> _kanjiGlows = new Dictionary<string, KanjiGlowConfig>();
        private Color _bladeBaseColor = Color.FromRgb(0, 217, 255);
        private Color _wheelBaseColor = Color.FromRgb(0, 217, 255);
        private double _maxGlowIntensity = 0;

        public event Action<Color, Color> OnGlowChanged;  // blade color, wheel color

        public KanjiGlowSystem()
        {
            InitializeKanjiGlows();
        }

        private void InitializeKanjiGlows()
        {
            _kanjiGlows["power"] = new KanjiGlowConfig
            {
                Character = "力",
                Color = Color.FromRgb(255, 0, 85),      // Magenta
                Icon = "⚡",
                BaseGlowIntensity = 0.5,
                MaxGlowIntensity = 1.0,
                EffectRadius = 100,
                SoundType = "power"
            };

            _kanjiGlows["sword"] = new KanjiGlowConfig
            {
                Character = "刀",
                Color = Color.FromRgb(255, 215, 0),     // Amber
                Icon = "⚔",
                BaseGlowIntensity = 0.6,
                MaxGlowIntensity = 1.0,
                EffectRadius = 120,
                SoundType = "sword"
            };

            _kanjiGlows["light"] = new KanjiGlowConfig
            {
                Character = "光",
                Color = Color.FromRgb(100, 200, 255),   // Light Blue
                Icon = "✨",
                BaseGlowIntensity = 0.7,
                MaxGlowIntensity = 1.0,
                EffectRadius = 110,
                SoundType = "light"
            };

            _kanjiGlows["flow"] = new KanjiGlowConfig
            {
                Character = "流",
                Color = Color.FromRgb(0, 255, 65),      // Green
                Icon = "≈",
                BaseGlowIntensity = 0.5,
                MaxGlowIntensity = 1.0,
                EffectRadius = 105,
                SoundType = "flow"
            };

            _kanjiGlows["soul"] = new KanjiGlowConfig
            {
                Character = "魂",
                Color = Color.FromRgb(255, 100, 150),   // Pink
                Icon = "♡",
                BaseGlowIntensity = 0.6,
                MaxGlowIntensity = 1.0,
                EffectRadius = 115,
                SoundType = "soul"
            };

            _kanjiGlows["machine"] = new KanjiGlowConfig
            {
                Character = "機",
                Color = Color.FromRgb(0, 217, 255),     // Cyan
                Icon = "⚙",
                BaseGlowIntensity = 0.8,
                MaxGlowIntensity = 1.0,
                EffectRadius = 100,
                SoundType = "machine"
            };
        }

        public void ActivateKanjiGlow(string kanjiKey, double intensity)
        {
            if (!_kanjiGlows.ContainsKey(kanjiKey)) return;

            var config = _kanjiGlows[kanjiKey];
            _maxGlowIntensity = Math.Max(_maxGlowIntensity, intensity);

            // Mix colors based on active kanji
            Color bladeGlow = BlendColors(_bladeBaseColor, config.Color, intensity);
            Color wheelGlow = BlendColors(_wheelBaseColor, config.Color, intensity * 0.7);

            OnGlowChanged?.Invoke(bladeGlow, wheelGlow);
        }

        public void ResetGlows()
        {
            _maxGlowIntensity = 0;
            OnGlowChanged?.Invoke(_bladeBaseColor, _wheelBaseColor);
        }

        private Color BlendColors(Color base1, Color color2, double amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            byte r = (byte)(base1.R * (1 - amount) + color2.R * amount);
            byte g = (byte)(base1.G * (1 - amount) + color2.G * amount);
            byte b = (byte)(base1.B * (1 - amount) + color2.B * amount);
            return Color.FromRgb(r, g, b);
        }

        public KanjiGlowConfig GetKanjiConfig(string key)
        {
            return _kanjiGlows.ContainsKey(key) ? _kanjiGlows[key] : null;
        }
    }

    public class KanjiGlowConfig
    {
        public string Character { get; set; }
        public Color Color { get; set; }
        public string Icon { get; set; }
        public double BaseGlowIntensity { get; set; }
        public double MaxGlowIntensity { get; set; }
        public double EffectRadius { get; set; }
        public string SoundType { get; set; }
    }
}
