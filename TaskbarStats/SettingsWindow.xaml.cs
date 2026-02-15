using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskbarStats.Services;

namespace TaskbarStats
{
    public partial class SettingsWindow : Window
    {
        private AppConfig _currentConfig;

        public SettingsWindow()
        {
            InitializeComponent();
            _currentConfig = ConfigService.Load();
            LoadSettings();
        }

        private void LoadSettings()
        {
            SetButtonColor(BtnBgColor, _currentConfig.BackgroundColor);
            SetButtonColor(BtnTextColor, _currentConfig.TextColor);
            
            SetButtonColor(BtnColorWarn, _currentConfig.ColorWarning);
            SetButtonColor(BtnColorCrit, _currentConfig.ColorCritical);

            TxtCpuWarn.Text = _currentConfig.CpuWarningThreshold.ToString();
            TxtCpuCrit.Text = _currentConfig.CpuCriticalThreshold.ToString();

            SetButtonColor(BtnTrayCpuLabel, _currentConfig.TrayCpuLabelColor);
            SetButtonColor(BtnTrayGpuLabel, _currentConfig.TrayGpuLabelColor);
            SetButtonColor(BtnTrayText, _currentConfig.TrayTextColor);

            // Check registry for auto-start
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("TaskbarStatsWidget");
                        if (value != null)
                        {
                            ChkStartWithWindows.IsChecked = true;
                            _currentConfig.StartWithWindows = true;
                        }
                        else
                        {
                            ChkStartWithWindows.IsChecked = false;
                            _currentConfig.StartWithWindows = false;
                        }
                    }
                }
            }
            catch { } // Ignore registry access errors
        }

        private void SetButtonColor(System.Windows.Controls.Button btn, string hexColor)
        {
            try
            {
                var converter = new BrushConverter();
                var brush = (System.Windows.Media.Brush?)converter.ConvertFromString(hexColor) ?? System.Windows.Media.Brushes.White;
                btn.Background = brush;
                btn.Tag = hexColor; // Store hex in Tag for easy retrieval
            }
            catch
            {
                btn.Background = System.Windows.Media.Brushes.White;
                btn.Tag = "#FFFFFF";
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                var colorDialog = new System.Windows.Forms.ColorDialog();
                
                // Try to set initial color from Tag
                if (btn.Tag is string hex)
                {
                    try
                    {
                        var color = System.Drawing.ColorTranslator.FromHtml(hex);
                        colorDialog.Color = color;
                    }
                    catch { }
                }

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var newColor = colorDialog.Color;
                    string newHex = $"#{newColor.A:X2}{newColor.R:X2}{newColor.G:X2}{newColor.B:X2}";
                    
                    SetButtonColor(btn, newHex);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentConfig.BackgroundColor = (string)BtnBgColor.Tag;
                _currentConfig.TextColor = (string)BtnTextColor.Tag;
                
                _currentConfig.ColorWarning = (string)BtnColorWarn.Tag;
                _currentConfig.ColorCritical = (string)BtnColorCrit.Tag;

                if (int.TryParse(TxtCpuWarn.Text, out int cpuWarn)) _currentConfig.CpuWarningThreshold = cpuWarn;
                if (int.TryParse(TxtCpuCrit.Text, out int cpuCrit)) _currentConfig.CpuCriticalThreshold = cpuCrit;
                
                _currentConfig.GpuWarningThreshold = _currentConfig.CpuWarningThreshold;
                _currentConfig.GpuCriticalThreshold = _currentConfig.CpuCriticalThreshold;

                _currentConfig.TrayCpuLabelColor = (string)BtnTrayCpuLabel.Tag;
                _currentConfig.TrayGpuLabelColor = (string)BtnTrayGpuLabel.Tag;
                _currentConfig.TrayTextColor = (string)BtnTrayText.Tag;

                _currentConfig.StartWithWindows = ChkStartWithWindows.IsChecked ?? false;

                // Update Registry
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            if (_currentConfig.StartWithWindows)
                            {
                                // Point to current executable
                                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                                key.SetValue("TaskbarStatsWidget", $"\"{exePath}\"");
                            }
                            else
                            {
                                key.DeleteValue("TaskbarStatsWidget", false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error updating registry: {ex.Message}", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                ConfigService.Save(_currentConfig);
                
                // Reload main window settings
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWin)
                {
                    mainWin.LoadConfig();
                }

                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
