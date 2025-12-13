using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using BattleShips.Core;
namespace BattleShips.Core
{
    public class PlaceMineExpression : IExpression
    {
        private int _x;
        private int _y;

        public PlaceMineExpression(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Usage: place mine <x> <y> (e.g., place mine A 5)");
            }

            // Parse X coordinate (A-J)
            string xInput = args[0].ToUpper();
            if (xInput.Length == 1 && xInput[0] >= 'A' && xInput[0] <= 'J')
            {
                _x = xInput[0] - 'A'; // Convert A-J to 0-9
            }
            else
            {
                throw new ArgumentException("X coordinate must be a letter A-J");
            }

            // Parse Y coordinate (1-10)
            if (!int.TryParse(args[1], out _y) || _y < 1 || _y > 10)
            {
                throw new ArgumentException("Y coordinate must be a number 1-10");
            }
            _y = _y - 1; // Convert 1-10 to 0-9
        }

        public string Interpret(CommandContext ctx)
        {
            try
            {
                if (ctx.GameModel.State != AppState.MineSelection)
                {
                    return $"Cannot place mines now. Current state: {ctx.GameModel.State}";
                }

                var position = new Point(_x, _y);

                // Check if mine already exists at this position
                if (ctx.GameModel.YourMines.Exists(m => m.Position == position))
                {
                    char coll = (char)('A' + _x);
                    int roww = _y + 1;
                    return $"Mine already exists at {coll}{roww}.";
                }

                // Place mine with default category
                ctx.GameModel.SelectedMineCategory = MineCategory.AntiDisaster_Restore;
                ctx.GameModel.PlaceMine(position);

                int mineCount = ctx.GameModel.YourMines.Count;
                char col = (char)('A' + _x);
                int row = _y + 1;
                string result = $"Placed mine at {col}{row}. Total mines: {mineCount}/3";

                // Auto-submit when 3 mines are placed
                if (mineCount >= 3)
                {
                    var minePositions = ctx.GameModel.YourMines.Select(m => m.Position).ToList();
                    var mineCategories = ctx.GameModel.YourMines.Select(m => m.Category.ToString()).ToList();

                    Task.Run(async () => await ctx.GameClient.PlaceMines(minePositions, mineCategories)).Wait();
                    ctx.GameModel.CurrentStatus = "Mines submitted. Waiting for opponent...";

                    result += " All mines placed and sent to server!";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Failed to place mine: {ex.Message}";
            }
        }

        public string GetHelp()
        {
            return "place mine <x> <y> - Place a naval mine at coordinates\n" +
                   "                         X: A-J (columns), Y: 1-10 (rows)\n" +
                   "                         Example: place mine A 5";
        }
    }
}
