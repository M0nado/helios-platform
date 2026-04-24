using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MonadoBlade.GUI.Effects
{
    /// <summary>
    /// Represents a single particle in the particle system.
    /// </summary>
    public class Particle
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public double Lifetime { get; set; }
        public double MaxLifetime { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }
        public double Rotation { get; set; }
        public ParticleType Type { get; set; }
        public bool IsAlive => Lifetime > 0;

        public Particle(Vector2 position, Vector2 velocity, double lifetime, Color color, double size, ParticleType type = ParticleType.Standard)
        {
            Position = position;
            Velocity = velocity;
            MaxLifetime = lifetime;
            Lifetime = lifetime;
            Color = color;
            Size = size;
            Type = type;
            Rotation = 0;
        }

        public void Update(double deltaTime, Vector2 gravity = default)
        {
            if (Lifetime <= 0) return;

            Lifetime -= deltaTime;
            Velocity += gravity * deltaTime;
            Position += Velocity * deltaTime;
        }

        public Color GetCurrentColor()
        {
            double alpha = (Lifetime / MaxLifetime);
            return Color.FromArgb(
                (byte)(Color.A * alpha),
                Color.R,
                Color.G,
                Color.B);
        }
    }

    /// <summary>
    /// Particle types for different visual effects.
    /// </summary>
    public enum ParticleType
    {
        Standard,
        Laser,
        Lightning,
        Kanji,
        Glow,
        Trail,
        Spark,
        Ember
    }

    /// <summary>
    /// Simple 2D vector structure for physics calculations.
    /// </summary>
    public struct Vector2
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 v, double scalar) => new Vector2(v.X * scalar, v.Y * scalar);
        public static Vector2 operator *(double scalar, Vector2 v) => v * scalar;

        public double Length => Math.Sqrt(X * X + Y * Y);
        public Vector2 Normalized => Length > 0 ? new Vector2(X / Length, Y / Length) : default;
    }

    /// <summary>
    /// Manages particle emission and rendering with high performance characteristics.
    /// Supports multiple particle types with different physics and rendering styles.
    /// </summary>
    public class ParticleSystem
    {
        private List<Particle> _particles = new List<Particle>(1000);
        private Queue<Particle> _particlePool = new Queue<Particle>(500);
        private Vector2 _gravity;
        private Vector2 _position;
        private double _emissionRate;
        private double _emissionTimer;
        private Random _random = new Random();

        public bool IsActive { get; set; }
        public Vector2 Gravity
        {
            get => _gravity;
            set => _gravity = value;
        }

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public double EmissionRate
        {
            get => _emissionRate;
            set => _emissionRate = value;
        }

        public int ActiveParticleCount => _particles.Count(p => p.IsAlive);
        public int TotalParticles => _particles.Count;

        public ParticleSystem(Vector2 position = default, Vector2 gravity = default)
        {
            _position = position;
            _gravity = gravity;
            IsActive = true;
            _emissionRate = 50; // particles per second
            InitializeParticlePool(1000);
        }

        /// <summary>
        /// Pre-allocate particle pool for better performance.
        /// </summary>
        private void InitializeParticlePool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _particlePool.Enqueue(new Particle(Vector2.Zero, Vector2.Zero, 1, Colors.White, 1));
            }
        }

        /// <summary>
        /// Emit particles at the current position with specified properties.
        /// </summary>
        public void Emit(int count, Vector2 velocity, Color color, double size, ParticleType type = ParticleType.Standard)
        {
            Emit(count, _position, velocity, color, size, 1.0, type);
        }

        /// <summary>
        /// Emit particles with full control over all parameters.
        /// </summary>
        public void Emit(int count, Vector2 position, Vector2 velocity, Color color, double size, double lifetime, ParticleType type = ParticleType.Standard)
        {
            for (int i = 0; i < count; i++)
            {
                Particle particle = AcquireParticle();
                double angleVariation = (_random.NextDouble() - 0.5) * Math.PI * 0.5;
                double speedVariation = 1.0 + (_random.NextDouble() - 0.5) * 0.3;

                Vector2 spreadVelocity = new Vector2(
                    velocity.X * Math.Cos(angleVariation) - velocity.Y * Math.Sin(angleVariation),
                    velocity.X * Math.Sin(angleVariation) + velocity.Y * Math.Cos(angleVariation)
                ) * speedVariation;

                particle.Position = position + new Vector2((_random.NextDouble() - 0.5) * size * 2, (_random.NextDouble() - 0.5) * size * 2);
                particle.Velocity = spreadVelocity;
                particle.Lifetime = lifetime;
                particle.MaxLifetime = lifetime;
                particle.Color = color;
                particle.Size = size;
                particle.Type = type;
                particle.Rotation = _random.NextDouble() * 360;

                _particles.Add(particle);
            }
        }

        /// <summary>
        /// Emit particles in a cone/spray pattern.
        /// </summary>
        public void EmitSpray(int count, Vector2 position, double angle, double spread, double speed, Color color, double size, double lifetime, ParticleType type = ParticleType.Standard)
        {
            for (int i = 0; i < count; i++)
            {
                Particle particle = AcquireParticle();
                double particleAngle = angle + ((_random.NextDouble() - 0.5) * spread);
                double particleSpeed = speed * (1.0 + (_random.NextDouble() - 0.5) * 0.3);

                Vector2 velocity = new Vector2(
                    Math.Cos(particleAngle) * particleSpeed,
                    Math.Sin(particleAngle) * particleSpeed
                );

                particle.Position = position;
                particle.Velocity = velocity;
                particle.Lifetime = lifetime;
                particle.MaxLifetime = lifetime;
                particle.Color = color;
                particle.Size = size;
                particle.Type = type;

                _particles.Add(particle);
            }
        }

        /// <summary>
        /// Update all particles. Call this every frame.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!IsActive) return;

            // Update existing particles
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update(deltaTime, _gravity);

                if (!_particles[i].IsAlive)
                {
                    ReturnParticleToPool(_particles[i]);
                    _particles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Get all currently active particles for rendering.
        /// </summary>
        public IEnumerable<Particle> GetActiveParticles()
        {
            return _particles.Where(p => p.IsAlive);
        }

        /// <summary>
        /// Clear all particles immediately.
        /// </summary>
        public void Clear()
        {
            foreach (var particle in _particles)
            {
                ReturnParticleToPool(particle);
            }
            _particles.Clear();
        }

        /// <summary>
        /// Burst emission - emit many particles at once for impact effects.
        /// </summary>
        public void EmitBurst(int count, Vector2 position, double speed, Color color, double size, double lifetime, ParticleType type = ParticleType.Standard)
        {
            EmitSpray(count, position, 0, Math.PI * 2, speed, color, size, lifetime, type);
        }

        /// <summary>
        /// Get particle from pool or create new if pool is empty.
        /// </summary>
        private Particle AcquireParticle()
        {
            if (_particlePool.Count > 0)
            {
                return _particlePool.Dequeue();
            }
            return new Particle(Vector2.Zero, Vector2.Zero, 1, Colors.White, 1);
        }

        /// <summary>
        /// Return particle to pool for reuse.
        /// </summary>
        private void ReturnParticleToPool(Particle particle)
        {
            if (_particlePool.Count < 1000) // Cap pool size
            {
                _particlePool.Enqueue(particle);
            }
        }

        /// <summary>
        /// Interpolate between colors smoothly.
        /// </summary>
        public static Color InterpolateColor(Color from, Color to, double t)
        {
            t = Math.Clamp(t, 0, 1);
            return Color.FromArgb(
                (byte)(from.A + (to.A - from.A) * t),
                (byte)(from.R + (to.R - from.R) * t),
                (byte)(from.G + (to.G - from.G) * t),
                (byte)(from.B + (to.B - from.B) * t));
        }

        /// <summary>
        /// Get eased value using various easing functions.
        /// </summary>
        public static double Ease(double t, EasingType easing)
        {
            t = Math.Clamp(t, 0, 1);
            return easing switch
            {
                EasingType.Linear => t,
                EasingType.EaseIn => t * t,
                EasingType.EaseOut => t * (2 - t),
                EasingType.EaseInOut => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t,
                EasingType.CubicIn => t * t * t,
                EasingType.CubicOut => 1 + (--t) * t * t,
                EasingType.CubicInOut => t < 0.5 ? 4 * t * t * t : 1 + (--t) * 2 * (t * t * 2),
                _ => t
            };
        }
    }

    public enum EasingType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        CubicIn,
        CubicOut,
        CubicInOut
    }
}
