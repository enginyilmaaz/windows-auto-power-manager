using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using WindowsAutoPowerManager.Lang;

namespace WindowsAutoPowerManager.Functions
{
    internal class LanguageSelector

    {
        private static readonly string[] SupportedLanguageOrder = { "en", "tr", "it", "de", "fr", "ru" };
        private static readonly HashSet<string> SupportedLanguages = new HashSet<string>(SupportedLanguageOrder, StringComparer.Ordinal);
        private static readonly Dictionary<string, Lazy<Language>> LanguageCache = new Dictionary<string, Lazy<Language>>(StringComparer.Ordinal)
        {
            ["en"] = new Lazy<Language>(English.LangEnglish, LazyThreadSafetyMode.ExecutionAndPublication),
            ["tr"] = new Lazy<Language>(Turkish.LangTurkish, LazyThreadSafetyMode.ExecutionAndPublication),
            ["it"] = new Lazy<Language>(Italian.LangItalian, LazyThreadSafetyMode.ExecutionAndPublication),
            ["de"] = new Lazy<Language>(German.LangGerman, LazyThreadSafetyMode.ExecutionAndPublication),
            ["fr"] = new Lazy<Language>(French.LangFrench, LazyThreadSafetyMode.ExecutionAndPublication),
            ["ru"] = new Lazy<Language>(Russian.LangRussian, LazyThreadSafetyMode.ExecutionAndPublication)
        };

        private static readonly object LangFileEnsureSyncRoot = new object();
        private static int _langFileEnsureScheduled;
        private static bool _langFilesEnsured;

        private static Language GetDefaults(string langCode)
        {
            if (!LanguageCache.TryGetValue(langCode, out Lazy<Language> lazyLang))
            {
                lazyLang = LanguageCache["en"];
            }

            return lazyLang.Value;
        }

        public static Language LanguageFile()
        {
            Settings settings = SettingsStorage.LoadOrDefault();

            // Determine language code
            string langCode;
            if (settings.Language == "auto" || string.IsNullOrEmpty(settings.Language))
            {
                string systemLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                langCode = SupportedLanguages.Contains(systemLang) ? systemLang : "en";
            }
            else
            {
                langCode = SupportedLanguages.Contains(settings.Language) ? settings.Language : "en";
            }

            Language result = GetDefaults(langCode);

            if (Interlocked.Exchange(ref _langFileEnsureScheduled, 1) == 0)
            {
                Task.Run(EnsureLangFilesExist);
            }

            return result;
        }

        public static void EnsureLangFilesExist()
        {
            if (_langFilesEnsured)
            {
                return;
            }

            lock (LangFileEnsureSyncRoot)
            {
                if (_langFilesEnsured)
                {
                    return;
                }

                try
                {
                    string langDir = Path.Combine(AppContext.BaseDirectory, "lang");
                    Directory.CreateDirectory(langDir);

                    foreach (string code in SupportedLanguageOrder)
                    {
                        WriteLangIfMissing(langDir, code, GetDefaults(code));
                    }

                    _langFilesEnsured = true;
                }
                catch
                {
                    // Keep startup resilient.
                }
            }
        }

        private static void WriteLangIfMissing(string langDir, string code, Language lang)
        {
            try
            {
                string path = Path.Combine(langDir, "lang_" + code + ".json");
                if (!File.Exists(path))
                {
                    JsonWriter.WriteJson(path, true, lang);
                }
            }
            catch
            {
                // Keep language file generation best-effort only.
            }
        }

        public static List<LanguageNames> GetLanguageNames()
        {
            var list = new List<LanguageNames>(SupportedLanguageOrder.Length);
            foreach (string code in SupportedLanguageOrder)
            {
                Language lang = GetDefaults(code);
                list.Add(new LanguageNames
                {
                    LangCode = code,
                    LangName = lang?.LangNativeName ?? code.ToUpperInvariant()
                });
            }

            return list;
        }
    }
}
