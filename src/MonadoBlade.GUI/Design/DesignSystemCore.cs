// MONADO BLADE v3.4.0 - DESIGN SYSTEM FOUNDATION
// File: src/MonadoBlade.GUI/Design/DesignSystemCore.cs
//
// This file implements the core design system for v3.4.0 with
// complete theming, component styling, and animation integration.

using System;
using System.Windows;
using System.Windows.Media;

namespace MonadoBlade.GUI.Design
{
    /// <summary>
    /// Core design system for Monado Blade v3.4.0
    /// Provides unified theming, styling, and component definitions
    /// </summary>
    public static class DesignSystemCore
    {
        // ============================================================================
        // COLOR SYSTEM
        // ============================================================================

        /// <summary>
        /// Complete color palette for Monado Blade
        /// Light and dark mode variations included
        /// </summary>
        public static class Colors
        {
            // Primary Brand Colors
            public static class Primary
            {
                public static Color Light = Color.FromRgb(219, 234, 254);      // #dbeafe
                public static Color Default = Color.FromRgb(37, 99, 235);      // #2563eb
                public static Color Dark = Color.FromRgb(30, 64, 175);         // #1e40af
                public static Color VeryDark = Color.FromRgb(15, 23, 42);      // #0f172a
            }

            // Semantic Colors
            public static class Semantic
            {
                public static Color Success = Color.FromRgb(16, 185, 129);     // #10b981 (Green)
                public static Color Warning = Color.FromRgb(245, 158, 11);     // #f59e0b (Amber)
                public static Color Error = Color.FromRgb(239, 68, 68);        // #ef4444 (Red)
                public static Color Info = Color.FromRgb(59, 130, 246);        // #3b82f6 (Blue)
            }

            // Light Mode (Default)
            public static class Light
            {
                public static Color Background = Color.FromRgb(255, 255, 255);        // #ffffff
                public static Color BackgroundSecondary = Color.FromRgb(249, 250, 251); // #f9fafb
                public static Color BackgroundTertiary = Color.FromRgb(243, 244, 246);  // #f3f4f6
                public static Color Text = Color.FromRgb(17, 24, 39);                   // #111827
                public static Color TextSecondary = Color.FromRgb(107, 114, 128);       // #6b7280
                public static Color TextTertiary = Color.FromRgb(156, 163, 175);        // #9ca3af
                public static Color Border = Color.FromRgb(229, 231, 235);              // #e5e7eb
                public static Color BorderLight = Color.FromRgb(243, 244, 246);         // #f3f4f6
            }

            // Dark Mode
            public static class Dark
            {
                public static Color Background = Color.FromRgb(15, 23, 42);           // #0f172a
                public static Color BackgroundSecondary = Color.FromRgb(30, 41, 59);  // #1e293b
                public static Color BackgroundTertiary = Color.FromRgb(51, 65, 85);   // #334155
                public static Color Text = Color.FromRgb(241, 245, 249);              // #f1f5f9
                public static Color TextSecondary = Color.FromRgb(203, 213, 225);     // #cbd5e1
                public static Color TextTertiary = Color.FromRgb(148, 163, 184);      // #94a3b8
                public static Color Border = Color.FromRgb(51, 65, 85);               // #334155
                public static Color BorderLight = Color.FromRgb(71, 85, 105);         // #475569
            }
        }

        // ============================================================================
        // TYPOGRAPHY SYSTEM
        // ============================================================================

        /// <summary>
        /// Typography definitions for all text elements
        /// </summary>
        public static class Typography
        {
            public const double DisplayFontSize = 32;
            public const double HeadingFontSize = 24;
            public const double SubheadingFontSize = 20;
            public const double BodyFontSize = 14;
            public const double SmallTextFontSize = 12;
            public const double CaptionFontSize = 11;

            public const string DefaultFontFamily = "Segoe UI";
            public const string CodeFontFamily = "Consolas";
        }

        // ============================================================================
        // SPACING SYSTEM
        // ============================================================================

        /// <summary>
        /// 4px-based spacing scale for consistent layout
        /// </summary>
        public static class Spacing
        {
            public const double XS = 4;   // 4px
            public const double SM = 8;   // 8px
            public const double MD = 12;  // 12px
            public const double LG = 16;  // 16px
            public const double XL = 20;  // 20px
            public const double XXL = 24; // 24px
            public const double XXXL = 32; // 32px
        }

        // ============================================================================
        // COMPONENT SIZING
        // ============================================================================

        /// <summary>
        /// Standard component sizes for consistency
        /// </summary>
        public static class ComponentSizes
        {
            // Button sizes
            public const double ButtonSmallHeight = 24;
            public const double ButtonDefaultHeight = 32;
            public const double ButtonLargeHeight = 40;
            public const double ButtonExtraLargeHeight = 48;

            // Icon sizes
            public const double IconSmall = 16;
            public const double IconDefault = 24;
            public const double IconLarge = 32;
            public const double IconExtraLarge = 48;

            // Input field height
            public const double InputFieldHeight = 32;
            public const double InputFieldLargeHeight = 40;

            // Card border radius
            public const double CardBorderRadius = 8;
            public const double ButtonBorderRadius = 6;
            public const double InputBorderRadius = 4;
        }

        // ============================================================================
        // ANIMATION TIMING
        // ============================================================================

        /// <summary>
        /// Standard animation durations for consistency
        /// </summary>
        public static class AnimationTimings
        {
            public static TimeSpan Quick = TimeSpan.FromMilliseconds(100);
            public static TimeSpan Fast = TimeSpan.FromMilliseconds(150);
            public static TimeSpan Normal = TimeSpan.FromMilliseconds(200);
            public static TimeSpan Slow = TimeSpan.FromMilliseconds(300);
            public static TimeSpan VerySlow = TimeSpan.FromMilliseconds(500);
        }

        // ============================================================================
        // BREAKPOINTS (RESPONSIVE DESIGN)
        // ============================================================================

        /// <summary>
        /// Responsive design breakpoints
        /// </summary>
        public static class Breakpoints
        {
            public const double XSmall = 320;
            public const double Small = 640;
            public const double Medium = 1024;
            public const double Large = 1280;
            public const double XLarge = 1536;
            public const double XXLarge = 1920;
        }

        // ============================================================================
        // THEME CONFIGURATION
        // ============================================================================

        /// <summary>
        /// Complete theme configuration
        /// </summary>
        public class ThemeConfiguration
        {
            public string Name { get; set; }
            public Color PrimaryColor { get; set; }
            public Color BackgroundColor { get; set; }
            public Color TextColor { get; set; }
            public Color BorderColor { get; set; }
            public bool IsDarkMode { get; set; }

            /// <summary>
            /// Predefined Monado theme
            /// </summary>
            public static ThemeConfiguration Monado => new ThemeConfiguration
            {
                Name = "Monado Default",
                PrimaryColor = Colors.Primary.Default,
                BackgroundColor = Colors.Light.Background,
                TextColor = Colors.Light.Text,
                BorderColor = Colors.Light.Border,
                IsDarkMode = false
            };

            /// <summary>
            /// Predefined dark theme
            /// </summary>
            public static ThemeConfiguration MonadoDark => new ThemeConfiguration
            {
                Name = "Monado Dark",
                PrimaryColor = Colors.Primary.Light,
                BackgroundColor = Colors.Dark.Background,
                TextColor = Colors.Dark.Text,
                BorderColor = Colors.Dark.Border,
                IsDarkMode = true
            };
        }

        // ============================================================================
        // DESIGN SYSTEM VALIDATION
        // ============================================================================

        /// <summary>
        /// Validates theme meets accessibility requirements
        /// </summary>
        public static bool ValidateContrast(Color foreground, Color background)
        {
            // Calculate relative luminance
            var fgL = GetRelativeLuminance(foreground);
            var bgL = GetRelativeLuminance(background);

            // Calculate contrast ratio
            var lighter = Math.Max(fgL, bgL);
            var darker = Math.Min(fgL, bgL);
            var contrastRatio = (lighter + 0.05) / (darker + 0.05);

            // WCAG AA requires 4.5:1 for normal text, 3:1 for large text
            return contrastRatio >= 4.5;
        }

        private static double GetRelativeLuminance(Color color)
        {
            var r = Normalize(color.R);
            var g = Normalize(color.G);
            var b = Normalize(color.B);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private static double Normalize(byte channel)
        {
            var c = channel / 255.0;
            return c <= 0.03928
                ? c / 12.92
                : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }
}
