using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Turns a viewer request into a query and a query result into the message the web view
    ///     receives. Lives here rather than in the forms because the main window and the detached
    ///     history window both serve the same page and had grown their own copies of it.
    /// </summary>
    internal static class LogViewPayload
    {
        public const int DefaultPageSize = 200;

        public static LogQueryRequest ParseRequest(JsonElement data)
        {
            var request = new LogQueryRequest
            {
                Window = LogWindows.All,
                Sort = LogSorts.Newest,
                Limit = DefaultPageSize
            };

            if (data.ValueKind != JsonValueKind.Object)
            {
                return request;
            }

            string window = JsonPayload.ReadString(data, "window");
            if (!string.IsNullOrWhiteSpace(window))
            {
                request.Window = window;
            }

            string sort = JsonPayload.ReadString(data, "sort");
            if (string.Equals(sort, LogSorts.Oldest, StringComparison.Ordinal))
            {
                request.Sort = LogSorts.Oldest;
            }

            request.Query = JsonPayload.ReadString(data, "query");
            request.OnlyErrors = JsonPayload.ReadBoolean(data, "onlyErrors", false);
            request.Offset = ReadInt(data, "offset", 0);
            request.Limit = ReadInt(data, "limit", DefaultPageSize);

            // The viewer names facets; the types they cover are decided here.
            if (data.TryGetProperty("facets", out JsonElement facets) &&
                facets.ValueKind == JsonValueKind.Array)
            {
                var keys = facets.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString())
                    .ToList();

                if (keys.Count > 0)
                {
                    request.Types = Config.LogFacets.ToTypes(keys);
                }
            }

            if (string.Equals(request.Window, LogWindows.Custom, StringComparison.Ordinal))
            {
                request.From = ReadDate(data, "from", endOfDay: false);
                request.To = ReadDate(data, "to", endOfDay: true);
            }

            return request;
        }

        public static object Build(
            LogQueryResult result,
            Func<string, string> translateAction,
            Func<string, string> translateSource,
            int retentionDays)
        {
            var facetCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string facet in Config.LogFacets.Keys)
            {
                int total = 0;
                foreach (string type in Config.LogFacets.TypesOf(facet))
                {
                    if (result.TypeCounts.TryGetValue(type, out int count))
                    {
                        total += count;
                    }
                }

                facetCounts[facet] = total;
            }

            return new
            {
                rows = result.Rows.Select(entry => BuildRow(entry, translateAction, translateSource)).ToList(),
                total = result.Total,
                grandTotal = result.GrandTotal,
                hasMore = result.HasMore,
                errorCount = result.ErrorCount,
                facetCounts,
                windowCounts = result.WindowCounts,
                oldestAt = result.OldestAt,
                retentionDays
            };
        }

        private static object BuildRow(
            LogSystem entry,
            Func<string, string> translateAction,
            Func<string, string> translateSource)
        {
            entry.TryGetMoment(out DateTimeOffset moment);

            return new
            {
                id = entry.Id ?? string.Empty,
                at = entry.At,
                // Split so the viewer groups by day without parsing a timestamp itself.
                day = moment == default ? null : moment.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                time = moment == default ? null : moment.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                type = entry.Type,
                facet = Config.LogFacets.FacetOf(entry.Type),
                label = translateAction?.Invoke(entry.Type) ?? entry.Type,
                outcome = string.IsNullOrWhiteSpace(entry.Outcome) ? LogOutcomes.Ok : entry.Outcome,
                isError = entry.IsError,
                source = entry.Source,
                sourceLabel = translateSource?.Invoke(entry.Source) ?? entry.Source,
                rule = entry.Rule,
                error = entry.Error
            };
        }

        private static int ReadInt(JsonElement data, string propertyName, int fallback)
        {
            if (!data.TryGetProperty(propertyName, out JsonElement element))
            {
                return fallback;
            }

            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int number))
            {
                return number;
            }

            return element.ValueKind == JsonValueKind.String &&
                   int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        /// <summary>
        ///     Reads a <c>yyyy-MM-dd</c> bound from the date pickers. The upper bound covers the
        ///     whole day, so picking the same date twice means "that day" rather than "that midnight".
        /// </summary>
        private static DateTimeOffset? ReadDate(JsonElement data, string propertyName, bool endOfDay)
        {
            string raw = JsonPayload.ReadString(data, propertyName);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (!DateTime.TryParseExact(
                    raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return null;
            }

            DateTime moment = endOfDay ? date.AddDays(1).AddTicks(-1) : date;
            return new DateTimeOffset(moment, TimeZoneInfo.Local.GetUtcOffset(moment));
        }
    }
}
