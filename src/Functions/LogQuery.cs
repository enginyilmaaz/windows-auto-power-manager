using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsAutoPowerManager.Functions
{
    internal static class LogWindows
    {
        public const string Today = "today";
        public const string Last7 = "7d";
        public const string Last30 = "30d";
        public const string All = "all";
        public const string Custom = "custom";
    }

    internal static class LogSorts
    {
        public const string Newest = "newest";
        public const string Oldest = "oldest";
    }

    internal sealed class LogQueryRequest
    {
        /// <summary>Empty means every type.</summary>
        public HashSet<string> Types { get; set; }

        public string Window { get; set; } = LogWindows.All;

        /// <summary>Only read when <see cref="Window"/> is <see cref="LogWindows.Custom"/>.</summary>
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }

        public string Query { get; set; }
        public string Sort { get; set; } = LogSorts.Newest;
        public int Offset { get; set; }
        public int Limit { get; set; } = 200;

        /// <summary>
        ///     Narrows to attempts that failed. A toggle rather than a type facet: an error is a
        ///     property of any action, so treating it as one more type would have to mean OR across
        ///     two dimensions, and "monitor entries, but only failed ones" would stop being sayable.
        /// </summary>
        public bool OnlyErrors { get; set; }
    }

    internal sealed class LogQueryResult
    {
        /// <summary>The requested page, already sorted.</summary>
        public List<LogSystem> Rows { get; set; } = new List<LogSystem>();

        /// <summary>Entries matching the whole filter, not just this page.</summary>
        public int Total { get; set; }

        /// <summary>Everything kept on disk, whatever the filter.</summary>
        public int GrandTotal { get; set; }

        /// <summary>True when rows beyond this page remain.</summary>
        public bool HasMore { get; set; }

        /// <summary>Per action type, within the time window and search but ignoring the type filter.</summary>
        public Dictionary<string, int> TypeCounts { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Per time window, within the type filter and search but ignoring the window.</summary>
        public Dictionary<string, int> WindowCounts { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Failed attempts within the window and type filter, ignoring the errors toggle.</summary>
        public int ErrorCount { get; set; }

        /// <summary>Oldest entry still stored, for "history covers N days" and the delete prompt.</summary>
        public string OldestAt { get; set; }
    }

    /// <summary>
    ///     Decides which entries a request matches. Free of UI and of file access so the rules are
    ///     testable, and — the point of the rewrite — applied to the <em>whole</em> history rather
    ///     than to a slice the viewer had already fetched. The old viewer asked for the last 250
    ///     entries and then filtered those in the browser, so a date range or a search that pointed
    ///     past them reported "no records" while the records sat in the file.
    ///
    ///     Counts are faceted the usual way: each facet's own dimension is excluded from its count,
    ///     so a type shows how many entries selecting it would add rather than zero while it is off.
    /// </summary>
    internal static class LogQuery
    {
        public const int MaxLimit = 1000;

        public static LogQueryResult Evaluate(
            IReadOnlyList<LogSystem> entries,
            LogQueryRequest request,
            DateTimeOffset now,
            Func<string, string> translate = null)
        {
            entries ??= Array.Empty<LogSystem>();
            request ??= new LogQueryRequest();

            var result = new LogQueryResult { GrandTotal = entries.Count };

            // Search applies to every count below, so it is resolved once up front.
            string needle = (request.Query ?? string.Empty).Trim();
            var searched = new List<Dated>(entries.Count);

            foreach (LogSystem entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (needle.Length > 0 && !Matches(entry, needle, translate))
                {
                    continue;
                }

                entry.TryGetMoment(out DateTimeOffset moment);
                searched.Add(new Dated(entry, moment));
            }

            ResolveWindow(request, now, out DateTimeOffset? from, out DateTimeOffset? to);
            int errorCount = 0;

            bool TypeAllowed(LogSystem candidate) =>
                request.Types == null || request.Types.Count == 0 ||
                request.Types.Contains(candidate.Type ?? string.Empty);

            bool InWindow(Dated row) =>
                (!from.HasValue || row.Moment >= from.Value) &&
                (!to.HasValue || row.Moment <= to.Value);

            // Facet counts.
            foreach (Dated row in searched)
            {
                if (InWindow(row))
                {
                    string type = row.Entry.Type ?? string.Empty;
                    result.TypeCounts.TryGetValue(type, out int typeCount);
                    result.TypeCounts[type] = typeCount + 1;
                }

                // Counted without the errors toggle applied, so the toggle reports what turning it
                // on would leave rather than what it has already filtered away.
                if (InWindow(row) && TypeAllowed(row.Entry) && row.Entry.IsError)
                {
                    ++errorCount;
                }

                if (TypeAllowed(row.Entry))
                {
                    foreach (string window in CountableWindows)
                    {
                        ResolveNamedWindow(window, now, out DateTimeOffset? wFrom, out DateTimeOffset? wTo);
                        if ((!wFrom.HasValue || row.Moment >= wFrom.Value) &&
                            (!wTo.HasValue || row.Moment <= wTo.Value))
                        {
                            result.WindowCounts.TryGetValue(window, out int windowCount);
                            result.WindowCounts[window] = windowCount + 1;
                        }
                    }
                }
            }

            result.ErrorCount = errorCount;

            var matched = searched
                .Where(row => InWindow(row) && TypeAllowed(row.Entry) &&
                              (!request.OnlyErrors || row.Entry.IsError))
                .ToList();
            result.Total = matched.Count;

            matched.Sort((a, b) => string.Equals(request.Sort, LogSorts.Oldest, StringComparison.Ordinal)
                ? a.Moment.CompareTo(b.Moment)
                : b.Moment.CompareTo(a.Moment));

            int offset = Math.Max(0, request.Offset);
            int limit = Math.Clamp(request.Limit <= 0 ? 200 : request.Limit, 1, MaxLimit);

            result.Rows = matched.Skip(offset).Take(limit).Select(row => row.Entry).ToList();
            result.HasMore = offset + result.Rows.Count < matched.Count;

            // Oldest overall, so the figure does not move with the filter.
            DateTimeOffset? oldest = null;
            foreach (LogSystem entry in entries)
            {
                if (entry != null && entry.TryGetMoment(out DateTimeOffset moment) &&
                    (!oldest.HasValue || moment < oldest.Value))
                {
                    oldest = moment;
                }
            }

            result.OldestAt = oldest.HasValue ? LogSystem.Timestamp(oldest.Value) : null;
            return result;
        }

        private static readonly string[] CountableWindows =
        {
            LogWindows.Today, LogWindows.Last7, LogWindows.Last30, LogWindows.All
        };

        private static void ResolveWindow(
            LogQueryRequest request, DateTimeOffset now,
            out DateTimeOffset? from, out DateTimeOffset? to)
        {
            if (string.Equals(request.Window, LogWindows.Custom, StringComparison.Ordinal))
            {
                from = request.From;
                to = request.To;
                return;
            }

            ResolveNamedWindow(request.Window, now, out from, out to);
        }

        private static void ResolveNamedWindow(
            string window, DateTimeOffset now,
            out DateTimeOffset? from, out DateTimeOffset? to)
        {
            to = null;

            // Whole days from local midnight: "last 7 days" means today plus the six before it, not
            // a rolling 168 hours, which is what a person reading a day-grouped list expects.
            switch (window)
            {
                case LogWindows.Today:
                    from = StartOfDay(now);
                    return;
                case LogWindows.Last7:
                    from = StartOfDay(now).AddDays(-6);
                    return;
                case LogWindows.Last30:
                    from = StartOfDay(now).AddDays(-29);
                    return;
                default:
                    from = null;
                    return;
            }
        }

        private static DateTimeOffset StartOfDay(DateTimeOffset moment) =>
            new DateTimeOffset(moment.Year, moment.Month, moment.Day, 0, 0, 0, moment.Offset);

        /// <summary>
        ///     Matches the search against the translated label too, so typing "kilit" finds the
        ///     entries a Turkish viewer sees as locks even though the file stores "LockComputer".
        /// </summary>
        private static bool Matches(LogSystem entry, string needle, Func<string, string> translate)
        {
            if (Contains(entry.Type, needle) ||
                Contains(entry.At, needle) ||
                Contains(entry.Source, needle) ||
                Contains(entry.Rule, needle) ||
                Contains(entry.Error, needle))
            {
                return true;
            }

            return translate != null && Contains(translate(entry.Type), needle);
        }

        private static bool Contains(string haystack, string needle) =>
            !string.IsNullOrEmpty(haystack) &&
            CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                haystack, needle, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

        private readonly struct Dated
        {
            public Dated(LogSystem entry, DateTimeOffset moment)
            {
                Entry = entry;
                Moment = moment;
            }

            public LogSystem Entry { get; }
            public DateTimeOffset Moment { get; }
        }
    }
}
