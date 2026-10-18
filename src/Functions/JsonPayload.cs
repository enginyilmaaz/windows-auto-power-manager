using System.Text.Json;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Tolerant readers for values coming from the web view, where a setting may arrive as a
    ///     real JSON type or as its string form, and may be absent entirely on an older payload.
    /// </summary>
    internal static class JsonPayload
    {
        public static bool ReadBoolean(JsonElement data, string propertyName, bool fallback)
        {
            if (!data.TryGetProperty(propertyName, out JsonElement element))
            {
                return fallback;
            }

            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            {
                return element.GetBoolean();
            }

            return element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out bool parsed)
                ? parsed
                : fallback;
        }

        /// <summary>
        ///     Null when the property is absent, so a caller can tell "not sent" from a sent zero.
        /// </summary>
        public static int? ReadNullableInt(JsonElement data, string propertyName)
        {
            if (!data.TryGetProperty(propertyName, out JsonElement element))
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int number))
            {
                return number;
            }

            return element.ValueKind == JsonValueKind.String &&
                   int.TryParse(element.GetString(), out int parsed)
                ? parsed
                : (int?)null;
        }

        public static string ReadString(JsonElement data, string propertyName)
        {
            return data.TryGetProperty(propertyName, out JsonElement element) &&
                   element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;
        }
    }
}
