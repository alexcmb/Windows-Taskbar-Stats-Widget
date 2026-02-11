using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TaskbarStats.Models;
using TaskbarStats.Services;

namespace TaskbarStats;

public partial class MainWindow : Window
{
    private readonly HardwareMonitorService _service;
    
    // P/Invoke for Always on Top
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public MainWindow(HardwareMonitorService service)
    {
        InitializeComponent();
        _service = service;
        _service.OnDataUpdated += UpdateStats;
        
        LoadConfig();
        
        // Load Config (for position and opacity)
        // Load Config (for position and opacity)
        var config = ConfigService.Load();
        
        this.Loaded += (s, e) => 
        {
             // Validate position
             var desktop = SystemParameters.WorkArea;
             bool isOffScreen = false;

             if (config.Top != -1 && config.Left != -1)
             {
                 // Check if the saved position is within current bounds
                 if (config.Left > desktop.Right - 50 || config.Top > desktop.Bottom - 50 ||
                     config.Left < desktop.Left || config.Top < desktop.Top)
                 {
                     isOffScreen = true;
                 }
                 else
                 {
                     this.Top = config.Top;
                     this.Left = config.Left;
                 }
             }
             else
             {
                 isOffScreen = true; // No config, so treat as 'reset needed'
             }

             if (isOffScreen)
             {
                 this.Left = desktop.Right - this.Width - 10;
                 this.Top = desktop.Bottom - this.Height - 5;
             }
        };
        
        this.Opacity = config.Opacity;

        this.Closing += (s, e) => 
        {
            ConfigService.Save(new AppConfig 
            {
                Top = this.Top,
                Left = this.Left,
                Opacity = this.Opacity
            });
        };
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0) 
            this.Opacity = Math.Min(1.0, this.Opacity + 0.1);
        else 
            this.Opacity = Math.Max(0.1, this.Opacity - 0.1);
    }

    public void LoadConfig()
    {
        var config = ConfigService.Load();
        
        try
        {
            var brushConverter = new BrushConverter();
            Background = (System.Windows.Media.Brush)brushConverter.ConvertFromString(config.BackgroundColor);
            
            var textBrush = (System.Windows.Media.Brush)brushConverter.ConvertFromString(config.TextColor);
            LblCpu.Foreground = textBrush;
            UnitCpu.Foreground = textBrush;
            LblGpu.Foreground = textBrush;
            UnitGpu.Foreground = textBrush;
        }
        catch { }
    }

    private void UpdateStats(TemperatureData data)
    {
        Dispatcher.Invoke(() =>
        {
            var config = ConfigService.Load();
            
            if (data.CpuTemp.HasValue)
            {
                CpuText.Text = $"{data.CpuTemp.Value:F1}";
                CpuText.Foreground = GetColorForTemp(data.CpuTemp.Value, config.CpuWarningThreshold, config.CpuCriticalThreshold, config);
            }
            else
            {
                CpuText.Text = "--";
                CpuText.Foreground = System.Windows.Media.Brushes.Gray;
            }

            if (data.GpuTemp.HasValue)
            {
                GpuText.Text = $"{data.GpuTemp.Value:F1}";
                GpuText.Foreground = GetColorForTemp(data.GpuTemp.Value, config.GpuWarningThreshold, config.GpuCriticalThreshold, config);
            }
            else
            {
                GpuText.Text = "--";
                GpuText.Foreground = System.Windows.Media.Brushes.Gray;
            }

            // Force Topmost every update
            ForceTopmost();
        });
    }

    private void ForceTopmost()
    {
        var handle = new WindowInteropHelper(this).Handle;
        SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    // P/Invoke for Window Styles
    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    private System.Windows.Media.Brush GetColorForTemp(float temp, int warn, int crit, AppConfig config)
    {
        var converter = new BrushConverter();
        if (temp >= crit) return (System.Windows.Media.Brush)converter.ConvertFromString(config.ColorCritical);
        if (temp >= warn) return (System.Windows.Media.Brush)converter.ConvertFromString(config.ColorWarning);
        return (System.Windows.Media.Brush)converter.ConvertFromString(config.TextColor);
    }
}