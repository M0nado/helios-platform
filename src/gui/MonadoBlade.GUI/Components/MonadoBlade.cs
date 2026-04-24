using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Monado Blade component - Xenoblade-style interactive blade with glow and animations.
    /// Features: Scale animation on hover, glow effects, ripple waves, particle trails.
    /// </summary>
    public class MonadoBlade : FrameworkElement
    {
        private BladeState _state = BladeState.Idle;
        private double _hoverScale = 1.0;
        private double _glowIntensity = 0.0;
        private double _chargeLevel = 0.0;
        private double _rotationAngle = 0.0;
        private ParticleSystem _particleSystem;
        private List<RippleWave> _ripples = new List<RippleWave>();
        private DateTime _lastRippleTime = DateTime.Now;
        private Random _random = new Random();

        public Vector2 Position { get; set; }
        public double BladeLength { get; set; }
        public double BladeWidth { get; set; }
        public BladeState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged();
                }
            }
        }

        public MonadoBlade()
        {
            Width = 200;
            Height = 200;
            BladeLength = 100;
            BladeWidth = 30;
            Position = new Vector2(100, 100);
            _particleSystem = new ParticleSystem(Position);

            // Event handlers for interaction
            MouseEnter += (s, e) => State = BladeState.Hover;
            MouseLeave += (s, e) => State = BladeState.Idle;
            MouseLeftButtonDown += (s, e) => OnClick(e.GetPosition(this));
        }

        /// <summary>
        /// Handle state transitions and animations.
        /// </summary>
        private void OnStateChanged()
        {
            switch (_state)
            {
                case BladeState.Idle:
                    AnimateToScale(1.0, 300);
                    _glowIntensity = 0.2;
                    break;

                case BladeState.Hover:
                    AnimateToScale(1.2, 200);
                    _glowIntensity = 0.6;
                    break;

                case BladeState.Active:
                    AnimateToScale(1.3, 150);
                    _glowIntensity = 1.0;
                    _chargeLevel = 0.0;
                    break;

                case BladeState.Charging:
                    _chargeLevel = Math.Min(1.0, _chargeLevel + 0.1);
                    break;

                case BladeState.Releasing:
                    ReleaseEnergy();
                    _chargeLevel = 0.0;
                    _state = BladeState.Active;
                    break;
            }
        }

        /// <summary>
        /// Animate blade scale with easing.
        /// </summary>
        private void AnimateToScale(double targetScale, int durationMs)
        {
            var animation = new DoubleAnimation
            {
                To = targetScale,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            BeginAnimation(OpacityProperty, animation);
            _hoverScale = targetScale;
        }

        /// <summary>
        /// Handle click to trigger ripple and energy release.
        /// </summary>
        private void OnClick(Point position)
        {
            if (_state == BladeState.Idle || _state == BladeState.Hover)
            {
                State = BladeState.Active;
                CreateRipple(position);
                EmitClickParticles(position);
            }
            else if (_state == BladeState.Active)
            {
                State = BladeState.Charging;
            }
        }

        /// <summary>
        /// Create a ripple wave effect from impact point.
        /// </summary>
        private void CreateRipple(Point impactPoint)
        {
            var ripple = new RippleWave
            {
                CenterX = impactPoint.X,
                CenterY = impactPoint.Y,
                CurrentRadius = 0,
                MaxRadius = 150,
                Duration = 0.5,
                RemainingTime = 0.5,
                Color = Color.FromRgb(0, 217, 255) // Cyan
            };

            _ripples.Add(ripple);
            _lastRippleTime = DateTime.Now;
        }

        /// <summary>
        /// Emit particles on click impact.
        /// </summary>
        private void EmitClickParticles(Point impactPoint)
        {
            var centerVec = new Vector2(impactPoint.X, impactPoint.Y);

            // Main burst (cyan)
            _particleSystem.EmitBurst(
                40,
                centerVec,
                150,
                Color.FromRgb(0, 217, 255),
                2.0,
                0.6,
                ParticleType.Glow
            );

            // Secondary burst (white)
            _particleSystem.EmitBurst(
                20,
                centerVec,
                100,
                Colors.White,
                1.5,
                0.5,
                ParticleType.Glow
            );
        }

        /// <summary>
        /// Release energy when charged.
        /// </summary>
        private void ReleaseEnergy()
        {
            var energyBurst = new Vector2(0, -250); // Upward burst

            _particleSystem.EmitSpray(
                (int)(_chargeLevel * 100),
                Position,
                -Math.PI / 2, // Straight up
                Math.PI / 4,
                200 + (_chargeLevel * 200),
                Color.FromRgb(255, 215, 0), // Gold when charged
                2.5 + (_chargeLevel * 1.5),
                0.8,
                ParticleType.Glow
            );

            CreateRipple(new Point(Position.X, Position.Y));
        }

        /// <summary>
        /// Update particles and ripples.
        /// </summary>
        public void Update(double deltaTime)
        {
            _particleSystem.Update(deltaTime);

            for (int i = _ripples.Count - 1; i >= 0; i--)
            {
                _ripples[i].RemainingTime -= deltaTime;
                _ripples[i].CurrentRadius = (_ripples[i].Duration - _ripples[i].RemainingTime) / _ripples[i].Duration * _ripples[i].MaxRadius;

                if (_ripples[i].RemainingTime <= 0)
                {
                    _ripples.RemoveAt(i);
                }
            }

            // Continuous glow pulsing
            _glowIntensity = Math.Max(_glowIntensity * 0.98, 0.2); // Fade to base level
        }

        /// <summary>
        /// Render the blade and effects.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double centerX = Position.X;
            double centerY = Position.Y;

            // Draw ripple waves
            foreach (var ripple in _ripples)
            {
                double alphaFactor = 1.0 - (ripple.CurrentRadius / ripple.MaxRadius);
                var rippleColor = Color.FromArgb(
                    (byte)(100 * alphaFactor),
                    ripple.Color.R,
                    ripple.Color.G,
                    ripple.Color.B
                );

                var ripplePen = new Pen(new SolidColorBrush(rippleColor), 2);
                drawingContext.DrawEllipse(null, ripplePen, new Point(ripple.CenterX, ripple.CenterY), ripple.CurrentRadius, ripple.CurrentRadius);
            }

            // Draw blade shape
            DrawBlade(drawingContext, centerX, centerY);

            // Draw particles
            DrawParticles(drawingContext);
        }

        /// <summary>
        /// Draw the blade geometry.
        /// </summary>
        private void DrawBlade(DrawingContext drawingContext, double centerX, double centerY)
        {
            double halfWidth = BladeWidth / 2 * _hoverScale;
            double halfLength = BladeLength / 2 * _hoverScale;

            // Blade path (diamond/pointed shape)
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();

            // Top point
            pathFigure.StartPoint = new Point(centerX, centerY - halfLength);
            pathFigure.Segments.Add(new LineSegment(new Point(centerX + halfWidth, centerY), true));
            pathFigure.Segments.Add(new LineSegment(new Point(centerX, centerY + halfLength), true));
            pathFigure.Segments.Add(new LineSegment(new Point(centerX - halfWidth, centerY), true));
            pathFigure.IsClosed = true;

            pathGeometry.Figures.Add(pathFigure);

            // Draw glow layers
            for (int i = 3; i >= 1; i--)
            {
                double glowAlpha = _glowIntensity * (1.0 - (i / 4.0)) * (255 / 3.0);
                var glowColor = Color.FromArgb(
                    (byte)glowAlpha,
                    0, // Cyan
                    217,
                    255
                );

                var glowBrush = new SolidColorBrush(glowColor);
                var glowPen = new Pen(glowBrush, 1 + (i * 0.5));
                drawingContext.DrawGeometry(null, glowPen, pathGeometry);
            }

            // Draw blade fill
            var bladeBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Colors.White, 0),
                    new GradientStop(Color.FromRgb(0, 217, 255), 0.5),
                    new GradientStop(Color.FromRgb(0, 150, 200), 1.0)
                }
            };

            drawingContext.DrawGeometry(bladeBrush, new Pen(Brushes.Cyan, 2), pathGeometry);

            // Draw charge indicator if charging
            if (_state == BladeState.Charging && _chargeLevel > 0)
            {
                double chargeRingRadius = halfLength * (1.0 + (_chargeLevel * 0.5));
                var chargePen = new Pen(
                    new SolidColorBrush(Color.FromArgb((byte)(255 * _chargeLevel), 255, 215, 0)),
                    2
                );
                drawingContext.DrawEllipse(null, chargePen, new Point(centerX, centerY), chargeRingRadius, chargeRingRadius);
            }
        }

        /// <summary>
        /// Draw particles.
        /// </summary>
        private void DrawParticles(DrawingContext drawingContext)
        {
            foreach (var particle in _particleSystem.GetActiveParticles())
            {
                var brush = new SolidColorBrush(particle.GetCurrentColor());
                drawingContext.DrawEllipse(brush, null, new Point(particle.Position.X, particle.Position.Y), particle.Size, particle.Size);
            }
        }

        /// <summary>
        /// Get active particles for external rendering.
        /// </summary>
        public IEnumerable<Particle> GetParticles()
        {
            return _particleSystem.GetActiveParticles();
        }

        /// <summary>
        /// Get ripples for external rendering.
        /// </summary>
        public IEnumerable<RippleWave> GetRipples()
        {
            return _ripples;
        }
    }

    /// <summary>
    /// Blade states for interaction.
    /// </summary>
    public enum BladeState
    {
        Idle,
        Hover,
        Active,
        Charging,
        Releasing
    }

    /// <summary>
    /// Ripple wave data.
    /// </summary>
    public class RippleWave
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double CurrentRadius { get; set; }
        public double MaxRadius { get; set; }
        public double Duration { get; set; }
        public double RemainingTime { get; set; }
        public Color Color { get; set; }
    }
}
