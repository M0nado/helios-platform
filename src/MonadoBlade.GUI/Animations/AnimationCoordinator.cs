using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace MonadoBlade.GUI.Animations
{
    /// <summary>
    /// Animation coordinator for UI transitions and effects
    /// Provides common animation patterns with consistent timing
    /// </summary>
    public class AnimationCoordinator
    {
        public static class Durations
        {
            public static TimeSpan Fast = TimeSpan.FromMilliseconds(150);
            public static TimeSpan Normal = TimeSpan.FromMilliseconds(300);
            public static TimeSpan Slow = TimeSpan.FromMilliseconds(500);
        }

        /// <summary>
        /// Fade in animation (0 to 1 opacity)
        /// </summary>
        public static Storyboard CreateFadeInAnimation(UIElement element, TimeSpan? duration = null)
        {
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(duration ?? Durations.Normal),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Fade out animation (1 to 0 opacity)
        /// </summary>
        public static Storyboard CreateFadeOutAnimation(UIElement element, TimeSpan? duration = null)
        {
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(duration ?? Durations.Normal),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Scale animation for hover effects
        /// </summary>
        public static Storyboard CreateScaleAnimation(UIElement element, double fromScale, double toScale, TimeSpan? duration = null)
        {
            var storyboard = new Storyboard();
            
            var scaleX = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = new Duration(duration ?? Durations.Fast),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleY = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = new Duration(duration ?? Durations.Fast),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(scaleX, element);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleX);

            Storyboard.SetTarget(scaleY, element);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            return storyboard;
        }

        /// <summary>
        /// Slide in animation from top
        /// </summary>
        public static Storyboard CreateSlideInFromTopAnimation(UIElement element, double distance = 50, TimeSpan? duration = null)
        {
            var storyboard = new Storyboard();
            
            var animation = new ThicknessAnimation
            {
                From = new Thickness(0, -distance, 0, 0),
                To = new Thickness(0),
                Duration = new Duration(duration ?? Durations.Normal),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.MarginProperty));
            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Shimmer loading animation
        /// </summary>
        public static Storyboard CreateShimmerAnimation(UIElement element)
        {
            var storyboard = new Storyboard();
            
            var animation = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = new Duration(Durations.Slow),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.InOut }
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Pulse animation for alerts
        /// </summary>
        public static Storyboard CreatePulseAnimation(UIElement element, int pulseCount = 3)
        {
            var storyboard = new Storyboard();
            
            for (int i = 0; i < pulseCount; i++)
            {
                var scaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.1,
                    Duration = new Duration(Durations.Fast),
                    BeginTime = TimeSpan.FromMilliseconds(i * 300),
                    AutoReverse = true
                };

                Storyboard.SetTarget(scaleAnimation, element);
                Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                storyboard.Children.Add(scaleAnimation);
            }

            return storyboard;
        }

        /// <summary>
        /// Rotation animation for spinners
        /// </summary>
        public static Storyboard CreateRotationAnimation(UIElement element)
        {
            var storyboard = new Storyboard();
            storyboard.RepeatBehavior = RepeatBehavior.Forever;

            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                EasingFunction = new LinearEase()
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Color change animation
        /// </summary>
        public static Storyboard CreateColorAnimation(
            UIElement element,
            System.Windows.Media.Color fromColor,
            System.Windows.Media.Color toColor,
            TimeSpan? duration = null)
        {
            var storyboard = new Storyboard();
            
            var animation = new ColorAnimation
            {
                From = fromColor,
                To = toColor,
                Duration = new Duration(duration ?? Durations.Normal),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Panel.Background).(SolidColorBrush.Color)"));
            storyboard.Children.Add(animation);

            return storyboard;
        }
    }
}
