namespace BattleShips.Core
{
    public class HelpExpression : IExpression
    {
        public string Interpret(CommandContext ctx)
        {
            return GetHelp();
        }

        public string GetHelp()
        {
            return "=== Available Commands ===\n\n" +
                   "Connection & Info:\n" +
                   "  help - Show this help message\n" +
                   "  quit - Exit the game\n" +
                   "  connect <url> - Connect to game server\n" +
                   "    Example: connect http://localhost:5000\n" +
                   "  status - Show current game status\n\n" +
                   "Ship & Mine Placement:\n" +
                   "  place auto - Auto-place all ships (recommended!)\n" +
                   "  place ship <type> <x> <y> <H|V> - Place a ship manually\n" +
                   "   Types: Carrier, Battleship, Cruiser, Destroyer\n" +
                   "   Fleet: 1x Carrier(5), 1x Battleship(4), 2x Cruiser(3), 1x Destroyer(2)\n" +
                   "    X: A-J (columns), Y: 1-10 (rows), H=Horizontal, V=Vertical\n" +
                   "    Example: place ship Carrier A 1 H\n" +
                   "  place mine <x> <y> - Place a naval mine (max 3)\n" +
                   "    X: A-J (columns), Y: 1-10 (rows)\n" +
                   "    Example: place mine C 5\n" +
                   "  submit - Submit placed mines to server\n\n" +
                   "Combat:\n" +
                   "  fire <x> <y> - Fire at target coordinates\n" +
                   "    X: A-J (columns), Y: 1-10 (rows)\n" +
                   "    Example: fire A 5, fire J 10\n\n" +
                   "Power-ups:\n" +
                   "  powerup <name> - Activate a power-up\n" +
                   "    MiniNuke - Destroy 3x3 area (Cost: 5 AP)\n" +
                   "    Repair - Repair a damaged cell (Cost: 3 AP)\n" +
                   "    ForceDisaster - Trigger immediate disaster (Cost: 4 AP)\n\n" +
                   "Coordinates:\n" +
                   "  Columns: A-J (left to right)\n" +
                   "  Rows: 1-10 (top to bottom)\n" +
                   "  Example: A 1 = top-left, J 10 = bottom-right\n\n" +
                   "==========================";
        }
    }
}
