using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WindowsAutoPowerManager.Functions
{
    internal sealed class AppDataTransferPayload
    {
        public string AppName { get; set; }
        public string ExportedAt { get; set; }
        public int SchemaVersion { get; set; }
        public Settings Settings { get; set; }
        public List<ActionModel> Actions { get; set; }
        public List<TransferLogEntry> Logs { get; set; }
    }

    /// <summary>
    ///     A history entry inside an exported package, carrying both the current field names and the
    ///     ones written up to schema 1, so a package exported by an older version still imports.
    /// </summary>
    internal sealed class TransferLogEntry
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string At { get; set; }
        public string Outcome { get; set; }
        public string Source { get; set; }
        public string Rule { get; set; }
        public string Error { get; set; }

        /// <summary>Schema 1 name for <see cref="Type"/>.</summary>
        public string ActionType { get; set; }

        /// <summary>Schema 1 name for <see cref="At"/>, in <c>dd.MM.yyyy HH:mm:ss</c>.</summary>
        public string ActionExecutedDate { get; set; }

        public static TransferLogEntry From(LogSystem entry) => new TransferLogEntry
        {
            Id = entry.Id,
            Type = entry.Type,
            At = entry.At,
            Outcome = entry.Outcome,
            Source = entry.Source,
            Rule = entry.Rule,
            Error = entry.Error
        };

        public LogSystem ToEntry()
        {
            string type = string.IsNullOrWhiteSpace(Type) ? ActionType : Type;
            string at = string.IsNullOrWhiteSpace(At) ? ActionExecutedDate : At;

            return new LogSystem
            {
                Id = string.IsNullOrWhiteSpace(Id) ? LogSystem.NewId() : Id,
                Type = type,
                At = at,
                Outcome = string.IsNullOrWhiteSpace(Outcome)
                    ? (string.Equals(type, Config.ActionTypes.TurnOffMonitorFailed, StringComparison.Ordinal)
                        ? LogOutcomes.Error
                        : LogOutcomes.Ok)
                    : Outcome,
                Source = string.IsNullOrWhiteSpace(Source) ? LogSources.System : Source,
                Rule = Rule,
                Error = Error
            };
        }
    }

    internal static class AppDataTransfer
    {
        // 2 adds outcome, source and rule to each history entry, and moves the timestamp to ISO
        // 8601. A package at version 1 still imports: TransferLogEntry reads both field sets.
        private const int CurrentSchemaVersion = 2;

        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static string BuildDefaultFileName()
        {
            string safeAppName = MakeSafeFileName(Config.Constants.AppName);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss", CultureInfo.CurrentCulture);
            return $"{safeAppName}_{timestamp}.settings.conf";
        }

        public static void ExportToFile(
            string filePath,
            Settings settings,
            IEnumerable<ActionModel> actions,
            IEnumerable<LogSystem> logs)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            var payload = new AppDataTransferPayload
            {
                AppName = Config.Constants.AppName,
                ExportedAt = DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                SchemaVersion = CurrentSchemaVersion,
                Settings = settings ?? Config.SettingsINI.DefaulSettingFile(),
                Actions = CloneActions(actions),
                Logs = ToTransferLogs(logs)
            };

            JsonWriter.WriteJson(filePath, true, payload);
        }

        public static bool TryImportFromFile(
            string filePath,
            out Settings settings,
            out List<ActionModel> actions,
            out List<LogSystem> logs,
            out string errorMessage)
        {
            settings = Config.SettingsINI.DefaulSettingFile();
            actions = new List<ActionModel>();
            logs = new List<LogSystem>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "File path is empty.";
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    errorMessage = "The selected file is empty.";
                    return false;
                }

                var payload = JsonSerializer.Deserialize<AppDataTransferPayload>(json, ReadOptions);
                if (payload == null)
                {
                    errorMessage = "The selected file is not a valid configuration package.";
                    return false;
                }

                settings = payload.Settings ?? Config.SettingsINI.DefaulSettingFile();
                actions = CloneActions(payload.Actions);
                logs = CloneLogs(payload.Logs);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static List<ActionModel> CloneActions(IEnumerable<ActionModel> actions)
        {
            if (actions == null)
            {
                return new List<ActionModel>();
            }

            return actions
                .Where(a => a != null)
                .Select(a => new ActionModel
                {
                    TriggerType = a.TriggerType,
                    ActionType = a.ActionType,
                    Value = a.Value,
                    ValueUnit = a.ValueUnit,
                    CreatedDate = a.CreatedDate
                })
                .ToList();
        }

        private static List<LogSystem> CloneLogs(IEnumerable<TransferLogEntry> logs)
        {
            if (logs == null)
            {
                return new List<LogSystem>();
            }

            return logs
                .Where(entry => entry != null)
                .Select(entry => entry.ToEntry())
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Type))
                .ToList();
        }

        private static List<TransferLogEntry> ToTransferLogs(IEnumerable<LogSystem> logs)
        {
            return (logs ?? Enumerable.Empty<LogSystem>())
                .Where(entry => entry != null)
                .Select(TransferLogEntry.From)
                .ToList();
        }

        private static string MakeSafeFileName(string name)
        {
            string value = string.IsNullOrWhiteSpace(name) ? "app" : name.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}
