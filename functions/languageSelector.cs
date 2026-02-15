using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using WindowsShutdownHelper.lang;

namespace WindowsShutdownHelper.functions
{
    internal class languageSelector

    {
        private static language getDefaults(string langCode)
        {
            switch (langCode)
            {
                case "tr": return lang_tr.lang_turkish();
                case "en": return lang_en.lang_english();
                case "it": return lang_it.lang_italian();
                case "de": return lang_de.lang_german();
                case "fr": return lang_fr.lang_french();
                case "ru": return lang_ru.lang_russian();
                default: return lang_en.lang_english();
            }
        }

        private static language mergeWithDefaults(language loaded, language defaults)
        {
            foreach (PropertyInfo prop in typeof(language).GetProperties())
            {
                if (prop.GetValue(loaded) == null && prop.GetValue(defaults) != null)
                {
                    prop.SetValue(loaded, prop.GetValue(defaults));
                }
            }
            return loaded;
        }

        public static language languageFile()
        {
            settings settings = new settings();
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
            {
                settings = JsonSerializer.Deserialize<settings>(
                    File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"));
            }

            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\lang");
            if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\lang"))
            {
                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_en.json", true,
                    lang_en.lang_english());
                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_tr.json", true,
                    lang_tr.lang_turkish());
                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_it.json", true,
                    lang_it.lang_italian());
                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_de.json", true,
                    lang_de.lang_german());
                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_fr.json", true,
                    lang_fr.lang_french());
                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_ru.json", true,
                    lang_ru.lang_russian());

                System.Collections.Generic.List<string> existLanguages = Directory
                    .GetFiles(AppDomain.CurrentDomain.BaseDirectory + "lang\\", "lang_??.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToList();

                string currentCultureLangCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;


                foreach (string lang in existLanguages)
                {
                    string langCode = lang.Substring(lang.Length - 2);
                    if (settings.language == "auto")
                    {
                        if (currentCultureLangCode == langCode)
                        {
                            language loaded = JsonSerializer.Deserialize<language>(
                                File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_" + langCode +
                                                 ".json"));
                            return mergeWithDefaults(loaded, getDefaults(langCode));
                        }
                    }
                    else
                    {
                        language loaded = JsonSerializer.Deserialize<language>(
                            File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_" + settings.language +
                                             ".json"));
                        return mergeWithDefaults(loaded, getDefaults(settings.language));
                    }
                }

                foreach (string lang in existLanguages)
                {

                    if (settings.language == "auto")
                    {
                        if (currentCultureLangCode != lang)
                        {
                            language loaded = JsonSerializer.Deserialize<language>(
                                File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "lang\\lang_" + "en" +
                                                 ".json"));
                            return mergeWithDefaults(loaded, getDefaults("en"));
                        }
                    }
                }


                return null;
            }

            return null;
        }



    }
}
