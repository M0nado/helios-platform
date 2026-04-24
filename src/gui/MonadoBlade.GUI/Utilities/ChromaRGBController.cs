using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace MonadoBlade.GUI.Utilities
{
    /// <summary>
    /// Chroma RGB Controller - Integrates with Razer Chroma API for synchronized lighting effects.
    /// Maps UI state to RGB colors and coordinates effects across hardware.
    /// </summary>
    public class ChromaRGBController
    {
        private ChromaEffect _currentEffect = ChromaEffect.Static;
        private Color _currentColor = Color.FromRgb(0, 217, 255); // Cyan default
        private double _intensity = 0.5;
        private bool _isConnected = false;
        private List<ChromaDevice> _devices = new List<ChromaDevice>();

        public bool IsConnected => _isConnected;
        public Color CurrentColor
        {
            get => _currentColor;
            set
            {
                _currentColor = value;
                ApplyEffect();
            }
        }

        public double Intensity
        {
            get => _intensity;
            set
            {
                _intensity = Math.Clamp(value, 0, 1);
                ApplyEffect();
            }
        }

        public ChromaRGBController()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize Chroma API connection.
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Attempt to connect to Razer Chroma SDK
                // This is a simplified version - full implementation would use:
                // - RzChromaSDK.dll (Razer Chroma SDK)
                // - Device enumeration
                // - Effect initialization

                _devices.Clear();

                // Add available device types
                _devices.Add(new ChromaDevice { Type = "Keyboard", IsAvailable = true });
                _devices.Add(new ChromaDevice { Type = "Mouse", IsAvailable = true });
                _devices.Add(new ChromaDevice { Type = "Headset", IsAvailable = true });
                _devices.Add(new ChromaDevice { Type = "Mousepad", IsAvailable = true });

                _isConnected = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chroma SDK initialization failed: {ex.Message}");
                _isConnected = false;
            }
        }

        /// <summary>
        /// Apply static color to all devices.
        /// </summary>
        public void ApplyStaticColor(Color color)
        {
            _currentEffect = ChromaEffect.Static;
            _currentColor = color;
            ApplyEffect();
        }

        /// <summary>
        /// Apply breathing/pulse effect.
        /// </summary>
        public void ApplyBreathingEffect(Color color, double duration = 1.0)
        {
            _currentEffect = ChromaEffect.Breathing;
            _currentColor = color;
            // Speed would be duration-dependent
            ApplyEffect();
        }

        /// <summary>
        /// Apply wave effect.
        /// </summary>
        public void ApplyWaveEffect(Color startColor, Color endColor, double speed = 1.0)
        {
            _currentEffect = ChromaEffect.Wave;
            _currentColor = startColor;
            // EndColor would be stored separately
            ApplyEffect();
        }

        /// <summary>
        /// Apply reactive effect (lights up on keypress).
        /// </summary>
        public void ApplyReactiveEffect(Color color, double speed = 1.0)
        {
            _currentEffect = ChromaEffect.Reactive;
            _currentColor = color;
            ApplyEffect();
        }

        /// <summary>
        /// Apply custom gradient across keyboard.
        /// </summary>
        public void ApplyGradientEffect(params Color[] colors)
        {
            _currentEffect = ChromaEffect.Gradient;
            // Store gradient colors
            ApplyEffect();
        }

        /// <summary>
        /// Apply the current effect to all devices.
        /// </summary>
        private void ApplyEffect()
        {
            if (!_isConnected) return;

            foreach (var device in _devices)
            {
                if (!device.IsAvailable) continue;

                try
                {
                    ApplyEffectToDevice(device, _currentEffect, _currentColor);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to apply effect to {device.Type}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Apply effect to a specific device.
        /// </summary>
        private void ApplyEffectToDevice(ChromaDevice device, ChromaEffect effect, Color color)
        {
            // This would call the Razer Chroma SDK
            // For now, it's a placeholder

            string effectName = effect switch
            {
                ChromaEffect.Static => "Static",
                ChromaEffect.Breathing => "Breathing",
                ChromaEffect.Wave => "Wave",
                ChromaEffect.Reactive => "Reactive",
                ChromaEffect.Gradient => "Gradient",
                ChromaEffect.Spectrum => "Spectrum",
                ChromaEffect.Custom => "Custom",
                _ => "Unknown"
            };

            System.Diagnostics.Debug.WriteLine($"Applying {effectName} to {device.Type}: RGB({color.R}, {color.G}, {color.B})");
        }

        /// <summary>
        /// Sync Chroma effects with UI state.
        /// </summary>
        public void SyncWithUIState(UIState state)
        {
            switch (state)
            {
                case UIState.Boot:
                    ApplyBreathingEffect(Color.FromRgb(0, 217, 255), 0.5); // Fast cyan breathing
                    break;

                case UIState.Idle:
                    ApplyStaticColor(Color.FromRgb(0, 217, 255)); // Cyan
                    break;

                case UIState.Active:
                    ApplyWaveEffect(Color.FromRgb(0, 217, 255), Color.FromRgb(255, 0, 255)); // Cyan to Magenta
                    break;

                case UIState.Alert:
                    ApplyBreathingEffect(Color.FromRgb(255, 0, 0), 1.0); // Red alert
                    break;

                case UIState.Success:
                    ApplyBreathingEffect(Color.FromRgb(0, 255, 0), 0.3); // Fast green
                    break;

                case UIState.Error:
                    ApplyReactiveEffect(Color.FromRgb(255, 0, 0)); // Red on keypress
                    break;

                case UIState.Shutdown:
                    ApplyWaveEffect(Color.FromRgb(255, 0, 0), Colors.Black, 2.0); // Slow red wave
                    break;
            }
        }

        /// <summary>
        /// Create a custom effect sequence.
        /// </summary>
        public void PlayEffectSequence(params ChromaEffectFrame[] frames)
        {
            foreach (var frame in frames)
            {
                _currentEffect = frame.Effect;
                _currentColor = frame.Color;
                _intensity = frame.Intensity;
                ApplyEffect();

                // In a real implementation, this would respect frame timing
            }
        }

        /// <summary>
        /// Reset to default state.
        /// </summary>
        public void Reset()
        {
            ApplyStaticColor(Color.FromRgb(0, 217, 255));
        }

        /// <summary>
        /// Disconnect from Chroma API.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                Reset();
                _isConnected = false;
            }
            catch { }
        }
    }

    /// <summary>
    /// Chroma effect types.
    /// </summary>
    public enum ChromaEffect
    {
        Static,
        Breathing,
        Wave,
        Reactive,
        Gradient,
        Spectrum,
        Custom
    }

    /// <summary>
    /// Chroma device information.
    /// </summary>
    public class ChromaDevice
    {
        public string Type { get; set; }
        public bool IsAvailable { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// A frame in a Chroma effect sequence.
    /// </summary>
    public class ChromaEffectFrame
    {
        public ChromaEffect Effect { get; set; }
        public Color Color { get; set; }
        public double Intensity { get; set; }
        public double Duration { get; set; } // In milliseconds
    }

    /// <summary>
    /// UI states that can trigger Chroma effects.
    /// </summary>
    public enum UIState
    {
        Boot,
        Idle,
        Active,
        Alert,
        Success,
        Error,
        Shutdown
    }

    /// <summary>
    /// Chroma effect library - Pre-configured effect sequences.
    /// </summary>
    public static class ChromaEffectLibrary
    {
        public static ChromaEffectFrame[] BootSequence => new[]
        {
            new ChromaEffectFrame { Effect = ChromaEffect.Static, Color = Color.FromRgb(0, 217, 255), Intensity = 0.5, Duration = 500 },
            new ChromaEffectFrame { Effect = ChromaEffect.Breathing, Color = Color.FromRgb(0, 217, 255), Intensity = 1.0, Duration = 1500 },
            new ChromaEffectFrame { Effect = ChromaEffect.Wave, Color = Color.FromRgb(0, 255, 65), Intensity = 1.0, Duration = 1000 }
        };

        public static ChromaEffectFrame[] LaserFireSequence => new[]
        {
            new ChromaEffectFrame { Effect = ChromaEffect.Static, Color = Color.FromRgb(255, 255, 255), Intensity = 1.0, Duration = 100 },
            new ChromaEffectFrame { Effect = ChromaEffect.Wave, Color = Color.FromRgb(0, 217, 255), Intensity = 0.8, Duration = 400 }
        };

        public static ChromaEffectFrame[] LightningStrikeSequence => new[]
        {
            new ChromaEffectFrame { Effect = ChromaEffect.Static, Color = Color.FromRgb(100, 150, 255), Intensity = 1.0, Duration = 50 },
            new ChromaEffectFrame { Effect = ChromaEffect.Static, Color = Color.FromRgb(255, 255, 255), Intensity = 1.0, Duration = 100 },
            new ChromaEffectFrame { Effect = ChromaEffect.Breathing, Color = Color.FromRgb(100, 150, 255), Intensity = 0.6, Duration = 500 }
        };

        public static ChromaEffectFrame[] BladeActivationSequence => new[]
        {
            new ChromaEffectFrame { Effect = ChromaEffect.Reactive, Color = Color.FromRgb(255, 215, 0), Intensity = 1.0, Duration = 200 },
            new ChromaEffectFrame { Effect = ChromaEffect.Wave, Color = Color.FromRgb(0, 217, 255), Intensity = 1.0, Duration = 800 }
        };

        public static ChromaEffectFrame[] AlertSequence => new[]
        {
            new ChromaEffectFrame { Effect = ChromaEffect.Breathing, Color = Color.FromRgb(255, 0, 0), Intensity = 1.0, Duration = 400 },
            new ChromaEffectFrame { Effect = ChromaEffect.Breathing, Color = Color.FromRgb(255, 0, 0), Intensity = 0.5, Duration = 400 }
        };

        public static ChromaEffectFrame[] SuccessSequence => new[]
        {
            new ChromaEffectFrame { Effect = ChromaEffect.Wave, Color = Color.FromRgb(0, 255, 0), Intensity = 1.0, Duration = 600 },
            new ChromaEffectFrame { Effect = ChromaEffect.Static, Color = Color.FromRgb(0, 255, 0), Intensity = 0.5, Duration = 1000 }
        };
    }
}
