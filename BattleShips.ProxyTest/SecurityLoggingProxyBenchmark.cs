using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using BattleShips.Core;

namespace BattleShips.ProxyBenchmark
{
    /// <summary>
    /// Benchmark comparing SecurityProxy and LoggingProxy overhead vs direct GameInstance
    /// Measures memory usage and performance impact of proxy protection layers
    /// </summary>
    public class SecurityLoggingProxyBenchmark
    {
        private static readonly string TempLogPath = Path.Combine(Path.GetTempPath(), "battleships_benchmark.log");

        public static void Run()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  SECURITYPROXY & LOGGINGPROXY PERFORMANCE BENCHMARK       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            // Scenario 1: Single proxy creation overhead
            Console.WriteLine("┌─ SCENARIO 1: Single Proxy Creation Overhead ──────────────┐\n");
            BenchmarkSingleProxyCreation();

            Console.WriteLine("\n┌─ SCENARIO 2: Operation Performance (100 shots) ───────────┐\n");
            BenchmarkOperationPerformance();

            Console.WriteLine("\n┌─ SCENARIO 3: Multiple Games with Proxies ─────────────────┐\n");
            BenchmarkMultipleGamesWithProxies();

            Console.WriteLine("\n┌─ SCENARIO 4: Proxy Overhead Comparison ───────────────────┐\n");
            BenchmarkProxyOverhead();

            // Cleanup
            if (File.Exists(TempLogPath))
                File.Delete(TempLogPath);

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  BENCHMARK COMPLETE                                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        }

        static void BenchmarkSingleProxyCreation()
        {
            // Force GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test 1: Direct GameInstance
            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(false);

            var directGame = new GameInstance("direct-test");
            directGame.PlayerA = "PlayerA";
            directGame.PlayerB = "PlayerB";

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            long memDirect = memAfter - memBefore;
            var timeDirect = sw.Elapsed;

            Console.WriteLine("Direct GameInstance:");
            Console.WriteLine($"  ⏱️  Creation Time: {timeDirect.TotalMicroseconds:F2} μs");
            Console.WriteLine($"  💾 Memory Used: {memDirect:N0} bytes");

            // Force GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test 2: SecurityProxy
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var gameForSecurity = new GameInstance("security-test");
            gameForSecurity.PlayerA = "PlayerA";
            gameForSecurity.PlayerB = "PlayerB";
            var securityProxy = new SecurityProxy(gameForSecurity, "PlayerA");

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memSecurity = memAfter - memBefore;
            var timeSecurity = sw.Elapsed;

            Console.WriteLine("\nGameInstance + SecurityProxy:");
            Console.WriteLine($"  ⏱️  Creation Time: {timeSecurity.TotalMicroseconds:F2} μs");
            Console.WriteLine($"  💾 Memory Used: {memSecurity:N0} bytes");
            Console.WriteLine($"  📊 Overhead: {memSecurity - memDirect:N0} bytes ({(memSecurity - memDirect) / (double)memDirect * 100:F1}%)");

            // Force GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test 3: LoggingProxy
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var gameForLogging = new GameInstance("logging-test");
            gameForLogging.PlayerA = "PlayerA";
            gameForLogging.PlayerB = "PlayerB";
            var loggingProxy = new LoggingProxy(gameForLogging, TempLogPath);

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memLogging = memAfter - memBefore;
            var timeLogging = sw.Elapsed;

            Console.WriteLine("\nGameInstance + LoggingProxy:");
            Console.WriteLine($"  ⏱️  Creation Time: {timeLogging.TotalMicroseconds:F2} μs");
            Console.WriteLine($"  💾 Memory Used: {memLogging:N0} bytes");
            Console.WriteLine($"  📊 Overhead: {memLogging - memDirect:N0} bytes ({(memLogging - memDirect) / (double)memDirect * 100:F1}%)");

            Console.WriteLine("\n💡 NOTE: SecurityProxy and LoggingProxy cannot be chained");
            Console.WriteLine("   They both require GameInstance as base, not other proxies.");
            Console.WriteLine("   In production, you'd use one or the other, or create a combined proxy.");
        }

        static void BenchmarkOperationPerformance()
        {
            const int shotCount = 100;

            // Suppress console output during benchmarking
            var originalOut = Console.Out;
            Console.SetOut(System.IO.TextWriter.Null);

            // Setup game instances
            var directGame = new GameInstance("perf-direct");
            SetupGame(directGame);

            var securityGame = new GameInstance("perf-security");
            SetupGame(securityGame);
            var securityProxy = new SecurityProxy(securityGame, "PlayerA");

            var loggingGame = new GameInstance("perf-logging");
            SetupGame(loggingGame);
            var loggingProxy = new LoggingProxy(loggingGame, TempLogPath);

            // Benchmark: Direct GameInstance
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < shotCount; i++)
            {
                var point = new Point(i % 10, i / 10);
                directGame.RegisterShot("PlayerB", point, out _);
                if (i % 2 == 0) directGame.SwitchTurn();
            }
            sw.Stop();
            var timeDirect = sw.Elapsed;

            // Restore console output before printing results
            Console.SetOut(originalOut);

            Console.WriteLine($"Direct GameInstance ({shotCount} operations):");
            Console.WriteLine($"  ⏱️  Total Time: {timeDirect.TotalMilliseconds:F3} ms");
            Console.WriteLine($"  ⏱️  Avg per Operation: {timeDirect.TotalMicroseconds / shotCount:F2} μs");

            // Benchmark: SecurityProxy
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Suppress console output again
            Console.SetOut(System.IO.TextWriter.Null);

            sw.Restart();
            for (int i = 0; i < shotCount; i++)
            {
                var point = new Point(i % 10, i / 10);
                securityProxy.RegisterShot("PlayerB", point, out _);
                if (i % 2 == 0) securityProxy.SwitchTurn();
            }
            sw.Stop();
            var timeSecurity = sw.Elapsed;

            // Restore console output
            Console.SetOut(originalOut);

            Console.WriteLine($"\nSecurityProxy ({shotCount} operations):");
            Console.WriteLine($"  ⏱️  Total Time: {timeSecurity.TotalMilliseconds:F3} ms");
            Console.WriteLine($"  ⏱️  Avg per Operation: {timeSecurity.TotalMicroseconds / shotCount:F2} μs");
            Console.WriteLine($"  📊 Overhead: {(timeSecurity - timeDirect).TotalMicroseconds:F2} μs ({timeSecurity.TotalMilliseconds / timeDirect.TotalMilliseconds:F2}x)");

            // Benchmark: LoggingProxy
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Suppress console output again
            Console.SetOut(System.IO.TextWriter.Null);

            sw.Restart();
            for (int i = 0; i < shotCount; i++)
            {
                var point = new Point(i % 10, i / 10);
                loggingProxy.RegisterShot("PlayerB", point, out _);
                if (i % 2 == 0) loggingProxy.SwitchTurn();
            }
            sw.Stop();
            var timeLogging = sw.Elapsed;

            // Restore console output
            Console.SetOut(originalOut);

            Console.WriteLine($"\nLoggingProxy ({shotCount} operations):");
            Console.WriteLine($"  ⏱️  Total Time: {timeLogging.TotalMilliseconds:F3} ms");
            Console.WriteLine($"  ⏱️  Avg per Operation: {timeLogging.TotalMicroseconds / shotCount:F2} μs");
            Console.WriteLine($"  📊 Overhead: {(timeLogging - timeDirect).TotalMicroseconds:F2} μs ({timeLogging.TotalMilliseconds / timeDirect.TotalMilliseconds:F2}x)");
            Console.WriteLine($"  💾 Note: Logging overhead includes file I/O operations");
        }

        static void BenchmarkMultipleGamesWithProxies()
        {
            const int gameCount = 50;

            // Benchmark: Direct GameInstances
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(false);

            var directGames = new List<GameInstance>();
            for (int i = 0; i < gameCount; i++)
            {
                var game = new GameInstance($"direct-{i}");
                SetupGame(game);
                directGames.Add(game);
            }

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            long memDirect = memAfter - memBefore;
            var timeDirect = sw.Elapsed;

            Console.WriteLine($"Direct GameInstances ({gameCount} games):");
            Console.WriteLine($"  ⏱️  Total Time: {timeDirect.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  ⏱️  Avg per Game: {timeDirect.TotalMicroseconds / gameCount:F2} μs");
            Console.WriteLine($"  💾 Total Memory: {memDirect:N0} bytes");
            Console.WriteLine($"  💾 Avg per Game: {memDirect / gameCount:N0} bytes");

            // Benchmark: SecurityProxy per player
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var securityGames = new List<(GameInstance game, SecurityProxy proxyA, SecurityProxy proxyB)>();
            for (int i = 0; i < gameCount; i++)
            {
                var game = new GameInstance($"security-{i}");
                SetupGame(game);
                var proxyA = new SecurityProxy(game, "PlayerA");
                var proxyB = new SecurityProxy(game, "PlayerB");
                securityGames.Add((game, proxyA, proxyB));
            }

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memSecurity = memAfter - memBefore;
            var timeSecurity = sw.Elapsed;

            Console.WriteLine($"\nSecurityProxies ({gameCount} games, 2 proxies each):");
            Console.WriteLine($"  ⏱️  Total Time: {timeSecurity.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  ⏱️  Avg per Game: {timeSecurity.TotalMicroseconds / gameCount:F2} μs");
            Console.WriteLine($"  💾 Total Memory: {memSecurity:N0} bytes");
            Console.WriteLine($"  💾 Avg per Game: {memSecurity / gameCount:N0} bytes");
            Console.WriteLine($"  📊 Overhead: {memSecurity - memDirect:N0} bytes ({(memSecurity - memDirect) / (double)memDirect * 100:F1}%)");

            // Benchmark: LoggingProxy
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var loggingGames = new List<(GameInstance game, LoggingProxy proxy)>();
            for (int i = 0; i < gameCount; i++)
            {
                var game = new GameInstance($"logging-{i}");
                SetupGame(game);
                var proxy = new LoggingProxy(game, TempLogPath);
                loggingGames.Add((game, proxy));
            }

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memLogging = memAfter - memBefore;
            var timeLogging = sw.Elapsed;

            Console.WriteLine($"\nLoggingProxies ({gameCount} games):");
            Console.WriteLine($"  ⏱️  Total Time: {timeLogging.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  ⏱️  Avg per Game: {timeLogging.TotalMicroseconds / gameCount:F2} μs");
            Console.WriteLine($"  💾 Total Memory: {memLogging:N0} bytes");
            Console.WriteLine($"  💾 Avg per Game: {memLogging / gameCount:N0} bytes");
            Console.WriteLine($"  📊 Overhead: {memLogging - memDirect:N0} bytes ({(memLogging - memDirect) / (double)memDirect * 100:F1}%)");
        }

        static void BenchmarkProxyOverhead()
        {
            Console.WriteLine("Memory Overhead per Proxy Type:\n");

            // Direct GameInstance
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memBefore = GC.GetTotalMemory(true);
            var directGame = new GameInstance("overhead-test");
            SetupGame(directGame);
            long memAfter = GC.GetTotalMemory(false);
            long memDirect = memAfter - memBefore;

            Console.WriteLine($"Base GameInstance: {memDirect:N0} bytes");

            // SecurityProxy overhead
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            memBefore = GC.GetTotalMemory(true);
            var securityProxy = new SecurityProxy(new GameInstance("sec-overhead"), "PlayerA");
            memAfter = GC.GetTotalMemory(false);
            long securityOverhead = memAfter - memBefore;

            Console.WriteLine($"SecurityProxy overhead: ~{securityOverhead:N0} bytes");

            // LoggingProxy overhead
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            memBefore = GC.GetTotalMemory(true);
            var loggingProxy = new LoggingProxy(new GameInstance("log-overhead"), TempLogPath);
            memAfter = GC.GetTotalMemory(false);
            long loggingOverhead = memAfter - memBefore;

            Console.WriteLine($"LoggingProxy overhead: ~{loggingOverhead:N0} bytes");

            // Performance overhead (security checks)
            Console.WriteLine("\n\nPerformance Overhead Analysis:");

            // Suppress console output during benchmarking
            var originalOut2 = Console.Out;
            Console.SetOut(System.IO.TextWriter.Null);

            var perfGame = new GameInstance("perf-analysis");
            SetupGame(perfGame);
            var perfProxy = new SecurityProxy(perfGame, "PlayerA");

            const int iterations = 1000;

            // Blocked operation (should be rejected)
            var swBlocked = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                perfProxy.RegisterShot("PlayerA", new Point(1, 1), out _); // Self-shot, blocked
            }
            swBlocked.Stop();

            // Allowed operation
            var swAllowed = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                perfProxy.RegisterShot("PlayerB", new Point(i % 10, i / 10), out _); // Valid shot
            }
            swAllowed.Stop();

            // Restore console output
            Console.SetOut(originalOut2);

            Console.WriteLine($"\nSecurityProxy validation overhead ({iterations} operations):");
            Console.WriteLine($"  Blocked operations: {swBlocked.Elapsed.TotalMicroseconds / iterations:F2} μs avg");
            Console.WriteLine($"  Allowed operations: {swAllowed.Elapsed.TotalMicroseconds / iterations:F2} μs avg");
            Console.WriteLine($"  Note: Blocked operations return faster (fail-fast)");

            // Projected overhead for production server
            Console.WriteLine("\n\n💡 PRODUCTION INSIGHTS:");
            Console.WriteLine($"   For 100 concurrent games with SecurityProxy (2 proxies/game):");
            Console.WriteLine($"   - Base memory: {memDirect * 100:N0} bytes");
            Console.WriteLine($"   - With SecurityProxy: ~{(memDirect + securityOverhead * 2) * 100:N0} bytes");
            Console.WriteLine($"   - Additional cost: ~{securityOverhead * 200:N0} bytes");
            Console.WriteLine($"   - Security benefit: Prevents cheating and exploits");

            Console.WriteLine($"\n   For 100 concurrent games with LoggingProxy:");
            Console.WriteLine($"   - Base memory: {memDirect * 100:N0} bytes");
            Console.WriteLine($"   - With LoggingProxy: ~{(memDirect + loggingOverhead) * 100:N0} bytes");
            Console.WriteLine($"   - Additional cost: ~{loggingOverhead * 100:N0} bytes");
            Console.WriteLine($"   - Benefit: Complete audit trail for debugging");
        }

        static void SetupGame(GameInstance game)
        {
            game.PlayerA = "PlayerA";
            game.PlayerB = "PlayerB";
            game.Started = true;
            game.CurrentTurn = "PlayerA";

            // Add some ships
            var ships = new List<IShip>();
            var shipClass = new BlockyClass();
            for (int i = 0; i < 3; i++)
            {
                var ship = shipClass.CreateDestroyer(3, i);
                ship.Position = new Point(i * 2, 0);
                ship.IsPlaced = true;
                ship.Orientation = ShipOrientation.Horizontal;
                ships.Add(ship);
            }
            game.ShipsA = ships;
            game.ShipsB = new List<IShip>(ships); // Copy for player B
        }
    }
}
