using System;
using System.IO;
using System.Text.Json;

namespace WindowsAutoPowerManager.Functions
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

                bool hasConfirmExitOnProgramExit = false;
                try
                {
                    using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                    });

                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        hasConfirmExitOnProgramExit = doc.RootElement.TryGetProperty("confirmExitOnProgramExit", out _);
                    }
                }
                catch
                {
                    // Ignore parse issues here; deserializer path below will handle fallback/default behavior.
                }

                var parsed = JsonSerializer.Deserialize<Settings>(json, ReadOptions);
                return Normalize(parsed, hasConfirmExitOnProgramExit);
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

        private static Settings Normalize(Settings settings, bool hasConfirmExitOnProgramExit = true)
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

            if (!hasConfirmExitOnProgramExit)
            {
                settings.ConfirmExitOnProgramExit = defaults.ConfirmExitOnProgramExit;
            }

            return settings;
        }
    }
}
