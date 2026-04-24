using System;
using System.Collections.Generic;
using System.Windows.Media;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Effects
{
    /// <summary>
    /// Kanji circle effect - rotating kanji characters with particle rings.
    /// Uses color coding: Red=Power, Cyan=Tech, Gold=Achievement
    /// </summary>
    public class KanjiCircleEffect
    {
        private ParticleSystem _particleSystem;
        private List<KanjiCircle> _circles;
        private Random _random;
        private double _elapsedTime;

        public Vector2 Position { get; set; }
        public bool IsActive { get; set; }

        public KanjiCircleEffect(Vector2 position = default)
        {
            _particleSystem = new ParticleSystem(position);
            _circles = new List<KanjiCircle>();
            _random = new Random();
            Position = position;
            IsActive = true;
        }

        /// <summary>
        /// Create a kanji circle effect with specified meaning.
        /// </summary>
        public void CreateCircle(KanjiType type, double radius = 60.0, double duration = 2.0)
        {
            if (!IsActive) return;

            var circle = new KanjiCircle(Position, type, radius, duration);
            _circles.Add(circle);
        }

        /// <summary>
        /// Create multiple kanji circles in a sequence.
        /// </summary>
        public void CreateSequence(params KanjiType[] types)
        {
            if (!IsActive) return;

            for (int i = 0; i < types.Length; i++)
            {
                CreateCircle(types[i], 60 + (i * 10), 2.0);
            }
        }

        /// <summary>
        /// Create concentric circles (stacked effect).
        /// </summary>
        public void CreateConcentricCircles(int count, KanjiType baseType, double startRadius = 40.0)
        {
            if (!IsActive) return;

            for (int i = 0; i < count; i++)
            {
                CreateCircle(baseType, startRadius + (i * 25), 2.0 + (i * 0.5));
            }
        }

        /// <summary>
        /// Update all circles and particles.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!IsActive) return;

            _elapsedTime += deltaTime;
            _particleSystem.Update(deltaTime);

            for (int i = _circles.Count - 1; i >= 0; i--)
            {
                _circles[i].Update(deltaTime, _particleSystem);
                if (_circles[i].IsComplete)
                {
                    _circles.RemoveAt(i);
                }
            }

            // Auto-deactivate
            if (_circles.Count == 0 && _particleSystem.ActiveParticleCount == 0)
            {
                IsActive = false;
            }
        }

        /// <summary>
        /// Get all active kanji circles for rendering.
        /// </summary>
        public IEnumerable<KanjiCircle> GetCircles()
        {
            return _circles;
        }

        /// <summary>
        /// Get all particles for rendering.
        /// </summary>
        public IEnumerable<Particle> GetParticles()
        {
            return _particleSystem.GetActiveParticles();
        }

        /// <summary>
        /// Clear all effects.
        /// </summary>
        public void Clear()
        {
            _circles.Clear();
            _particleSystem.Clear();
            IsActive = false;
        }
    }

    /// <summary>
    /// Types of kanji characters and their meanings.
    /// </summary>
    public enum KanjiType
    {
        Power,       // 力 - Strength, Power
        Blade,       // 刀 - Sword, Blade
        Light,       // 光 - Light, Radiance
        Flow,        // 流 - Flow, Stream
        Soul,        // 魂 - Soul, Spirit
        Machine      // 機 - Machine, Mechanism
    }

    /// <summary>
    /// Represents a single rotating kanji circle.
    /// </summary>
    public class KanjiCircle
    {
        public Vector2 Position { get; set; }
        public KanjiType Type { get; set; }
        public string Character { get; private set; }
        public double Radius { get; set; }
        public double Rotation { get; set; }
        public double Scale { get; set; }
        public double Lifetime { get; private set; }
        public double MaxLifetime { get; private set; }
        public bool IsComplete => Lifetime <= 0;

        private Color _baseColor;
        private double _glowIntensity;
        private Random _random;

        public KanjiCircle(Vector2 position, KanjiType type, double radius, double duration)
        {
            Position = position;
            Type = type;
            Radius = radius;
            Lifetime = duration;
            MaxLifetime = duration;
            Rotation = 0;
            Scale = 0.1; // Start small
            _glowIntensity = 0;
            _random = new Random();

            SetCharacterAndColor(type);
        }

        /// <summary>
        /// Set kanji character and color based on type.
        /// </summary>
        private void SetCharacterAndColor(KanjiType type)
        {
            (Character, _baseColor) = type switch
            {
                KanjiType.Power => ("力", Color.FromRgb(255, 0, 0)),       // Red
                KanjiType.Blade => ("刀", Color.FromRgb(0, 217, 255)),     // Cyan
                KanjiType.Light => ("光", Color.FromRgb(255, 215, 0)),     // Gold
                KanjiType.Flow => ("流", Color.FromRgb(0, 255, 65)),       // Green
                KanjiType.Soul => ("魂", Color.FromRgb(255, 0, 255)),      // Magenta
                KanjiType.Machine => ("機", Color.FromRgb(100, 150, 255)), // Blue
                _ => ("力", Color.FromRgb(255, 0, 0))
            };
        }

        /// <summary>
        /// Update the kanji circle.
        /// </summary>
        public void Update(double deltaTime, ParticleSystem particleSystem)
        {
            Lifetime -= deltaTime;
            double progress = 1.0 - (Lifetime / MaxLifetime);

            // Animate scale: grow from 0.1 to 1.0, then shrink
            if (progress < 0.5)
            {
                Scale = ParticleSystem.Ease(progress * 2, EasingType.EaseOut) * 1.0;
            }
            else
            {
                Scale = ParticleSystem.Ease(1.0 - (progress - 0.5) * 2, EasingType.EaseIn) * 1.0;
            }

            // Continuous rotation
            Rotation += deltaTime * 180; // 180 degrees per second
            Rotation %= 360;

            // Glow pulsing
            _glowIntensity = 0.5 + Math.Sin(progress * Math.PI * 2) * 0.5;

            // Emit particle ring every 50ms
            if ((int)(Lifetime * 20) % 1 == 0)
            {
                EmitParticleRing(particleSystem, progress);
            }
        }

        /// <summary>
        /// Emit a ring of particles around the kanji.
        /// </summary>
        private void EmitParticleRing(ParticleSystem particleSystem, double progress)
        {
            int particleCount = (int)(20 * Scale);
            double emissionRadius = Radius * Scale;

            for (int i = 0; i < particleCount; i++)
            {
                double angle = (i / (double)particleCount) * Math.PI * 2;
                Vector2 particlePos = Position + new Vector2(
                    Math.Cos(angle) * emissionRadius,
                    Math.Sin(angle) * emissionRadius
                );

                Vector2 velocity = new Vector2(
                    Math.Cos(angle) * (50 + progress * 50),
                    Math.Sin(angle) * (50 + progress * 50)
                );

                Color particleColor = ParticleSystem.InterpolateColor(_baseColor, Colors.White, _glowIntensity * 0.5);

                particleSystem.Emit(
                    1,
                    particlePos,
                    velocity,
                    particleColor,
                    1.5,
                    0.5,
                    ParticleType.Kanji
                );
            }
        }

        /// <summary>
        /// Get the current glow intensity (for rendering).
        /// </summary>
        public double GetGlowIntensity()
        {
            return _glowIntensity * ParticleSystem.Ease(Scale, EasingType.Linear);
        }

        /// <summary>
        /// Get current color with glow effect.
        /// </summary>
        public Color GetDisplayColor()
        {
            return ParticleSystem.InterpolateColor(
                _baseColor,
                Colors.White,
                _glowIntensity * 0.3
            );
        }

        /// <summary>
        /// Get alpha value based on lifetime.
        /// </summary>
        public double GetAlpha()
        {
            double alpha = Math.Min(1.0, Lifetime / 0.2); // Fade in first 200ms
            alpha = Math.Min(alpha, Math.Max(0, Lifetime / 0.2)); // Fade out last 200ms
            return alpha;
        }
    }
}
