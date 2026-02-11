using System;
using System.IO;
using System.Text.Json;

namespace TaskbarStats.Services
{
    public class AppConfig
    {
        public double Top { get; set; } = -1;
        public double Left { get; set; } = -1;
        public double Opacity { get; set; } = 1.0;
        
        // Colors
        public string BackgroundColor { get; set; } = "#CC000000"; // Default: Dark transparent
        public string TextColor { get; set; } = "#FFFFFF"; // Default: White
        public string ColorWarning { get; set; } = "#FFA500"; // Default: Orange
        public string ColorCritical { get; set; } = "#FF0000"; // Default: Red

        // Thresholds
        public int CpuWarningThreshold { get; set; } = 70;
        public int CpuCriticalThreshold { get; set; } = 85;
        public int GpuWarningThreshold { get; set; } = 75;
        public int GpuCriticalThreshold { get; set; } = 85;

        // Tray Icon Colors
        public string TrayCpuLabelColor { get; set; } = "#FF0000"; // Red
        public string TrayGpuLabelColor { get; set; } = "#FFA500"; // Orange
        public string TrayTextColor { get; set; } = "#FFFFFF"; // White
    }

    public static class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    
                    // Validate
                    if (config.Opacity < 0.1) config.Opacity = 0.1;
                    if (config.Opacity > 1.0) config.Opacity = 1.0;
                    
                    return config;
                }
            }
            catch (Exception)
            {
                // Ignore load errors, return default
            }
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception)
            {
                // Ignore save errors
            }
        }
    }
}
