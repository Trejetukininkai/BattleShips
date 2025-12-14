using System;

namespace BattleShips.Core
{
    public class QuitExpression : IExpression
    {
        public string Interpret(CommandContext ctx)
        {
            Environment.Exit(0);
            return "Exiting game...";
        }

        public string GetHelp()
        {
            return "quit - Exit the game";
        }
    }
}
