using System;
using System.Collections.Generic;
using System.Drawing;

namespace BattleShips.Core
{
    /// <summary>
    /// Demonstration program to test and measure the performance of all three proxies
    /// </summary>
    public class ProxyPerformanceDemo
    {
        private const int TEST_ITERATIONS = 1000;

        public static void RunDemo()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          PROXY PATTERN PERFORMANCE DEMONSTRATION                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine($"Running {TEST_ITERATIONS} operations per proxy...\n");

            // Test 1: Direct GameInstance (baseline)
            var baselineMonitor = TestDirectGameInstance();

            // Test 2: VirtualProxy
            var virtualMonitor = TestVirtualProxy();

            // Test 3: SecurityProxy
            var securityMonitor = TestSecurityProxy();

            // Test 4: LoggingProxy
            var loggingMonitor = TestLoggingProxy();

            // Compare results
            ProxyPerformanceComparer.CompareResults(
                baselineMonitor,
                virtualMonitor,
                securityMonitor,
                loggingMonitor
            );

            Console.WriteLine("\n📁 Check 'game_log.txt' for detailed logging output from LoggingProxy");
        }

        private static ProxyPerformanceMonitor TestDirectGameInstance()
        {
            Console.WriteLine("Testing Direct GameInstance (Baseline)...");
            var monitor = new ProxyPerformanceMonitor("Direct GameInstance");

            var game = new GameInstance("test-game-baseline");
            game.PlayerA = "PlayerA";
            game.PlayerB = "PlayerB";
            game.CurrentTurn = "PlayerA";

            monitor.StartMeasurement();

            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                // Simulate game operations
                game.AddActionPoints("PlayerA", 1);
                int points = game.GetActionPoints("PlayerA");
                int ships = game.GetRemainingShips("PlayerA");
                string? other = game.Other("PlayerA");

                // Simulate shot
                bool hit = game.RegisterShot("PlayerB", new Point(i % 10, i % 10), out bool lost);

                game.SwitchTurn();
                game.SwitchTurn(); // Switch back

                monitor.IncrementOperations();
            }

            monitor.StopMeasurement();
            Console.WriteLine("✓ Complete\n");

            return monitor;
        }

        private static ProxyPerformanceMonitor TestVirtualProxy()
        {
            Console.WriteLine("Testing VirtualProxy (Lazy Initialization)...");
            var monitor = new ProxyPerformanceMonitor("VirtualProxy");

            var virtualProxy = new VirtualProxy("test-game-virtual");
            virtualProxy.PlayerA = "PlayerA";
            virtualProxy.PlayerB = "PlayerB"; // This triggers initialization

            monitor.StartMeasurement();

            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                virtualProxy.AddActionPoints("PlayerA", 1);
                int points = virtualProxy.GetActionPoints("PlayerA");
                int ships = virtualProxy.GetRemainingShips("PlayerA");
                string? other = virtualProxy.Other("PlayerA");

                bool hit = virtualProxy.RegisterShot("PlayerB", new Point(i % 10, i % 10), out bool lost);

                virtualProxy.SwitchTurn();
                virtualProxy.SwitchTurn();

                monitor.IncrementOperations();
            }

            monitor.StopMeasurement();
            Console.WriteLine("✓ Complete\n");

            return monitor;
        }

        private static ProxyPerformanceMonitor TestSecurityProxy()
        {
            Console.WriteLine("Testing SecurityProxy (Access Control)...");
            var monitor = new ProxyPerformanceMonitor("SecurityProxy");

            var game = new GameInstance("test-game-security");
            game.PlayerA = "PlayerA";
            game.PlayerB = "PlayerB";
            game.CurrentTurn = "PlayerA";

            var securityProxy = new SecurityProxy(game, "PlayerA");

            monitor.StartMeasurement();

            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                securityProxy.AddActionPoints("PlayerA", 1);
                int points = securityProxy.GetActionPoints("PlayerA");
                int ships = securityProxy.GetRemainingShips("PlayerA");
                string? other = securityProxy.Other("PlayerA");

                bool hit = securityProxy.RegisterShot("PlayerB", new Point(i % 10, i % 10), out bool lost);

                securityProxy.SwitchTurn();
                game.CurrentTurn = "PlayerA"; // Reset turn for next iteration

                monitor.IncrementOperations();
            }

            monitor.StopMeasurement();
            Console.WriteLine("✓ Complete\n");

            return monitor;
        }

        private static ProxyPerformanceMonitor TestLoggingProxy()
        {
            Console.WriteLine("Testing LoggingProxy (Logging to File)...");
            var monitor = new ProxyPerformanceMonitor("LoggingProxy");

            var game = new GameInstance("test-game-logging");
            game.PlayerA = "PlayerA";
            game.PlayerB = "PlayerB";
            game.CurrentTurn = "PlayerA";

            var loggingProxy = new LoggingProxy(game, "game_log.txt");

            monitor.StartMeasurement();

            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                loggingProxy.AddActionPoints("PlayerA", 1);
                int points = loggingProxy.GetActionPoints("PlayerA");
                int ships = loggingProxy.GetRemainingShips("PlayerA");
                string? other = loggingProxy.Other("PlayerA");

                bool hit = loggingProxy.RegisterShot("PlayerB", new Point(i % 10, i % 10), out bool lost);

                loggingProxy.SwitchTurn();
                loggingProxy.SwitchTurn();

                monitor.IncrementOperations();
            }

            monitor.StopMeasurement();
            Console.WriteLine("✓ Complete\n");

            return monitor;
        }

        /// <summary>
        /// Demonstrates the behavior of each proxy with a simple scenario
        /// </summary>
        public static void DemonstrateBehaviors()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║             PROXY BEHAVIORAL DEMONSTRATION                         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝\n");

            // 1. VirtualProxy Demo
            Console.WriteLine("--- 1. VirtualProxy: Lazy Initialization ---");
            var virtualProxy = new VirtualProxy("demo-virtual");
            Console.WriteLine($"Game ready before second player: {virtualProxy.IsGameReady}");
            virtualProxy.PlayerA = "Alice";
            Console.WriteLine($"First player joined: {virtualProxy.PlayerA}");
            Console.WriteLine($"Game ready: {virtualProxy.IsGameReady}");
            virtualProxy.PlayerB = "Bob";
            Console.WriteLine($"Second player joined: {virtualProxy.PlayerB}");
            Console.WriteLine($"Game ready: {virtualProxy.IsGameReady}");
            Console.WriteLine();

            // 2. SecurityProxy Demo
            Console.WriteLine("--- 2. SecurityProxy: Access Control ---");
            var game = new GameInstance("demo-security");
            game.PlayerA = "Alice";
            game.PlayerB = "Bob";
            game.CurrentTurn = "Alice";

            var aliceProxy = new SecurityProxy(game, "Alice");
            var bobProxy = new SecurityProxy(game, "Bob");

            Console.WriteLine("Alice tries to shoot (her turn):");
            aliceProxy.RegisterShot("Bob", new Point(3, 3), out _);

            Console.WriteLine("\nBob tries to shoot (NOT his turn):");
            bobProxy.RegisterShot("Alice", new Point(5, 5), out _);

            Console.WriteLine("\nAlice switches turn:");
            aliceProxy.SwitchTurn();

            Console.WriteLine("\nBob tries to shoot (NOW his turn):");
            bobProxy.RegisterShot("Alice", new Point(5, 5), out _);
            Console.WriteLine();

            // 3. LoggingProxy Demo
            Console.WriteLine("--- 3. LoggingProxy: Action Logging ---");
            var logGame = new GameInstance("demo-logging");
            logGame.PlayerA = "Alice";
            logGame.PlayerB = "Bob";
            logGame.CurrentTurn = "Alice";

            var loggingProxy = new LoggingProxy(logGame, "demo_log.txt");
            Console.WriteLine("Performing actions (check 'demo_log.txt' for logs)...");
            loggingProxy.AddActionPoints("Alice", 5);
            loggingProxy.RegisterShot("Bob", new Point(2, 2), out _);
            loggingProxy.SwitchTurn();
            loggingProxy.GetRemainingShips("Bob");
            Console.WriteLine("Actions logged to 'demo_log.txt'\n");
        }
    }
}
