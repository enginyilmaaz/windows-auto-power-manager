using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsAutoPowerManager.Functions
{
    internal static class LogExportFormats
    {
        public const string Csv = "csv";
        public const string Jsonl = "jsonl";
    }

    /// <summary>
    ///     Writes the history out to a file the user keeps. Offered because clearing the history is
    ///     the one irreversible thing this app does, and until now there was no way to keep a copy
    ///     before doing it.
    /// </summary>
    internal static class LogExport
    {
        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string BuildDefaultFileName(string format)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss", CultureInfo.CurrentCulture);
            string extension = string.Equals(format, LogExportFormats.Jsonl, StringComparison.Ordinal)
                ? "jsonl"
                : "csv";

            return "WindowsAutoPowerManager_history_" + stamp + "." + extension;
        }

        public static void Write(
            string filePath,
            string format,
            IReadOnlyList<LogSystem> entries,
            Func<string, string> translateAction,
            Func<string, string> translateSource,
            IReadOnlyDictionary<string, string> headers)
        {
            if (string.Equals(format, LogExportFormats.Jsonl, StringComparison.Ordinal))
            {
                WriteJsonl(filePath, entries);
                return;
            }

            WriteCsv(filePath, entries, translateAction, translateSource, headers);
        }

        /// <summary>The stored form exactly, so an export can be put back or read by a tool.</summary>
        private static void WriteJsonl(string filePath, IReadOnlyList<LogSystem> entries)
        {
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(false));
            foreach (LogSystem entry in entries)
            {
                writer.WriteLine(JsonSerializer.Serialize(entry, WriteOptions));
            }
        }

        private static void WriteCsv(
            string filePath,
            IReadOnlyList<LogSystem> entries,
            Func<string, string> translateAction,
            Func<string, string> translateSource,
            IReadOnlyDictionary<string, string> headers)
        {
            // With a BOM: Excel otherwise reads a UTF-8 file as the system code page and mangles
            // every non-ASCII label, which is most of them in most languages.
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));

            writer.WriteLine(string.Join(",", new[]
            {
                Quote(Header(headers, "date", "Date")),
                Quote(Header(headers, "time", "Time")),
                Quote(Header(headers, "action", "Action")),
                Quote(Header(headers, "source", "Source")),
                Quote(Header(headers, "outcome", "Outcome")),
                Quote(Header(headers, "rule", "Rule")),
                Quote(Header(headers, "error", "Error"))
            }));

            foreach (LogSystem entry in entries)
            {
                entry.TryGetMoment(out DateTimeOffset moment);

                writer.WriteLine(string.Join(",", new[]
                {
                    Quote(moment == default ? entry.At : moment.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    Quote(moment == default ? string.Empty : moment.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
                    Quote(translateAction?.Invoke(entry.Type) ?? entry.Type),
                    Quote(translateSource?.Invoke(entry.Source) ?? entry.Source),
                    Quote(entry.IsError
                        ? Header(headers, "failed", "Failed")
                        : Header(headers, "succeeded", "Succeeded")),
                    Quote(entry.Rule),
                    Quote(entry.Error)
                }));
            }
        }

        private static string Header(IReadOnlyDictionary<string, string> headers, string key, string fallback)
        {
            return headers != null && headers.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
        }

        /// <summary>
        ///     Always quoted and doubled: a translated label can contain a comma, a quote or a line
        ///     break in any language, and one unquoted field shifts every later column.
        /// </summary>
        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }
    }
}
