using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MonadoBlade.GUI.Windows
{
    /// <summary>
    /// Professional Xenoblade Chronicles-inspired loading screen.
    /// Features: Spinning Monado wheel, orbiting kanji, laser effects, smooth transitions.
    /// </summary>
    public class MonadoLoadingScreen : Window
    {
        private Canvas _mainCanvas;
        private Canvas _wheelCanvas;
        private Canvas _kanji OrbitCanvas;
        private RotateTransform _wheelRotation;
        private RotateTransform _kanjiRotation;
        private TextBlock _statusLabel;
        private ProgressBar _progressBar;
        private List<OrbitalKanji> _orbitalKanji = new List<OrbitalKanji>();
        private bool _isLoadingComplete = false;

        public MonadoLoadingScreen()
        {
            InitializeWindow();
            SetupLoadingAnimation();
        }

        private void InitializeWindow()
        {
            Width = 1200;
            Height = 800;
            WindowStyle = WindowStyle.None;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(10, 20, 40));
            AllowsTransparency = true;

            // Main container
            var mainGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 20, 40))
            };

            // Background gradient
            var backgroundBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(10, 20, 40), 0),
                    new GradientStop(Color.FromRgb(5, 15, 35), 0.5),
                    new GradientStop(Color.FromRgb(15, 30, 55), 1)
                }
            };

            mainGrid.Background = backgroundBrush;

            // Main canvas for animations
            _mainCanvas = new Canvas
            {
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 1200,
                Height = 800
            };

            // Monado wheel canvas
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

            DrawMonadoWheel();

            // Kanji orbit canvas (larger, for circling kanji)
            _kanjiOrbitCanvas = new Canvas
            {
                Width = 600,
                Height = 600,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 0)
            };

            _kanjiRotation = new RotateTransform { CenterX = 300, CenterY = 300 };
            _kanjiOrbitCanvas.RenderTransform = _kanjiRotation;

            CreateOrbitalKanji();

            // Status info panel
            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 80)
            };

            _statusLabel = new TextBlock
            {
                Text = "MONADO BLADE INITIALIZING...",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            _progressBar = new ProgressBar
            {
                Width = 400,
                Height = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var versionLabel = new TextBlock
            {
                Text = "v3.4.0 PREMIUM EDITION",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            };

            statusPanel.Children.Add(_statusLabel);
            statusPanel.Children.Add(_progressBar);
            statusPanel.Children.Add(versionLabel);

            // Add everything to canvas
            _mainCanvas.Children.Add(_kanjiOrbitCanvas);
            _mainCanvas.Children.Add(_wheelCanvas);
            _mainCanvas.Children.Add(statusPanel);

            mainGrid.Children.Add(_mainCanvas);
            Content = mainGrid;
        }

        private void DrawMonadoWheel()
        {
            // Draw 5 concentric rings with laser effects
            var rings = new[] { 80, 110, 140, 170, 200 };
            var colors = new[] 
            { 
                Color.FromRgb(0, 217, 255),      // Cyan
                Color.FromRgb(0, 255, 65),       // Green
                Color.FromRgb(255, 215, 0),      // Amber
                Color.FromRgb(255, 0, 85),       // Magenta
                Color.FromRgb(100, 200, 255)     // Light Blue
            };

            for (int i = 0; i < rings.Length; i++)
            {
                // Ring circle
                var ring = new Ellipse
                {
                    Width = rings[i] * 2,
                    Height = rings[i] * 2,
                    Stroke = new SolidColorBrush(colors[i]),
                    StrokeThickness = 3,
                    Opacity = 0.8
                };

                Canvas.SetLeft(ring, 200 - rings[i]);
                Canvas.SetTop(ring, 200 - rings[i]);
                _wheelCanvas.Children.Add(ring);

                // Add segments (8 per ring)
                for (int j = 0; j < 8; j++)
                {
                    double angle = (j / 8.0) * Math.PI * 2;
                    double x1 = 200 + rings[i] * Math.Cos(angle);
                    double y1 = 200 + rings[i] * Math.Sin(angle);
                    double x2 = 200 + (rings[i] + 15) * Math.Cos(angle);
                    double y2 = 200 + (rings[i] + 15) * Math.Sin(angle);

                    var line = new Line
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        Stroke = new SolidColorBrush(colors[i]),
                        StrokeThickness = 2,
                        Opacity = 0.6
                    };

                    _wheelCanvas.Children.Add(line);
                }
            }

            // Center circle (laser glow)
            var centerGlow = new Ellipse
            {
                Width = 60,
                Height = 60,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Opacity = 0.3
            };

            Canvas.SetLeft(centerGlow, 170);
            Canvas.SetTop(centerGlow, 170);
            _wheelCanvas.Children.Add(centerGlow);

            // Center circle (bright)
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

            // Add laser sword effect (rotating blade shape)
            DrawLaserBlade();
        }

        private void DrawLaserBlade()
        {
            // Create blade shape using polygon
            var bladePoints = new PointCollection
            {
                new Point(200, 100),     // Top point
                new Point(205, 180),     // Right upper
                new Point(215, 200),     // Right middle
                new Point(205, 220),     // Right lower
                new Point(200, 300),     // Bottom point
                new Point(195, 220),     // Left lower
                new Point(185, 200),     // Left middle
                new Point(195, 180)      // Left upper
            };

            var blade = new Polygon
            {
                Points = bladePoints,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(0, 217, 255), 0),
                        new GradientStop(Color.FromRgb(255, 255, 255), 0.5),
                        new GradientStop(Color.FromRgb(0, 217, 255), 1)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                StrokeThickness = 2,
                Opacity = 0.85
            };

            _wheelCanvas.Children.Add(blade);

            // Add blade glow effect
            var bladeGlow = new Polygon
            {
                Points = bladePoints,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 217, 255)),
                StrokeThickness = 8,
                Opacity = 0.3
            };

            _wheelCanvas.Children.Add(bladeGlow);

            // Add blade outer glow
            var bladeOuterGlow = new Polygon
            {
                Points = bladePoints,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 15,
                Opacity = 0.15
            };

            _wheelCanvas.Children.Add(bladeOuterGlow);
        }

        private void CreateOrbitalKanji()
        {
            // 6 kanji characters orbiting around the wheel
            string[] kanjiChars = { "力", "刀", "光", "流", "魂", "機" };
            var colors = new[] 
            { 
                Color.FromRgb(255, 0, 85),       // Magenta - Power
                Color.FromRgb(255, 215, 0),      // Amber - Sword
                Color.FromRgb(100, 200, 255),    // Light Blue - Light
                Color.FromRgb(0, 255, 65),       // Green - Flow
                Color.FromRgb(255, 100, 150),    // Pink - Soul
                Color.FromRgb(150, 200, 255)     // Cyan - Machine
            };

            double radius = 280;
            double angleStep = (Math.PI * 2) / kanjiChars.Length;

            for (int i = 0; i < kanjiChars.Length; i++)
            {
                double angle = i * angleStep;
                double x = 300 + radius * Math.Cos(angle);
                double y = 300 + radius * Math.Sin(angle);

                var orbitalKanji = new OrbitalKanji
                {
                    Character = kanjiChars[i],
                    Color = colors[i],
                    Radius = radius,
                    StartAngle = angle,
                    Index = i
                };

                // Create kanji text with glow
                var kanjiText = new TextBlock
                {
                    Text = kanjiChars[i],
                    Foreground = new SolidColorBrush(colors[i]),
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.9
                };

                // Glow effect
                var effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = colors[i],
                    ShadowDepth = 0,
                    BlurRadius = 15,
                    Opacity = 0.8
                };

                kanjiText.Effect = effect;

                Canvas.SetLeft(kanjiText, x - 24);
                Canvas.SetTop(kanjiText, y - 24);

                _kanjiOrbitCanvas.Children.Add(kanjiText);
                orbitalKanji.TextElement = kanjiText;
                _orbitalKanji.Add(orbitalKanji);
            }
        }

        private void SetupLoadingAnimation()
        {
            // Wheel rotation animation (continuous spin)
            var wheelRotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(8),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var wheelEase = new LinearDoubleKeyFrame { Value = 360, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(8)) };
            _wheelRotation.BeginAnimation(RotateTransform.AngleProperty, wheelRotationAnimation);

            // Kanji orbit animation (counter-clockwise, slightly slower)
            var kanjiRotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(12),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _kanjiRotation.BeginAnimation(RotateTransform.AngleProperty, kanjiRotationAnimation);

            // Pulse animation for kanji (brightness)
            foreach (var kanji in _orbitalKanji)
            {
                var pulseAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(2),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                var delayedStart = new DoubleAnimation
                {
                    To = 0.7,
                    Duration = TimeSpan.FromSeconds(kanji.Index * 0.3),
                    FillBehavior = FillBehavior.Stop
                };

                kanji.TextElement.BeginAnimation(OpacityProperty, delayedStart);
                kanji.TextElement.BeginAnimation(OpacityProperty, pulseAnimation);
            }

            // Progress bar animation
            var progressAnimation = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromSeconds(5),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseInOut }
            };

            _progressBar.BeginAnimation(ProgressBar.ValueProperty, progressAnimation);

            // Status text transitions
            AnimateStatusText();

            // Complete loading after 5 seconds
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5.5)
            };

            dispatcherTimer.Tick += (s, e) =>
            {
                dispatcherTimer.Stop();
                CompleteLoading();
            };

            dispatcherTimer.Start();
        }

        private void AnimateStatusText()
        {
            var statusMessages = new[]
            {
                "MONADO BLADE INITIALIZING...",
                "LOADING VISUAL EFFECTS...",
                "SYNCHRONIZING KANJI ENERGY...",
                "PRIMING LASER SYSTEMS...",
                "READY FOR OPERATION"
            };

            int messageIndex = 0;
            var statusTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            statusTimer.Tick += (s, e) =>
            {
                if (messageIndex < statusMessages.Length)
                {
                    // Fade out
                    var fadeOut = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.3)
                    };

                    fadeOut.Completed += (s2, e2) =>
                    {
                        _statusLabel.Text = statusMessages[messageIndex];

                        // Fade in
                        var fadeIn = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };

                        _statusLabel.BeginAnimation(OpacityProperty, fadeIn);
                    };

                    _statusLabel.BeginAnimation(OpacityProperty, fadeOut);
                    messageIndex++;
                }
                else
                {
                    statusTimer.Stop();
                }
            };

            statusTimer.Start();
        }

        private void CompleteLoading()
        {
            _isLoadingComplete = true;

            // Final fade-out animation
            var finalFadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            finalFadeOut.Completed += (s, e) =>
            {
                DialogResult = true;
                Close();
            };

            BeginAnimation(OpacityProperty, finalFadeOut);
        }

        private class OrbitalKanji
        {
            public string Character { get; set; }
            public Color Color { get; set; }
            public double Radius { get; set; }
            public double StartAngle { get; set; }
            public int Index { get; set; }
            public TextBlock TextElement { get; set; }
        }
    }
}
