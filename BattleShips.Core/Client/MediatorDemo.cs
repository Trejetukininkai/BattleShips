using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShips.Client;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Demo to show Mediator pattern in action
    /// </summary>
    public static class MediatorDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== MEDIATOR PATTERN DEMO ===\n");

            // 1. Create mediator
            IGameMediator mediator = new GameMediator();

            // 2. Create components
            var gameModel = new GameModel(mediator);
            var sfxService = new SFXService(mediator);
            var formWrapper = new FormMediatorWrapper(mediator);

            // 3. Demonstrate communication
            Console.WriteLine("\n--- Simulating Form Events ---");
            formWrapper.SimulateCellClick(3, 4);
            Thread.Sleep(1000);

            formWrapper.SimulateCellClick(7, 2);
            Thread.Sleep(1000);

            Console.WriteLine("\n--- Simulating Game Events ---");
            gameModel.OnGameStarted(true);
            Thread.Sleep(1000);

            gameModel.OnShipHit(5, 5);
            Thread.Sleep(1000);

            gameModel.OnShipDestroyed("Battleship");
            Thread.Sleep(1000);

            gameModel.OnGameOver("Player 1");

            Console.WriteLine("\n=== DEMO COMPLETE ===");
        }
    }
}
