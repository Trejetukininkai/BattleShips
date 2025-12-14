using System.Linq;

namespace BattleShips.Core
{
    public class StatusExpression : IExpression
    {
        public string Interpret(CommandContext ctx)
        {
            var model = ctx.GameModel;
            var client = ctx.GameClient;

            var status = "=== Game Status ===\n" +
                        $"Connection: {(client.IsConnected ? "Connected" : "Disconnected")}\n" +
                        $"State: {model.State}\n" +
                        $"Current Status: {model.CurrentStatus}\n" +
                        $"Turn: {(model.IsMyTurn ? "Your Turn" : "Opponent's Turn")}\n" +
                        $"Action Points: {model.ActionPoints}\n\n" +
                        $"Ships Placed: {model.YourShips.Count(s => s.IsPlaced)}/{model.YourShips.Count}\n" +
                        $"Mines Placed: {model.YourMines.Count}\n\n" +
                        $"Your Hits on Opponent: {model.YourFiredHits.Count}\n" +
                        $"Opponent Hits on You: {model.YourHitsByOpponent.Count}\n\n" +
                        $"Disaster Countdown: {(model.DisasterCountdown >= 0 ? model.DisasterCountdown.ToString() : "N/A")}\n" +
                        "==================";

            return status;
        }

        public string GetHelp()
        {
            return "status - Show current game status";
        }
    }
}
