using System.Configuration;

namespace SpireBarcodeScan
{
    public static class AppSettings
    {
        public static string ScanFolder => GetStringConfigValue("ScanFolder", "");
        public static string ProcessedFolder => GetStringConfigValue("ProcessedFolder", "");
        public static string QueryFolder => GetStringConfigValue("QueryFolder", "");
        public static string StorageFolder => GetStringConfigValue("StorageFolder", "");
        public static string ArchiveFolder => GetStringConfigValue("ArchiveFolder", "");
        public static string Environment => GetStringConfigValue("Environment", "local");
        public static int NumberOfFilesToFetch => GetIntConfigValue("NumberOfFilesToFetch", 5);
        public static bool RecheckInProgressFolder => GetBoolConfigValue("RecheckInProgressFolder", false);
        public static bool KeepConsoleOpen => GetBoolConfigValue("KeepConsoleOpen", false);

        private static string GetStringConfigValue(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];

            return value ?? defaultValue;
        }

        private static bool GetBoolConfigValue(string key, bool defaultValue)
        {
            var stringValue = GetStringConfigValue(key, "");

            switch (stringValue.ToLower())
            {
                case "true":
                    return true;
                case "false":
                    return false;
                default:
                    return defaultValue;
            }
        }

        private static int GetIntConfigValue(string key, int defaultValue)
        {
            var stringValue = GetStringConfigValue(key, "");

            var intValue =  int.TryParse(stringValue,out var result);
            return intValue ? result : defaultValue;
        }
    }
}
