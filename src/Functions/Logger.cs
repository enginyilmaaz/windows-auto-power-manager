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
                settings = SettingsStorage.LoadOrDefault();
            }

            if (settings.LogsEnabled)
            {
                List<LogSystem> logLists = new List<LogSystem>();

                if (File.Exists(AppContext.BaseDirectory + "\\Logs.json"))
                {
                    try
                    {
                        logLists = JsonSerializer.Deserialize<List<LogSystem>>(
                            File.ReadAllText(AppContext.BaseDirectory + "\\Logs.json"));
                    }
                    catch
                    {
                        logLists = new List<LogSystem>();
                    }
                }

                if (logLists == null)
                {
                    logLists = new List<LogSystem>();
                }

                LogSystem newLog = new LogSystem
                {
                    ActionType = actionType,
                    ActionExecutedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                };

                logLists.Add(newLog);

                JsonWriter.WriteJson(AppContext.BaseDirectory + "\\Logs.json", true, logLists);
            }
        }
    }
}
