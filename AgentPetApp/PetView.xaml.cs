using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AgentPetApp
{
    public partial class PetView : UserControl
    {
        private List<BitmapSource>? _currentClip;
        private int _frameIndex = 0;
        private DispatcherTimer _timer;
        private DispatcherTimer _chatTimer;

        public PetView()
        {
            InitializeComponent();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0 / 12.0)
            };
            _timer.Tick += Timer_Tick;
            
            _chatTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3.5)
            };
            _chatTimer.Tick += (s, e) =>
            {
                _chatTimer.Stop();
                ChatPopup.IsOpen = false;
            };
        }

        public void SetFps(double fps)
        {
            if (fps <= 0) return;
            _timer.Interval = TimeSpan.FromSeconds(1.0 / fps);
        }

        public void PlayClip(List<BitmapSource>? clip)
        {
            if (clip == null || clip.Count == 0)
            {
                _timer.Stop();
                SpriteImage.Source = null;
                _currentClip = null;
                return;
            }

            // If it's the exact same clip reference, don't reset the frame index
            if (_currentClip != clip)
            {
                _currentClip = clip;
                _frameIndex = 0;
                SpriteImage.Source = _currentClip[_frameIndex];
                
                if (!_timer.IsEnabled)
                {
                    _timer.Start();
                }
            }
        }

        public void ShowChat(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            
            ChatText.Text = message;
            ChatPopup.IsOpen = true;
            
            _chatTimer.Stop();
            _chatTimer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_currentClip == null || _currentClip.Count == 0) return;

            _frameIndex = (_frameIndex + 1) % _currentClip.Count;
            SpriteImage.Source = _currentClip[_frameIndex];
        }
    }
}
