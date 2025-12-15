using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using BattleShips.Core;

namespace BattleShips.ProxyBenchmark
{
    /// <summary>
    /// Benchmark comparing VirtualProxy lazy initialization vs direct GameInstance creation
    /// Measures memory usage and performance differences
    /// </summary>
    public class VirtualProxyBenchmark
    {
        public static void Run()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  VIRTUALPROXY vs GAMEINSTANCE PERFORMANCE BENCHMARK       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            // Scenario 1: Single game creation
            Console.WriteLine("┌─ SCENARIO 1: Single Game Creation ─────────────────────┐\n");
            BenchmarkSingleGameCreation();

            Console.WriteLine("\n┌─ SCENARIO 2: 100 Games - Only 20% Get Second Player ──┐\n");
            BenchmarkMultipleGamesWithLowJoinRate();

            Console.WriteLine("\n┌─ SCENARIO 3: 100 Games - All Get Second Player ───────┐\n");
            BenchmarkMultipleGamesWithFullJoinRate();

            Console.WriteLine("\n┌─ SCENARIO 4: Memory Overhead Comparison ──────────────┐\n");
            BenchmarkMemoryOverhead();

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  BENCHMARK COMPLETE                                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        }

        static void BenchmarkSingleGameCreation()
        {
            // Force GC before each test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test 1: VirtualProxy (no second player)
            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(false);

            var virtualProxy = new VirtualProxy("test-virtual-1");
            virtualProxy.PlayerA = "Player1";

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            long memUsedVirtual = memAfter - memBefore;

            Console.WriteLine("VirtualProxy (1 player only):");
            Console.WriteLine($"  ⏱️  Creation Time: {sw.Elapsed.TotalMicroseconds:F2} μs");
            Console.WriteLine($"  💾 Memory Used: {memUsedVirtual:N0} bytes");
            Console.WriteLine($"  ✓  Game Ready: {virtualProxy.IsGameReady}");

            // Force GC before next test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test 2: Direct GameInstance creation
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var gameInstance = new GameInstance("test-direct-1");
            gameInstance.PlayerA = "Player1";

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memUsedDirect = memAfter - memBefore;

            Console.WriteLine("\nDirect GameInstance (1 player):");
            Console.WriteLine($"  ⏱️  Creation Time: {sw.Elapsed.TotalMicroseconds:F2} μs");
            Console.WriteLine($"  💾 Memory Used: {memUsedDirect:N0} bytes");

            // Test 3: VirtualProxy with both players (triggers initialization)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var virtualProxyFull = new VirtualProxy("test-virtual-2");
            virtualProxyFull.PlayerA = "Player1";
            virtualProxyFull.PlayerB = "Player2"; // Triggers full initialization

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memUsedVirtualFull = memAfter - memBefore;

            Console.WriteLine("\nVirtualProxy (2 players - fully initialized):");
            Console.WriteLine($"  ⏱️  Creation Time: {sw.Elapsed.TotalMicroseconds:F2} μs");
            Console.WriteLine($"  💾 Memory Used: {memUsedVirtualFull:N0} bytes");
            Console.WriteLine($"  ✓  Game Ready: {virtualProxyFull.IsGameReady}");

            // Comparison
            Console.WriteLine("\n📊 COMPARISON:");
            Console.WriteLine($"  Memory Saved (VirtualProxy 1p vs Direct): {memUsedDirect - memUsedVirtual:N0} bytes");
            Console.WriteLine($"  Savings Percentage: {((memUsedDirect - memUsedVirtual) / (double)memUsedDirect * 100):F1}%");
        }

        static void BenchmarkMultipleGamesWithLowJoinRate()
        {
            const int totalGames = 100;
            const int gamesWithBothPlayers = 20; // Only 20% get second player

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test with VirtualProxy
            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(false);

            var virtualProxies = new List<VirtualProxy>();
            for (int i = 0; i < totalGames; i++)
            {
                var proxy = new VirtualProxy($"virtual-{i}");
                proxy.PlayerA = $"Player-{i}-A";

                if (i < gamesWithBothPlayers)
                {
                    proxy.PlayerB = $"Player-{i}-B"; // Only some get second player
                }

                virtualProxies.Add(proxy);
            }

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            long memVirtualProxies = memAfter - memBefore;
            var timeVirtualProxies = sw.Elapsed;

            Console.WriteLine($"VirtualProxy ({totalGames} games, {gamesWithBothPlayers} with 2 players):");
            Console.WriteLine($"  ⏱️  Total Time: {timeVirtualProxies.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  ⏱️  Avg per Game: {timeVirtualProxies.TotalMicroseconds / totalGames:F2} μs");
            Console.WriteLine($"  💾 Total Memory: {memVirtualProxies:N0} bytes");
            Console.WriteLine($"  💾 Avg per Game: {memVirtualProxies / totalGames:N0} bytes");
            Console.WriteLine($"  ✓  Games Ready: {virtualProxies.Count(p => p.IsGameReady)}");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test with direct GameInstance
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var gameInstances = new List<GameInstance>();
            for (int i = 0; i < totalGames; i++)
            {
                var game = new GameInstance($"direct-{i}");
                game.PlayerA = $"Player-{i}-A";

                if (i < gamesWithBothPlayers)
                {
                    game.PlayerB = $"Player-{i}-B";
                    game.Started = true;
                }

                gameInstances.Add(game);
            }

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memGameInstances = memAfter - memBefore;
            var timeGameInstances = sw.Elapsed;

            Console.WriteLine($"\nDirect GameInstance ({totalGames} games, {gamesWithBothPlayers} with 2 players):");
            Console.WriteLine($"  ⏱️  Total Time: {timeGameInstances.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  ⏱️  Avg per Game: {timeGameInstances.TotalMicroseconds / totalGames:F2} μs");
            Console.WriteLine($"  💾 Total Memory: {memGameInstances:N0} bytes");
            Console.WriteLine($"  💾 Avg per Game: {memGameInstances / totalGames:N0} bytes");

            // Comparison
            Console.WriteLine("\n📊 COMPARISON:");
            Console.WriteLine($"  Time Difference: {(timeGameInstances - timeVirtualProxies).TotalMilliseconds:F2} ms");
            Console.WriteLine($"  Memory Saved: {memGameInstances - memVirtualProxies:N0} bytes");
            Console.WriteLine($"  Savings Percentage: {((memGameInstances - memVirtualProxies) / (double)memGameInstances * 100):F1}%");
            Console.WriteLine($"  Performance Ratio: {timeVirtualProxies.TotalMilliseconds / timeGameInstances.TotalMilliseconds:F2}x");
        }

        static void BenchmarkMultipleGamesWithFullJoinRate()
        {
            const int totalGames = 100;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test with VirtualProxy (all games get both players)
            var sw = Stopwatch.StartNew();
            long memBefore = GC.GetTotalMemory(false);

            var virtualProxies = new List<VirtualProxy>();
            for (int i = 0; i < totalGames; i++)
            {
                var proxy = new VirtualProxy($"virtual-full-{i}");
                proxy.PlayerA = $"Player-{i}-A";
                proxy.PlayerB = $"Player-{i}-B"; // All get second player
                virtualProxies.Add(proxy);
            }

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            long memVirtualProxies = memAfter - memBefore;
            var timeVirtualProxies = sw.Elapsed;

            Console.WriteLine($"VirtualProxy ({totalGames} games, ALL with 2 players):");
            Console.WriteLine($"  ⏱️  Total Time: {timeVirtualProxies.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  ⏱️  Avg per Game: {timeVirtualProxies.TotalMicroseconds / totalGames:F2} μs");
            Console.WriteLine($"  💾 Total Memory: {memVirtualProxies:N0} bytes");
            Console.WriteLine($"  💾 Avg per Game: {memVirtualProxies / totalGames:N0} bytes");
            Console.WriteLine($"  ✓  Games Ready: {virtualProxies.Count(p => p.IsGameReady)}");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test with direct GameInstance
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            var gameInstances = new List<GameInstance>();
            for (int i = 0; i < totalGames; i++)
            {
                var game = new GameInstance($"direct-full-{i}");
                game.PlayerA = $"Player-{i}-A";
                game.PlayerB = $"Player-{i}-B";
                game.Started = true;
                gameInstances.Add(game);
            }

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);
            long memGameInstances = memAfter - memBefore;
            var timeGameInstances = sw.Elapsed;

            Console.WriteLine($"\nDirect GameInstance ({totalGames} games, ALL with 2 players):");
            Console.WriteLine($"  ⏱️  Total Time: {timeGameInstances.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  ⏱️  Avg per Game: {timeGameInstances.TotalMicroseconds / totalGames:F2} μs");
            Console.WriteLine($"  💾 Total Memory: {memGameInstances:N0} bytes");
            Console.WriteLine($"  💾 Avg per Game: {memGameInstances / totalGames:N0} bytes");

            // Comparison
            Console.WriteLine("\n📊 COMPARISON:");
            Console.WriteLine($"  Time Difference: {(timeGameInstances - timeVirtualProxies).TotalMilliseconds:F2} ms");
            Console.WriteLine($"  Memory Difference: {Math.Abs(memGameInstances - memVirtualProxies):N0} bytes");
            Console.WriteLine($"  Note: When all games need initialization, proxy overhead should be minimal");
        }

        static void BenchmarkMemoryOverhead()
        {
            // Measure the fixed overhead of GameInstance
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memBefore = GC.GetTotalMemory(true);
            var game = new GameInstance("memory-test");
            long memAfter = GC.GetTotalMemory(false);
            long baseMemory = memAfter - memBefore;

            Console.WriteLine($"Base GameInstance memory: {baseMemory:N0} bytes");

            // Measure with typical game data
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            memBefore = GC.GetTotalMemory(true);
            var gameWithData = new GameInstance("memory-test-2");
            gameWithData.PlayerA = "Player1";
            gameWithData.PlayerB = "Player2";
            gameWithData.Started = true;

            // Add typical game data
            gameWithData.ShipsA = CreateTypicalShips();
            gameWithData.ShipsB = CreateTypicalShips();
            gameWithData.AddActionPoints("Player1", 10);
            gameWithData.AddActionPoints("Player2", 10);

            memAfter = GC.GetTotalMemory(false);
            long fullGameMemory = memAfter - memBefore;

            Console.WriteLine($"GameInstance with data: {fullGameMemory:N0} bytes");
            Console.WriteLine($"Additional data overhead: {fullGameMemory - baseMemory:N0} bytes");

            // Estimate savings with VirtualProxy for waiting games
            Console.WriteLine("\n💡 INSIGHT:");
            Console.WriteLine($"   For 1000 games where only 30% get a second player:");
            Console.WriteLine($"   - Direct approach: ~{fullGameMemory * 1000:N0} bytes");
            Console.WriteLine($"   - VirtualProxy approach: ~{baseMemory * 700 + fullGameMemory * 300:N0} bytes");
            Console.WriteLine($"   - Savings: ~{(fullGameMemory - baseMemory) * 700:N0} bytes");
            Console.WriteLine($"   - Percentage: {((fullGameMemory - baseMemory) * 700.0 / (fullGameMemory * 1000) * 100):F1}%");
        }

        static List<IShip> CreateTypicalShips()
        {
            var ships = new List<IShip>();
            var shipClass = new BlockyClass();

            // Create 5 typical ships
            for (int i = 0; i < 5; i++)
            {
                var ship = shipClass.CreateDestroyer(3, i);
                ship.Position = new Point(i * 2, 0);
                ship.IsPlaced = true;
                ship.Orientation = ShipOrientation.Horizontal;
                ships.Add(ship);
            }

            return ships;
        }
    }
}
