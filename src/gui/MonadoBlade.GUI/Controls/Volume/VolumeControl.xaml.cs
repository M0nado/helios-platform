using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using NAudio.CoreAudioApi;

namespace MonadoBlade.GUI.Controls.Volume
{
    public partial class VolumeControl : Window
    {
        private MMDeviceEnumerator _deviceEnumerator;
        private MMDevice _defaultDevice;
        private bool _isUpdatingFromSystem;

        public VolumeControl()
        {
            InitializeComponent();
            InitializeAudioDevice();
            LoadSystemVolume();
            UpdateVolumeDisplay();
        }

        private void InitializeAudioDevice()
        {
            try
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render, Role.Multimedia);

                if (_defaultDevice != null)
                {
                    _defaultDevice.AudioEndpointVolume.OnVolumeNotification += 
                        AudioEndpointVolume_OnVolumeNotification;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize audio device: {ex.Message}");
            }
        }

        private void LoadSystemVolume()
        {
            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                _isUpdatingFromSystem = true;
                VolumeSlider.Value = _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
                _isUpdatingFromSystem = false;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingFromSystem && _defaultDevice?.AudioEndpointVolume != null)
            {
                _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = 
                    (float)(VolumeSlider.Value / 100.0);
            }

            UpdateVolumeDisplay();
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            Dispatcher.Invoke(() =>
            {
                _isUpdatingFromSystem = true;
                VolumeSlider.Value = data.MasterVolume * 100;
                _isUpdatingFromSystem = false;
                UpdateVolumeDisplay();
            });
        }

        private void UpdateVolumeDisplay()
        {
            int volumePercent = (int)VolumeSlider.Value;
            VolumeLevelText.Text = $"{volumePercent}%";

            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                bool isMuted = _defaultDevice.AudioEndpointVolume.Mute;
                MuteIndicator.Text = isMuted ? "🔇" : "🔊";
            }

            // Animate slider color based on volume
            AnimateSliderGlow(volumePercent);
        }

        private void AnimateSliderGlow(int volumePercent)
        {
            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation
            {
                From = 0.2,
                To = 0.5 + (volumePercent / 200.0),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(doubleAnimation, this);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(UIElement.Effect).(DropShadowEffect.Opacity)"));
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }

        private void VolumeSlider_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            const double volumeStep = 5;
            switch (e.Key)
            {
                case Key.Up:
                case Key.Right:
                    VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + volumeStep);
                    e.Handled = true;
                    break;
                case Key.Down:
                case Key.Left:
                    VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - volumeStep);
                    e.Handled = true;
                    break;
                case Key.M:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        ToggleMute();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void ToggleMute()
        {
            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                _defaultDevice.AudioEndpointVolume.Mute = !_defaultDevice.AudioEndpointVolume.Mute;
                UpdateVolumeDisplay();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                _defaultDevice.AudioEndpointVolume.OnVolumeNotification -= 
                    AudioEndpointVolume_OnVolumeNotification;
            }

            _deviceEnumerator?.Dispose();
            base.OnClosed(e);
        }
    }
}
