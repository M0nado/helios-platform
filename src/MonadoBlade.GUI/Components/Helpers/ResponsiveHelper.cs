using System;
using System.Windows;

namespace MonadoBlade.GUI.Components.Helpers
{
    /// <summary>
    /// Responsive helper for adaptive UI layouts (100 LOC)
    /// Provides breakpoints, scaling, and responsive utilities for components.
    /// </summary>
    public static class ResponsiveHelper
    {
        public enum BreakPoint
        {
            Mobile,      // < 600px
            Tablet,      // 600px - 1024px
            Desktop,     // 1024px - 1440px
            HighRes      // > 1440px
        }

        private static double _screenWidth = SystemParameters.PrimaryScreenWidth;
        private static double _screenHeight = SystemParameters.PrimaryScreenHeight;
        private static BreakPoint _currentBreakPoint = GetBreakPoint(_screenWidth);

        public static event Action<BreakPoint> BreakPointChanged;

        public static double ScreenWidth => _screenWidth;
        public static double ScreenHeight => _screenHeight;
        public static BreakPoint CurrentBreakPoint => _currentBreakPoint;

        /// <summary>
        /// Get current breakpoint from screen width
        /// </summary>
        public static BreakPoint GetBreakPoint(double width)
        {
            return width switch
            {
                < 600 => BreakPoint.Mobile,
                < 1024 => BreakPoint.Tablet,
                < 1440 => BreakPoint.Desktop,
                _ => BreakPoint.HighRes
            };
        }

        /// <summary>
        /// Check if current breakpoint is mobile
        /// </summary>
        public static bool IsMobile() => _currentBreakPoint == BreakPoint.Mobile;

        /// <summary>
        /// Check if current breakpoint is tablet or smaller
        /// </summary>
        public static bool IsTabletOrSmaller() => 
            _currentBreakPoint == BreakPoint.Mobile || 
            _currentBreakPoint == BreakPoint.Tablet;

        /// <summary>
        /// Check if current breakpoint is desktop or larger
        /// </summary>
        public static bool IsDesktopOrLarger() => 
            _currentBreakPoint == BreakPoint.Desktop || 
            _currentBreakPoint == BreakPoint.HighRes;

        /// <summary>
        /// Get responsive font size based on breakpoint
        /// </summary>
        public static double GetResponsiveFontSize(double baseSize)
        {
            return _currentBreakPoint switch
            {
                BreakPoint.Mobile => baseSize * 0.85,
                BreakPoint.Tablet => baseSize * 0.92,
                BreakPoint.Desktop => baseSize,
                BreakPoint.HighRes => baseSize * 1.1,
                _ => baseSize
            };
        }

        /// <summary>
        /// Get responsive spacing based on breakpoint
        /// </summary>
        public static double GetResponsiveSpacing(double baseSpacing)
        {
            return _currentBreakPoint switch
            {
                BreakPoint.Mobile => baseSpacing * 0.5,
                BreakPoint.Tablet => baseSpacing * 0.75,
                BreakPoint.Desktop => baseSpacing,
                BreakPoint.HighRes => baseSpacing * 1.25,
                _ => baseSpacing
            };
        }

        /// <summary>
        /// Get responsive width
        /// </summary>
        public static double GetResponsiveWidth(double baseWidth)
        {
            var percentage = Math.Min(baseWidth, _screenWidth * 0.95);
            return _currentBreakPoint switch
            {
                BreakPoint.Mobile => Math.Min(percentage, 500),
                BreakPoint.Tablet => Math.Min(percentage, 800),
                BreakPoint.Desktop => Math.Min(percentage, 1200),
                BreakPoint.HighRes => Math.Min(percentage, 1600),
                _ => baseWidth
            };
        }

        /// <summary>
        /// Get responsive height
        /// </summary>
        public static double GetResponsiveHeight(double baseHeight)
        {
            return Math.Min(baseHeight, _screenHeight * 0.9);
        }

        /// <summary>
        /// Update screen dimensions (call when window is resized)
        /// </summary>
        public static void UpdateScreenDimensions(double width, double height)
        {
            _screenWidth = width;
            _screenHeight = height;

            var newBreakPoint = GetBreakPoint(width);
            if (newBreakPoint != _currentBreakPoint)
            {
                _currentBreakPoint = newBreakPoint;
                BreakPointChanged?.Invoke(newBreakPoint);
            }
        }

        /// <summary>
        /// Get grid column count for responsive layout
        /// </summary>
        public static int GetResponsiveColumnCount()
        {
            return _currentBreakPoint switch
            {
                BreakPoint.Mobile => 1,
                BreakPoint.Tablet => 2,
                BreakPoint.Desktop => 3,
                BreakPoint.HighRes => 4,
                _ => 1
            };
        }

        /// <summary>
        /// Get stack orientation (Vertical for mobile, Horizontal for larger)
        /// </summary>
        public static System.Windows.Controls.Orientation GetResponsiveOrientation()
        {
            return IsTabletOrSmaller() 
                ? System.Windows.Controls.Orientation.Vertical 
                : System.Windows.Controls.Orientation.Horizontal;
        }

        /// <summary>
        /// Get scale factor for current screen
        /// </summary>
        public static double GetScaleFactor()
        {
            var dpi = SystemParameters.DpiY;
            return dpi / 96.0; // 96 DPI is standard
        }
    }
}
