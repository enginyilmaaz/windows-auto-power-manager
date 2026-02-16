using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using WindowsShutdownHelper.Lang;

namespace WindowsShutdownHelper.Functions
{
    internal class LanguageSelector

    {
        private static Language getDefaults(string langCode)
        {
            switch (langCode)
            {
                case "tr": return Turkish.LangTurkish();
                case "en": return English.LangEnglish();
                case "it": return Italian.LangItalian();
                case "de": return German.LangGerman();
                case "fr": return French.LangFrench();
                case "ru": return Russian.LangRussian();
                default: return English.LangEnglish();
            }
        }

        private static readonly string[] _supportedLangs = { "en", "tr", "it", "de", "fr", "ru" };

        public static Language LanguageFile()
        {
            Settings settings = new Settings();
            string settingsPath = AppContext.BaseDirectory + "\\Settings.json";
            if (File.Exists(settingsPath))
            {
                settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath));
            }

            // Determine language code
            string langCode;
            if (settings.Language == "auto" || string.IsNullOrEmpty(settings.Language))
            {
                string systemLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                langCode = Array.Exists(_supportedLangs, l => l == systemLang) ? systemLang : "en";
            }
            else
            {
                langCode = Array.Exists(_supportedLangs, l => l == settings.Language) ? settings.Language : "en";
            }

            // Use C# object directly - no JSON round-trip
            Language result = getDefaults(langCode);

            // Write lang JSON files in background (for settings dropdown)
            Task.Run(() => EnsureLangFilesExist());

            return result;
        }

        public static void EnsureLangFilesExist()
        {
            try
            {
                string langDir = AppContext.BaseDirectory + "\\lang";
                Directory.CreateDirectory(langDir);

                WriteLangIfMissing(langDir, "en", English.LangEnglish());
                WriteLangIfMissing(langDir, "tr", Turkish.LangTurkish());
                WriteLangIfMissing(langDir, "it", Italian.LangItalian());
                WriteLangIfMissing(langDir, "de", German.LangGerman());
                WriteLangIfMissing(langDir, "fr", French.LangFrench());
                WriteLangIfMissing(langDir, "ru", Russian.LangRussian());
            }
            catch { }
        }

        private static void WriteLangIfMissing(string langDir, string code, Language lang)
        {
            string path = Path.Combine(langDir, "lang_" + code + ".json");
            if (!File.Exists(path))
            {
                JsonWriter.WriteJson(path, true, lang);
            }
        }

        public static List<LanguageNames> GetLanguageNames()
        {
            var list = new List<LanguageNames>();
            var langs = new (string code, Language lang)[]
            {
                ("en", English.LangEnglish()),
                ("tr", Turkish.LangTurkish()),
                ("it", Italian.LangItalian()),
                ("de", German.LangGerman()),
                ("fr", French.LangFrench()),
                ("ru", Russian.LangRussian()),
            };

            foreach (var entry in langs)
            {
                list.Add(new LanguageNames
                {
                    LangCode = entry.code,
                    LangName = entry.lang?.LangNativeName ?? entry.code.ToUpper()
                });
            }

            return list;
        }
    }
}
