using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class PlaceAutoExpression : IExpression
    {
        private Random _random = new Random();

        public string Interpret(CommandContext ctx)
        {
            try
            {
                if (ctx.GameModel.State != AppState.Placement)
                {
                    return $"Cannot auto-place ships now. Current state: {ctx.GameModel.State}";
                }

                var unplacedShips = ctx.GameModel.YourShips.Where(s => !s.IsPlaced).ToList();

                if (unplacedShips.Count == 0)
                {
                    return "All ships are already placed.";
                }

                int placedCount = 0;
                foreach (var ship in unplacedShips)
                {
                    if (TryAutoPlaceShip(ctx, ship))
                    {
                        placedCount++;
                    }
                }

                // Send the placement to the server if all ships are now placed
                if (placedCount > 0 && ctx.GameModel.YourShips.All(s => s.IsPlaced))
                {
                    var shipCells = ctx.GameModel.GetAllShipCells();
                    Task.Run(async () => await ctx.GameClient.PlaceShips(shipCells)).Wait();
                    ctx.GameModel.State = AppState.Waiting;
                    ctx.GameModel.CurrentStatus = "Ships placed! Waiting for opponent...";
                    return $"Auto-placed {placedCount}/{unplacedShips.Count} ships successfully. Sent to server.";
                }

                return $"Auto-placed {placedCount}/{unplacedShips.Count} ships successfully.";
            }
            catch (Exception ex)
            {
                return $"Auto-placement failed: {ex.Message}";
            }
        }

        private bool TryAutoPlaceShip(CommandContext ctx, IShip ship)
        {
            const int maxAttempts = 100;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Random position and orientation
                int x = _random.Next(Board.Size);
                int y = _random.Next(Board.Size);
                var orientation = _random.Next(2) == 0 ? ShipOrientation.Horizontal : ShipOrientation.Vertical;

                ship.Orientation = orientation;
                var position = new Point(x, y);

                if (ctx.GameModel.CanPlaceShip(ship, position))
                {
                    ctx.GameModel.PlaceShip(ship, position);
                    return true;
                }
            }

            return false;
        }

        public string GetHelp()
        {
            return "place auto - Automatically place all unplaced ships";
        }
    }
}
