using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsAutoPowerManager.Config
{
    /// <summary>
    ///     Groups action types into the buckets the history viewer filters by.
    ///
    ///     The grouping used to live in the web view as a literal map, which meant the browser
    ///     decided what a filter meant while the records it filtered came from here. A type the map
    ///     forgot simply became unfilterable — which is what happened to a failed monitor off.
    /// </summary>
    public static class LogFacets
    {
        public const string Locks = "locks";
        public const string Unlocks = "unlocks";
        public const string Monitor = "monitor";
        public const string Sleeps = "sleeps";
        public const string Shutdowns = "shutdowns";
        public const string Restarts = "restarts";
        public const string LogOffs = "logOffs";
        public const string App = "app";

        private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [Locks] = new[] { ActionTypes.LockComputer, ActionTypes.LockComputerManually },
            [Unlocks] = new[] { ActionTypes.UnlockComputer },
            [Monitor] = new[] { ActionTypes.TurnOffMonitor, ActionTypes.TurnOffMonitorFailed },
            [Sleeps] = new[] { ActionTypes.SleepComputer },
            [Shutdowns] = new[] { ActionTypes.ShutdownComputer },
            [Restarts] = new[] { ActionTypes.RestartComputer },
            [LogOffs] = new[] { ActionTypes.LogOffWindows },
            [App] = new[] { ActionTypes.AppStarted, ActionTypes.AppTerminated }
        };

        /// <summary>Facet keys in the order the viewer lists them.</summary>
        public static readonly string[] Keys =
        {
            Locks, Unlocks, Monitor, Sleeps, Shutdowns, Restarts, LogOffs, App
        };

        public static IReadOnlyList<string> TypesOf(string facetKey) =>
            Map.TryGetValue(facetKey ?? string.Empty, out string[] types) ? types : Array.Empty<string>();

        /// <summary>Expands facet keys to the action types they cover.</summary>
        public static HashSet<string> ToTypes(IEnumerable<string> facetKeys)
        {
            var types = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in facetKeys ?? Enumerable.Empty<string>())
            {
                foreach (string type in TypesOf(key))
                {
                    types.Add(type);
                }
            }

            return types;
        }

        /// <summary>The facet an action type belongs to, or null when it belongs to none.</summary>
        public static string FacetOf(string actionType)
        {
            foreach (KeyValuePair<string, string[]> pair in Map)
            {
                if (Array.IndexOf(pair.Value, actionType) >= 0)
                {
                    return pair.Key;
                }
            }

            return null;
        }
    }
}
