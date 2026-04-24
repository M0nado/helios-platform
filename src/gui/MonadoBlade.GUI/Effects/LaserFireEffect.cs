using System;
using System.Windows.Media;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Effects
{
    /// <summary>
    /// Laser fire effect - creates photon-like particles with glow trails.
    /// Color cycles through cyan → white → magenta for visual impact.
    /// </summary>
    public class LaserFireEffect
    {
        private ParticleSystem _particleSystem;
        private double _elapsedTime;
        private double _duration;
        private Random _random;

        public Vector2 Position { get; set; }
        public double Speed { get; set; }
        public double Spread { get; set; }
        public bool IsActive { get; set; }

        public LaserFireEffect(Vector2 position = default, double duration = 2.0)
        {
            _particleSystem = new ParticleSystem(position, new Vector2(0, 9.8 * 0.5)); // Subtle gravity
            _duration = duration;
            _elapsedTime = 0;
            Position = position;
            Speed = 200;
            Spread = Math.PI / 4;
            IsActive = true;
            _random = new Random();
        }

        /// <summary>
        /// Fire a laser burst in a specified direction.
        /// </summary>
        public void FireBurst(Vector2 direction, int particleCount = 50)
        {
            if (!IsActive) return;

            direction = direction.Length > 0 ? direction.Normalized : new Vector2(0, -1);

            // Core photon burst
            _particleSystem.EmitSpray(
                particleCount,
                Position,
                Math.Atan2(direction.Y, direction.X),
                Spread,
                Speed,
                GetCycleColor(0.0), // Cyan
                3.0,
                1.0,
                ParticleType.Laser
            );

            // Secondary glow particles (20% of main burst)
            _particleSystem.EmitSpray(
                (int)(particleCount * 0.2),
                Position,
                Math.Atan2(direction.Y, direction.X),
                Spread * 1.5,
                Speed * 0.7,
                GetCycleColor(0.5), // White
                2.0,
                0.8,
                ParticleType.Glow
            );

            // Trail particles
            _particleSystem.EmitSpray(
                (int)(particleCount * 0.3),
                Position,
                Math.Atan2(direction.Y, direction.X),
                Spread * 2,
                Speed * 0.4,
                GetCycleColor(1.0), // Magenta
                1.5,
                0.6,
                ParticleType.Trail
            );
        }

        /// <summary>
        /// Continuous beam effect - emit particles continuously in a direction.
        /// </summary>
        public void ContinuousBeam(Vector2 direction, double deltaTime, int particlesPerSecond = 100)
        {
            if (!IsActive) return;

            direction = direction.Length > 0 ? direction.Normalized : new Vector2(0, -1);

            int particlesToEmit = (int)(particlesPerSecond * deltaTime);
            for (int i = 0; i < particlesToEmit; i++)
            {
                _particleSystem.Emit(
                    1,
                    Position,
                    direction * Speed,
                    GetCycleColor(_elapsedTime * 0.5),
                    2.5,
                    1.0,
                    ParticleType.Laser
                );
            }
        }

        /// <summary>
        /// Impact effect - laser hits target and explodes.
        /// </summary>
        public void ImpactEffect(Vector2 impactPosition)
        {
            if (!IsActive) return;

            // Main impact burst (white hot)
            _particleSystem.EmitBurst(
                80,
                impactPosition,
                250,
                Color.FromRgb(255, 255, 255), // White
                4.0,
                0.5,
                ParticleType.Spark
            );

            // Secondary sparks (orange/yellow from heat)
            _particleSystem.EmitBurst(
                40,
                impactPosition,
                150,
                Color.FromRgb(255, 165, 0), // Orange
                2.5,
                0.7,
                ParticleType.Spark
            );

            // Glow halo
            _particleSystem.EmitBurst(
                30,
                impactPosition,
                100,
                GetCycleColor(0.0), // Cyan glow
                3.0,
                0.8,
                ParticleType.Glow
            );
        }

        /// <summary>
        /// Update the laser effect.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!IsActive) return;

            _elapsedTime += deltaTime;
            if (_elapsedTime >= _duration)
            {
                IsActive = false;
            }

            _particleSystem.Update(deltaTime);
        }

        /// <summary>
        /// Get the current particles for rendering.
        /// </summary>
        public System.Collections.Generic.IEnumerable<Particle> GetParticles()
        {
            return _particleSystem.GetActiveParticles();
        }

        /// <summary>
        /// Get color based on cycle time (cyan → white → magenta).
        /// </summary>
        private Color GetCycleColor(double timeOffset)
        {
            double cycleTime = (_elapsedTime + timeOffset) % 1.0;

            if (cycleTime < 0.33)
            {
                // Cyan to White
                double t = cycleTime / 0.33;
                return ParticleSystem.InterpolateColor(
                    Color.FromRgb(0, 217, 255),      // Cyan
                    Color.FromRgb(255, 255, 255),    // White
                    t
                );
            }
            else if (cycleTime < 0.67)
            {
                // White to Magenta
                double t = (cycleTime - 0.33) / 0.33;
                return ParticleSystem.InterpolateColor(
                    Color.FromRgb(255, 255, 255),    // White
                    Color.FromRgb(255, 0, 255),      // Magenta
                    t
                );
            }
            else
            {
                // Magenta to Cyan
                double t = (cycleTime - 0.67) / 0.33;
                return ParticleSystem.InterpolateColor(
                    Color.FromRgb(255, 0, 255),      // Magenta
                    Color.FromRgb(0, 217, 255),      // Cyan
                    t
                );
            }
        }

        /// <summary>
        /// Clear all particles immediately.
        /// </summary>
        public void Clear()
        {
            _particleSystem.Clear();
            IsActive = false;
        }

        /// <summary>
        /// Get statistics about active particles.
        /// </summary>
        public (int activeParticles, int totalParticles) GetStats()
        {
            return (_particleSystem.ActiveParticleCount, _particleSystem.TotalParticles);
        }
    }
}
