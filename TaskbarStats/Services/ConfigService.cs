using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TaskbarStats.Services
{
    public class AppConfig
    {
        public double Top { get; set; } = -1;
        public double Left { get; set; } = -1;
        public double Opacity { get; set; } = 1.0;
        public bool StartWithWindows { get; set; } = false;
        
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
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TaskbarStats");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        // Matches #RGB, #RRGGBB, #AARRGGBB hex color strings
        private static readonly Regex HexColorRegex = new Regex(
            @"^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
            RegexOptions.Compiled);

        /// <summary>
        /// Validates that a string is a valid hex color. Returns the fallback if invalid.
        /// </summary>
        public static string ValidateColor(string? color, string fallback)
        {
            if (string.IsNullOrWhiteSpace(color) || !HexColorRegex.IsMatch(color))
                return fallback;
            return color;
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    
                    ValidateConfig(config);
                    return config;
                }
            }
            catch (Exception)
            {
                // Config corrupted or unreadable — return safe defaults
            }
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception)
            {
                // Ignore save errors
            }
        }

        private static void ValidateConfig(AppConfig config)
        {
            var defaults = new AppConfig();

            // Opacity
            if (config.Opacity < 0.1) config.Opacity = 0.1;
            if (config.Opacity > 1.0) config.Opacity = 1.0;

            // Thresholds: clamp to sane range and ensure warning < critical
            config.CpuWarningThreshold = Math.Clamp(config.CpuWarningThreshold, 30, 120);
            config.CpuCriticalThreshold = Math.Clamp(config.CpuCriticalThreshold, 30, 120);
            if (config.CpuWarningThreshold >= config.CpuCriticalThreshold)
            {
                config.CpuWarningThreshold = defaults.CpuWarningThreshold;
                config.CpuCriticalThreshold = defaults.CpuCriticalThreshold;
            }

            config.GpuWarningThreshold = Math.Clamp(config.GpuWarningThreshold, 30, 120);
            config.GpuCriticalThreshold = Math.Clamp(config.GpuCriticalThreshold, 30, 120);
            if (config.GpuWarningThreshold >= config.GpuCriticalThreshold)
            {
                config.GpuWarningThreshold = defaults.GpuWarningThreshold;
                config.GpuCriticalThreshold = defaults.GpuCriticalThreshold;
            }

            // Colors: validate hex format, fallback to defaults
            config.BackgroundColor = ValidateColor(config.BackgroundColor, defaults.BackgroundColor);
            config.TextColor = ValidateColor(config.TextColor, defaults.TextColor);
            config.ColorWarning = ValidateColor(config.ColorWarning, defaults.ColorWarning);
            config.ColorCritical = ValidateColor(config.ColorCritical, defaults.ColorCritical);
            config.TrayCpuLabelColor = ValidateColor(config.TrayCpuLabelColor, defaults.TrayCpuLabelColor);
            config.TrayGpuLabelColor = ValidateColor(config.TrayGpuLabelColor, defaults.TrayGpuLabelColor);
            config.TrayTextColor = ValidateColor(config.TrayTextColor, defaults.TrayTextColor);
        }
    }
}
