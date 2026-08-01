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

        public static string ReadString(JsonElement data, string propertyName)
        {
            return data.TryGetProperty(propertyName, out JsonElement element) &&
                   element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;
        }
    }
}
