using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Interactive orbiting kanji with custom icons, glow effects, and sound.
    /// Each kanji affects blade/wheel color and emits unique audio.
    /// </summary>
    public class InteractiveOrbitalKanji : Canvas
    {
        private Grid _kanjiContainer;
        private Ellipse _glowRing;
        private Ellipse _innerGlow;
        private TextBlock _kanjiText;
        private TextBlock _iconText;
        private TextBlock _labelText;
        private RotateTransform _containerRotation;
        private DoubleAnimation _glowAnimation;
        private DoubleAnimation _scaleAnimation;
        private MonadoSoundManager _soundManager;
        private KanjiGlowSystem _glowSystem;
        private KanjiConfig _config;
        private bool _isHovered = false;
        private double _currentGlowIntensity = 0.5;

        public event Action<string> OnKanjiHovered;
        public event Action<string> OnKanjiLeft;
        public event Action<Color, Color> OnGlowIntensified;

        public InteractiveOrbitalKanji(KanjiConfig config, MonadoSoundManager soundManager, KanjiGlowSystem glowSystem)
        {
            _config = config;
            _soundManager = soundManager;
            _glowSystem = glowSystem;

            Width = 150;
            Height = 150;
            Background = Brushes.Transparent;

            _containerRotation = new RotateTransform { CenterX = 75, CenterY = 75 };
            RenderTransform = _containerRotation;

            BuildVisuals();
            SetupInteractivity();
            AnimateBasicState();
        }

        private void BuildVisuals()
        {
            _kanjiContainer = new Grid
            {
                Width = 150,
                Height = 150,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Outer glow ring (inactive)
            _glowRing = new Ellipse
            {
                Width = 140,
                Height = 140,
                Stroke = new SolidColorBrush(_config.Color),
                StrokeThickness = 2,
                Opacity = 0.3
            };

            Canvas.SetLeft(_glowRing, 5);
            Canvas.SetTop(_glowRing, 5);
            Children.Add(_glowRing);

            // Inner glow (intensifies on hover)
            _innerGlow = new Ellipse
            {
                Width = 120,
                Height = 120,
                Fill = new SolidColorBrush(_config.Color),
                Opacity = 0.1,
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 15 }
            };

            Canvas.SetLeft(_innerGlow, 15);
            Canvas.SetTop(_innerGlow, 15);
            Children.Add(_innerGlow);

            // Icon (visual representation)
            _iconText = new TextBlock
            {
                Text = _config.Icon,
                FontSize = 64,
                Foreground = new SolidColorBrush(_config.Color),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0),
                Opacity = 0.85
            };

            // Icon glow effect
            _iconText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = _config.Color,
                ShadowDepth = 0,
                BlurRadius = 12,
                Opacity = 0.6
            };

            Children.Add(_iconText);

            // Kanji character (larger)
            _kanjiText = new TextBlock
            {
                Text = _config.Character,
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_config.Color),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 0, 0),
                Opacity = 0.9
            };

            _kanjiText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = _config.Color,
                ShadowDepth = 0,
                BlurRadius = 10,
                Opacity = 0.7
            };

            Children.Add(_kanjiText);

            // Label (meaning in English)
            _labelText = new TextBlock
            {
                Text = _config.Meaning,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 8),
                Opacity = 0.6
            };

            Children.Add(_labelText);
        }

        private void SetupInteractivity()
        {
            MouseEnter += (s, e) =>
            {
                _isHovered = true;
                OnKanjiHovered?.Invoke(_config.Type);
                _soundManager.PlayKanjiSound(_config.Type);

                // Intensify glow
                AnimateGlowIntensity(1.0);
                AnimateScale(1.3);
                AnimateOuterGlow(0.8);
            };

            MouseLeave += (s, e) =>
            {
                _isHovered = false;
                OnKanjiLeft?.Invoke(_config.Type);

                // Revert to normal
                AnimateGlowIntensity(0.5);
                AnimateScale(1.0);
                AnimateOuterGlow(0.3);
            };

            MouseDown += (s, e) =>
            {
                // Activate full power
                AnimateGlowIntensity(1.0);
                AnimateScale(1.4);
                _soundManager.PlayGlowSound();
                OnGlowIntensified?.Invoke(_config.Color, _config.ComplementColor);
            };
        }

        private void AnimateBasicState()
        {
            // Subtle pulsing glow when idle
            _glowAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 0.7,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            _innerGlow.BeginAnimation(OpacityProperty, _glowAnimation);
        }

        private void AnimateGlowIntensity(double targetIntensity)
        {
            var animation = new DoubleAnimation
            {
                To = targetIntensity,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            _currentGlowIntensity = targetIntensity;
            _glowSystem.ActivateKanjiGlow(_config.Type, targetIntensity);
            _innerGlow.BeginAnimation(OpacityProperty, animation);
        }

        private void AnimateScale(double scale)
        {
            var scaleTransform = new ScaleTransform { CenterX = 75, CenterY = 75 };
            RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    _containerRotation,
                    scaleTransform
                }
            };

            var animation = new DoubleAnimation
            {
                To = scale,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void AnimateOuterGlow(double opacity)
        {
            var animation = new DoubleAnimation
            {
                To = opacity,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            _glowRing.BeginAnimation(OpacityProperty, animation);
        }
    }

    public class KanjiConfig
    {
        public string Character { get; set; }
        public string Type { get; set; }                // "power", "sword", etc.
        public string Icon { get; set; }                // Visual symbol
        public string Meaning { get; set; }             // English name
        public Color Color { get; set; }                // Primary color
        public Color ComplementColor { get; set; }      // Complementary color for blade mix
        public double Radius { get; set; }              // Orbit radius
        public double StartAngle { get; set; }          // Starting position
    }

    /// <summary>
    /// Enhanced Monado loading screen with interactive kanji system.
    /// </summary>
    public class MonadoLoadingScreenWithInteractiveKanji : Window
    {
        private Canvas _mainCanvas;
        private Canvas _wheelCanvas;
        private Canvas _kanjiContainer;
        private MonadoSoundManager _soundManager;
        private KanjiGlowSystem _glowSystem;
        private List<InteractiveOrbitalKanji> _interactiveKanji = new List<InteractiveOrbitalKanji>();
        private RotateTransform _wheelRotation;
        private RotateTransform _kanjiRotation;

        public MonadoLoadingScreenWithInteractiveKanji()
        {
            _soundManager = new MonadoSoundManager();
            _glowSystem = new KanjiGlowSystem();

            InitializeWindow();
            SetupKanjiSystem();
            SetupAnimations();
        }

        private void InitializeWindow()
        {
            Width = 1200;
            Height = 800;
            WindowStyle = WindowStyle.None;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(10, 20, 40));

            var mainGrid = new Grid
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(10, 20, 40), 0),
                        new GradientStop(Color.FromRgb(5, 15, 35), 0.5),
                        new GradientStop(Color.FromRgb(15, 30, 55), 1)
                    }
                }
            };

            _mainCanvas = new Canvas
            {
                Width = 1200,
                Height = 800,
                Background = Brushes.Transparent
            };

            // Monado wheel
            _wheelCanvas = new Canvas
            {
                Width = 400,
                Height = 400,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 100, 0, 0)
            };

            _wheelRotation = new RotateTransform { CenterX = 200, CenterY = 200 };
            _wheelCanvas.RenderTransform = _wheelRotation;

            DrawMonadoWheelDynamic();

            // Kanji container
            _kanjiContainer = new Canvas
            {
                Width = 600,
                Height = 600,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 0)
            };

            _kanjiRotation = new RotateTransform { CenterX = 300, CenterY = 300 };
            _kanjiContainer.RenderTransform = _kanjiRotation;

            // Status panel
            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 80)
            };

            var statusLabel = new TextBlock
            {
                Text = "MONADO BLADE INITIALIZING...",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            var progressBar = new ProgressBar
            {
                Width = 400,
                Height = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var versionLabel = new TextBlock
            {
                Text = "v3.4.0 PREMIUM EDITION - INTERACTIVE KANJI SYSTEM",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            };

            statusPanel.Children.Add(statusLabel);
            statusPanel.Children.Add(progressBar);
            statusPanel.Children.Add(versionLabel);

            _mainCanvas.Children.Add(_kanjiContainer);
            _mainCanvas.Children.Add(_wheelCanvas);
            _mainCanvas.Children.Add(statusPanel);

            mainGrid.Children.Add(_mainCanvas);
            Content = mainGrid;
        }

        private void DrawMonadoWheelDynamic()
        {
            // Simplified wheel for dynamic color updates
            var rings = new[] { 80, 110, 140, 170, 200 };

            for (int i = 0; i < rings.Length; i++)
            {
                var ring = new Ellipse
                {
                    Width = rings[i] * 2,
                    Height = rings[i] * 2,
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                    StrokeThickness = 3,
                    Opacity = 0.8
                };

                Canvas.SetLeft(ring, 200 - rings[i]);
                Canvas.SetTop(ring, 200 - rings[i]);
                _wheelCanvas.Children.Add(ring);
            }

            // Center glow
            var center = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Opacity = 0.9
            };

            Canvas.SetLeft(center, 180);
            Canvas.SetTop(center, 180);
            _wheelCanvas.Children.Add(center);

            // Connect to glow system
            _glowSystem.OnGlowChanged += (bladeColor, wheelColor) =>
            {
                if (center.Fill is SolidColorBrush brush)
                {
                    brush.Color = wheelColor;
                }

                foreach (var child in _wheelCanvas.Children.OfType<Ellipse>())
                {
                    if (child.Stroke is SolidColorBrush strokeBrush && child != center)
                    {
                        strokeBrush.Color = wheelColor;
                    }
                }
            };
        }

        private void SetupKanjiSystem()
        {
            var kanjiConfigs = new[]
            {
                new KanjiConfig
                {
                    Character = "力",
                    Type = "power",
                    Icon = "⚡",
                    Meaning = "Power",
                    Color = Color.FromRgb(255, 0, 85),
                    ComplementColor = Color.FromRgb(0, 217, 255),
                    Radius = 280,
                    StartAngle = 0
                },
                new KanjiConfig
                {
                    Character = "刀",
                    Type = "sword",
                    Icon = "⚔",
                    Meaning = "Sword",
                    Color = Color.FromRgb(255, 215, 0),
                    ComplementColor = Color.FromRgb(100, 200, 255),
                    Radius = 280,
                    StartAngle = Math.PI / 3
                },
                new KanjiConfig
                {
                    Character = "光",
                    Type = "light",
                    Icon = "✨",
                    Meaning = "Light",
                    Color = Color.FromRgb(100, 200, 255),
                    ComplementColor = Color.FromRgb(255, 215, 0),
                    Radius = 280,
                    StartAngle = 2 * Math.PI / 3
                },
                new KanjiConfig
                {
                    Character = "流",
                    Type = "flow",
                    Icon = "≈",
                    Meaning = "Flow",
                    Color = Color.FromRgb(0, 255, 65),
                    ComplementColor = Color.FromRgb(255, 100, 150),
                    Radius = 280,
                    StartAngle = Math.PI
                },
                new KanjiConfig
                {
                    Character = "魂",
                    Type = "soul",
                    Icon = "♡",
                    Meaning = "Soul",
                    Color = Color.FromRgb(255, 100, 150),
                    ComplementColor = Color.FromRgb(0, 255, 65),
                    Radius = 280,
                    StartAngle = 4 * Math.PI / 3
                },
                new KanjiConfig
                {
                    Character = "機",
                    Type = "machine",
                    Icon = "⚙",
                    Meaning = "Machine",
                    Color = Color.FromRgb(0, 217, 255),
                    ComplementColor = Color.FromRgb(255, 0, 85),
                    Radius = 280,
                    StartAngle = 5 * Math.PI / 3
                }
            };

            foreach (var config in kanjiConfigs)
            {
                var kanji = new InteractiveOrbitalKanji(config, _soundManager, _glowSystem);
                double x = 300 + config.Radius * Math.Cos(config.StartAngle) - 75;
                double y = 300 + config.Radius * Math.Sin(config.StartAngle) - 75;

                Canvas.SetLeft(kanji, x);
                Canvas.SetTop(kanji, y);

                _kanjiContainer.Children.Add(kanji);
                _interactiveKanji.Add(kanji);
            }
        }

        private void SetupAnimations()
        {
            // Wheel rotation
            var wheelAnim = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(8),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _wheelRotation.BeginAnimation(RotateTransform.AngleProperty, wheelAnim);

            // Kanji orbit
            var kanjiAnim = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(12),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _kanjiRotation.BeginAnimation(RotateTransform.AngleProperty, kanjiAnim);

            // Progress animation
            _soundManager.PlayLoadingSound("start");

            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            dispatcherTimer.Tick += (s, e) =>
            {
                dispatcherTimer.Stop();
                _soundManager.PlayLoadingSound("complete");

                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(1)
                };

                fadeOut.Completed += (s2, e2) =>
                {
                    DialogResult = true;
                    Close();
                };

                BeginAnimation(OpacityProperty, fadeOut);
            };

            dispatcherTimer.Start();
        }
    }
}
