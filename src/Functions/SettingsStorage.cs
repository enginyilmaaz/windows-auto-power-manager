using System;
using System.IO;
using System.Text.Json;

namespace WindowsShutdownHelper.Functions
{
    internal static class SettingsStorage
    {
        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");

        public static Settings LoadOrDefault()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return Config.SettingsINI.DefaulSettingFile();
                }

                string json = File.ReadAllText(SettingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Config.SettingsINI.DefaulSettingFile();
                }

                var parsed = JsonSerializer.Deserialize<Settings>(json, ReadOptions);
                return Normalize(parsed);
            }
            catch
            {
                return Config.SettingsINI.DefaulSettingFile();
            }
        }

        public static void Save(Settings settings)
        {
            JsonWriter.WriteJson(SettingsPath, true, Normalize(settings));
        }

        private static Settings Normalize(Settings settings)
        {
            var defaults = Config.SettingsINI.DefaulSettingFile();
            if (settings == null)
            {
                return defaults;
            }

            if (string.IsNullOrWhiteSpace(settings.Language))
            {
                settings.Language = defaults.Language;
            }

            if (string.IsNullOrWhiteSpace(settings.Theme))
            {
                settings.Theme = defaults.Theme;
            }

            if (settings.CountdownNotifierSeconds < 0)
            {
                settings.CountdownNotifierSeconds = defaults.CountdownNotifierSeconds;
            }

            return settings;
        }
    }
}
