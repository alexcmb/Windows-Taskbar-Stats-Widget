using System.Windows;
using TaskbarStats.Services;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarStats;

public partial class App : Application
{
    private HardwareMonitorService? _monitorService;
    private NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private bool _isWidgetVisible = true;
    private int _trayUpdateCounter = 0;
    private bool _isExiting;

    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "TaskbarStatsWidget_Mutex";
        bool createdNew;

        _mutex = new Mutex(true, appName, out createdNew);

        if (!createdNew)
        {
            System.Windows.MessageBox.Show("An instance of the application is already running.", "Taskbar Stats", MessageBoxButton.OK, MessageBoxImage.Information);
            Application.Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        // Initialize Service
        _monitorService = new HardwareMonitorService();
        _monitorService.OnDataUpdated += OnServiceDataUpdated;
        _monitorService.Start();

        // Initialize Main Window
        _mainWindow = new MainWindow(_monitorService);
        _mainWindow.Show();

        // Initialize Tray Icon
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = SystemIcons.Application; 
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Taskbar Stats: Initializing...";
        
        var contextMenu = new ContextMenuStrip();
        
        var settingsItem = new ToolStripMenuItem("Settings", null, (s, args) => OpenSettings());
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add("-");

        var toggleItem = new ToolStripMenuItem("Hide Widget", null, (s, args) => ToggleWidget());
        toggleItem.Name = "Toggle";
        
        contextMenu.Items.Add(toggleItem);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, args) => ExitApplication());
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    private void ToggleWidget()
    {
        if (_mainWindow == null) return;

        _isWidgetVisible = !_isWidgetVisible;
        if(_isWidgetVisible) 
        {
            _mainWindow.Show();
            _notifyIcon!.ContextMenuStrip!.Items["Toggle"]!.Text = "Hide Widget";
        }
        else
        {
            _mainWindow.Hide();
            _notifyIcon!.ContextMenuStrip!.Items["Toggle"]!.Text = "Show Widget";
        }
    }

    private void OnServiceDataUpdated(TaskbarStats.Models.TemperatureData data)
    {
        _trayUpdateCounter++;
        UpdateTrayIcon(data);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    extern static bool DestroyIcon(IntPtr handle);

    private IntPtr _currentIconHandle = IntPtr.Zero;

    private void UpdateTrayIcon(TaskbarStats.Models.TemperatureData data)
    {
        if (_notifyIcon == null) return;

        var config = ConfigService.Load();
        Color cpuLabelColor, gpuLabelColor, textColor;

        try { cpuLabelColor = ColorTranslator.FromHtml(config.TrayCpuLabelColor); }
        catch { cpuLabelColor = Color.Red; }

        try { gpuLabelColor = ColorTranslator.FromHtml(config.TrayGpuLabelColor); }
        catch { gpuLabelColor = Color.Orange; }

        try { textColor = ColorTranslator.FromHtml(config.TrayTextColor); }
        catch { textColor = Color.White; }

        // Update Tooltip
        string cpuStr = data.CpuTemp.HasValue ? $"{data.CpuTemp.Value:F0}°C" : "--";
        string gpuStr = data.GpuTemp.HasValue ? $"{data.GpuTemp.Value:F0}°C" : "--";
        _notifyIcon.Text = $"CPU: {cpuStr}\nGPU: {gpuStr}";

        // Update Icon
        // Cycle every ~2 seconds (assuming 1s update rate, so every 2 updates)
        // State 0: "CPU"
        // State 1: CPU Temp
        // State 2: "GPU"
        // State 3: GPU Temp
        int cycleState = (_trayUpdateCounter / 2) % 4;

        using (var bitmap = new Bitmap(16, 16))
        using (var g = Graphics.FromImage(bitmap))
        {
            using (var font = new Font("Tahoma", 7, System.Drawing.FontStyle.Bold)) 
            using (var brush = new SolidBrush(textColor))
            {
                string text = "";
                
                switch (cycleState)
                {
                    case 0:
                        text = "CPU";
                        brush.Color = cpuLabelColor;
                        break;
                    case 1:
                        text = data.CpuTemp.HasValue ? data.CpuTemp.Value.ToString("F0") : "--";
                        brush.Color = textColor;
                        break;
                    case 2:
                        text = "GPU";
                        brush.Color = gpuLabelColor;
                        break;
                    case 3:
                        text = data.GpuTemp.HasValue ? data.GpuTemp.Value.ToString("F0") : "--";
                        brush.Color = textColor;
                        break;
                }

                // Measure and Draw
                var textSize = g.MeasureString(text, font);
                
                if (textSize.Width > 16)
                {
                     using (var smallFont = new Font("Tahoma", 6, System.Drawing.FontStyle.Bold))
                     {
                         textSize = g.MeasureString(text, smallFont);
                         float x = (16 - textSize.Width) / 2;
                         float y = (16 - textSize.Height) / 2;
                         g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                         g.DrawString(text, smallFont, brush, x, y);
                     }
                }
                else
                {
                    float x = (16 - textSize.Width) / 2;
                    float y = (16 - textSize.Height) / 2;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    g.DrawString(text, font, brush, x, y);
                }
            }

            // --- FIX FOR GDI+ LEAK ---
            IntPtr hIcon = bitmap.GetHicon();
            var newIcon = Icon.FromHandle(hIcon);

            var oldIcon = _notifyIcon.Icon;
            _notifyIcon.Icon = newIcon;

            // Dispose managed resource wrapper if needed (it doesn't hurt)
            if (oldIcon != null && oldIcon != SystemIcons.Application) 
            {
                oldIcon.Dispose();
            }

            // Destroy the old handle explicitly to free GDI resources
            if (_currentIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_currentIconHandle);
            }
            
            _currentIconHandle = hIcon;
        }
    }

    private void ExitApplication()
    {
        if (_isExiting) return;
        _isExiting = true;

        _monitorService?.Dispose();
        _monitorService = null;

        _notifyIcon?.Dispose();
        _notifyIcon = null;
        
        if (_currentIconHandle != IntPtr.Zero)
        {
            DestroyIcon(_currentIconHandle);
            _currentIconHandle = IntPtr.Zero;
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (!_isExiting)
        {
            _monitorService?.Dispose();
            _notifyIcon?.Dispose();
        }

        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;

        base.OnExit(e);
    }
}
