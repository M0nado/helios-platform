using System;
using System.Collections.Generic;
using System.Windows.Media;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Effects
{
    /// <summary>
    /// Lightning effect - creates branching, fractal-like lightning bolts
    /// with glow halos and impact effects.
    /// </summary>
    public class LightningEffect
    {
        private ParticleSystem _particleSystem;
        private List<LightningBolt> _bolts;
        private Random _random;
        private double _elapsedTime;

        public Vector2 Position { get; set; }
        public bool IsActive { get; set; }

        public LightningEffect(Vector2 position = default)
        {
            _particleSystem = new ParticleSystem(position);
            _bolts = new List<LightningBolt>();
            _random = new Random();
            Position = position;
            IsActive = true;
        }

        /// <summary>
        /// Strike lightning from position to target.
        /// </summary>
        public void Strike(Vector2 target, LightningType type = LightningType.Bolt)
        {
            if (!IsActive) return;

            var bolt = new LightningBolt(Position, target, type, _random);
            _bolts.Add(bolt);

            // Impact particles at target
            GenerateImpactParticles(target, type);
        }

        /// <summary>
        /// Chain lightning - strike, then chain to nearby locations.
        /// </summary>
        public void ChainStrike(Vector2 target, int chains = 2)
        {
            if (!IsActive || chains <= 0) return;

            Strike(target, LightningType.Chain);

            // Chain to random nearby locations
            for (int i = 0; i < chains; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double distance = 150 + _random.NextDouble() * 150;
                Vector2 chainTarget = target + new Vector2(
                    Math.Cos(angle) * distance,
                    Math.Sin(angle) * distance
                );
                Strike(chainTarget, LightningType.Arc);
            }
        }

        /// <summary>
        /// Arc lightning - curved path between points.
        /// </summary>
        public void ArcStrike(Vector2 target)
        {
            if (!IsActive) return;
            Strike(target, LightningType.Arc);
        }

        /// <summary>
        /// Generate impact particles and glow effect.
        /// </summary>
        private void GenerateImpactParticles(Vector2 impactPoint, LightningType type)
        {
            // Blue-white explosion
            _particleSystem.EmitBurst(
                60,
                impactPoint,
                180,
                Color.FromRgb(200, 220, 255), // Light blue
                3.0,
                0.6,
                ParticleType.Spark
            );

            // Electric glow
            _particleSystem.EmitBurst(
                40,
                impactPoint,
                100,
                Color.FromRgb(0, 150, 255), // Bright blue
                2.5,
                0.8,
                ParticleType.Glow
            );

            // Smoke/dissipation
            _particleSystem.EmitBurst(
                30,
                impactPoint,
                60,
                Color.FromRgb(150, 150, 150), // Gray
                2.0,
                1.0,
                ParticleType.Ember
            );

            if (type == LightningType.Chain || type == LightningType.Arc)
            {
                // Additional violet undertones for chain effects
                _particleSystem.EmitBurst(
                    20,
                    impactPoint,
                    120,
                    Color.FromRgb(100, 0, 255), // Violet
                    1.5,
                    0.7,
                    ParticleType.Glow
                );
            }
        }

        /// <summary>
        /// Update all lightning bolts and particles.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!IsActive) return;

            _elapsedTime += deltaTime;
            _particleSystem.Update(deltaTime);

            // Update bolts and remove completed ones
            for (int i = _bolts.Count - 1; i >= 0; i--)
            {
                _bolts[i].Update(deltaTime);
                if (_bolts[i].IsComplete)
                {
                    _bolts.RemoveAt(i);
                }
            }

            // Auto-deactivate after all bolts dissipate
            if (_bolts.Count == 0 && _particleSystem.ActiveParticleCount == 0)
            {
                IsActive = false;
            }
        }

        /// <summary>
        /// Get all lightning segments for rendering.
        /// </summary>
        public IEnumerable<LightningSegment> GetLightningSegments()
        {
            foreach (var bolt in _bolts)
            {
                foreach (var segment in bolt.GetSegments())
                {
                    yield return segment;
                }
            }
        }

        /// <summary>
        /// Get particles for rendering.
        /// </summary>
        public IEnumerable<Particle> GetParticles()
        {
            return _particleSystem.GetActiveParticles();
        }

        /// <summary>
        /// Clear all effects immediately.
        /// </summary>
        public void Clear()
        {
            _bolts.Clear();
            _particleSystem.Clear();
            IsActive = false;
        }
    }

    /// <summary>
    /// Types of lightning strikes.
    /// </summary>
    public enum LightningType
    {
        Bolt,    // Single straight bolt
        Chain,   // Branches to multiple targets
        Arc,     // Curved path
        Web      // Multiple interconnected bolts
    }

    /// <summary>
    /// Represents a single lightning bolt with branches.
    /// </summary>
    public class LightningBolt
    {
        private List<LightningSegment> _segments;
        private Vector2 _start;
        private Vector2 _end;
        private LightningType _type;
        private Random _random;
        private double _lifetime;
        private double _maxLifetime;
        private int _subdivisions;

        public bool IsComplete => _lifetime <= 0;

        public LightningBolt(Vector2 start, Vector2 end, LightningType type, Random random)
        {
            _start = start;
            _end = end;
            _type = type;
            _random = random;
            _lifetime = 0.3; // Lightning lasts 300ms
            _maxLifetime = _lifetime;
            _segments = new List<LightningSegment>();
            _subdivisions = 4;

            GenerateBolt();
        }

        /// <summary>
        /// Generate the lightning bolt structure using fractal subdivision.
        /// </summary>
        private void GenerateBolt()
        {
            _segments.Clear();
            SubdivideLine(_start, _end, _subdivisions, 20); // Max displacement of 20 pixels
        }

        /// <summary>
        /// Recursively subdivide line to create lightning fractal pattern.
        /// </summary>
        private void SubdivideLine(Vector2 p1, Vector2 p2, int depth, double maxDisplacement)
        {
            if (depth <= 0)
            {
                _segments.Add(new LightningSegment(p1, p2));
                return;
            }

            Vector2 mid = new Vector2(
                (p1.X + p2.X) / 2,
                (p1.Y + p2.Y) / 2
            );

            // Perpendicular displacement for jagged effect
            Vector2 direction = (p2 - p1).Normalized;
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);

            double displacement = ((_random.NextDouble() - 0.5) * 2) * maxDisplacement;
            mid = mid + perpendicular * displacement;

            SubdivideLine(p1, mid, depth - 1, maxDisplacement * 0.5);
            SubdivideLine(mid, p2, depth - 1, maxDisplacement * 0.5);

            // Branch probability for chain/web effects
            if ((_type == LightningType.Chain || _type == LightningType.Web) && depth == 2)
            {
                if (_random.NextDouble() < 0.3) // 30% chance to branch
                {
                    Vector2 branchEnd = mid + direction * 80 + perpendicular * (_random.NextDouble() - 0.5) * 100;
                    SubdivideLine(mid, branchEnd, depth - 2, maxDisplacement * 0.4);
                }
            }
        }

        /// <summary>
        /// Update the bolt (mostly for animation timing).
        /// </summary>
        public void Update(double deltaTime)
        {
            _lifetime -= deltaTime;
        }

        /// <summary>
        /// Get all segments of this bolt.
        /// </summary>
        public IEnumerable<LightningSegment> GetSegments()
        {
            double alphaFactor = Math.Max(0, _lifetime / _maxLifetime);
            foreach (var segment in _segments)
            {
                segment.Alpha = alphaFactor;
                yield return segment;
            }
        }
    }

    /// <summary>
    /// A single line segment of a lightning bolt.
    /// </summary>
    public class LightningSegment
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public double Alpha { get; set; }
        public double Thickness { get; set; }
        public Color Color { get; set; }

        public LightningSegment(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
            Alpha = 1.0;
            Thickness = 2.5;
            Color = Color.FromRgb(100, 150, 255); // Blue-white
        }
    }
}
