using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Wrapper for Form to demonstrate mediator pattern
    /// </summary>
    public class FormMediatorWrapper
    {
        private readonly IGameMediator _mediator;

        public FormMediatorWrapper(IGameMediator mediator)
        {
            _mediator = mediator;
            _mediator.RegisterForm(this);
        }

        // Simulate form events
        public void SimulateCellClick(int x, int y)
        {
            Console.WriteLine($"[Form] Cell clicked at ({x},{y})");

            // Send event to mediator
            _mediator.SendNotification("Form", "CellClicked", Tuple.Create(x, y));
        }

        public void SimulateShipsPlaced()
        {
            Console.WriteLine("[Form] Ships placed");

            // Send event to mediator
            _mediator.SendNotification("Form", "ShipsPlaced", null);
        }

        // Methods called by mediator
        public void OnGameStarted(bool youStart)
        {
            Console.WriteLine($"[Form] Game started notification received: youStart={youStart}");
            // Update UI accordingly
        }

        public void OnShipHit(int x, int y)
        {
            Console.WriteLine($"[Form] Hit notification at ({x},{y})");
            // Update UI to show hit
        }

        public void OnShipMissed(int x, int y)
        {
            Console.WriteLine($"[Form] Miss notification at ({x},{y})");
            // Update UI to show miss
        }

        public void OnShipDestroyed(string shipName)
        {
            Console.WriteLine($"[Form] Ship destroyed: {shipName}");
            // Update UI to show ship destroyed
        }

        public void OnGameOver(string winner)
        {
            Console.WriteLine($"[Form] Game over: {winner}");
            // Show game over screen
        }
    }
}
