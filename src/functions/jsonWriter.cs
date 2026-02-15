using System.IO;
using System.Text.Json;

namespace WindowsShutdownHelper.functions
{
    public class jsonWriter
    {
        public static void WriteJson(string fileNameWithExtension, bool writeIndented, object willWriteListOrClass)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = writeIndented
            };

            string json = JsonSerializer.Serialize(willWriteListOrClass, options);
            StreamWriter sw = new StreamWriter(fileNameWithExtension, false);
            sw.WriteLine(json);
            sw.Close();
        }
    }
}