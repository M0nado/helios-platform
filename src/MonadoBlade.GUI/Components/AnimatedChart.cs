using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Animated Chart Component for visualizing metrics
    /// Supports line, bar, and area charts with smooth animations
    /// </summary>
    public class AnimatedChart : Control
    {
        private Canvas _canvas;
        private Polyline _line;
        private ItemsControl _bars;

        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register("DataPoints", typeof(ObservableCollection<ChartDataPoint>),
                typeof(AnimatedChart), new PropertyMetadata(null, OnDataPointsChanged));

        public static readonly DependencyProperty ChartTypeProperty =
            DependencyProperty.Register("ChartType", typeof(ChartType),
                typeof(AnimatedChart), new PropertyMetadata(ChartType.Line));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string),
                typeof(AnimatedChart), new PropertyMetadata("Chart"));

        public static readonly DependencyProperty YMaxProperty =
            DependencyProperty.Register("YMax", typeof(double),
                typeof(AnimatedChart), new PropertyMetadata(100.0));

        public ObservableCollection<ChartDataPoint> DataPoints
        {
            get => (ObservableCollection<ChartDataPoint>)GetValue(DataPointsProperty);
            set => SetValue(DataPointsProperty, value);
        }

        public ChartType ChartType
        {
            get => (ChartType)GetValue(ChartTypeProperty);
            set => SetValue(ChartTypeProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public double YMax
        {
            get => (double)GetValue(YMaxProperty);
            set => SetValue(YMaxProperty, value);
        }

        static AnimatedChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedChart),
                new FrameworkPropertyMetadata(typeof(AnimatedChart)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = GetTemplateChild("PART_Canvas") as Canvas;
            _line = GetTemplateChild("PART_Line") as Polyline;
            _bars = GetTemplateChild("PART_Bars") as ItemsControl;

            if (_canvas != null && DataPoints != null)
            {
                RedrawChart();
            }
        }

        private static void OnDataPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (AnimatedChart)d;
            if (chart._canvas != null)
            {
                chart.RedrawChart();
            }
        }

        private void RedrawChart()
        {
            if (_canvas == null || DataPoints == null || DataPoints.Count == 0)
                return;

            switch (ChartType)
            {
                case ChartType.Line:
                    DrawLineChart();
                    break;
                case ChartType.Bar:
                    DrawBarChart();
                    break;
                case ChartType.Area:
                    DrawAreaChart();
                    break;
            }
        }

        private void DrawLineChart()
        {
            if (_line == null) return;

            _line.Points.Clear();
            double width = _canvas.ActualWidth;
            double height = _canvas.ActualHeight;
            double stepX = width / (DataPoints.Count - 1);

            // Build points
            for (int i = 0; i < DataPoints.Count; i++)
            {
                double x = i * stepX;
                double y = height - (DataPoints[i].Value / YMax) * height;
                _line.Points.Add(new Point(x, y));
            }

            // Animate line drawing
            AnimateLineChart();
        }

        private void AnimateLineChart()
        {
            // Opacity animation
            var opacityAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            _line.BeginAnimation(OpacityProperty, opacityAnim);
        }

        private void DrawBarChart()
        {
            if (_bars == null) return;

            _bars.Items.Clear();
            double width = _canvas.ActualWidth;
            double height = _canvas.ActualHeight;
            double barWidth = width / DataPoints.Count * 0.8;
            double stepX = width / DataPoints.Count;

            for (int i = 0; i < DataPoints.Count; i++)
            {
                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = (DataPoints[i].Value / YMax) * height,
                    Fill = new SolidColorBrush(Colors.CornflowerBlue)
                };

                Canvas.SetLeft(bar, i * stepX + stepX * 0.1);
                Canvas.SetTop(bar, height - bar.Height);

                _bars.Items.Add(bar);

                // Animate bar height
                var heightAnim = new DoubleAnimation
                {
                    From = 0,
                    To = bar.Height,
                    Duration = TimeSpan.FromMilliseconds(400 + i * 50),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                bar.BeginAnimation(HeightProperty, heightAnim);
            }
        }

        private void DrawAreaChart()
        {
            // Combine line and fill
            DrawLineChart();
            // Add area fill (semi-transparent)
        }
    }

    /// <summary>
    /// Represents a single data point on the chart
    /// </summary>
    public class ChartDataPoint
    {
        public double Value { get; set; }
        public string Label { get; set; }
        public DateTime Timestamp { get; set; }

        public ChartDataPoint(double value, string label = "", DateTime? timestamp = null)
        {
            Value = value;
            Label = label;
            Timestamp = timestamp ?? DateTime.Now;
        }
    }

    /// <summary>
    /// Chart type enumeration
    /// </summary>
    public enum ChartType
    {
        Line,
        Bar,
        Area
    }

    /// <summary>
    /// Gauge Component for showing a single metric value
    /// </summary>
    public class AnimatedGauge : Control
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double),
                typeof(AnimatedGauge), new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double),
                typeof(AnimatedGauge), new PropertyMetadata(100.0));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string),
                typeof(AnimatedGauge), new PropertyMetadata("Gauge"));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        static AnimatedGauge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedGauge),
                new FrameworkPropertyMetadata(typeof(AnimatedGauge)));
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gauge = (AnimatedGauge)d;
            double oldValue = (double)e.OldValue;
            double newValue = (double)e.NewValue;

            // Animate value change
            var animation = new DoubleAnimation
            {
                From = oldValue,
                To = newValue,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            gauge.BeginAnimation(ValueProperty, animation);
        }
    }
}
