using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Advanced interactive Monado Blade with laser sword effects, particle trails, and state machine.
    /// Xenoblade Chronicles-inspired premium visual component.
    /// </summary>
    public class MonadoBladeAdvanced : Canvas
    {
        private Canvas _bladeCanvas;
        private Canvas _effectsCanvas;
        private RotateTransform _bladeRotation;
        private BladeState _currentState = BladeState.Idle;
        private double _rotationAngle = 0;
        private Path _bladePath;
        private ParticleSystem _particleSystem;

        public event Action OnBladeActivated;
        public event Action OnBladeCharged;
        public event Action OnBladeReleased;

        public BladeState CurrentState
        {
            get => _currentState;
            set => SetState(value);
        }

        public MonadoBladeAdvanced()
        {
            Width = 500;
            Height = 500;
            Background = Brushes.Transparent;

            _bladeCanvas = new Canvas
            {
                Width = 500,
                Height = 500,
                Background = Brushes.Transparent
            };

            _effectsCanvas = new Canvas
            {
                Width = 500,
                Height = 500,
                Background = Brushes.Transparent
            };

            _bladeRotation = new RotateTransform { CenterX = 250, CenterY = 250 };
            _bladeCanvas.RenderTransform = _bladeRotation;

            _particleSystem = new ParticleSystem { MaxParticles = 500 };

            DrawBlade();
            SetupInteractivity();

            Children.Add(_effectsCanvas);
            Children.Add(_bladeCanvas);

            // Animation loop
            CompositionTarget.Rendering += (s, e) =>
            {
                _particleSystem.Update(0.016); // ~60fps
                _rotationAngle += 0.5;
                if (_rotationAngle > 360) _rotationAngle -= 360;

                UpdateBladeVisuals();
            };
        }

        private void DrawBlade()
        {
            // Main blade geometry (sword shape with laser glow)
            var bladeGeometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(250, 80) };

            // Blade outline (wide at base, sharp at tip)
            figure.Segments.Add(new LineSegment { Point = new Point(265, 200) });      // Right edge upper
            figure.Segments.Add(new LineSegment { Point = new Point(280, 240) });      // Right edge middle
            figure.Segments.Add(new BezierSegment 
            { 
                Point1 = new Point(285, 250),
                Point2 = new Point(290, 260),
                Point3 = new Point(250, 350)                                           // Tip
            });
            figure.Segments.Add(new BezierSegment 
            { 
                Point1 = new Point(210, 260),
                Point2 = new Point(215, 250),
                Point3 = new Point(220, 240)
            });
            figure.Segments.Add(new LineSegment { Point = new Point(235, 200) });      // Left edge upper
            figure.Segments.Add(new LineSegment { Point = new Point(250, 80) });       // Back to start
            figure.IsClosed = true;

            bladeGeometry.Figures.Add(figure);

            // Main blade fill with gradient
            _bladePath = new Path
            {
                Data = bladeGeometry,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(100, 200, 255), 0),     // Light blue edge
                        new GradientStop(Color.FromRgb(0, 217, 255), 0.3),     // Cyan core
                        new GradientStop(Color.FromRgb(255, 255, 255), 0.5),   // White highlight
                        new GradientStop(Color.FromRgb(0, 217, 255), 0.7),     // Cyan core
                        new GradientStop(Color.FromRgb(100, 200, 255), 1)      // Light blue edge
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                StrokeThickness = 1.5,
                Opacity = 0.95
            };

            _bladeCanvas.Children.Add(_bladePath);

            // Blade glow effect (outer)
            var glowPath = new Path
            {
                Data = bladeGeometry,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 12,
                Opacity = 0.25,
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 }
            };

            _bladeCanvas.Children.Add(glowPath);

            // Energy core at blade base
            var baseGlow = new Ellipse
            {
                Width = 60,
                Height = 60,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Opacity = 0.4
            };

            Canvas.SetLeft(baseGlow, 220);
            Canvas.SetTop(baseGlow, 210);
            _bladeCanvas.Children.Add(baseGlow);

            var baseCenter = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Opacity = 0.8
            };

            Canvas.SetLeft(baseCenter, 230);
            Canvas.SetTop(baseCenter, 220);
            _bladeCanvas.Children.Add(baseCenter);

            // Draw crosshair on blade
            var horizontalLine = new Line
            {
                X1 = 200,
                Y1 = 250,
                X2 = 300,
                Y2 = 250,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 1,
                Opacity = 0.5
            };

            var verticalLine = new Line
            {
                X1 = 250,
                Y1 = 200,
                X2 = 250,
                Y2 = 300,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 1,
                Opacity = 0.5
            };

            _bladeCanvas.Children.Add(horizontalLine);
            _bladeCanvas.Children.Add(verticalLine);
        }

        private void SetupInteractivity()
        {
            MouseEnter += (s, e) =>
            {
                if (_currentState == BladeState.Idle)
                    SetState(BladeState.Hover);
            };

            MouseLeave += (s, e) =>
            {
                if (_currentState == BladeState.Hover)
                    SetState(BladeState.Idle);
            };

            MouseLeftButtonDown += (s, e) =>
            {
                if (_currentState != BladeState.Charging)
                    SetState(BladeState.Charging);
            };

            MouseLeftButtonUp += (s, e) =>
            {
                if (_currentState == BladeState.Charging)
                    SetState(BladeState.Releasing);
            };
        }

        private void SetState(BladeState newState)
        {
            if (_currentState == newState) return;

            BladeState oldState = _currentState;
            _currentState = newState;

            switch (newState)
            {
                case BladeState.Idle:
                    AnimateToIdle();
                    break;
                case BladeState.Hover:
                    AnimateToHover();
                    break;
                case BladeState.Active:
                    AnimateToActive();
                    OnBladeActivated?.Invoke();
                    break;
                case BladeState.Charging:
                    AnimateToCharging();
                    OnBladeCharged?.Invoke();
                    break;
                case BladeState.Releasing:
                    AnimateRelease();
                    OnBladeReleased?.Invoke();
                    break;
            }
        }

        private void AnimateToIdle()
        {
            var scaleAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var opacityAnimation = new DoubleAnimation
            {
                To = 0.9,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            _bladePath.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void AnimateToHover()
        {
            var scaleAnimation = new DoubleAnimation
            {
                To = 1.1,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var opacityAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.2)
            };

            _bladePath.BeginAnimation(OpacityProperty, opacityAnimation);

            // Add subtle glow pulse on hover
            EmitParticlesInCircle(250, 250, 30, 20, Color.FromRgb(0, 217, 255));
        }

        private void AnimateToActive()
        {
            // Laser burst effect
            var scaleAnimation = new DoubleAnimation
            {
                To = 1.3,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2 }
            };

            // Emit particles around blade
            EmitParticlesInCircle(250, 250, 50, 40, Color.FromRgb(255, 0, 85));
        }

        private void AnimateToCharging()
        {
            // Scale up during charge
            var keyFrames = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            keyFrames.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 1.0, KeyTime = KeyTime.FromPercent(0) });
            keyFrames.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 1.15, KeyTime = KeyTime.FromPercent(0.5) });
            keyFrames.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 1.0, KeyTime = KeyTime.FromPercent(1) });

            var rotationAnimation = new DoubleAnimation
            {
                From = _rotationAngle,
                To = _rotationAngle + 360,
                Duration = TimeSpan.FromSeconds(3),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _bladeRotation.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);

            // Emit continuous particles during charge
            var chargeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };

            chargeTimer.Tick += (s, e) =>
            {
                if (_currentState == BladeState.Charging)
                {
                    EmitParticlesInCone(250, 250, 30, 8, Color.FromRgb(100, 200, 255));
                }
                else
                {
                    chargeTimer.Stop();
                }
            };

            chargeTimer.Start();
        }

        private void AnimateRelease()
        {
            // Large burst
            EmitParticlesInCircle(250, 250, 100, 100, Color.FromRgb(255, 255, 255));
            EmitParticlesInCircle(250, 250, 80, 80, Color.FromRgb(0, 217, 255));

            // Revert to hover after release
            var resetTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.3)
            };

            resetTimer.Tick += (s, e) =>
            {
                resetTimer.Stop();
                if (_currentState == BladeState.Releasing)
                    SetState(BladeState.Hover);
            };

            resetTimer.Start();
        }

        private void UpdateBladeVisuals()
        {
            // Draw particles on effects canvas
            _effectsCanvas.Children.Clear();

            foreach (var particle in _particleSystem.GetActiveParticles())
            {
                var dot = new Ellipse
                {
                    Width = particle.Size,
                    Height = particle.Size,
                    Fill = new SolidColorBrush(particle.Color),
                    Opacity = particle.Opacity
                };

                Canvas.SetLeft(dot, particle.Position.X - particle.Size / 2);
                Canvas.SetTop(dot, particle.Position.Y - particle.Size / 2);

                _effectsCanvas.Children.Add(dot);
            }
        }

        private void EmitParticlesInCircle(double centerX, double centerY, double radius, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                double angle = (i / (double)count) * Math.PI * 2;
                double vx = Math.Cos(angle) * 2;
                double vy = Math.Sin(angle) * 2;

                _particleSystem.Emit(
                    new Vector2(centerX, centerY),
                    new Vector2(vx, vy),
                    color,
                    1.0f,
                    5.0f
                );
            }
        }

        private void EmitParticlesInCone(double centerX, double centerY, double radius, int count, Color color)
        {
            var random = new Random();
            for (int i = 0; i < count; i++)
            {
                double angle = (_rotationAngle + random.NextDouble() * 60 - 30) * Math.PI / 180;
                double speed = 1.0 + random.NextDouble() * 1.5;
                double vx = Math.Cos(angle) * speed;
                double vy = Math.Sin(angle) * speed;

                _particleSystem.Emit(
                    new Vector2(centerX, centerY),
                    new Vector2(vx, vy),
                    color,
                    0.8f,
                    3.0f
                );
            }
        }
    }

    public enum BladeState
    {
        Idle,
        Hover,
        Active,
        Charging,
        Releasing
    }

    /// <summary>
    /// Vector2 for particle physics.
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
        public static Vector2 operator *(Vector2 a, double s) => new Vector2(a.X * s, a.Y * s);
    }

    /// <summary>
    /// Simple particle for effects.
    /// </summary>
    public class Particle
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Color Color { get; set; }
        public double Lifetime { get; set; }
        public double MaxLifetime { get; set; }
        public float Size { get; set; }

        public double Opacity => Math.Max(0, Lifetime / MaxLifetime);
    }

    /// <summary>
    /// Lightweight particle system for blade effects.
    /// </summary>
    public class ParticleSystem
    {
        private List<Particle> _particles = new List<Particle>();
        public int MaxParticles { get; set; } = 1000;

        public void Emit(Vector2 position, Vector2 velocity, Color color, float size, double lifetime)
        {
            if (_particles.Count >= MaxParticles) return;

            _particles.Add(new Particle
            {
                Position = position,
                Velocity = velocity,
                Color = color,
                Size = size,
                Lifetime = lifetime,
                MaxLifetime = lifetime
            });
        }

        public void Update(double deltaTime)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Lifetime -= deltaTime;

                if (p.Lifetime <= 0)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                p.Position = p.Position + p.Velocity * deltaTime;
                p.Velocity = p.Velocity * 0.98; // Friction
            }
        }

        public IEnumerable<Particle> GetActiveParticles() => _particles;
    }
}
