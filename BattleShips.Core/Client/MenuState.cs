using System.Drawing;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: Menu state - initial state when game is not connected
    /// </summary>
    public class MenuState : IGameState
    {
        public AppState StateType => AppState.Menu;

        public void OnEnter(GameModel model)
        {
            model.CurrentStatus = "Ready to start your naval adventure";
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
            "Ready to start your naval adventure";

        public void HandleBoardClick(Point cell, GameModel model)
        {
            // No action in menu state
        }

        public void HandleShipPlacement(IShip ship, Point position, GameModel model)
        {
            // Not allowed in menu state
        }

        public void HandleMinePlacement(Point position, GameModel model)
        {
            // Not allowed in menu state
        }

        public void HandleFire(Point target, GameModel model)
        {
            // Not allowed in menu state
        }

        public void Update(GameModel model)
        {
            // No updates needed in menu state
        }
    }
}

