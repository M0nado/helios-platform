using System;
using System.Collections.Generic;
using System.Windows.Media;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Systems
{
    /// <summary>
    /// Unified kanji effect system handling all kanji interactions.
    /// Centralizes hover effects, activation, color mapping, and tone generation.
    /// </summary>
    public class KanjiEffectSystem
    {
        private Dictionary<string, KanjiEffectConfig> _kanjiConfigs;
        private string _activeKanjiId;
        private double _lastActivationTime;
        private Random _random = new Random();

        public event Action<string, Color> OnKanjiHoverChanged;
        public event Action<string> OnKanjiActivated;

        public KanjiEffectSystem()
        {
            InitializeConfigurations();
        }

        /// <summary>
        /// Initialize all kanji configurations with properties and effects.
        /// </summary>
        private void InitializeConfigurations()
        {
            _kanjiConfigs = new Dictionary<string, KanjiEffectConfig>
            {
                { KanjiConstants.KANJI_POWER, new KanjiEffectConfig
                {
                    Id = KanjiConstants.KANJI_POWER,
                    Character = KanjiConstants.CHAR_POWER,
                    Icon = KanjiConstants.ICON_POWER,
                    Color = BladeConstants.COLOR_MAGENTA,
                    Frequency = BladeConstants.FREQ_POWER,
                    BaseGlowIntensity = KanjiConstants.BASE_GLOW_POWER,
                    EffectRadius = KanjiConstants.EFFECT_RADIUS_DEFAULT
                }},
                { KanjiConstants.KANJI_SWORD, new KanjiEffectConfig
                {
                    Id = KanjiConstants.KANJI_SWORD,
                    Character = KanjiConstants.CHAR_SWORD,
                    Icon = KanjiConstants.ICON_SWORD,
                    Color = BladeConstants.COLOR_GOLD,
                    Frequency = BladeConstants.FREQ_SWORD,
                    BaseGlowIntensity = KanjiConstants.BASE_GLOW_SWORD,
                    EffectRadius = KanjiConstants.EFFECT_RADIUS_SWORD
                }},
                { KanjiConstants.KANJI_LIGHT, new KanjiEffectConfig
                {
                    Id = KanjiConstants.KANJI_LIGHT,
                    Character = KanjiConstants.CHAR_LIGHT,
                    Icon = KanjiConstants.ICON_LIGHT,
                    Color = BladeConstants.COLOR_LIGHT_BLUE,
                    Frequency = BladeConstants.FREQ_LIGHT,
                    BaseGlowIntensity = KanjiConstants.BASE_GLOW_LIGHT,
                    EffectRadius = KanjiConstants.EFFECT_RADIUS_LIGHT
                }},
                { KanjiConstants.KANJI_FLOW, new KanjiEffectConfig
                {
                    Id = KanjiConstants.KANJI_FLOW,
                    Character = KanjiConstants.CHAR_FLOW,
                    Icon = KanjiConstants.ICON_FLOW,
                    Color = BladeConstants.COLOR_GREEN,
                    Frequency = BladeConstants.FREQ_FLOW,
                    BaseGlowIntensity = KanjiConstants.BASE_GLOW_FLOW,
                    EffectRadius = KanjiConstants.EFFECT_RADIUS_FLOW
                }},
                { KanjiConstants.KANJI_SOUL, new KanjiEffectConfig
                {
                    Id = KanjiConstants.KANJI_SOUL,
                    Character = KanjiConstants.CHAR_SOUL,
                    Icon = KanjiConstants.ICON_SOUL,
                    Color = BladeConstants.COLOR_PINK,
                    Frequency = BladeConstants.FREQ_SOUL,
                    BaseGlowIntensity = KanjiConstants.BASE_GLOW_SOUL,
                    EffectRadius = KanjiConstants.EFFECT_RADIUS_SOUL
                }},
                { KanjiConstants.KANJI_MACHINE, new KanjiEffectConfig
                {
                    Id = KanjiConstants.KANJI_MACHINE,
                    Character = KanjiConstants.CHAR_MACHINE,
                    Icon = KanjiConstants.ICON_MACHINE,
                    Color = BladeConstants.COLOR_CYAN,
                    Frequency = BladeConstants.FREQ_MACHINE,
                    BaseGlowIntensity = KanjiConstants.BASE_GLOW_MACHINE,
                    EffectRadius = KanjiConstants.EFFECT_RADIUS_DEFAULT
                }}
            };
        }

        /// <summary>
        /// Handle kanji hover interaction - triggers glow and color change.
        /// </summary>
        public void OnKanjiHover(string kanjiId)
        {
            if (!_kanjiConfigs.ContainsKey(kanjiId))
                return;

            var config = _kanjiConfigs[kanjiId];
            _activeKanjiId = kanjiId;
            
            // Notify subscribers of color change with enhanced glow
            Color hoverColor = BlendWithGlow(config.Color, BladeConstants.GLOW_HOVER);
            OnKanjiHoverChanged?.Invoke(kanjiId, hoverColor);
        }

        /// <summary>
        /// Handle kanji activation - triggers full effects.
        /// </summary>
        public void OnKanjiActive(string kanjiId)
        {
            if (!_kanjiConfigs.ContainsKey(kanjiId))
                return;

            _activeKanjiId = kanjiId;
            _lastActivationTime = DateTime.Now.Millisecond;
            OnKanjiActivated?.Invoke(kanjiId);
        }

        /// <summary>
        /// Get the frequency (Hz) associated with a kanji for tone generation.
        /// </summary>
        public int GetKanjiTone(string kanjiId)
        {
            if (_kanjiConfigs.ContainsKey(kanjiId))
            {
                return _kanjiConfigs[kanjiId].Frequency;
            }
            return BladeConstants.FREQ_POWER; // Default fallback
        }

        /// <summary>
        /// Get the color associated with a kanji.
        /// </summary>
        public Color GetKanjiColor(string kanjiId)
        {
            if (_kanjiConfigs.ContainsKey(kanjiId))
            {
                return _kanjiConfigs[kanjiId].Color;
            }
            return BladeConstants.COLOR_CYAN; // Default fallback
        }

        /// <summary>
        /// Get complete kanji configuration.
        /// </summary>
        public KanjiEffectConfig GetKanjiConfig(string kanjiId)
        {
            return _kanjiConfigs.ContainsKey(kanjiId) ? _kanjiConfigs[kanjiId] : null;
        }

        /// <summary>
        /// Get currently active kanji ID.
        /// </summary>
        public string GetActiveKanjiId()
        {
            return _activeKanjiId;
        }

        /// <summary>
        /// Blend a color with white glow based on intensity.
        /// </summary>
        private Color BlendWithGlow(Color baseColor, double glowIntensity)
        {
            glowIntensity = Math.Clamp(glowIntensity, 0, 1);
            byte r = (byte)(baseColor.R + (255 - baseColor.R) * glowIntensity * 0.5);
            byte g = (byte)(baseColor.G + (255 - baseColor.G) * glowIntensity * 0.5);
            byte b = (byte)(baseColor.B + (255 - baseColor.B) * glowIntensity * 0.5);
            return Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// Check if a kanji is currently active.
        /// </summary>
        public bool IsKanjiActive(string kanjiId)
        {
            return _activeKanjiId == kanjiId;
        }

        /// <summary>
        /// Reset active kanji state.
        /// </summary>
        public void Reset()
        {
            _activeKanjiId = null;
            _lastActivationTime = 0;
        }
    }

    /// <summary>
    /// Configuration data for a single kanji effect.
    /// </summary>
    public class KanjiEffectConfig
    {
        public string Id { get; set; }
        public string Character { get; set; }
        public string Icon { get; set; }
        public Color Color { get; set; }
        public int Frequency { get; set; }
        public double BaseGlowIntensity { get; set; }
        public double EffectRadius { get; set; }
    }
}
