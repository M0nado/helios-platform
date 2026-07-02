// MONADO BLADE v3.4.0 - ANIMATION ENGINE
// File: src/MonadoBlade.GUI/Animations/AnimationEngine.cs

using System;
using System.Windows.Media.Animation;

namespace MonadoBlade.GUI.Animations
{
    /// <summary>
    /// High-performance animation engine with GPU acceleration
    /// </summary>
    public static class AnimationEngine
    {
        public static class EasingFunctions
        {
            public static IEasingFunction Linear => new LinearEasingFunction();
            public static IEasingFunction EaseInCubic => new CubicEase { EasingMode = EasingMode.EaseIn };
            public static IEasingFunction EaseOutCubic => new CubicEase { EasingMode = EasingMode.EaseOut };
            public static IEasingFunction EaseInOutCubic => new CubicEase { EasingMode = EasingMode.EaseInOut };
            public static IEasingFunction Bounce => new BounceEase { EasingMode = EasingMode.EaseOut };
            public static IEasingFunction Back => new BackEase { EasingMode = EasingMode.EaseOut };
            public static IEasingFunction Circular => new CircleEase { EasingMode = EasingMode.EaseInOut };
        }

        public static class Animations
        {
            public class FadeIn
            {
                public static TimeSpan Duration = TimeSpan.FromMilliseconds(300);
                public static IEasingFunction Easing = EasingFunctions.EaseOutCubic;
                public static double FromOpacity = 0.0;
                public static double ToOpacity = 1.0;
            }

            public class FadeOut
            {
                public static TimeSpan Duration = TimeSpan.FromMilliseconds(300);
                public static IEasingFunction Easing = EasingFunctions.EaseInCubic;
                public static double FromOpacity = 1.0;
                public static double ToOpacity = 0.0;
            }

            public class SlideInLeft
            {
                public static TimeSpan Duration = TimeSpan.FromMilliseconds(400);
                public static IEasingFunction Easing = EasingFunctions.EaseOutCubic;
                public static double FromTranslateX = -100;
                public static double ToTranslateX = 0;
            }

            public class SlideInRight
            {
                public static TimeSpan Duration = TimeSpan.FromMilliseconds(400);
                public static IEasingFunction Easing = EasingFunctions.EaseOutCubic;
                public static double FromTranslateX = 100;
                public static double ToTranslateX = 0;
            }

            public class ScaleUp
            {
                public static TimeSpan Duration = TimeSpan.FromMilliseconds(300);
                public static IEasingFunction Easing = EasingFunctions.EaseOutCubic;
                public static double FromScale = 0.8;
                public static double ToScale = 1.0;
            }

            public class Pulse
            {
                public static TimeSpan Duration = TimeSpan.FromMilliseconds(500);
                public static IEasingFunction Easing = EasingFunctions.Bounce;
                public static double MinScale = 0.95;
                public static double MaxScale = 1.05;
            }

            public class Shimmer
            {
                public static TimeSpan Duration = TimeSpan.FromMilliseconds(1500);
                public static IEasingFunction Easing = EasingFunctions.Linear;
                public static double FromOpacity = 0.3;
                public static double ToOpacity = 1.0;
            }
        }

        public class AnimationProfile
        {
            public string Name { get; set; }
            public TimeSpan Duration { get; set; }
            public IEasingFunction EasingFunction { get; set; }

            public static AnimationProfile Quick => new AnimationProfile
            {
                Name = "Quick",
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = EasingFunctions.EaseOutCubic
            };

            public static AnimationProfile Normal => new AnimationProfile
            {
                Name = "Normal",
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = EasingFunctions.EaseOutCubic
            };

            public static AnimationProfile Slow => new AnimationProfile
            {
                Name = "Slow",
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = EasingFunctions.EaseInOutCubic
            };
        }
    }
}
