using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
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

        private static readonly string[] _supportedLangs = { "en", "tr", "it", "de", "fr", "ru" };

        public static language languageFile()
        {
            settings settings = new settings();
            string settingsPath = AppDomain.CurrentDomain.BaseDirectory + "\\settings.json";
            if (File.Exists(settingsPath))
            {
                settings = JsonSerializer.Deserialize<settings>(File.ReadAllText(settingsPath));
            }

            // Determine language code
            string langCode;
            if (settings.language == "auto" || string.IsNullOrEmpty(settings.language))
            {
                string systemLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                langCode = Array.Exists(_supportedLangs, l => l == systemLang) ? systemLang : "en";
            }
            else
            {
                langCode = Array.Exists(_supportedLangs, l => l == settings.language) ? settings.language : "en";
            }

            // Use C# object directly - no JSON round-trip
            language result = getDefaults(langCode);

            // Write lang JSON files in background (for settings dropdown)
            Task.Run(() => EnsureLangFilesExist());

            return result;
        }

        public static void EnsureLangFilesExist()
        {
            try
            {
                string langDir = AppDomain.CurrentDomain.BaseDirectory + "\\lang";
                Directory.CreateDirectory(langDir);

                WriteLangIfMissing(langDir, "en", lang_en.lang_english());
                WriteLangIfMissing(langDir, "tr", lang_tr.lang_turkish());
                WriteLangIfMissing(langDir, "it", lang_it.lang_italian());
                WriteLangIfMissing(langDir, "de", lang_de.lang_german());
                WriteLangIfMissing(langDir, "fr", lang_fr.lang_french());
                WriteLangIfMissing(langDir, "ru", lang_ru.lang_russian());
            }
            catch { }
        }

        private static void WriteLangIfMissing(string langDir, string code, language lang)
        {
            string path = Path.Combine(langDir, "lang_" + code + ".json");
            if (!File.Exists(path))
            {
                jsonWriter.WriteJson(path, true, lang);
            }
        }

        public static List<languageNames> GetLanguageNames()
        {
            var list = new List<languageNames>();
            var langs = new (string code, language lang)[]
            {
                ("en", lang_en.lang_english()),
                ("tr", lang_tr.lang_turkish()),
                ("it", lang_it.lang_italian()),
                ("de", lang_de.lang_german()),
                ("fr", lang_fr.lang_french()),
                ("ru", lang_ru.lang_russian()),
            };

            foreach (var entry in langs)
            {
                list.Add(new languageNames
                {
                    langCode = entry.code,
                    LangName = entry.lang?.langNativeName ?? entry.code.ToUpper()
                });
            }

            return list;
        }
    }
}
