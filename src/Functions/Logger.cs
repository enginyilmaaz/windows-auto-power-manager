using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WindowsShutdownHelper.Functions
{
    public class Logger
    {
        public static void DoLog(string actionType, Settings cachedSettings = null)
        {
            Settings settings = cachedSettings;

            if (settings == null)
            {
                if (File.Exists(AppContext.BaseDirectory + "\\settings.json"))
                {
                    settings = JsonSerializer.Deserialize<Settings>(
                        File.ReadAllText(AppContext.BaseDirectory + "\\settings.json"));
                }
                else
                {
                    settings = new Settings();
                    settings.LogsEnabled = true;
                }
            }

            if (settings.LogsEnabled)
            {
                List<LogSystem> logLists = new List<LogSystem>();

                if (File.Exists(AppContext.BaseDirectory + "\\logs.json"))
                {
                    logLists = JsonSerializer.Deserialize<List<LogSystem>>(
                        File.ReadAllText(AppContext.BaseDirectory + "\\logs.json"));
                }

                LogSystem newLog = new LogSystem
                {
                    ActionType = actionType,
                    ActionExecutedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                };

                logLists.Add(newLog);

                JsonWriter.WriteJson(AppContext.BaseDirectory + "\\logs.json", true, logLists);
            }
        }
    }
}