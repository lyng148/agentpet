using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;
using AgentPetCore;

namespace AgentPetApp
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x00000020;

        private EventServer _server;
        private SessionStore _sessionStore;
        private NotifyIcon _notifyIcon;
        public bool IsDraggable { get; set; } = true;
        private FlyoutWindow _flyout;

        public MainWindow()
        {
            InitializeComponent();
            
            _sessionStore = new SessionStore();

            // Setup Tray Icon
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "AgentPet";
            
            _flyout = new FlyoutWindow(this);
            
            _notifyIcon.MouseClick += (s, ev) =>
            {
                if (ev.Button == MouseButtons.Left || ev.Button == MouseButtons.Right)
                {
                    if (_flyout.IsVisible)
                    {
                        _flyout.Hide();
                    }
                    else
                    {
                        // Position above tray
                        var workArea = SystemParameters.WorkArea;
                        _flyout.Left = workArea.Right - _flyout.Width - 10;
                        _flyout.Top = workArea.Bottom - _flyout.Height - 10;
                        _flyout.Show();
                        _flyout.Activate();
                    }
                }
            };

            this.MouseLeftButtonDown += (s, e) =>
            {
                if (IsDraggable)
                {
                    this.DragMove();
                }
            };
            
            PetController.Shared.OnMoodChanged = (mood, chat) =>
            {
                _flyout.UpdateMood(mood, chat);
                
                if (AppSettingsStore.Shared.Settings.ShowChatBubble && !string.IsNullOrEmpty(chat))
                {
                    Dispatcher.Invoke(() => MyPetView.ShowChat(chat));
                }
            };

            
            // Wire up PetController to PetView
            PetController.Shared.OnClipChanged = clip => 
            {
                Dispatcher.Invoke(() => MyPetView.PlayClip(clip));
            };

            // Start Event Server
            _server = new EventServer(OnEventReceived);
            _server.Start();

            // Load default pet
            var petsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pets", "windsurf");
            if (Directory.Exists(petsDir))
            {
                PetController.Shared.LoadPack(petsDir);
                if (PetController.Shared.CurrentPack == null)
                {
                    System.Windows.MessageBox.Show("Pack failed to load! (CurrentPack is null)");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Pets directory not found!");
            }
            // Initialize PetSize and PetFps
            var size = AppSettingsStore.Shared.Settings.PetSize;
            if (size > 0)
            {
                SetPetSize((int)size);
            }
            
            var fps = AppSettingsStore.Shared.Settings.PetFps;
            if (fps > 0)
            {
                SetPetFps(fps);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Position above taskbar (bottom right)
            this.Left = SystemParameters.WorkArea.Right - this.Width - 20; // 20px padding from right edge
            this.Top = SystemParameters.WorkArea.Bottom - this.Height;
            
            // Make window click-through
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        public void LoadPet(string path)
        {
            PetController.Shared.LoadPack(path);
        }

        public void SetPetSize(int size)
        {
            MyPetView.Width = size;
            MyPetView.Height = size;
        }

        public void SetPetFps(double fps)
        {
            MyPetView.SetFps(fps);
        }

        private void OnEventReceived(AgentEvent ev)
        {
            Dispatcher.Invoke(() =>
            {
                var now = DateTime.UtcNow;
                _sessionStore.Apply(ev, now);
                _sessionStore.Prune(now);
                
                PetController.Shared.UpdateSessions(_sessionStore.Sessions);
            });
        }

        public void SetDragMode(bool enable)
        {
            IsDraggable = enable;
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            if (IsDraggable)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 0, 0));
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                this.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        public void UpdateVisibility()
        {
            this.Visibility = AppSettingsStore.Shared.Settings.ShowPet ? Visibility.Visible : Visibility.Hidden;
        }

        protected override void OnClosed(EventArgs e)
        {
            _flyout?.Close();
            _notifyIcon.Dispose();
            _server.Stop();
            base.OnClosed(e);
        }
    }
}