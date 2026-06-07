using System;
using System.Windows;
using System.Windows.Threading;

namespace AgentPetApp
{
    public partial class FlyoutWindow : Window
    {
        private MainWindow _mainWindow;
        private DispatcherTimer _timer;
        private DateTime _lastUpdate = DateTime.UtcNow;

        public FlyoutWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            // Sync initial state
            ShowPetCheck.IsChecked = AppSettingsStore.Shared.Settings.ShowPet;
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => UpdateTimer();
            _timer.Start();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void ShowPetCheck_Click(object sender, RoutedEventArgs e)
        {
            AppSettingsStore.Shared.Settings.ShowPet = ShowPetCheck.IsChecked == true;
            AppSettingsStore.Shared.Save();
            _mainWindow.UpdateVisibility();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var settings = new SettingsWindow(_mainWindow);
            settings.Show();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        public void UpdateMood(string mood, string chat)
        {
            Dispatcher.Invoke(() => 
            {
                AgentStatusText.Text = chat;
                _lastUpdate = DateTime.UtcNow;
                UpdateTimer();
            });
        }
        
        private void UpdateTimer()
        {
            var span = DateTime.UtcNow - _lastUpdate;
            AgentTimeText.Text = $"{(int)span.TotalMinutes}m {span.Seconds}s";
        }
    }
}
