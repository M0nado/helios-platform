// MONADO BLADE v3.4.0 - CORE UI COMPONENTS
// File: src/MonadoBlade.GUI/Components/CoreComponents.cs

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MonadoBlade.GUI.Design;
using MonadoBlade.GUI.Animations;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Core reusable UI components with design system integration
    /// </summary>
    public partial class CoreComponents
    {
        // ============================================================================
        // BUTTON COMPONENT
        // ============================================================================

        /// <summary>
        /// Modern button component with animation support
        /// </summary>
        public class MonadoButton : Button
        {
            public enum ButtonVariant { Primary, Secondary, Danger, Success }
            public enum ButtonSize { Small, Medium, Large, ExtraLarge }

            static MonadoButton()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoButton),
                    new FrameworkPropertyMetadata(typeof(MonadoButton)));
            }

            public ButtonVariant Variant
            {
                get { return (ButtonVariant)GetValue(VariantProperty); }
                set { SetValue(VariantProperty, value); }
            }

            public static readonly DependencyProperty VariantProperty =
                DependencyProperty.Register(
                    "Variant",
                    typeof(ButtonVariant),
                    typeof(MonadoButton),
                    new PropertyMetadata(ButtonVariant.Primary, OnVariantChanged));

            public ButtonSize Size
            {
                get { return (ButtonSize)GetValue(SizeProperty); }
                set { SetValue(SizeProperty, value); }
            }

            public static readonly DependencyProperty SizeProperty =
                DependencyProperty.Register(
                    "Size",
                    typeof(ButtonSize),
                    typeof(MonadoButton),
                    new PropertyMetadata(ButtonSize.Medium, OnSizeChanged));

            public bool IsLoading
            {
                get { return (bool)GetValue(IsLoadingProperty); }
                set { SetValue(IsLoadingProperty, value); }
            }

            public static readonly DependencyProperty IsLoadingProperty =
                DependencyProperty.Register(
                    "IsLoading",
                    typeof(bool),
                    typeof(MonadoButton),
                    new PropertyMetadata(false));

            private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var button = d as MonadoButton;
                if (button != null)
                {
                    ApplyVariantStyle(button, (ButtonVariant)e.NewValue);
                }
            }

            private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var button = d as MonadoButton;
                if (button != null)
                {
                    ApplySizeStyle(button, (ButtonSize)e.NewValue);
                }
            }

            private static void ApplyVariantStyle(MonadoButton button, ButtonVariant variant)
            {
                switch (variant)
                {
                    case ButtonVariant.Primary:
                        button.Background = new SolidColorBrush(DesignSystemCore.Colors.Primary.Default);
                        button.Foreground = new SolidColorBrush(Colors.White);
                        break;
                    case ButtonVariant.Secondary:
                        button.Background = new SolidColorBrush(DesignSystemCore.Colors.Light.BackgroundSecondary);
                        button.Foreground = new SolidColorBrush(DesignSystemCore.Colors.Light.Text);
                        break;
                    case ButtonVariant.Danger:
                        button.Background = new SolidColorBrush(DesignSystemCore.Colors.Semantic.Error);
                        button.Foreground = new SolidColorBrush(Colors.White);
                        break;
                    case ButtonVariant.Success:
                        button.Background = new SolidColorBrush(DesignSystemCore.Colors.Semantic.Success);
                        button.Foreground = new SolidColorBrush(Colors.White);
                        break;
                }
            }

            private static void ApplySizeStyle(MonadoButton button, ButtonSize size)
            {
                switch (size)
                {
                    case ButtonSize.Small:
                        button.Height = DesignSystemCore.ComponentSizes.ButtonSmallHeight;
                        button.Padding = new Thickness(DesignSystemCore.Spacing.SM);
                        button.FontSize = DesignSystemCore.Typography.SmallTextFontSize;
                        break;
                    case ButtonSize.Medium:
                        button.Height = DesignSystemCore.ComponentSizes.ButtonDefaultHeight;
                        button.Padding = new Thickness(DesignSystemCore.Spacing.MD);
                        button.FontSize = DesignSystemCore.Typography.BodyFontSize;
                        break;
                    case ButtonSize.Large:
                        button.Height = DesignSystemCore.ComponentSizes.ButtonLargeHeight;
                        button.Padding = new Thickness(DesignSystemCore.Spacing.LG);
                        button.FontSize = DesignSystemCore.Typography.BodyFontSize;
                        break;
                    case ButtonSize.ExtraLarge:
                        button.Height = DesignSystemCore.ComponentSizes.ButtonExtraLargeHeight;
                        button.Padding = new Thickness(DesignSystemCore.Spacing.XL);
                        button.FontSize = DesignSystemCore.Typography.SubheadingFontSize;
                        break;
                }
            }
        }

        // ============================================================================
        // CARD COMPONENT
        // ============================================================================

        /// <summary>
        /// Card container component for content grouping
        /// </summary>
        public class MonadoCard : Control
        {
            static MonadoCard()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoCard),
                    new FrameworkPropertyMetadata(typeof(MonadoCard)));
            }

            public object Header
            {
                get { return GetValue(HeaderProperty); }
                set { SetValue(HeaderProperty, value); }
            }

            public static readonly DependencyProperty HeaderProperty =
                DependencyProperty.Register("Header", typeof(object), typeof(MonadoCard));

            public object Footer
            {
                get { return GetValue(FooterProperty); }
                set { SetValue(FooterProperty, value); }
            }

            public static readonly DependencyProperty FooterProperty =
                DependencyProperty.Register("Footer", typeof(object), typeof(MonadoCard));

            public object Content
            {
                get { return GetValue(ContentProperty); }
                set { SetValue(ContentProperty, value); }
            }

            public static readonly DependencyProperty ContentProperty =
                DependencyProperty.Register("Content", typeof(object), typeof(MonadoCard));

            public bool ShowBorder
            {
                get { return (bool)GetValue(ShowBorderProperty); }
                set { SetValue(ShowBorderProperty, value); }
            }

            public static readonly DependencyProperty ShowBorderProperty =
                DependencyProperty.Register(
                    "ShowBorder",
                    typeof(bool),
                    typeof(MonadoCard),
                    new PropertyMetadata(true));
        }

        // ============================================================================
        // TEXT INPUT COMPONENT
        // ============================================================================

        /// <summary>
        /// Enhanced text input with validation and states
        /// </summary>
        public class MonadoTextInput : TextBox
        {
            public enum InputState { Normal, Focused, Error, Disabled }

            static MonadoTextInput()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoTextInput),
                    new FrameworkPropertyMetadata(typeof(MonadoTextInput)));
            }

            public string Label
            {
                get { return (string)GetValue(LabelProperty); }
                set { SetValue(LabelProperty, value); }
            }

            public static readonly DependencyProperty LabelProperty =
                DependencyProperty.Register("Label", typeof(string), typeof(MonadoTextInput));

            public string Placeholder
            {
                get { return (string)GetValue(PlaceholderProperty); }
                set { SetValue(PlaceholderProperty, value); }
            }

            public static readonly DependencyProperty PlaceholderProperty =
                DependencyProperty.Register("Placeholder", typeof(string), typeof(MonadoTextInput));

            public InputState State
            {
                get { return (InputState)GetValue(StateProperty); }
                set { SetValue(StateProperty, value); }
            }

            public static readonly DependencyProperty StateProperty =
                DependencyProperty.Register("State", typeof(InputState), typeof(MonadoTextInput));

            public string ErrorMessage
            {
                get { return (string)GetValue(ErrorMessageProperty); }
                set { SetValue(ErrorMessageProperty, value); }
            }

            public static readonly DependencyProperty ErrorMessageProperty =
                DependencyProperty.Register("ErrorMessage", typeof(string), typeof(MonadoTextInput));

            public bool IsValid
            {
                get { return (bool)GetValue(IsValidProperty); }
                set { SetValue(IsValidProperty, value); }
            }

            public static readonly DependencyProperty IsValidProperty =
                DependencyProperty.Register("IsValid", typeof(bool), typeof(MonadoTextInput), 
                    new PropertyMetadata(true, OnValidationChanged));

            private static void OnValidationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var input = d as MonadoTextInput;
                if (input != null)
                {
                    input.State = (bool)e.NewValue ? InputState.Normal : InputState.Error;
                }
            }
        }

        // ============================================================================
        // BADGE COMPONENT
        // ============================================================================

        /// <summary>
        /// Badge component for labels and status indicators
        /// </summary>
        public class MonadoBadge : Control
        {
            public enum BadgeVariant { Default, Success, Warning, Error, Info }

            static MonadoBadge()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoBadge),
                    new FrameworkPropertyMetadata(typeof(MonadoBadge)));
            }

            public BadgeVariant Variant
            {
                get { return (BadgeVariant)GetValue(VariantProperty); }
                set { SetValue(VariantProperty, value); }
            }

            public static readonly DependencyProperty VariantProperty =
                DependencyProperty.Register(
                    "Variant",
                    typeof(BadgeVariant),
                    typeof(MonadoBadge),
                    new PropertyMetadata(BadgeVariant.Default, OnVariantChanged));

            public string Text
            {
                get { return (string)GetValue(TextProperty); }
                set { SetValue(TextProperty, value); }
            }

            public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register("Text", typeof(string), typeof(MonadoBadge));

            private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var badge = d as MonadoBadge;
                if (badge != null)
                {
                    ApplyVariantStyle(badge, (BadgeVariant)e.NewValue);
                }
            }

            private static void ApplyVariantStyle(MonadoBadge badge, BadgeVariant variant)
            {
                switch (variant)
                {
                    case BadgeVariant.Success:
                        badge.Background = new SolidColorBrush(DesignSystemCore.Colors.Semantic.Success);
                        break;
                    case BadgeVariant.Warning:
                        badge.Background = new SolidColorBrush(DesignSystemCore.Colors.Semantic.Warning);
                        break;
                    case BadgeVariant.Error:
                        badge.Background = new SolidColorBrush(DesignSystemCore.Colors.Semantic.Error);
                        break;
                    case BadgeVariant.Info:
                        badge.Background = new SolidColorBrush(DesignSystemCore.Colors.Semantic.Info);
                        break;
                    default:
                        badge.Background = new SolidColorBrush(DesignSystemCore.Colors.Primary.Default);
                        break;
                }
                badge.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        // ============================================================================
        // PROGRESS BAR COMPONENT
        // ============================================================================

        /// <summary>
        /// Animated progress bar component
        /// </summary>
        public class MonadoProgressBar : ProgressBar
        {
            static MonadoProgressBar()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(MonadoProgressBar),
                    new FrameworkPropertyMetadata(typeof(MonadoProgressBar)));
            }

            public string Label
            {
                get { return (string)GetValue(LabelProperty); }
                set { SetValue(LabelProperty, value); }
            }

            public static readonly DependencyProperty LabelProperty =
                DependencyProperty.Register("Label", typeof(string), typeof(MonadoProgressBar));

            public bool ShowPercentage
            {
                get { return (bool)GetValue(ShowPercentageProperty); }
                set { SetValue(ShowPercentageProperty, value); }
            }

            public static readonly DependencyProperty ShowPercentageProperty =
                DependencyProperty.Register("ShowPercentage", typeof(bool), typeof(MonadoProgressBar), 
                    new PropertyMetadata(true));
        }
    }
}
