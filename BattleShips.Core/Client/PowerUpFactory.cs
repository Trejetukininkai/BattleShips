using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    public interface IPowerUp
    {
        string Name { get; }
        int Cost { get; }
        string Description { get; }
        bool CanActivate(GameModel model, int currentAP);
        void ActivatePowerUp(GameModel model, GameClientController controller);
    }
    public static class PowerUpFactory
    {
        public static IPowerUp CreatePowerUp(string powerUpName)
        {
            return powerUpName.ToLower() switch
            {
                "initiatedisaster" or "disaster" => new InitiateDisasterPowerUp(),
                "repair" => new RepairPowerUp(),
                "mininuke" or "nuke" => new MiniNukePowerUp(),
                _ => throw new ArgumentException($"Unknown powerup: {powerUpName}")
            };
        }

        public static List<IPowerUp> GetAllPowerUps()
        {
            return new List<IPowerUp>
        {
            new InitiateDisasterPowerUp(),
            new RepairPowerUp(),
            new MiniNukePowerUp()
        };
        }
    }

    public class InitiateDisasterPowerUp : IPowerUp
    {
        public string Name => "InitiateDisaster";
        public int Cost => 2;
        public string Description => "Forces a disaster to occur after your turn";

        public bool CanActivate(GameModel model, int currentAP)
        {
            return currentAP >= Cost && model.State == AppState.Playing && model.IsMyTurn;
        }

        public void ActivatePowerUp(GameModel model, GameClientController controller)
        {
            Console.WriteLine($"[PowerUp] {Name} activated!");
            // This will be handled by the server
            controller.ActivatePowerUp(Name);
        }
    }

    // Repair PowerUp (5 AP)
    public class RepairPowerUp : IPowerUp
    {
        public string Name => "Repair";
        public int Cost => 5;
        public string Description => "Heals one damaged ship segment";

        public bool CanActivate(GameModel model, int currentAP)
        {
            return currentAP >= Cost && model.State == AppState.Playing && model.IsMyTurn;
        }

        public void ActivatePowerUp(GameModel model, GameClientController controller)
        {
            Console.WriteLine($"[PowerUp] {Name} activated!");

            var damagedCells = model.YourHitsByOpponent
                .Where(hit => model.YourShips.Any(ship =>
                    ship.IsPlaced && ship.GetOccupiedCells().Contains(hit)))
                .ToList();

            if (damagedCells.Any())
            {
                var cellToHeal = damagedCells.First();
                model.YourHitsByOpponent.Remove(cellToHeal);
                Console.WriteLine($"[PowerUp] Auto-healed cell at ({cellToHeal.X},{cellToHeal.Y})");

                controller.ActivatePowerUp(Name);
            }
            else
            {
                Console.WriteLine($"[PowerUp] No damaged ship segments to repair");
                controller.ActivatePowerUp(Name);
            }
        }
    }

    // MiniNuke PowerUp (10 AP)
    public class MiniNukePowerUp : IPowerUp
    {
        public string Name => "MiniNuke";
        public int Cost => 10;
        public string Description => "Next shot hits a 3x3 area";

        public bool CanActivate(GameModel model, int currentAP)
        {
            return currentAP >= Cost && model.State == AppState.Playing && model.IsMyTurn;
        }

        public void ActivatePowerUp(GameModel model, GameClientController controller)
        {
            Console.WriteLine($"[PowerUp] {Name} activated!");
            model.HasMiniNukeActive = true;
            model.CurrentStatus = "MiniNuke active! Your next shot will hit a 3x3 area";
            controller.ActivatePowerUp(Name);
        }
    }
}
