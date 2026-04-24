using System;
using System.Windows.Media;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Systems
{
    /// <summary>
    /// Unified blade visualization controller managing all blade rendering effects.
    /// Handles color updates, scale transformations, glow effects, and particle emission.
    /// </summary>
    public class BladeVisualsController
    {
        private Color _currentBladeColor = BladeConstants.COLOR_CYAN;
        private double _currentScale = BladeConstants.SCALE_IDLE;
        private double _currentGlow = BladeConstants.GLOW_IDLE;
        private ParticleSystem _particleSystem;

        public event Action<Color> OnBladeColorChanged;
        public event Action<double> OnBladeScaleChanged;
        public event Action<double> OnBladeGlowChanged;

        public BladeVisualsController(ParticleSystem particleSystem = null)
        {
            _particleSystem = particleSystem ?? new ParticleSystem();
        }

        /// <summary>
        /// Update blade color with smooth blending.
        /// </summary>
        public void UpdateBladeColor(Color targetColor)
        {
            _currentBladeColor = targetColor;
            OnBladeColorChanged?.Invoke(_currentBladeColor);
        }

        /// <summary>
        /// Update blade scale based on interaction intensity.
        /// </summary>
        public void UpdateBladeScale(double intensity)
        {
            // Clamp intensity to valid range
            intensity = Math.Clamp(intensity, 0, 1);
            
            // Map intensity to scale range
            _currentScale = BladeConstants.SCALE_IDLE + 
                           (BladeConstants.SCALE_CHARGED - BladeConstants.SCALE_IDLE) * intensity;
            
            OnBladeScaleChanged?.Invoke(_currentScale);
        }

        /// <summary>
        /// Update blade glow effect based on intensity.
        /// </summary>
        public void UpdateBladeGlow(double intensity)
        {
            intensity = Math.Clamp(intensity, 0, 1);
            _currentGlow = intensity;
            OnBladeGlowChanged?.Invoke(_currentGlow);
        }

        /// <summary>
        /// Emit particles from blade center with specified color and count.
        /// </summary>
        public void EmitParticles(Color kanjiColor, int count)
        {
            if (_particleSystem == null || count <= 0)
                return;

            // Create circular burst pattern
            Vector2 centerPos = new Vector2(250, 250); // Standard blade center
            
            _particleSystem.EmitBurst(
                count,
                centerPos,
                BladeConstants.PARTICLE_SPEED_NORMAL,
                kanjiColor,
                2.0,
                BladeConstants.PARTICLE_LIFETIME_NORMAL,
                ParticleType.Glow
            );
        }

        /// <summary>
        /// Emit particles in a cone pattern (useful for charging effects).
        /// </summary>
        public void EmitParticlesCone(Color kanjiColor, int count, double angle, double spread)
        {
            if (_particleSystem == null || count <= 0)
                return;

            Vector2 centerPos = new Vector2(250, 250);
            
            _particleSystem.EmitSpray(
                count,
                centerPos,
                angle,
                spread,
                BladeConstants.PARTICLE_SPEED_FAST,
                kanjiColor,
                1.5,
                BladeConstants.PARTICLE_LIFETIME_NORMAL,
                ParticleType.Glow
            );
        }

        /// <summary>
        /// Emit a secondary lighter burst for layered effects.
        /// </summary>
        public void EmitSecondaryBurst(Color primaryColor)
        {
            if (_particleSystem == null)
                return;

            Vector2 centerPos = new Vector2(250, 250);
            
            // Secondary lighter burst with white color
            _particleSystem.EmitBurst(
                BladeConstants.PARTICLES_BURST_SECONDARY,
                centerPos,
                BladeConstants.PARTICLE_SPEED_NORMAL - 50,
                BladeConstants.COLOR_WHITE,
                1.0,
                BladeConstants.PARTICLE_LIFETIME_SHORT,
                ParticleType.Glow
            );
        }

        /// <summary>
        /// Get current blade color.
        /// </summary>
        public Color GetBladeColor()
        {
            return _currentBladeColor;
        }

        /// <summary>
        /// Get current blade scale.
        /// </summary>
        public double GetBladeScale()
        {
            return _currentScale;
        }

        /// <summary>
        /// Get current glow intensity.
        /// </summary>
        public double GetBladeGlow()
        {
            return _currentGlow;
        }

        /// <summary>
        /// Reset blade to idle state.
        /// </summary>
        public void ResetToIdle()
        {
            UpdateBladeColor(BladeConstants.COLOR_CYAN);
            UpdateBladeScale(0);
            UpdateBladeGlow(BladeConstants.GLOW_IDLE);
        }

        /// <summary>
        /// Update to hover state - intermediate intensity.
        /// </summary>
        public void SetHoverState()
        {
            UpdateBladeScale(0.5);
            UpdateBladeGlow(BladeConstants.GLOW_HOVER);
        }

        /// <summary>
        /// Update to active state - high intensity.
        /// </summary>
        public void SetActiveState()
        {
            UpdateBladeScale(1.0);
            UpdateBladeGlow(BladeConstants.GLOW_ACTIVE);
        }

        /// <summary>
        /// Apply kanji color to blade with appropriate glow.
        /// </summary>
        public void ApplyKanjiColor(Color kanjiColor, double glowIntensity)
        {
            // Blend kanji color with glow
            Color blendedColor = BlendColors(_currentBladeColor, kanjiColor, 0.6);
            UpdateBladeColor(blendedColor);
            UpdateBladeGlow(glowIntensity);
        }

        /// <summary>
        /// Blend two colors with a given ratio.
        /// </summary>
        private Color BlendColors(Color color1, Color color2, double ratio)
        {
            ratio = Math.Clamp(ratio, 0, 1);
            byte r = (byte)(color1.R * (1 - ratio) + color2.R * ratio);
            byte g = (byte)(color1.G * (1 - ratio) + color2.G * ratio);
            byte b = (byte)(color1.B * (1 - ratio) + color2.B * ratio);
            return Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// Update particle system position (for mobile blade effects).
        /// </summary>
        public void SetParticleEmitterPosition(double x, double y)
        {
            if (_particleSystem != null)
            {
                _particleSystem.Position = new Vector2(x, y);
            }
        }

        /// <summary>
        /// Update particle system for frame.
        /// </summary>
        public void UpdateParticles(double deltaTime)
        {
            _particleSystem?.Update(deltaTime);
        }

        /// <summary>
        /// Get active particles for rendering.
        /// </summary>
        public System.Collections.Generic.IEnumerable<Particle> GetActiveParticles()
        {
            return _particleSystem?.GetActiveParticles() ?? System.Linq.Enumerable.Empty<Particle>();
        }

        /// <summary>
        /// Clear all particles immediately.
        /// </summary>
        public void ClearParticles()
        {
            _particleSystem?.Clear();
        }
    }
}
