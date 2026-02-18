using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WindowsAutoPowerManager.Functions
{
    internal static class DebugPerformanceTracker
    {
#if DEBUG
        private sealed class MetricState
        {
            public long Count;
            public readonly List<double> Samples = new List<double>(512);
        }

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, MetricState> Metrics = new Dictionary<string, MetricState>();
        private const int MaxSamples = 512;
        private const int ReportInterval = 200;

        public static long Start()
        {
            return Stopwatch.GetTimestamp();
        }

        public static void Record(string metricName, long startTimestamp)
        {
            if (startTimestamp <= 0 || string.IsNullOrWhiteSpace(metricName))
            {
                return;
            }

            double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000d / Stopwatch.Frequency;

            lock (SyncRoot)
            {
                if (!Metrics.TryGetValue(metricName, out MetricState state))
                {
                    state = new MetricState();
                    Metrics[metricName] = state;
                }

                ++state.Count;
                if (state.Samples.Count >= MaxSamples)
                {
                    state.Samples.RemoveAt(0);
                }

                state.Samples.Add(elapsedMs);

                if (state.Count % ReportInterval == 0)
                {
                    ReportLocked(metricName, state);
                }
            }
        }

        private static void ReportLocked(string metricName, MetricState state)
        {
            if (state.Samples.Count == 0)
            {
                return;
            }

            double[] ordered = state.Samples.OrderBy(v => v).ToArray();
            double p95 = Percentile(ordered, 0.95d);
            double p99 = Percentile(ordered, 0.99d);
            double avg = ordered.Average();

            Debug.WriteLine(
                "[PERF] " + metricName +
                " count=" + state.Count +
                " avgMs=" + avg.ToString("F3") +
                " p95Ms=" + p95.ToString("F3") +
                " p99Ms=" + p99.ToString("F3"));
        }

        private static double Percentile(double[] ordered, double percentile)
        {
            if (ordered == null || ordered.Length == 0)
            {
                return 0d;
            }

            if (ordered.Length == 1)
            {
                return ordered[0];
            }

            double index = percentile * (ordered.Length - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);

            if (lower == upper)
            {
                return ordered[lower];
            }

            double weight = index - lower;
            return ordered[lower] + ((ordered[upper] - ordered[lower]) * weight);
        }
#else
        public static long Start()
        {
            return 0;
        }

        public static void Record(string metricName, long startTimestamp)
        {
            _ = metricName;
            _ = startTimestamp;
        }
#endif
    }
}
