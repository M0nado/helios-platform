using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Authentic Xenoblade Chronicles Monado blade component.
    /// Interacts with kanji system, expands on interaction, emits energy effects.
    /// Based on the iconic Monado weapon from XBC1.
    /// </summary>
    public class XenobladeMondo : Canvas
    {
        private Canvas _bladeCanvas;
        private Canvas _effectsCanvas;
        private Canvas _energyCanvas;
        private Path _mainBlade;
        private Path _bladeGlow;
        private Path _bladeOuterGlow;
        private Ellipse _bladeTip;
        private Ellipse _bladeTipGlow;
        private Ellipse _crossGuard;
        private Ellipse _handle;
        private RotateTransform _bladeRotation;
        private ScaleTransform _bladeScale;
        private TransformGroup _bladeTransform;
        private double _currentScale = 1.0;
        private double _currentRotation = 0;
        private Color _currentColor = Color.FromRgb(0, 217, 255);
        private Color _targetColor = Color.FromRgb(0, 217, 255);
        private double _colorTransitionProgress = 0;
        private MonadoSoundManager _soundManager;
        private KanjiGlowSystem _glowSystem;
        private MonadoState _currentState = MonadoState.Idle;
        private List<EnergyWave> _activeWaves = new List<EnergyWave>();

        public event Action OnMonadoExpanded;
        public event Action OnMonadoActivated;

        public Color CurrentColor
        {
            get => _currentColor;
            set
            {
                _targetColor = value;
                _colorTransitionProgress = 0;
            }
        }

        public MonadoState State
        {
            get => _currentState;
            set => SetState(value);
        }

        public XenobladeMondo(MonadoSoundManager soundManager, KanjiGlowSystem glowSystem)
        {
            _soundManager = soundManager;
            _glowSystem = glowSystem;

            Width = 600;
            Height = 700;
            Background = Brushes.Transparent;

            _bladeCanvas = new Canvas
            {
                Width = 600,
                Height = 700,
                Background = Brushes.Transparent
            };

            _effectsCanvas = new Canvas
            {
                Width = 600,
                Height = 700,
                Background = Brushes.Transparent
            };

            _energyCanvas = new Canvas
            {
                Width = 600,
                Height = 700,
                Background = Brushes.Transparent
            };

            _bladeRotation = new RotateTransform { CenterX = 300, CenterY = 350 };
            _bladeScale = new ScaleTransform { CenterX = 300, CenterY = 350 };
            _bladeTransform = new TransformGroup
            {
                Children = new TransformCollection { _bladeRotation, _bladeScale }
            };

            _bladeCanvas.RenderTransform = _bladeTransform;

            BuildBlade();
            SetupInteractivity();
            StartAnimationLoop();

            Children.Add(_energyCanvas);
            Children.Add(_effectsCanvas);
            Children.Add(_bladeCanvas);
        }

        private void BuildBlade()
        {
            // Main blade geometry - Xenoblade Chronicles Monado shape
            // Curved blade with energy effects
            var bladeGeometry = new PathGeometry();
            var bladePoints = new PathFigure { StartPoint = new Point(300, 80) };

            // Blade outline - curved and tapered
            bladePoints.Segments.Add(new BezierSegment
            {
                Point1 = new Point(310, 120),
                Point2 = new Point(330, 160),
                Point3 = new Point(340, 200)
            });

            bladePoints.Segments.Add(new BezierSegment
            {
                Point1 = new Point(345, 240),
                Point2 = new Point(350, 280),
                Point3 = new Point(355, 320)
            });

            // Blade tip (sharp point with glow)
            bladePoints.Segments.Add(new BezierSegment
            {
                Point1 = new Point(358, 350),
                Point2 = new Point(360, 380),
                Point3 = new Point(300, 450)  // Curved tip
            });

            // Back down the other side
            bladePoints.Segments.Add(new BezierSegment
            {
                Point1 = new Point(240, 380),
                Point2 = new Point(242, 350),
                Point3 = new Point(245, 320)
            });

            bladePoints.Segments.Add(new BezierSegment
            {
                Point1 = new Point(250, 280),
                Point2 = new Point(255, 240),
                Point3 = new Point(260, 200)
            });

            bladePoints.Segments.Add(new BezierSegment
            {
                Point1 = new Point(270, 160),
                Point2 = new Point(290, 120),
                Point3 = new Point(300, 80)
            });

            bladePoints.IsClosed = true;
            bladeGeometry.Figures.Add(bladePoints);

            // Main blade with gradient (cyan with white hot core)
            _mainBlade = new Path
            {
                Data = bladeGeometry,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(50, 150, 200), 0),      // Deep blue
                        new GradientStop(Color.FromRgb(100, 200, 255), 0.2),   // Light blue
                        new GradientStop(Color.FromRgb(0, 217, 255), 0.35),    // Cyan
                        new GradientStop(Color.FromRgb(255, 255, 255), 0.5),   // White hot core
                        new GradientStop(Color.FromRgb(0, 217, 255), 0.65),    // Cyan
                        new GradientStop(Color.FromRgb(100, 200, 255), 0.8),   // Light blue
                        new GradientStop(Color.FromRgb(50, 150, 200), 1)       // Deep blue
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                StrokeThickness = 1.5,
                Opacity = 0.95
            };

            _bladeCanvas.Children.Add(_mainBlade);

            // Blade glow (inner)
            _bladeGlow = new Path
            {
                Data = bladeGeometry,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 217, 255)),
                StrokeThickness = 8,
                Opacity = 0.4,
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 6 }
            };

            _bladeCanvas.Children.Add(_bladeGlow);

            // Blade glow (outer - intense)
            _bladeOuterGlow = new Path
            {
                Data = bladeGeometry,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 15,
                Opacity = 0.2,
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 12 }
            };

            _bladeCanvas.Children.Add(_bladeOuterGlow);

            // Blade tip (energy point)
            _bladeTip = new Ellipse
            {
                Width = 20,
                Height = 30,
                Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Opacity = 0.9
            };

            Canvas.SetLeft(_bladeTip, 290);
            Canvas.SetTop(_bladeTip, 435);
            _bladeCanvas.Children.Add(_bladeTip);

            // Blade tip glow
            _bladeTipGlow = new Ellipse
            {
                Width = 50,
                Height = 70,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Opacity = 0.3,
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 15 }
            };

            Canvas.SetLeft(_bladeTipGlow, 275);
            Canvas.SetTop(_bladeTipGlow, 415);
            _bladeCanvas.Children.Add(_bladeTipGlow);

            // Cross guard (protective ring)
            _crossGuard = new Ellipse
            {
                Width = 90,
                Height = 90,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 4,
                Opacity = 0.7
            };

            Canvas.SetLeft(_crossGuard, 255);
            Canvas.SetTop(_crossGuard, 335);
            _bladeCanvas.Children.Add(_crossGuard);

            // Handle/Grip
            _handle = new Ellipse
            {
                Width = 50,
                Height = 120,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(30, 50, 70), 0),
                        new GradientStop(Color.FromRgb(50, 70, 90), 0.5),
                        new GradientStop(Color.FromRgb(30, 50, 70), 1)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 2,
                Opacity = 0.85
            };

            Canvas.SetLeft(_handle, 275);
            Canvas.SetTop(_handle, 410);
            _bladeCanvas.Children.Add(_handle);

            // Energy core at handle
            var handleCore = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Opacity = 0.8,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 217, 255),
                    ShadowDepth = 0,
                    BlurRadius = 12,
                    Opacity = 0.8
                }
            };

            Canvas.SetLeft(handleCore, 285);
            Canvas.SetTop(handleCore, 560);
            _bladeCanvas.Children.Add(handleCore);
        }

        private void SetupInteractivity()
        {
            MouseEnter += (s, e) =>
            {
                if (_currentState == MonadoState.Idle)
                    SetState(MonadoState.Hover);
            };

            MouseLeave += (s, e) =>
            {
                if (_currentState == MonadoState.Hover)
                    SetState(MonadoState.Idle);
            };

            MouseLeftButtonDown += (s, e) =>
            {
                SetState(MonadoState.Expanding);
            };

            MouseLeftButtonUp += (s, e) =>
            {
                if (_currentState == MonadoState.Expanding)
                    SetState(MonadoState.Active);
            };
        }

        private void SetState(MonadoState newState)
        {
            _currentState = newState;

            switch (newState)
            {
                case MonadoState.Idle:
                    AnimateToIdle();
                    break;
                case MonadoState.Hover:
                    AnimateToHover();
                    break;
                case MonadoState.Expanding:
                    AnimateExpanding();
                    break;
                case MonadoState.Active:
                    AnimateActive();
                    break;
            }
        }

        private void AnimateToIdle()
        {
            // Scale back to normal
            var scaleAnim = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            _bladeScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            _bladeScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // Reduce glow
            var glowAnim = new DoubleAnimation
            {
                To = 0.2,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            _bladeOuterGlow.BeginAnimation(OpacityProperty, glowAnim);
        }

        private void AnimateToHover()
        {
            _soundManager.PlayBladeSound("hover");

            // Slight scale increase
            var scaleAnim = new DoubleAnimation
            {
                To = 1.1,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            _bladeScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            _bladeScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // Increase glow
            var glowAnim = new DoubleAnimation
            {
                To = 0.5,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            _bladeOuterGlow.BeginAnimation(OpacityProperty, glowAnim);

            // Emit subtle particles
            EmitParticles(10, Color.FromRgb(0, 217, 255));
        }

        private void AnimateExpanding()
        {
            _soundManager.PlayBladeSound("charge");
            OnMonadoExpanded?.Invoke();

            // Rapid expansion
            var scaleAnim = new DoubleAnimation
            {
                To = 1.4,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            _bladeScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            _bladeScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // Full glow
            var glowAnim = new DoubleAnimation
            {
                To = 0.8,
                Duration = TimeSpan.FromSeconds(0.2)
            };

            _bladeOuterGlow.BeginAnimation(OpacityProperty, glowAnim);

            // Emit energy waves
            for (int i = 0; i < 3; i++)
            {
                var wave = new EnergyWave
                {
                    Color = _currentColor,
                    Radius = 40,
                    MaxRadius = 180,
                    Opacity = 0.8
                };

                _activeWaves.Add(wave);
            }

            // Emit particles
            EmitParticles(30, _currentColor);
        }

        private void AnimateActive()
        {
            _soundManager.PlayBladeSound("release");
            OnMonadoActivated?.Invoke();

            // Sustain at expanded scale
            var scaleAnim = new DoubleAnimation
            {
                To = 1.3,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2 }
            };

            _bladeScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            _bladeScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // Pulse glow
            var glowAnim = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever
            };

            glowAnim.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 0.6, KeyTime = KeyTime.FromPercent(0) });
            glowAnim.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 1.0, KeyTime = KeyTime.FromPercent(0.5) });
            glowAnim.KeyFrames.Add(new LinearDoubleKeyFrame { Value = 0.6, KeyTime = KeyTime.FromPercent(1) });

            _bladeOuterGlow.BeginAnimation(OpacityProperty, glowAnim);

            // Emit continuous particles
            var particleTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            particleTimer.Tick += (s, e) =>
            {
                if (_currentState == MonadoState.Active)
                {
                    EmitParticles(5, _currentColor);
                }
                else
                {
                    particleTimer.Stop();
                }
            };

            particleTimer.Start();

            // Auto-revert after 3 seconds
            var revertTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            revertTimer.Tick += (s, e) =>
            {
                revertTimer.Stop();
                if (_currentState == MonadoState.Active)
                    SetState(MonadoState.Idle);
            };

            revertTimer.Start();
        }

        private void EmitParticles(int count, Color color)
        {
            var random = new Random();
            for (int i = 0; i < count; i++)
            {
                double angle = random.NextDouble() * Math.PI * 2;
                double speed = 2.0 + random.NextDouble() * 3.0;
                double x = 300 + Math.Cos(angle) * 50;
                double y = 350 + Math.Sin(angle) * 50;

                var particle = new Ellipse
                {
                    Width = 4 + random.NextDouble() * 3,
                    Height = 4 + random.NextDouble() * 3,
                    Fill = new SolidColorBrush(color),
                    Opacity = 0.8
                };

                Canvas.SetLeft(particle, x);
                Canvas.SetTop(particle, y);
                _effectsCanvas.Children.Add(particle);

                // Animate outward
                var moveX = new DoubleAnimation
                {
                    To = x + Math.Cos(angle) * 200,
                    Duration = TimeSpan.FromSeconds(0.5 + random.NextDouble() * 0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var moveY = new DoubleAnimation
                {
                    To = y + Math.Sin(angle) * 200,
                    Duration = TimeSpan.FromSeconds(0.5 + random.NextDouble() * 0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.7 + random.NextDouble() * 0.3)
                };

                fadeOut.Completed += (s, e) =>
                {
                    _effectsCanvas.Children.Remove(particle);
                };

                particle.BeginAnimation(Canvas.LeftProperty, moveX);
                particle.BeginAnimation(Canvas.TopProperty, moveY);
                particle.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void StartAnimationLoop()
        {
            CompositionTarget.Rendering += (s, e) =>
            {
                // Update color transition
                if (_colorTransitionProgress < 1.0)
                {
                    _colorTransitionProgress += 0.05;
                    _currentColor = BlendColors(_currentColor, _targetColor, 0.05);
                    UpdateBladeColor(_currentColor);
                }

                // Update energy waves
                for (int i = _activeWaves.Count - 1; i >= 0; i--)
                {
                    var wave = _activeWaves[i];
                    wave.Radius += 3;

                    if (wave.Radius >= wave.MaxRadius)
                    {
                        _activeWaves.RemoveAt(i);
                    }
                    else
                    {
                        DrawEnergyWave(wave);
                    }
                }
            };
        }

        private void UpdateBladeColor(Color color)
        {
            if (_bladeOuterGlow.Stroke is SolidColorBrush brush)
            {
                brush.Color = color;
            }

            if (_bladeTip.Fill is SolidColorBrush tipBrush)
            {
                tipBrush.Color = BlendColors(color, Color.FromRgb(255, 255, 255), 0.3);
            }

            if (_crossGuard.Stroke is SolidColorBrush guardBrush)
            {
                guardBrush.Color = color;
            }
        }

        private Color BlendColors(Color color1, Color color2, double amount)
        {
            byte r = (byte)(color1.R * (1 - amount) + color2.R * amount);
            byte g = (byte)(color1.G * (1 - amount) + color2.G * amount);
            byte b = (byte)(color1.B * (1 - amount) + color2.B * amount);
            return Color.FromRgb(r, g, b);
        }

        private void DrawEnergyWave(EnergyWave wave)
        {
            var waveCircle = new Ellipse
            {
                Width = wave.Radius * 2,
                Height = wave.Radius * 2,
                Stroke = new SolidColorBrush(wave.Color),
                StrokeThickness = 2,
                Opacity = wave.Opacity * (1.0 - (wave.Radius / wave.MaxRadius))
            };

            Canvas.SetLeft(waveCircle, 300 - wave.Radius);
            Canvas.SetTop(waveCircle, 350 - wave.Radius);
            _energyCanvas.Children.Add(waveCircle);

            // Remove after fade
            if (_energyCanvas.Children.Count > 20)
            {
                _energyCanvas.Children.RemoveAt(0);
            }
        }
    }

    public enum MonadoState
    {
        Idle,
        Hover,
        Expanding,
        Active
    }

    public class EnergyWave
    {
        public Color Color { get; set; }
        public double Radius { get; set; }
        public double MaxRadius { get; set; }
        public double Opacity { get; set; }
    }
}
