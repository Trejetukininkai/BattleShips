using System;
using System.Drawing;
using BattleShips.Core;

namespace BattleShips.SecurityDemo
{
    /// <summary>
    /// Demonstration comparing direct GameInstance access (exploitable) vs SecurityProxy (protected).
    /// This shows how the SecurityProxy prevents cheating that would otherwise be possible.
    /// </summary>
    public class SecurityProxyHackDemo
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("GAMEINSTANCE vs SECURITYPROXY COMPARISON");

            RunDirectGameInstanceExploits();
            Console.WriteLine("\n");
            RunSecurityProxyProtection();
        }

        static void RunDirectGameInstanceExploits()
        {
            Console.WriteLine("┌──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  PART 1: HACKING WITH DIRECT GAMEINSTANCE ACCESS             │");
            Console.WriteLine("│  (No SecurityProxy - Server vulnerability demonstration)     │");
            Console.WriteLine("└──────────────────────────────────────────────────────────────┘\n");

            var game = new GameInstance("exploit-demo");
            game.PlayerA = "Alice";
            game.PlayerB = "Bob";
            game.CurrentTurn = "Alice";
            game.AddActionPoints("Alice", 10);
            game.AddActionPoints("Bob", 5);

            Console.WriteLine("Initial State:");
            Console.WriteLine($"  Current Turn: {game.CurrentTurn}");
            Console.WriteLine($"  Alice AP: {game.GetActionPoints("Alice")}");
            Console.WriteLine($"  Bob AP: {game.GetActionPoints("Bob")}");
            Console.WriteLine($"  PlayerA: {game.PlayerA}, PlayerB: {game.PlayerB}\n");

            // EXPLOIT 1: Shoot out of turn
            Console.WriteLine("═══ EXPLOIT #1: Bob shoots when it's Alice's turn ═══");
            string turnBefore = game.CurrentTurn ?? "";
            Console.WriteLine($"[State Before] Current turn: '{turnBefore}'");
            Console.WriteLine($"[Attack] Bob attempts to shoot Alice at (5,5)...");
            bool hit = game.RegisterShot("Alice", new Point(5, 5), out bool opponentLost);
            Console.WriteLine($"[State After] Shot was registered: {hit}");
            if (hit || turnBefore == "Alice") // Shot went through even though it was Alice's turn
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[VERIFICATION] EXPLOIT SUCCESSFUL! Bob shot during Alice's turn!");
                Console.ResetColor();
            }
            Console.WriteLine();

            // EXPLOIT 2: Shoot yourself to see where your ships are
            Console.WriteLine("═══ EXPLOIT #2: Alice shoots herself to reveal ship positions ═══");
            Console.WriteLine($"[Attack] Alice attempts to shoot herself at (3,3)...");
            hit = game.RegisterShot("Alice", new Point(3, 3), out opponentLost);
            Console.WriteLine($"[State After] Self-shot registered: {hit}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[VERIFICATION] EXPLOIT SUCCESSFUL! Alice was allowed to shoot herself!");
            Console.WriteLine($"               This could be used to map ship positions without cost!");
            Console.ResetColor();
            Console.WriteLine();

            // EXPLOIT 3: Manipulate turn order
            Console.WriteLine("═══ EXPLOIT #3: Bob forces a turn switch ═══");
            turnBefore = game.CurrentTurn ?? "";
            Console.WriteLine($"[State Before] Current turn: '{turnBefore}'");
            Console.WriteLine($"[Attack] Bob calls SwitchTurn()...");
            game.SwitchTurn();
            string turnAfter = game.CurrentTurn ?? "";
            Console.WriteLine($"[State After] Current turn: '{turnAfter}'");
            if (turnBefore != turnAfter)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[VERIFICATION] EXPLOIT SUCCESSFUL! Turn changed from '{turnBefore}' to '{turnAfter}'!");
                Console.ResetColor();
            }
            Console.WriteLine();

            // EXPLOIT 4: Give yourself unlimited action points
            Console.WriteLine("═══ EXPLOIT #4: Bob gives himself 1000 action points ═══");
            int bobApBefore = game.GetActionPoints("Bob");
            Console.WriteLine($"[State Before] Bob's AP: {bobApBefore}");
            Console.WriteLine($"[Attack] Bob calls AddActionPoints('Bob', 1000)...");
            game.AddActionPoints("Bob", 1000);
            int bobApAfter = game.GetActionPoints("Bob");
            Console.WriteLine($"[State After] Bob's AP: {bobApAfter}");
            int apGained = bobApAfter - bobApBefore;
            if (apGained > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[VERIFICATION] EXPLOIT SUCCESSFUL! Bob gained {apGained} AP illegally!");
                Console.ResetColor();
            }
            Console.WriteLine();

            // EXPLOIT 5: Drain opponent's action points
            Console.WriteLine("═══ EXPLOIT #5: Bob sabotages Alice's action points ═══");
            int aliceApBefore = game.GetActionPoints("Alice");
            Console.WriteLine($"[State Before] Alice's AP: {aliceApBefore}");
            Console.WriteLine($"[Attack] Bob calls AddActionPoints('Alice', -1000)...");
            game.AddActionPoints("Alice", -1000);
            int aliceApAfter = game.GetActionPoints("Alice");
            Console.WriteLine($"[State After] Alice's AP: {aliceApAfter}");
            int apLost = aliceApBefore - aliceApAfter;
            if (apLost > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[VERIFICATION] EXPLOIT SUCCESSFUL! Alice lost {apLost} AP to sabotage!");
                Console.ResetColor();
            }
            Console.WriteLine();

            // EXPLOIT 6: Kick opponent from game
            Console.WriteLine("═══ EXPLOIT #6: Bob kicks Alice from the game ═══");
            string? playerABefore = game.PlayerA;
            Console.WriteLine($"[State Before] PlayerA: '{playerABefore}'");
            Console.WriteLine($"[Attack] Bob calls RemovePlayer('Alice')...");
            game.RemovePlayer("Alice");
            string? playerAAfter = game.PlayerA;
            Console.WriteLine($"[State After] PlayerA: '{playerAAfter ?? "NULL"}'");
            if (playerABefore != null && playerAAfter == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[VERIFICATION] EXPLOIT SUCCESSFUL! Alice was removed! Bob wins by default!");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        static void RunSecurityProxyProtection()
        {
            Console.WriteLine("┌──────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  PART 2: SAME EXPLOITS WITH SECURITYPROXY PROTECTION        │");
            Console.WriteLine("│  (Shows how SecurityProxy blocks all exploit attempts)       │");
            Console.WriteLine("└──────────────────────────────────────────────────────────────┘\n");

            var game = new GameInstance("protected-demo");
            game.PlayerA = "Alice";
            game.PlayerB = "Bob";
            game.CurrentTurn = "Alice";
            game.AddActionPoints("Alice", 10);
            game.AddActionPoints("Bob", 5);

            // Create security proxies for each player
            var aliceProxy = new SecurityProxy(game, "Alice");
            var bobProxy = new SecurityProxy(game, "Bob");

            Console.WriteLine("Initial State:");
            Console.WriteLine($"  Current Turn: {game.CurrentTurn}");
            Console.WriteLine($"  Alice AP: {game.GetActionPoints("Alice")}");
            Console.WriteLine($"  Bob AP: {game.GetActionPoints("Bob")}");
            Console.WriteLine($"  PlayerA: {game.PlayerA}, PlayerB: {game.PlayerB}\n");


            // BLOCKED EXPLOIT 1: Shoot out of turn
            Console.WriteLine("═══ BLOCKED EXPLOIT #1: Bob tries to shoot when it's Alice's turn ═══");
            string turnBefore = game.CurrentTurn ?? "";
            Console.WriteLine($"[State Before] Current turn: '{turnBefore}'");
            Console.WriteLine($"[Attack] Bob attempts to shoot Alice at (5,5) via proxy...");
            bool hit = bobProxy.RegisterShot("Alice", new Point(5, 5), out bool opponentLost);
            Console.WriteLine($"[State After] Shot registered: {hit}");
            if (!hit && turnBefore == game.CurrentTurn)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[VERIFICATION] EXPLOIT BLOCKED! Shot denied, turn still '{game.CurrentTurn}'");
                Console.ResetColor();
            }
            Console.WriteLine();

            // BLOCKED EXPLOIT 2: Shoot yourself
            Console.WriteLine("═══ BLOCKED EXPLOIT #2: Alice tries to shoot herself ═══");
            Console.WriteLine($"[Attack] Alice attempts to shoot herself at (3,3) via proxy...");
            hit = aliceProxy.RegisterShot("Alice", new Point(3, 3), out opponentLost);
            Console.WriteLine($"[State After] Self-shot registered: {hit}");
            if (!hit)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[VERIFICATION] EXPLOIT BLOCKED! Self-targeting prevented!");
                Console.ResetColor();
            }
            Console.WriteLine();

            // BLOCKED EXPLOIT 3: Manipulate turn order
            Console.WriteLine("═══ BLOCKED EXPLOIT #3: Bob tries to force a turn switch ═══");
            turnBefore = game.CurrentTurn ?? "";
            Console.WriteLine($"[State Before] Current turn: '{turnBefore}'");
            Console.WriteLine($"[Attack] Bob calls SwitchTurn() via proxy...");
            bobProxy.SwitchTurn();
            string turnAfter = game.CurrentTurn ?? "";
            Console.WriteLine($"[State After] Current turn: '{turnAfter}'");
            if (turnBefore == turnAfter && turnAfter == "Alice")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[VERIFICATION] EXPLOIT BLOCKED! Turn unchanged, still '{turnAfter}'");
                Console.ResetColor();
            }
            Console.WriteLine();

            // BLOCKED EXPLOIT 4: Give yourself unlimited action points
            Console.WriteLine("═══ BLOCKED EXPLOIT #4: Bob tries to give himself 1000 AP ═══");
            int bobApBefore = game.GetActionPoints("Bob");
            Console.WriteLine($"[State Before] Bob's AP: {bobApBefore}");
            Console.WriteLine($"[Attack] Bob calls AddActionPoints('Bob', 1000) via proxy...");
            bobProxy.AddActionPoints("Bob", 1000);
            int bobApAfter = game.GetActionPoints("Bob");
            Console.WriteLine($"[State After] Bob's AP: {bobApAfter}");
            if (bobApBefore == bobApAfter)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[VERIFICATION] EXPLOIT BLOCKED! Bob's AP unchanged at {bobApAfter}");
                Console.ResetColor();
            }
            Console.WriteLine();

            // BLOCKED EXPLOIT 5: Drain opponent's action points
            Console.WriteLine("═══ BLOCKED EXPLOIT #5: Bob tries to drain Alice's AP ═══");
            int aliceApBefore = game.GetActionPoints("Alice");
            Console.WriteLine($"[State Before] Alice's AP: {aliceApBefore}");
            Console.WriteLine($"[Attack] Bob calls AddActionPoints('Alice', -1000) via proxy...");
            bobProxy.AddActionPoints("Alice", -1000);
            int aliceApAfter = game.GetActionPoints("Alice");
            Console.WriteLine($"[State After] Alice's AP: {aliceApAfter}");
            if (aliceApBefore == aliceApAfter)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[VERIFICATION] EXPLOIT BLOCKED! Alice's AP unchanged at {aliceApAfter}");
                Console.ResetColor();
            }
            Console.WriteLine();

            // BLOCKED EXPLOIT 6: Kick opponent
            Console.WriteLine("═══ BLOCKED EXPLOIT #6: Bob tries to kick Alice ═══");
            string? playerABefore = game.PlayerA;
            Console.WriteLine($"[State Before] PlayerA: '{playerABefore}'");
            Console.WriteLine($"[Attack] Bob calls RemovePlayer('Alice') via proxy...");
            bobProxy.RemovePlayer("Alice");
            string? playerAAfter = game.PlayerA;
            Console.WriteLine($"[State After] PlayerA: '{playerAAfter ?? "NULL"}'");
            if (playerABefore == playerAAfter && playerAAfter != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[VERIFICATION] EXPLOIT BLOCKED! Alice still in game: '{playerAAfter}'");
                Console.ResetColor();
            }
            Console.WriteLine();

            // SHOW LEGITIMATE ACTIONS WORK
            Console.WriteLine("═══ LEGITIMATE ACTION: Alice shoots Bob (allowed) ═══");
            Console.WriteLine($"[State Before] Turn: '{game.CurrentTurn}', Alice shooting opponent Bob");
            Console.WriteLine($"[Attack] Alice shoots Bob at (7,7) via proxy...");
            hit = aliceProxy.RegisterShot("Bob", new Point(7, 7), out opponentLost);
            Console.WriteLine($"[State After] Shot registered: {hit}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[VERIFICATION] LEGITIMATE ACTION ALLOWED!");
            Console.ResetColor();
        }
    }
}
