using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;
using Windows.Graphics;
using Windows.UI;

namespace HELIOS.Platform.SystemIntegration
{
    /// <summary>
    /// Manages dynamic RGB lighting and chromatic effects for system devices.
    /// Integrates with Windows.Devices.Lights API for per-monitor lighting control.
    /// </summary>
    public class DynamicLightingController : IDisposable
    {
        private readonly Dictionary<uint, LampArray> _lampArrays;
        private LampArray? _primaryLampArray;
        private bool _isLightingEnabled;
        private bool _isBatteryMode;
        private bool _disposed;

        public event EventHandler<LightingEventArgs> LightingStateChanged;
        public event EventHandler<Exception> LightingError;

        public DynamicLightingController()
        {
            _lampArrays = new Dictionary<uint, LampArray>();
            _isLightingEnabled = true;
            _isBatteryMode = false;
            InitializeLightingDevices();
        }

        /// <summary>
        /// Initializes and discovers available lighting devices.
        /// </summary>
        private void InitializeLightingDevices()
        {
            try
            {
                InitializeLightingDevicesAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DynamicLighting] Warning: Could not initialize lighting devices: {ex.Message}");
                LightingError?.Invoke(this, ex);
            }
        }

        private async Task InitializeLightingDevicesAsync()
        {
            var selector = LampArray.GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(selector);
            Debug.WriteLine($"[DynamicLighting] Discovered {devices.Count} lamp arrays");

            uint index = 0;
            foreach (var device in devices)
            {
                var lampArray = await LampArray.FromIdAsync(device.Id);
                if (lampArray == null)
                {
                    continue;
                }

                _lampArrays[index++] = lampArray;
                _primaryLampArray ??= lampArray;
            }

            if (_primaryLampArray != null)
            {
                Debug.WriteLine(
                    $"[DynamicLighting] Primary lamp array initialized with {_primaryLampArray.LampCount} lamps");
            }
        }

        /// <summary>
        /// Sets the theme-coordinated RGB color across all available lamps.
        /// </summary>
        public async Task SetThemeColorAsync(Color color, float intensity = 1.0f)
        {
            try
            {
                if (!_isLightingEnabled || _primaryLampArray == null)
                    return;

                var adjustedColor = AdjustColorForBatteryMode(color, intensity);
                
                if (_primaryLampArray.LampCount > 0)
                {
                    var indices = Enumerable.Range(0, (int)_primaryLampArray.LampCount).ToArray();
                    _primaryLampArray.SetColor(adjustedColor);
                    
                    Debug.WriteLine($"[DynamicLighting] Set theme color to RGB({adjustedColor.R}, {adjustedColor.G}, {adjustedColor.B})");
                    
                    LightingStateChanged?.Invoke(this, new LightingEventArgs { Color = adjustedColor, Intensity = intensity });
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DynamicLighting] Error setting theme color: {ex.Message}");
                LightingError?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Applies notification lighting effects (pulse, flash, gradient).
        /// </summary>
        public async Task ApplyNotificationEffectAsync(NotificationLightEffect effect, Color color, int durationMs = 1000)
        {
            try
            {
                if (!_isLightingEnabled || _primaryLampArray == null)
                    return;

                switch (effect)
                {
                    case NotificationLightEffect.Pulse:
                        await ApplyPulseEffect(color, durationMs);
                        break;
                    case NotificationLightEffect.Flash:
                        await ApplyFlashEffect(color, durationMs);
                        break;
                    case NotificationLightEffect.Gradient:
                        await ApplyGradientEffect(color, durationMs);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DynamicLighting] Error applying notification effect: {ex.Message}");
                LightingError?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Applies a smooth pulsing animation.
        /// </summary>
        private async Task ApplyPulseEffect(Color color, int durationMs)
        {
            const int steps = 20;
            var stepDuration = durationMs / steps;

            for (int i = 0; i < steps; i++)
            {
                float intensity = (float)Math.Sin(i * Math.PI / steps);
                var adjustedColor = Color.FromArgb(
                    (byte)(color.A * intensity),
                    color.R,
                    color.G,
                    color.B);

                if (_primaryLampArray != null)
                {
                    _primaryLampArray.SetColor(adjustedColor);
                }

                await Task.Delay(stepDuration);
            }

            // Return to last set theme color
            _primaryLampArray?.SetColor(color);
        }

        /// <summary>
        /// Applies a rapid flash animation for alerts.
        /// </summary>
        private async Task ApplyFlashEffect(Color color, int durationMs)
        {
            const int flashCount = 4;
            const int onDuration = 100;
            const int offDuration = 100;

            for (int i = 0; i < flashCount; i++)
            {
                if (_primaryLampArray != null)
                {
                    _primaryLampArray.SetColor(color);
                }
                await Task.Delay(onDuration);

                if (_primaryLampArray != null)
                {
                    _primaryLampArray.SetColor(Color.FromArgb(255, 0, 0, 0));
                }
                await Task.Delay(offDuration);
            }
        }

        /// <summary>
        /// Applies a color gradient sweep animation.
        /// </summary>
        private async Task ApplyGradientEffect(Color targetColor, int durationMs)
        {
            const int steps = 15;
            var stepDuration = durationMs / steps;
            
            Color[] gradientColors = GenerateGradient(Color.FromArgb(0, 0, 0, 0), targetColor, steps);

            foreach (var gradColor in gradientColors)
            {
                if (_primaryLampArray != null)
                {
                    _primaryLampArray.SetColor(gradColor);
                }
                await Task.Delay(stepDuration);
            }
        }

        /// <summary>
        /// Generates a color gradient between two colors.
        /// </summary>
        private Color[] GenerateGradient(Color fromColor, Color toColor, int steps)
        {
            var colors = new Color[steps];
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                colors[i] = Color.FromArgb(
                    (byte)(fromColor.A + (toColor.A - fromColor.A) * t),
                    (byte)(fromColor.R + (toColor.R - fromColor.R) * t),
                    (byte)(fromColor.G + (toColor.G - fromColor.G) * t),
                    (byte)(fromColor.B + (toColor.B - fromColor.B) * t));
            }
            return colors;
        }

        /// <summary>
        /// Adjusts color intensity based on battery mode.
        /// </summary>
        private Color AdjustColorForBatteryMode(Color color, float intensity)
        {
            if (_isBatteryMode)
            {
                intensity *= 0.5f;
            }

            return Color.FromArgb(
                (byte)(color.A * intensity),
                (byte)(color.R * intensity),
                (byte)(color.G * intensity),
                (byte)(color.B * intensity));
        }

        /// <summary>
        /// Enables or disables lighting effects.
        /// </summary>
        public void SetLightingEnabled(bool enabled)
        {
            _isLightingEnabled = enabled;
            
            if (!enabled && _primaryLampArray != null)
            {
                _primaryLampArray.SetColor(Color.FromArgb(255, 0, 0, 0));
            }

            Debug.WriteLine($"[DynamicLighting] Lighting {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Updates battery mode state to reduce lighting intensity.
        /// </summary>
        public void SetBatteryMode(bool isBatteryMode)
        {
            _isBatteryMode = isBatteryMode;
            Debug.WriteLine($"[DynamicLighting] Battery mode {(isBatteryMode ? "activated" : "deactivated")}");
        }

        /// <summary>
        /// Resets all lighting to default state.
        /// </summary>
        public void ResetLighting()
        {
            try
            {
                if (_primaryLampArray != null)
                {
                    _primaryLampArray.SetColor(Color.FromArgb(255, 0, 0, 0));
                    Debug.WriteLine("[DynamicLighting] Lighting reset to default");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DynamicLighting] Error resetting lighting: {ex.Message}");
                LightingError?.Invoke(this, ex);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                ResetLighting();
                _primaryLampArray = null;
                _disposed = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DynamicLighting] Error during disposal: {ex.Message}");
            }
        }
    }

    public enum NotificationLightEffect
    {
        Pulse,
        Flash,
        Gradient
    }

    public class LightingEventArgs : EventArgs
    {
        public Color Color { get; set; }
        public float Intensity { get; set; }
    }
}
