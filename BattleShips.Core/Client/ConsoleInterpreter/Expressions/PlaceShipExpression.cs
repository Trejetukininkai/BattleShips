using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class PlaceShipExpression : IExpression
    {
        private string _shipType;
        private int _x;
        private int _y;
        private ShipOrientation _orientation;

        public PlaceShipExpression(string[] args)
        {
            if (args.Length < 4)
            {
                throw new ArgumentException("Usage: place ship <type> <x> <y> <H|V> (e.g., place ship Carrier A 1 H)");
            }

            _shipType = args[0];

            // Parse X coordinate (A-J)
            string xInput = args[1].ToUpper();
            if (xInput.Length == 1 && xInput[0] >= 'A' && xInput[0] <= 'J')
            {
                _x = xInput[0] - 'A'; // Convert A-J to 0-9
            }
            else
            {
                throw new ArgumentException("X coordinate must be a letter A-J");
            }

            // Parse Y coordinate (1-10)
            if (!int.TryParse(args[2], out _y) || _y < 1 || _y > 10)
            {
                throw new ArgumentException("Y coordinate must be a number 1-10");
            }
            _y = _y - 1; // Convert 1-10 to 0-9

            string orientStr = args[3].ToUpper();
            _orientation = orientStr == "H" ? ShipOrientation.Horizontal : ShipOrientation.Vertical;
        }

        public string Interpret(CommandContext ctx)
        {
            try
            {
                if (ctx.GameModel.State != AppState.Placement)
                {
                    return $"Cannot place ships now. Current state: {ctx.GameModel.State}";
                }

                // Normalize ship type aliases
                var normalizedType = NormalizeShipType(_shipType);

                // Find the ship by type name
                var ship = ctx.GameModel.YourShips.FirstOrDefault(s =>
                    s.GetType().Name.Equals(normalizedType, StringComparison.OrdinalIgnoreCase) && !s.IsPlaced);

                if (ship == null)
                {
                    return $"No unplaced ship of type '{_shipType}' found. Available types: AircraftCarrier (or Carrier), Battleship, Cruiser, Destroyer";
                }

                // Set orientation
                ship.Orientation = _orientation;

                // Try to place the ship
                var position = new Point(_x, _y);
                char col = (char)('A' + _x);
                int row = _y + 1;

                if (!ctx.GameModel.CanPlaceShip(ship, position))
                {
                    return $"Cannot place {_shipType} at {col}{row}. Invalid position or collision.";
                }

                ctx.GameModel.PlaceShip(ship, position);

                // Check if all ships are now placed and send to server if so
                if (ctx.GameModel.YourShips.All(s => s.IsPlaced))
                {
                    var shipCells = ctx.GameModel.GetAllShipCells();
                    Task.Run(async () => await ctx.GameClient.PlaceShips(shipCells)).Wait();
                    ctx.GameModel.State = AppState.Waiting;
                    ctx.GameModel.CurrentStatus = "Ships placed! Waiting for opponent...";
                    return $"Placed {_shipType} at {col}{row} facing {_orientation}. All ships placed! Sent to server.";
                }

                return $"Placed {_shipType} at {col}{row} facing {_orientation}";
            }
            catch (Exception ex)
            {
                return $"Failed to place ship: {ex.Message}";
            }
        }

        private static string NormalizeShipType(string shipType)
        {
            // Map common aliases to actual class names
            return shipType.ToLower() switch
            {
                "carrier" => "AircraftCarrier",
                "aircraftcarrier" => "AircraftCarrier",
                "battleship" => "Battleship",
                "cruiser" => "Cruiser",
                "destroyer" => "Destroyer",
                "submarine" => "Cruiser",  // Alias for compatibility
                "patrolboat" => "Destroyer", // Alias for compatibility
                "patrol" => "Destroyer",
                _ => shipType // Return original if no match
            };
        }

        public string GetHelp()
        {
            return "place ship <type> <x> <y> <H|V> - Place a ship (H=Horizontal, V=Vertical)\n" +
                   "                                    Types: Carrier, Battleship, Cruiser, Destroyer\n" +
                   "                                    Fleet: 1x Carrier(5), 1x Battleship(4), 2x Cruiser(3), 1x Destroyer(2)\n" +
                   "                                    X: A-J (columns), Y: 1-10 (rows)\n" +
                   "                                    Example: place ship Carrier A 1 H";
        }
    }
}
