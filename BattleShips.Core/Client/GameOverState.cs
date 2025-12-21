using System.Drawing;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: GameOver state - game has ended
    /// </summary>
    public class GameOverState : IGameState
    {
        public AppState StateType => AppState.GameOver;

        public void OnEnter(GameModel model)
        {
            model.CurrentStatus = "Game over";
        }

        public void OnExit(GameModel model)
        {
            // Cleanup if needed
        }

        public bool CanPlaceShip(GameModel model) => false;
        public bool CanPlaceMine(GameModel model) => false;
        public bool CanFire(GameModel model) => false;
        public bool CanActivatePowerUp(GameModel model) => false;

        public string GetStatusMessage(GameModel model) => 
            model.CurrentStatus ?? "Game over";

        public void HandleBoardClick(Point cell, GameModel model)
        {
            // No action after game over
        }

        public void HandleShipPlacement(IShip ship, Point position, GameModel model)
        {
            // Not allowed after game over
        }

        public void HandleMinePlacement(Point position, GameModel model)
        {
            // Not allowed after game over
        }

        public void HandleFire(Point target, GameModel model)
        {
            // Not allowed after game over
        }

        public void Update(GameModel model)
        {
            // No updates needed after game over
        }
    }
}

