using System;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class ConnectExpression : IExpression
    {
        private string _url;

        public ConnectExpression(string url)
        {
            _url = url;
        }

        public string Interpret(CommandContext ctx)
        {
            try
            {
                if (ctx.GameClient.IsConnected)
                {
                    return "Already connected to server. Disconnect first before connecting to a new server.";
                }

                // Connect asynchronously
                Task.Run(async () => await ctx.GameClient.ConnectAsync(_url)).Wait();

                // Update game state to indicate we're connected and waiting
                ctx.GameModel.State = AppState.Waiting;
                ctx.GameModel.CurrentStatus = "Connected - waiting for opponent";

                return $"Successfully connected to {_url}";
            }
            catch (Exception ex)
            {
                return $"Connection failed: {ex.Message}";
            }
        }

        public string GetHelp()
        {
            return "connect <url> - Connect to game server (e.g., connect http://localhost:5000)";
        }
    }
}
