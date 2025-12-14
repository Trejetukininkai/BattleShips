using System;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class SubmitExpression : IExpression
    {
        public string Interpret(CommandContext ctx)
        {
            try
            {
                if (ctx.GameModel.State != AppState.MineSelection)
                {
                    return $"Cannot submit mines now. Current state: {ctx.GameModel.State}";
                }

                if (ctx.GameModel.YourMines.Count == 0)
                {
                    return "No mines to submit. Place at least one mine first.";
                }

                // Send all placed mines to the server
                var minePositions = ctx.GameModel.YourMines.Select(m => m.Position).ToList();
                var mineCategories = ctx.GameModel.YourMines.Select(m => m.Category.ToString()).ToList();

                Task.Run(async () => await ctx.GameClient.PlaceMines(minePositions, mineCategories)).Wait();
                ctx.GameModel.CurrentStatus = "Mines submitted. Waiting for opponent...";

                return $"Submitted {minePositions.Count} mine(s) to server. Waiting for opponent...";
            }
            catch (Exception ex)
            {
                return $"Failed to submit mines: {ex.Message}";
            }
        }

        public string GetHelp()
        {
            return "submit - Submit placed mines to the server (max 3 mines will auto-submit)";
        }
    }
}
