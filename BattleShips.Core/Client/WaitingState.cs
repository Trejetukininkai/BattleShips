using System.Drawing;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: Waiting state - waiting for opponent or server response
    /// </summary>
    public class WaitingState : IGameState
    {
        public AppState StateType => AppState.Waiting;

        public void OnEnter(GameModel model)
        {
            // Status message is usually set by the event that triggered this state
            if (string.IsNullOrEmpty(model.CurrentStatus))
            {
                model.CurrentStatus = "Waiting...";
            }
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
            model.CurrentStatus ?? "Waiting...";

        public void HandleBoardClick(Point cell, GameModel model)
        {
            // No action while waiting
        }

        public void HandleShipPlacement(IShip ship, Point position, GameModel model)
        {
            // Not allowed while waiting
        }

        public void HandleMinePlacement(Point position, GameModel model)
        {
            // Not allowed while waiting
        }

        public void HandleFire(Point target, GameModel model)
        {
            // Not allowed while waiting
        }

        public void Update(GameModel model)
        {
            // No updates needed while waiting
        }
    }
}

