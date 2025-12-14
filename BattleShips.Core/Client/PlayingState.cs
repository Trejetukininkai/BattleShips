using System.Drawing;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: Playing state - active game in progress
    /// </summary>
    public class PlayingState : IGameState
    {
        public AppState StateType => AppState.Playing;

        public void OnEnter(GameModel model)
        {
            model.CurrentStatus = model.IsMyTurn 
                ? "Your turn - click opponent's board to fire!" 
                : "Opponent's turn";
        }

        public void OnExit(GameModel model)
        {
            // Cleanup if needed
        }

        public bool CanPlaceShip(GameModel model) => false;
        public bool CanPlaceMine(GameModel model) => false;
        public bool CanFire(GameModel model) => model.IsMyTurn;
        public bool CanActivatePowerUp(GameModel model) => model.IsMyTurn && model.State == AppState.Playing;

        public string GetStatusMessage(GameModel model)
        {
            if (model.IsMyTurn)
            {
                if (model.DisasterCountdown > 0)
                {
                    return $"Your turn - Disaster in {model.DisasterCountdown} turns";
                }
                return "Your turn - click opponent's board to fire!";
            }
            else
            {
                return "Opponent's turn";
            }
        }

        public void HandleBoardClick(Point cell, GameModel model)
        {
            // In playing state, clicks on opponent's board trigger fire
            // Actual fire handling is done through HandleFire
        }

        public void HandleShipPlacement(IShip ship, Point position, GameModel model)
        {
            // Not allowed during gameplay
        }

        public void HandleMinePlacement(Point position, GameModel model)
        {
            // Not allowed during gameplay
        }

        public void HandleFire(Point target, GameModel model)
        {
            if (!CanFire(model))
            {
                // Not player's turn or invalid state
                return;
            }

            // Fire action is handled by GameClientController
            // This method just validates that firing is allowed
        }

        public void Update(GameModel model)
        {
            // Update status based on turn and disaster countdown
            if (model.IsMyTurn)
            {
                if (model.DisasterCountdown > 0)
                {
                    model.CurrentStatus = $"Your turn - Disaster in {model.DisasterCountdown} turns";
                }
                else if (model.DisasterCountdown == 0)
                {
                    model.CurrentStatus = "Your turn - Disaster imminent!";
                }
                else
                {
                    model.CurrentStatus = "Your turn - click opponent's board to fire!";
                }
            }
            else
            {
                model.CurrentStatus = "Opponent's turn";
            }
        }
    }
}

