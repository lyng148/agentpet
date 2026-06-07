using System;
using System.Windows;
using System.Windows.Controls;

namespace AgentPetApp
{
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            UpdateAgentButtons();
            
            DragModeCheck.IsChecked = AppSettingsStore.Shared.Settings.IsDraggable;
            ShowChatBubbleCheck.IsChecked = AppSettingsStore.Shared.Settings.ShowChatBubble;
            SizeSlider.Value = AppSettingsStore.Shared.Settings.PetSize;
            FpsSlider.Value = AppSettingsStore.Shared.Settings.PetFps;
            
            LoadPets();
            LoadAnimations();
        }
        
        public class PetItem
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
        }
        
        private void LoadPets()
        {
            var petsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pets");
            if (System.IO.Directory.Exists(petsDir))
            {
                // Auto-extract any ZIP files
                foreach (var zipFile in System.IO.Directory.GetFiles(petsDir, "*.zip"))
                {
                    try
                    {
                        string extractPath = System.IO.Path.Combine(petsDir, System.IO.Path.GetFileNameWithoutExtension(zipFile));
                        if (!System.IO.Directory.Exists(extractPath))
                        {
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, extractPath);
                        }
                    } catch { }
                }

                var pets = new System.Collections.Generic.List<PetItem>();
                foreach (var dir in System.IO.Directory.GetDirectories(petsDir))
                {
                    pets.Add(new PetItem { Name = System.IO.Path.GetFileName(dir), Path = dir });
                }
                PetsList.ItemsSource = pets;
                
                // Select current pet
                var currentId = AppSettingsStore.Shared.Settings.CurrentPetId;
                foreach (var item in pets)
                {
                    if (item.Name == currentId)
                    {
                        PetsList.SelectedItem = item;
                        break;
                    }
                }
            }
        }
        
        private void PetsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PetsList.SelectedItem is PetItem item)
            {
                _mainWindow.LoadPet(item.Path);
                AppSettingsStore.Shared.Settings.CurrentPetId = item.Name;
                AppSettingsStore.Shared.Save();
                LoadAnimations();
            }
        }

        private void ImportPetBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ZIP Files (*.zip)|*.zip",
                Title = "Select Pet Package"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var zipFile = openFileDialog.FileName;
                var petsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pets");
                System.IO.Directory.CreateDirectory(petsDir);
                
                try
                {
                    string extractPath = System.IO.Path.Combine(petsDir, System.IO.Path.GetFileNameWithoutExtension(zipFile));
                    if (!System.IO.Directory.Exists(extractPath))
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, extractPath);
                    }
                    LoadPets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing pet: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public class AnimationBindingModel
        {
            public string MoodName { get; set; } = "";
            public AgentPetCore.PetMood Mood { get; set; }
            public System.Collections.Generic.List<string> AvailableClips { get; set; } = new();
            public int SelectedClipIndex { get; set; }
        }

        private void LoadAnimations()
        {
            var pack = PetController.Shared.CurrentPack;
            if (pack == null) return;
            
            var packId = System.IO.Path.GetFileName(pack.Directory);
            var clipCount = pack.Clips.Count;
            
            var list = new System.Collections.Generic.List<AnimationBindingModel>();
            var available = new System.Collections.Generic.List<string>();
            for(int i = 0; i < clipCount; i++) available.Add($"Clip {i + 1}");

            foreach (AgentPetCore.PetMood mood in Enum.GetValues(typeof(AgentPetCore.PetMood)))
            {
                var idx = PetBindingsStore.Shared.ClipIndex(packId, clipCount, mood);
                list.Add(new AnimationBindingModel
                {
                    MoodName = mood.ToString(),
                    Mood = mood,
                    AvailableClips = available,
                    SelectedClipIndex = idx
                });
            }
            AnimationsList.ItemsSource = list;
        }

        private void AnimationPill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is AnimationBindingModel model)
            {
                var menu = new ContextMenu();
                for (int i = 0; i < model.AvailableClips.Count; i++)
                {
                    int clipIndex = i;
                    var item = new MenuItem { Header = model.AvailableClips[i], IsChecked = clipIndex == model.SelectedClipIndex };
                    item.Click += (s, ev) => 
                    {
                        model.SelectedClipIndex = clipIndex;
                        var pack = PetController.Shared.CurrentPack;
                        if (pack != null)
                        {
                            var packId = System.IO.Path.GetFileName(pack.Directory);
                            PetBindingsStore.Shared.SetClip(clipIndex, model.Mood, packId, pack.Clips.Count);
                        }
                    };
                    menu.Items.Add(item);
                }
                btn.ContextMenu = menu;
                menu.PlacementTarget = btn;
                menu.IsOpen = true;
            }
        }
        
        private void UpdateAgentButtons()
        {
            UpdateAgentButtonState(AgentPetCore.AgentKind.Claude, ClaudeInstallBtn, ClaudeRemoveBtn);
            UpdateAgentButtonState(AgentPetCore.AgentKind.Codex, CodexInstallBtn, CodexRemoveBtn);
            UpdateAgentButtonState(AgentPetCore.AgentKind.Cursor, CursorInstallBtn, CursorRemoveBtn);
            UpdateAgentButtonState(AgentPetCore.AgentKind.Windsurf, WindsurfInstallBtn, WindsurfRemoveBtn);
        }

        private void UpdateAgentButtonState(AgentPetCore.AgentKind kind, Button installBtn, Button removeBtn)
        {
            bool isInstalled = AgentPetCore.HookInstaller.IsInstalled(kind);
            if (isInstalled)
            {
                installBtn.Visibility = Visibility.Collapsed;
                removeBtn.Visibility = Visibility.Visible;
            }
            else
            {
                installBtn.Visibility = Visibility.Visible;
                removeBtn.Visibility = Visibility.Collapsed;
            }
        }
        
        private string FindCLIPath()
        {
            try
            {
                // 1. Same directory as the actual EXE (works for Single-File Publish)
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var exeDir = System.IO.Path.GetDirectoryName(exePath);
                    if (exeDir != null)
                    {
                        var sameDir = System.IO.Path.Combine(exeDir, "AgentPetCLI.exe");
                        if (System.IO.File.Exists(sameDir)) return sameDir;
                    }
                }

                // 2. Base directory
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var cliPath = System.IO.Path.Combine(appDir, "AgentPetCLI.exe");
                if (System.IO.File.Exists(cliPath)) return cliPath;

                // 3. Search upwards (for debugging in VS Code / VS)
                var currentDir = new System.IO.DirectoryInfo(appDir);
                while (currentDir != null)
                {
                    var debugPath = System.IO.Path.Combine(currentDir.FullName, "AgentPetCLI", "bin", "Debug", "net8.0", "AgentPetCLI.exe");
                    if (System.IO.File.Exists(debugPath)) return debugPath;
                    
                    var releasePath = System.IO.Path.Combine(currentDir.FullName, "AgentPetCLI", "bin", "Release", "net8.0", "AgentPetCLI.exe");
                    if (System.IO.File.Exists(releasePath)) return releasePath;

                    currentDir = currentDir.Parent;
                }
            } catch { }
            
            return null;
        }

        private void InstallAgentHook(AgentPetCore.AgentKind kind, string displayName)
        {
            var cliPath = FindCLIPath();
            
            if (string.IsNullOrEmpty(cliPath))
            {
                MessageBox.Show("AgentPetCLI.exe not found! Please ensure it is in the same folder as AgentPetApp.exe.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            AgentPetCore.HookInstaller.Install(kind, cliPath);
            UpdateAgentButtons();
            MessageBox.Show($"{displayName} hook installed successfully!\nStart using {displayName} and AgentPet will react.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UninstallAgentHook(AgentPetCore.AgentKind kind)
        {
            AgentPetCore.HookInstaller.Uninstall(kind);
            UpdateAgentButtons();
        }

        private void ClaudeInstallBtn_Click(object sender, RoutedEventArgs e) => InstallAgentHook(AgentPetCore.AgentKind.Claude, "Claude Code");
        private void ClaudeRemoveBtn_Click(object sender, RoutedEventArgs e) => UninstallAgentHook(AgentPetCore.AgentKind.Claude);
        
        private void CodexInstallBtn_Click(object sender, RoutedEventArgs e) => InstallAgentHook(AgentPetCore.AgentKind.Codex, "Codex");
        private void CodexRemoveBtn_Click(object sender, RoutedEventArgs e) => UninstallAgentHook(AgentPetCore.AgentKind.Codex);
        
        private void CursorInstallBtn_Click(object sender, RoutedEventArgs e) => InstallAgentHook(AgentPetCore.AgentKind.Cursor, "Cursor");
        private void CursorRemoveBtn_Click(object sender, RoutedEventArgs e) => UninstallAgentHook(AgentPetCore.AgentKind.Cursor);
        
        private void WindsurfInstallBtn_Click(object sender, RoutedEventArgs e) => InstallAgentHook(AgentPetCore.AgentKind.Windsurf, "Windsurf");
        private void WindsurfRemoveBtn_Click(object sender, RoutedEventArgs e) => UninstallAgentHook(AgentPetCore.AgentKind.Windsurf);
        
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DragModeCheck_Click(object sender, RoutedEventArgs e)
        {
            AppSettingsStore.Shared.Settings.IsDraggable = DragModeCheck.IsChecked == true;
            AppSettingsStore.Shared.Save();
            _mainWindow.SetDragMode(AppSettingsStore.Shared.Settings.IsDraggable);
        }

        private void ShowChatBubbleCheck_Click(object sender, RoutedEventArgs e)
        {
            AppSettingsStore.Shared.Settings.ShowChatBubble = ShowChatBubbleCheck.IsChecked == true;
            AppSettingsStore.Shared.Save();
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mainWindow == null || !IsLoaded) return;
            
            AppSettingsStore.Shared.Settings.PetSize = e.NewValue;
            AppSettingsStore.Shared.Save();
            
            _mainWindow.SetPetSize((int)e.NewValue);
        }

        private void FpsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mainWindow == null || !IsLoaded) return;
            
            AppSettingsStore.Shared.Settings.PetFps = e.NewValue;
            AppSettingsStore.Shared.Save();
            
            _mainWindow.SetPetFps(e.NewValue);
        }
    }
}
