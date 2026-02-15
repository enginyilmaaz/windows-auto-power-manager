using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WindowsShutdownHelper.functions
{
    public class Logger
    {
        public static void doLog(string actionType, Settings cachedSettings = null)
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
                    settings.logsEnabled = true;
                }
            }

            if (settings.logsEnabled)
            {
                List<logSystem> logLists = new List<logSystem>();

                if (File.Exists(AppContext.BaseDirectory + "\\logs.json"))
                {
                    logLists = JsonSerializer.Deserialize<List<logSystem>>(
                        File.ReadAllText(AppContext.BaseDirectory + "\\logs.json"));
                }

                logSystem newLog = new logSystem
                {
                    actionType = actionType,
                    actionExecutedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                };

                logLists.Add(newLog);

                jsonWriter.WriteJson(AppContext.BaseDirectory + "\\logs.json", true, logLists);
            }
        }
    }
}