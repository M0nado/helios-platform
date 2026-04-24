using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MonadoBlade.GUI.SystemTray
{
    /// <summary>
    /// Manages system tray icon, context menu, and notification badges.
    /// Provides quick access to key Monado Blade features.
    /// </summary>
    public class TrayIconController : IDisposable
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private System.Windows.Forms.ContextMenuStrip _contextMenu;
        private bool _disposed;
        private SystemTrayStatus _currentStatus;

        public event EventHandler<TrayMenuEventArgs> MenuItemClicked;
        public event EventHandler<SystemTrayStatusChangedEventArgs> StatusChanged;

        public TrayIconController()
        {
            _currentStatus = SystemTrayStatus.Ready;
            InitializeTrayIcon();
            InitializeContextMenu();
        }

        private void InitializeTrayIcon()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = GetMonadoThemeIcon(),
                    Visible = true,
                    Text = "Monado Blade - Ready"
                };

                _notifyIcon.MouseClick += NotifyIcon_MouseClick;
                _notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;
                
                Debug.WriteLine("[TrayIcon] System tray icon initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayIcon] Error initializing tray icon: {ex.Message}");
            }
        }

        private void InitializeContextMenu()
        {
            try
            {
                _contextMenu = new System.Windows.Forms.ContextMenuStrip();

                // Add menu items with organized sections
                var showItem = new System.Windows.Forms.ToolStripMenuItem("Show Monado Blade", null, (s, e) => 
                    MenuItemClicked?.Invoke(this, new TrayMenuEventArgs { Action = "Show" }));
                
                var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Settings", null, (s, e) => 
                    MenuItemClicked?.Invoke(this, new TrayMenuEventArgs { Action = "Settings" }));
                
                var performanceItem = new System.Windows.Forms.ToolStripMenuItem("Performance Monitor", null, (s, e) => 
                    MenuItemClicked?.Invoke(this, new TrayMenuEventArgs { Action = "Performance" }));
                
                var themesItem = new System.Windows.Forms.ToolStripMenuItem("Themes", null, (s, e) => 
                    MenuItemClicked?.Invoke(this, new TrayMenuEventArgs { Action = "Themes" }));

                _contextMenu.Items.Add(showItem);
                _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                _contextMenu.Items.Add(settingsItem);
                _contextMenu.Items.Add(performanceItem);
                _contextMenu.Items.Add(themesItem);
                _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

                var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit", null, (s, e) => 
                    MenuItemClicked?.Invoke(this, new TrayMenuEventArgs { Action = "Exit" }));
                
                _contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = _contextMenu;
                Debug.WriteLine("[TrayIcon] Context menu initialized with 5 menu items");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayIcon] Error initializing context menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the tray icon status with color-coded indicator.
        /// </summary>
        public void SetStatus(SystemTrayStatus status, string tooltipText = null)
        {
            _currentStatus = status;
            
            if (_notifyIcon != null)
            {
                _notifyIcon.Icon = GetStatusIcon(status);
                _notifyIcon.Text = tooltipText ?? GetStatusText(status);
                
                Debug.WriteLine($"[TrayIcon] Status changed to {status}");
                StatusChanged?.Invoke(this, new SystemTrayStatusChangedEventArgs { Status = status });
            }
        }

        /// <summary>
        /// Shows a notification badge on the tray icon.
        /// </summary>
        public void ShowNotificationBadge(int count)
        {
            if (count > 0 && _notifyIcon != null)
            {
                string text = count > 99 ? "99+" : count.ToString();
                _notifyIcon.Text = $"Monado Blade - {text} notifications";
                Debug.WriteLine($"[TrayIcon] Showing notification badge: {text}");
            }
        }

        /// <summary>
        /// Displays a balloon notification in the system tray.
        /// </summary>
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                if (_notifyIcon == null)
                    return;

                var tipIcon = type switch
                {
                    NotificationType.Info => System.Windows.Forms.ToolTipIcon.Info,
                    NotificationType.Warning => System.Windows.Forms.ToolTipIcon.Warning,
                    NotificationType.Error => System.Windows.Forms.ToolTipIcon.Error,
                    _ => System.Windows.Forms.ToolTipIcon.Info
                };

                _notifyIcon.ShowBalloonTip(5000, title, message, tipIcon);
                Debug.WriteLine($"[TrayIcon] Notification shown: {title} - {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayIcon] Error showing notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the Monado-themed icon based on status.
        /// </summary>
        private System.Drawing.Icon GetStatusIcon(SystemTrayStatus status)
        {
            return status switch
            {
                SystemTrayStatus.Ready => GetMonadoThemeIcon(),
                SystemTrayStatus.Active => GetActiveStateIcon(),
                SystemTrayStatus.Warning => GetWarningIcon(),
                SystemTrayStatus.Error => GetErrorIcon(),
                _ => GetMonadoThemeIcon()
            };
        }

        private string GetStatusText(SystemTrayStatus status)
        {
            return status switch
            {
                SystemTrayStatus.Ready => "Monado Blade - Ready",
                SystemTrayStatus.Active => "Monado Blade - Active",
                SystemTrayStatus.Warning => "Monado Blade - Warning",
                SystemTrayStatus.Error => "Monado Blade - Error",
                _ => "Monado Blade"
            };
        }

        /// <summary>
        /// Generates the Monado theme icon using gradient colors.
        /// </summary>
        private System.Drawing.Icon GetMonadoThemeIcon()
        {
            return GenerateColoredIcon(new System.Drawing.Color[] {
                System.Drawing.Color.FromArgb(0, 212, 255),     // Cyan
                System.Drawing.Color.FromArgb(127, 79, 255),    // Purple
                System.Drawing.Color.FromArgb(255, 20, 147)     // Pink
            });
        }

        private System.Drawing.Icon GetActiveStateIcon()
        {
            return GenerateColoredIcon(new System.Drawing.Color[] {
                System.Drawing.Color.FromArgb(0, 255, 150),     // Green
                System.Drawing.Color.FromArgb(127, 200, 255)    // Light Blue
            });
        }

        private System.Drawing.Icon GetWarningIcon()
        {
            return GenerateColoredIcon(new System.Drawing.Color[] {
                System.Drawing.Color.FromArgb(255, 193, 7)      // Amber
            });
        }

        private System.Drawing.Icon GetErrorIcon()
        {
            return GenerateColoredIcon(new System.Drawing.Color[] {
                System.Drawing.Color.FromArgb(255, 69, 69)      // Red
            });
        }

        /// <summary>
        /// Generates a 16x16 icon with specified color gradient.
        /// </summary>
        private System.Drawing.Icon GenerateColoredIcon(System.Drawing.Color[] colors)
        {
            const int size = 16;
            var bitmap = new System.Drawing.Bitmap(size, size);
            var graphics = System.Drawing.Graphics.FromImage(bitmap);

            graphics.Clear(System.Drawing.Color.Transparent);

            // Draw gradient circle
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new System.Drawing.Point(0, 0),
                new System.Drawing.Point(size, size),
                colors[0],
                colors[colors.Length - 1]))
            {
                graphics.FillEllipse(brush, 2, 2, size - 4, size - 4);
            }

            // Draw border
            using (var pen = new System.Drawing.Pen(colors[0], 1.5f))
            {
                graphics.DrawEllipse(pen, 2, 2, size - 4, size - 4);
            }

            graphics.Dispose();

            // Convert bitmap to icon
            var hIcon = bitmap.GetHicon();
            return System.Drawing.Icon.FromHandle(hIcon);
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                MenuItemClicked?.Invoke(this, new TrayMenuEventArgs { Action = "Show" });
            }
        }

        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            MenuItemClicked?.Invoke(this, new TrayMenuEventArgs { Action = "NotificationClicked" });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
                _disposed = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayIcon] Error during disposal: {ex.Message}");
            }
        }
    }

    public enum SystemTrayStatus
    {
        Ready,
        Active,
        Warning,
        Error
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }

    public class TrayMenuEventArgs : EventArgs
    {
        public string Action { get; set; }
    }

    public class SystemTrayStatusChangedEventArgs : EventArgs
    {
        public SystemTrayStatus Status { get; set; }
    }
}
