using System.Windows;
using MonadoBlade.GUI.Themes;

namespace MonadoBlade.GUI.Showcase
{
    /// <summary>
    /// Component Gallery Showcase Window
    /// Demonstrates all Monado Blade components with interactive examples
    /// </summary>
    public partial class ComponentGalleryWindow : Window
    {
        public ComponentGalleryWindow()
        {
            InitializeComponent();
            LoadComponentExamples();
        }

        private void LoadComponentExamples()
        {
            // Set up theme manager
            ThemeManager.Instance.ThemeChanged += (s, e) => 
            {
                Title = $"Monado Blade Component Gallery ({e.ThemeName})";
            };
        }

        // Button Examples
        private void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Primary Button Clicked!", "Component Example");
        }

        private void OnSecondaryButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Secondary Button Clicked!", "Component Example");
        }

        private void OnDangerButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Danger Button Clicked!", "Component Example");
        }

        // Theme Toggle
        private void OnToggleTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ToggleTheme();
        }

        // Show Animations
        private void OnShowAnimations(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Animation Gallery\n\nAvailable animations:\n" +
                "• FadeIn / FadeOut\n" +
                "• ScaleUp / ScaleDown\n" +
                "• SlideIn from Top\n" +
                "• Shimmer (Loading)\n" +
                "• Pulse (Alerts)\n" +
                "• Rotation (Spinners)\n" +
                "• Color Transitions",
                "Animations");
        }

        // Show Responsive Layout
        private void OnTestResponsive(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Responsive Breakpoints\n\n" +
                "Mobile: < 768px\n" +
                "Tablet: 768px - 1024px\n" +
                "Desktop: > 1024px\n\n" +
                "Resize window to test responsive behavior.",
                "Responsive Design");
        }
    }
}
