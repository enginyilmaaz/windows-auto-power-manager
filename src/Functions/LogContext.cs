using System;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Carries what set the current action off, so the action methods can record it without
    ///     every one of them taking the rule as a parameter.
    ///
    ///     The dispatcher knows the rule; the action method knows whether the attempt succeeded.
    ///     Threading the rule through each signature would have meant changing every action and
    ///     every caller, including the tray and the countdown window, to pass something most of
    ///     them do not have.
    /// </summary>
    internal static class LogContext
    {
        // Per thread: session-switch notifications arrive on their own thread, and a scope opened
        // on the tick must not leak into them. An unset thread simply reports "system".
        [ThreadStatic] private static string _source;
        [ThreadStatic] private static string _rule;

        public static string Source => string.IsNullOrEmpty(_source) ? LogSources.System : _source;

        public static string Rule => _rule;

        /// <summary>Sets the context for the duration of the returned scope.</summary>
        public static IDisposable Use(string source, string rule = null)
        {
            var restore = new Scope(_source, _rule);
            _source = source;
            _rule = rule;
            return restore;
        }

        private sealed class Scope : IDisposable
        {
            private readonly string _previousSource;
            private readonly string _previousRule;
            private bool _disposed;

            public Scope(string previousSource, string previousRule)
            {
                _previousSource = previousSource;
                _previousRule = previousRule;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _source = _previousSource;
                _rule = _previousRule;
            }
        }
    }
}
