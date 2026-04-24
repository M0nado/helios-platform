using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Monado Wheel component - Interactive spinning wheel with 3-5 rotating rings.
    /// Features: Status color indication, drag-to-rotate, momentum physics, hover effects.
    /// </summary>
    public class MonadoWheel : FrameworkElement
    {
        private List<WheelRing> _rings = new List<WheelRing>();
        private double _totalRotation = 0;
        private double _rotationVelocity = 0; // Degrees per second
        private const double FRICTION = 0.95; // Momentum friction factor
        private Vector2 _center;
        private double _baseRadius;
        private bool _isHovered = false;
        private double _glowIntensity = 0.0;
        private Point _lastMousePosition;
        private DateTime _lastMouseTime = DateTime.Now;

        public Vector2 Position { get; set; }
        public double Radius
        {
            get => _baseRadius;
            set
            {
                _baseRadius = value;
                RebuildRings();
            }
        }

        public int RingCount
        {
            get => _rings.Count;
            set
            {
                _rings.Clear();
                CreateRings(value);
            }
        }

        public MonadoWheel()
        {
            Width = 300;
            Height = 300;
            Position = new Vector2(150, 150);
            _baseRadius = 80;
            _center = new Vector2(150, 150);

            RingCount = 4; // Create 4 rings by default

            MouseEnter += (s, e) =>
            {
                _isHovered = true;
                _glowIntensity = 1.0;
            };

            MouseLeave += (s, e) =>
            {
                _isHovered = false;
                _glowIntensity = 0.3;
            };

            MouseDown += (s, e) =>
            {
                _lastMousePosition = e.GetPosition(this);
                _lastMouseTime = DateTime.Now;
            };

            MouseMove += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    HandleMouseDrag(e.GetPosition(this));
                }
            };

            MouseUp += (s, e) =>
            {
                _rotationVelocity *= 0.95; // Preserve momentum
            };
        }

        /// <summary>
        /// Create wheel rings with different radii.
        /// </summary>
        private void CreateRings(int count)
        {
            double ringSpacing = _baseRadius / (count + 1);

            for (int i = 0; i < count; i++)
            {
                var ring = new WheelRing
                {
                    Index = i,
                    Radius = ringSpacing * (i + 1),
                    Rotation = 0,
                    SegmentCount = 8 + (i * 2), // More segments on outer rings
                    Status = WheelStatus.Healthy,
                    Label = $"Ring {i + 1}",
                    Color = GetRingColor(i, count)
                };

                _rings.Add(ring);
            }
        }

        /// <summary>
        /// Rebuild rings when radius changes.
        /// </summary>
        private void RebuildRings()
        {
            int count = _rings.Count;
            _rings.Clear();
            CreateRings(count);
        }

        /// <summary>
        /// Get color for ring based on position.
        /// </summary>
        private Color GetRingColor(int index, int totalRings)
        {
            double hue = (index / (double)totalRings) * 360;
            return HSVToRGB(hue, 0.7, 0.9);
        }

        /// <summary>
        /// Convert HSV to RGB color.
        /// </summary>
        private Color HSVToRGB(double h, double s, double v)
        {
            double c = v * s;
            double hDash = h / 60;
            double x = c * (1 - Math.Abs(hDash % 2 - 1));

            double r = 0, g = 0, b = 0;

            if (hDash < 1) { r = c; g = x; }
            else if (hDash < 2) { r = x; g = c; }
            else if (hDash < 3) { g = c; b = x; }
            else if (hDash < 4) { g = x; b = c; }
            else if (hDash < 5) { r = x; b = c; }
            else { r = c; b = x; }

            double m = v - c;
            return Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255)
            );
        }

        /// <summary>
        /// Handle mouse drag for rotation.
        /// </summary>
        private void HandleMouseDrag(Point currentPosition)
        {
            Vector2 dragVector = new Vector2(
                currentPosition.X - _lastMousePosition.X,
                currentPosition.Y - _lastMousePosition.Y
            );

            // Calculate rotation from drag
            double dragDistance = dragVector.Length;
            double rotationDelta = (dragDistance / _baseRadius) * 180; // Max rotation based on distance

            // Determine direction
            Vector2 centerToLastMouse = new Vector2(
                _lastMousePosition.X - _center.X,
                _lastMousePosition.Y - _center.Y
            );

            double cross = centerToLastMouse.X * dragVector.Y - centerToLastMouse.Y * dragVector.X;
            if (cross < 0)
            {
                rotationDelta *= -1;
            }

            _rotationVelocity = rotationDelta * 2; // Convert to degrees/second estimate
            _lastMousePosition = currentPosition;
            _lastMouseTime = DateTime.Now;
        }

        /// <summary>
        /// Update rotation with momentum.
        /// </summary>
        public void Update(double deltaTime)
        {
            // Apply friction
            _rotationVelocity *= FRICTION;

            // Update total rotation
            _totalRotation += _rotationVelocity * deltaTime;
            _totalRotation %= 360;

            // Update individual rings with slight speed variations
            for (int i = 0; i < _rings.Count; i++)
            {
                double speedMult = 1.0 + (i * 0.1); // Outer rings rotate faster
                _rings[i].Rotation = (_totalRotation * speedMult) % 360;
            }

            // Fade glow when not hovered
            if (!_isHovered)
            {
                _glowIntensity = Math.Max(0.3, _glowIntensity - (deltaTime * 0.5));
            }
        }

        /// <summary>
        /// Set status color of a specific ring.
        /// </summary>
        public void SetRingStatus(int ringIndex, WheelStatus status)
        {
            if (ringIndex >= 0 && ringIndex < _rings.Count)
            {
                _rings[ringIndex].Status = status;
                _rings[ringIndex].StatusColor = status switch
                {
                    WheelStatus.Healthy => Colors.Lime,
                    WheelStatus.Warning => Color.FromRgb(255, 200, 0),
                    WheelStatus.Critical => Colors.Red,
                    _ => Colors.White
                };
            }
        }

        /// <summary>
        /// Rotate wheel programmatically.
        /// </summary>
        public void Rotate(double angleInDegrees, double duration = 0.5)
        {
            _rotationVelocity = angleInDegrees / duration;
        }

        /// <summary>
        /// Get all rings for external access.
        /// </summary>
        public IEnumerable<WheelRing> GetRings()
        {
            return _rings;
        }

        /// <summary>
        /// Render the wheel.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Draw center circle (glow)
            double glowRadius = 15 + (_glowIntensity * 10);
            var glowColor = Color.FromArgb(
                (byte)(100 * _glowIntensity),
                0,
                217,
                255
            );
            var glowBrush = new SolidColorBrush(glowColor);
            drawingContext.DrawEllipse(glowBrush, null, new Point(_center.X, _center.Y), glowRadius, glowRadius);

            // Draw each ring
            foreach (var ring in _rings)
            {
                DrawRing(drawingContext, ring);
            }

            // Draw center core
            drawingContext.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                new Pen(Brushes.White, 2),
                new Point(_center.X, _center.Y),
                8,
                8
            );
        }

        /// <summary>
        /// Draw a single ring.
        /// </summary>
        private void DrawRing(DrawingContext drawingContext, WheelRing ring)
        {
            double segmentAngle = 360.0 / ring.SegmentCount;
            double innerRadius = ring.Radius * 0.8;
            double outerRadius = ring.Radius;

            for (int i = 0; i < ring.SegmentCount; i++)
            {
                double startAngle = (ring.Rotation + (i * segmentAngle)) * Math.PI / 180;
                double endAngle = startAngle + (segmentAngle * Math.PI / 180);

                // Segment color varies by status
                Color segmentColor = ring.Status == WheelStatus.Healthy
                    ? ring.Color
                    : ring.StatusColor;

                // Darker on alternate segments for contrast
                if (i % 2 == 0)
                {
                    segmentColor = Color.FromRgb(
                        (byte)(segmentColor.R * 0.7),
                        (byte)(segmentColor.G * 0.7),
                        (byte)(segmentColor.B * 0.7)
                    );
                }

                DrawSegment(drawingContext, startAngle, endAngle, innerRadius, outerRadius, segmentColor);
            }

            // Draw ring border
            var ringPen = new Pen(new SolidColorBrush(ring.Color), 1.5);
            drawingContext.DrawEllipse(null, ringPen, new Point(_center.X, _center.Y), outerRadius, outerRadius);
            drawingContext.DrawEllipse(null, ringPen, new Point(_center.X, _center.Y), innerRadius, innerRadius);
        }

        /// <summary>
        /// Draw a ring segment (pie slice).
        /// </summary>
        private void DrawSegment(DrawingContext drawingContext, double startAngle, double endAngle, double innerRadius, double outerRadius, Color color)
        {
            // Calculate points
            Point p1 = GetPointOnCircle(_center.X, _center.Y, startAngle, innerRadius);
            Point p2 = GetPointOnCircle(_center.X, _center.Y, startAngle, outerRadius);
            Point p3 = GetPointOnCircle(_center.X, _center.Y, endAngle, outerRadius);
            Point p4 = GetPointOnCircle(_center.X, _center.Y, endAngle, innerRadius);

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = p1 };

            pathFigure.Segments.Add(new LineSegment(p2, true));

            // Arc segment
            bool largeArc = (endAngle - startAngle) > Math.PI;
            pathFigure.Segments.Add(new ArcSegment(p3, new Size(outerRadius, outerRadius), 0, largeArc, SweepDirection.Clockwise, true));

            pathFigure.Segments.Add(new LineSegment(p4, true));

            pathFigure.Segments.Add(new ArcSegment(p1, new Size(innerRadius, innerRadius), 0, largeArc, SweepDirection.Counterclockwise, true));
            pathFigure.IsClosed = true;

            pathGeometry.Figures.Add(pathFigure);

            drawingContext.DrawGeometry(new SolidColorBrush(color), null, pathGeometry);
        }

        /// <summary>
        /// Get point on circle.
        /// </summary>
        private Point GetPointOnCircle(double cx, double cy, double angle, double radius)
        {
            return new Point(
                cx + Math.Cos(angle) * radius,
                cy + Math.Sin(angle) * radius
            );
        }
    }

    /// <summary>
    /// Wheel status indicators.
    /// </summary>
    public enum WheelStatus
    {
        Healthy,
        Warning,
        Critical
    }

    /// <summary>
    /// Data for a single wheel ring.
    /// </summary>
    public class WheelRing
    {
        public int Index { get; set; }
        public double Radius { get; set; }
        public double Rotation { get; set; }
        public int SegmentCount { get; set; }
        public string Label { get; set; }
        public Color Color { get; set; }
        public WheelStatus Status { get; set; }
        public Color StatusColor { get; set; }
    }
}
