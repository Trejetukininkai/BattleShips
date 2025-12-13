namespace BattleShips.Core
{
    public interface IExpression
    {
        string Interpret(CommandContext ctx);
        string GetHelp();
    }
}
