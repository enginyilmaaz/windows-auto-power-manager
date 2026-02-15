using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WindowsShutdownHelper.functions
{
    public class Logger
    {
        public static void doLog(string actionType, settings cachedSettings = null)
        {
            settings settings = cachedSettings;

            if (settings == null)
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
                {
                    settings = JsonSerializer.Deserialize<settings>(
                        File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"));
                }
                else
                {
                    settings = new settings();
                    settings.logsEnabled = true;
                }
            }

            if (settings.logsEnabled)
            {
                List<logSystem> logLists = new List<logSystem>();

                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\logs.json"))
                {
                    logLists = JsonSerializer.Deserialize<List<logSystem>>(
                        File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\logs.json"));
                }

                logSystem newLog = new logSystem
                {
                    actionType = actionType,
                    actionExecutedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                };

                logLists.Add(newLog);

                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "\\logs.json", true, logLists);
            }
        }
    }
}