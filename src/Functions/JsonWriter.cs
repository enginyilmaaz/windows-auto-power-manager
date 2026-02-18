using System;
using System.IO;
using System.Text.Json;

namespace WindowsAutoPowerManager.Functions
{
    public class JsonWriter
    {
        private static readonly JsonSerializerOptions IndentedWriteOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static readonly JsonSerializerOptions CompactWriteOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public static void WriteJson(string fileNameWithExtension, bool writeIndented, object willWriteListOrClass)
        {
            string json = JsonSerializer.Serialize(
                willWriteListOrClass,
                writeIndented ? IndentedWriteOptions : CompactWriteOptions);

            File.WriteAllText(fileNameWithExtension, json + Environment.NewLine);
        }
    }
}
