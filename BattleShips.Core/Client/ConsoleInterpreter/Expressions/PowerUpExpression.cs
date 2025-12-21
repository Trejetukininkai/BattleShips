using System;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class PowerUpExpression : IExpression
    {
        private string _powerUpName;

        public PowerUpExpression(string powerUpName)
        {
            _powerUpName = powerUpName;
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
                    return $"Cannot use power-ups now. Current state: {ctx.GameModel.State}";
                }

                // Find the power-up
                var powerUp = ctx.GameModel.AvailablePowerUps.FirstOrDefault(p =>
                    p.Name.Equals(_powerUpName, StringComparison.OrdinalIgnoreCase));

                if (powerUp == null)
                {
                    return $"Unknown power-up: {_powerUpName}. Available: {string.Join(", ", ctx.GameModel.AvailablePowerUps.Select(p => p.Name))}";
                }

                // Check if can activate
                if (!ctx.GameModel.CanActivatePowerUp(powerUp))
                {
                    return $"Cannot activate {powerUp.Name}. Cost: {powerUp.Cost} AP, You have: {ctx.GameModel.ActionPoints} AP";
                }

                // Activate via server
                Task.Run(async () => await ctx.GameClient.ActivatePowerUp(_powerUpName)).Wait();

                return $"Activated power-up: {_powerUpName}";
            }
            catch (Exception ex)
            {
                return $"Power-up activation failed: {ex.Message}";
            }
        }

        public string GetHelp()
        {
            return @"powerup <name> - Activate a power-up
Available power-ups:
  - MiniNuke (Cost: 5 AP)
  - Repair (Cost: 3 AP)
  - ForceDisaster (Cost: 4 AP)";
        }
    }
}
