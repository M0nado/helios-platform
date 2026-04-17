using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HELIOS.Platform.Presentation.Pages;

namespace HELIOS.Platform
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(new Grid());

            // Load initial page
            LoadPage("dashboard");
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is string tag)
            {
                LoadPage(tag);
            }
            else if (args.IsSettingsInvoked)
            {
                LoadPage("settings");
            }
        }

        private void LoadPage(string pageTag)
        {
            ContentFrame.Navigate(pageTag switch
            {
                "dashboard" => typeof(DashboardPage),
                "system" => typeof(SystemManagementPage),
                "ai" => typeof(AIHubPage),
                "tools" => typeof(ToolsPage),
                "settings" => typeof(SettingsPage),
                "help" => typeof(HelpPage),
                _ => typeof(DashboardPage)
            });

            TitleText.Text = pageTag switch
            {
                "dashboard" => "Dashboard",
                "system" => "System Management",
                "ai" => "AI Hub",
                "tools" => "Tools & Utilities",
                "settings" => "Settings",
                "help" => "Help & Support",
                _ => "Enterprise Management System"
            };
        }
    }
}
