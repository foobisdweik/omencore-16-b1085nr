namespace OmenCore.Models
{
    public class MonitoringPreferences
    {
        public int PollIntervalMs { get; set; } = 1500;
        public int HistoryCount { get; set; } = 120;
        public bool LowOverheadMode { get; set; }
        
        // Hotkey and notification settings
        public bool HotkeysEnabled { get; set; } = true;
        public bool NotificationsEnabled { get; set; } = true;
        public bool GameNotificationsEnabled { get; set; } = true;
        public bool ModeChangeNotificationsEnabled { get; set; } = true;
        public bool TemperatureWarningsEnabled { get; set; } = true;
        
        // UI preferences
        public bool StartMinimized { get; set; } = false;
        public bool MinimizeToTrayOnClose { get; set; } = true;
    }
}
