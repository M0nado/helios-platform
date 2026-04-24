using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Manages smooth theme transition animations for dark mode switching.
    /// Provides fade, color transition, and property animations for UI elements.
    /// </summary>
    public class ThemeTransitionAnimator
    {
        public enum TransitionType
        {
            Fade,
            ColorShift,
            SlideIn,
            ScaleDown
        }

        private const int ANIMATION_DURATION_MS = 250;
        private const int ANIMATION_DELAY_MS = 0;

        /// <summary>
        /// Creates a fade transition animation.
        /// </summary>
        public static Storyboard CreateFadeTransition(UIElement element)
        {
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS / 2)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DELAY_MS)
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Creates a smooth color transition animation.
        /// </summary>
        public static Storyboard CreateColorTransition(UIElement element, 
            System.Windows.Media.Color fromColor, 
            System.Windows.Media.Color toColor,
            string propertyPath)
        {
            var storyboard = new Storyboard();
            var animation = new ColorAnimation
            {
                From = fromColor,
                To = toColor,
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DELAY_MS),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(propertyPath));

            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Creates a slide-in transition animation.
        /// </summary>
        public static Storyboard CreateSlideInTransition(UIElement element, double fromX = 0, double fromY = -20)
        {
            var storyboard = new Storyboard();

            // Slide animation
            var slideAnimation = new ThicknessAnimation
            {
                From = new Thickness(fromX, fromY, 0, 0),
                To = new Thickness(0),
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DELAY_MS),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Fade animation
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DELAY_MS),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(slideAnimation, element);
            Storyboard.SetTargetProperty(slideAnimation, new PropertyPath(FrameworkElement.MarginProperty));

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(slideAnimation);
            storyboard.Children.Add(fadeAnimation);

            return storyboard;
        }

        /// <summary>
        /// Creates a scale-down transition animation.
        /// </summary>
        public static Storyboard CreateScaleTransition(UIElement element, double scale = 0.95)
        {
            var storyboard = new Storyboard();

            // Scale animation for RenderTransform
            var scaleAnimation = new DoubleAnimation
            {
                From = scale,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DELAY_MS),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Fade animation
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DELAY_MS),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(scaleAnimation, element);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("RenderTransform.ScaleX"));

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(scaleAnimation);
            storyboard.Children.Add(fadeAnimation);

            return storyboard;
        }

        /// <summary>
        /// Creates a complete theme transition with fade effect.
        /// </summary>
        public static Storyboard CreateCompleteThemeTransition(UIElement element)
        {
            var storyboard = new Storyboard();

            // Fade out
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS / 2)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DELAY_MS)
            };

            // Fade in
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS / 2)),
                BeginTime = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS / 2 + ANIMATION_DELAY_MS)
            };

            Storyboard.SetTarget(fadeOutAnimation, element);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));

            Storyboard.SetTarget(fadeInAnimation, element);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(fadeOutAnimation);
            storyboard.Children.Add(fadeInAnimation);

            return storyboard;
        }

        /// <summary>
        /// Gets the recommended animation duration.
        /// </summary>
        public static int GetAnimationDuration() => ANIMATION_DURATION_MS;

        /// <summary>
        /// Creates an easing function with cubic ease-in-out.
        /// </summary>
        public static IEasingFunction GetThemeTransitionEasing()
        {
            return new CubicEase { EasingMode = EasingMode.EaseInOut };
        }

        /// <summary>
        /// Validates if a theme transition is in progress.
        /// </summary>
        public static bool IsAnimationComplete(Storyboard storyboard)
        {
            return storyboard?.Children.Count == 0 || storyboard?.Children[^1].BeginTime == null;
        }
    }
}
