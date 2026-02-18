using System.Collections.Generic;
using System.Reflection;

namespace WindowsAutoPowerManager.Functions
{
    internal static class LanguagePayloadCache
    {
        private static readonly object SyncRoot = new object();
        private static readonly PropertyInfo[] LanguageProperties = typeof(Language).GetProperties();

        private static Language _cachedLanguage;
        private static Dictionary<string, string> _cachedPayload;

        public static IReadOnlyDictionary<string, string> Get(Language language)
        {
            if (language == null)
            {
                return new Dictionary<string, string>();
            }

            lock (SyncRoot)
            {
                if (ReferenceEquals(_cachedLanguage, language) && _cachedPayload != null)
                {
                    return _cachedPayload;
                }

                var payload = new Dictionary<string, string>(LanguageProperties.Length);
                foreach (PropertyInfo prop in LanguageProperties)
                {
                    object val = prop.GetValue(language);
                    if (val != null)
                    {
                        payload[prop.Name] = val.ToString();
                    }
                }

                _cachedLanguage = language;
                _cachedPayload = payload;
                return payload;
            }
        }

        public static void Invalidate()
        {
            lock (SyncRoot)
            {
                _cachedLanguage = null;
                _cachedPayload = null;
            }
        }
    }
}
