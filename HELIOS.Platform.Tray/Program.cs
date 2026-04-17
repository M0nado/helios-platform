using System;
using System.Windows.Forms;

namespace HELIOS.Platform.Tray;

/// <summary>
/// System tray application entry point for HELIOS Platform.
/// </summary>
static class Program
{
    /// <summary>
    /// Main entry point for the tray application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        using (var app = new TrayApplication())
        {
            Application.Run(app);
        }
    }
}

/// <summary>
/// System tray application for HELIOS Platform.
/// </summary>
public partial class TrayApplication : Form
{
    private NotifyIcon _trayIcon;
    private ContextMenuStrip _contextMenu;

    public TrayApplication()
    {
        InitializeComponent();
        InitializeTrayIcon();
    }

    /// <summary>
    /// Initializes the system tray icon and context menu.
    /// </summary>
    private void InitializeTrayIcon()
    {
        _contextMenu = new ContextMenuStrip();
        
        _contextMenu.Items.Add("Dashboard", null, (s, e) => ShowMainWindow());
        _contextMenu.Items.Add("Status Monitor", null, (s, e) => ShowStatusMonitor());
        _contextMenu.Items.Add("-"); // Separator
        _contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
        _contextMenu.Items.Add("About HELIOS", null, (s, e) => ShowAbout());
        _contextMenu.Items.Add("-"); // Separator
        _contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "HELIOS Platform - Ready",
            ContextMenuStrip = _contextMenu,
            Tag = "HELIOS Platform"
        };

        _trayIcon.MouseClick += TrayIcon_MouseClick;
    }

    /// <summary>
    /// Handles tray icon mouse clicks.
    /// </summary>
    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
            else
            {
                WindowState = FormWindowState.Minimized;
            }
        }
    }

    /// <summary>
    /// Shows the main window.
    /// </summary>
    private void ShowMainWindow()
    {
        WindowState = FormWindowState.Normal;
        Activate();
    }

    /// <summary>
    /// Shows the status monitor window.
    /// </summary>
    private void ShowStatusMonitor()
    {
        MessageBox.Show(
            "System Status Monitor\n\n" +
            "Implementation of status monitoring window pending.",
            "Status Monitor",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <summary>
    /// Shows the settings window.
    /// </summary>
    private void ShowSettings()
    {
        MessageBox.Show(
            "HELIOS Settings\n\n" +
            "Implementation of settings window pending.",
            "Settings",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <summary>
    /// Shows the about dialog.
    /// </summary>
    private void ShowAbout()
    {
        MessageBox.Show(
            "HELIOS Platform v1.0.0\n\n" +
            "Enterprise Windows Optimization & Automation\n\n" +
            "© 2024 HELIOS Solutions. All rights reserved.",
            "About HELIOS Platform",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    private void ExitApplication()
    {
        _trayIcon?.Dispose();
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
        }
        base.OnFormClosing(e);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
        WindowState = FormWindowState.Minimized;
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        
        ClientSize = new System.Drawing.Size(400, 300);
        Name = "TrayApplication";
        Text = "HELIOS Platform";
        
        ResumeLayout(false);
    }
}
