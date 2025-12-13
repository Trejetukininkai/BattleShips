namespace BattleShips.Core
{
    public class CommandContext
    {
        public GameModel GameModel { get; set; }
        public GameClient GameClient { get; set; }

        public CommandContext(GameModel gameModel, GameClient gameClient)
        {
            GameModel = gameModel;
            GameClient = gameClient;
        }
    }
}
