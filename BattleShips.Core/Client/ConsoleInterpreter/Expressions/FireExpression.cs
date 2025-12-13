using System;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class FireExpression : IExpression
    {
        private int _x;
        private int _y;

        public FireExpression(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public string Interpret(CommandContext ctx)
        {
            try
            {
                if (!ctx.GameClient.IsConnected)
                {
                    return "Not connected to server. Use 'connect <url>' first.";
                }

                if (ctx.GameModel.State != AppState.Playing)
                {
                    return $"Cannot fire now. Current state: {ctx.GameModel.State}";
                }

                if (!ctx.GameModel.IsMyTurn)
                {
                    return "It's not your turn!";
                }

                // Validate coordinates
                if (_x < 0 || _x >= Board.Size || _y < 0 || _y >= Board.Size)
                {
                    return $"Invalid coordinates. Must be between 0 and {Board.Size - 1}.";
                }

                // Send the move
                Task.Run(async () => await ctx.GameClient.MakeMove(_x, _y)).Wait();

                return $"Fired at ({_x}, {_y})!";
            }
            catch (Exception ex)
            {
                return $"Fire failed: {ex.Message}";
            }
        }

        public string GetHelp()
        {
            return "fire <x> <y> - Fire at target coordinates (e.g., fire A 5, fire J 10)\n" +
                   "                X: A-J (columns), Y: 1-10 (rows)";
        }
    }
}
