using System;

namespace BattleShips.Core
{
    public class PlaceExpression : IExpression
    {
        private string _subCommand;
        private string[] _args;

        public PlaceExpression(string subCommand, string[] args)
        {
            _subCommand = subCommand;
            _args = args;
        }

        public string Interpret(CommandContext ctx)
        {
            try
            {
                switch (_subCommand.ToLower())
                {
                    case "ship":
                        return new PlaceShipExpression(_args).Interpret(ctx);
                    case "mine":
                        return new PlaceMineExpression(_args).Interpret(ctx);
                    case "auto":
                        return new PlaceAutoExpression().Interpret(ctx);
                    default:
                        return $"Unknown place subcommand: {_subCommand}. Use: place ship|mine|auto";
                }
            }
            catch (Exception ex)
            {
                return $"Place command failed: {ex.Message}";
            }
        }

        public string GetHelp()
        {
            return @"place <ship|mine|auto> [args] - Place ships or mines
  place ship <type> <x> <y> <H|V> - Place a ship
  place mine <x> <y>               - Place a naval mine
  place auto                       - Auto-place all ships";
        }
    }
}
