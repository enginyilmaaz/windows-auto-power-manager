using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>Outcome of a recorded action. Stored as the literal string, so it reads plainly in the file.</summary>
    internal static class LogOutcomes
    {
        public const string Ok = "ok";
        public const string Error = "error";
    }

    /// <summary>
    ///     What set an action off. Stored as a short token rather than translated text: the viewer
    ///     translates it for display, so the file stays readable on a machine of any language.
    /// </summary>
    internal static class LogSources
    {
        public const string Manual = "manual";
        public const string Tray = "tray";
        public const string Countdown = "countdown";
        public const string System = "system";

        /// <summary>An idle rule, carrying its own threshold: <c>idle:70</c>.</summary>
        public static string Idle(uint seconds) =>
            "idle:" + seconds.ToString(CultureInfo.InvariantCulture);

        /// <summary>A time-of-day rule, carrying its scheduled time: <c>time:09:30:00</c>.</summary>
        public static string Time(string timeOfDay) => "time:" + (timeOfDay ?? string.Empty);

        /// <summary>A one-off "from now" rule.</summary>
        public const string Once = "once";
    }

    /// <summary>
    ///     One recorded action.
    ///
    ///     The timestamp is ISO 8601 with the offset, written with <see cref="CultureInfo.InvariantCulture"/>.
    ///     The old format was <c>dd.MM.yyyy HH:mm:ss</c> rendered in the current culture, which produced a
    ///     string the viewer could not parse on a non-Latin calendar and carried no offset, so an entry
    ///     written before a DST change could not be ordered against one written after.
    /// </summary>
    public class LogSystem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>One of <see cref="Config.ActionTypes"/>.</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>ISO 8601 with offset, e.g. <c>2026-10-18T17:29:53+03:00</c>.</summary>
        [JsonPropertyName("at")]
        public string At { get; set; }

        [JsonPropertyName("outcome")]
        public string Outcome { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        /// <summary>Identifies the rule that fired, when one did. Omitted for manual actions.</summary>
        [JsonPropertyName("rule")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Rule { get; set; }

        /// <summary>Why the action failed. Only present when <see cref="Outcome"/> is an error.</summary>
        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Error { get; set; }

        /// <summary>
        ///     Derived from <see cref="Outcome"/>, so it is never written: a computed value stored
        ///     next to the value it is computed from is one more thing that can disagree.
        /// </summary>
        [JsonIgnore]
        public bool IsError => string.Equals(Outcome, LogOutcomes.Error, StringComparison.Ordinal);

        public static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        /// <summary>Formats "now" the way the store writes it.</summary>
        public static string Timestamp(DateTimeOffset moment) =>
            moment.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

        /// <summary>
        ///     Reads <see cref="At"/> back. Also accepts the legacy <c>dd.MM.yyyy HH:mm:ss</c> form so
        ///     migrated entries stay sortable and filterable.
        /// </summary>
        public bool TryGetMoment(out DateTimeOffset moment)
        {
            if (string.IsNullOrWhiteSpace(At))
            {
                moment = default;
                return false;
            }

            if (DateTimeOffset.TryParse(
                    At, CultureInfo.InvariantCulture, DateTimeStyles.None, out moment))
            {
                return true;
            }

            if (DateTime.TryParseExact(
                    At, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime legacy))
            {
                moment = new DateTimeOffset(legacy, TimeZoneInfo.Local.GetUtcOffset(legacy));
                return true;
            }

            moment = default;
            return false;
        }
    }
}
