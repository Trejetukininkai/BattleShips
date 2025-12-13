using System;
using System.Diagnostics;

namespace BattleShips.Core
{
    /// <summary>
    /// Performance monitoring utility for measuring proxy overhead.
    /// Tracks execution time and memory usage.
    /// </summary>
    public class ProxyPerformanceMonitor
    {
        public string ProxyName { get; set; }
        public long ExecutionTimeMs { get; private set; }
        public long MemoryUsedBytes { get; private set; }
        public int OperationCount { get; private set; }

        private Stopwatch _stopwatch;
        private long _startMemory;

        public ProxyPerformanceMonitor(string proxyName)
        {
            ProxyName = proxyName;
            _stopwatch = new Stopwatch();
        }

        public void StartMeasurement()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _startMemory = GC.GetTotalMemory(false);
            _stopwatch.Restart();
        }

        public void StopMeasurement()
        {
            _stopwatch.Stop();
            ExecutionTimeMs = _stopwatch.ElapsedMilliseconds;

            long endMemory = GC.GetTotalMemory(false);
            MemoryUsedBytes = endMemory - _startMemory;
        }

        public void IncrementOperations()
        {
            OperationCount++;
        }

        public void Reset()
        {
            ExecutionTimeMs = 0;
            MemoryUsedBytes = 0;
            OperationCount = 0;
            _stopwatch.Reset();
        }

        public void PrintResults()
        {
            Console.WriteLine($"\n=== Performance Results for {ProxyName} ===");
            Console.WriteLine($"Total Operations: {OperationCount}");
            Console.WriteLine($"Execution Time: {ExecutionTimeMs} ms");
            Console.WriteLine($"Average Time per Operation: {(OperationCount > 0 ? (double)ExecutionTimeMs / OperationCount : 0):F3} ms");
            Console.WriteLine($"Memory Used: {MemoryUsedBytes} bytes ({MemoryUsedBytes / 1024.0:F2} KB)");
            Console.WriteLine($"========================================\n");
        }

        public string GetSummary()
        {
            return $"{ProxyName}: {ExecutionTimeMs}ms, {MemoryUsedBytes / 1024.0:F2}KB, {OperationCount} ops, " +
                   $"Avg: {(OperationCount > 0 ? (double)ExecutionTimeMs / OperationCount : 0):F3}ms/op";
        }
    }

    /// <summary>
    /// Comparison utility for analyzing multiple proxy performance results
    /// </summary>
    public class ProxyPerformanceComparer
    {
        public static void CompareResults(params ProxyPerformanceMonitor[] monitors)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          PROXY PERFORMANCE COMPARISON REPORT                       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝\n");

            // Print individual results
            foreach (var monitor in monitors)
            {
                monitor.PrintResults();
            }

            // Find fastest and most memory efficient
            var fastest = monitors[0];
            var mostMemoryEfficient = monitors[0];

            foreach (var monitor in monitors)
            {
                if (monitor.ExecutionTimeMs < fastest.ExecutionTimeMs)
                    fastest = monitor;
                if (monitor.MemoryUsedBytes < mostMemoryEfficient.MemoryUsedBytes)
                    mostMemoryEfficient = monitor;
            }

            Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         SUMMARY                                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"⚡ Fastest: {fastest.ProxyName} ({fastest.ExecutionTimeMs} ms)");
            Console.WriteLine($"💾 Most Memory Efficient: {mostMemoryEfficient.ProxyName} ({mostMemoryEfficient.MemoryUsedBytes / 1024.0:F2} KB)");

            // Calculate overhead percentages relative to base
            if (monitors.Length > 1)
            {
                Console.WriteLine("\n--- Overhead Analysis (vs first proxy) ---");
                var baseline = monitors[0];
                for (int i = 1; i < monitors.Length; i++)
                {
                    var current = monitors[i];
                    double timeOverhead = baseline.ExecutionTimeMs > 0
                        ? ((double)(current.ExecutionTimeMs - baseline.ExecutionTimeMs) / baseline.ExecutionTimeMs) * 100
                        : 0;
                    double memoryOverhead = baseline.MemoryUsedBytes > 0
                        ? ((double)(current.MemoryUsedBytes - baseline.MemoryUsedBytes) / baseline.MemoryUsedBytes) * 100
                        : 0;

                    Console.WriteLine($"{current.ProxyName}:");
                    Console.WriteLine($"  Time Overhead: {timeOverhead:+0.00;-0.00;0}%");
                    Console.WriteLine($"  Memory Overhead: {memoryOverhead:+0.00;-0.00;0}%");
                }
            }

            Console.WriteLine("\n" + new string('═', 70) + "\n");
        }
    }
}
