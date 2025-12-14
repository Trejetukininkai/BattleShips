using System.Drawing;
using BattleShips.Core;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: Placement state - player is placing ships on the board
    /// </summary>
    public class PlacementState : IGameState
    {
        public AppState StateType => AppState.Placement;

        public void OnEnter(GameModel model)
        {
            var placedCount = model.YourShips.Count(s => s.IsPlaced);
            var totalCount = model.YourShips.Count;
            model.CurrentStatus = $"Placement: drag ships from palette below ({placedCount}/{totalCount})";
        }

        public void OnExit(GameModel model)
        {
            // Cleanup if needed
        }

        public bool CanPlaceShip(GameModel model) => true;
        public bool CanPlaceMine(GameModel model) => false;
        public bool CanFire(GameModel model) => false;
        public bool CanActivatePowerUp(GameModel model) => false;

        public string GetStatusMessage(GameModel model)
        {
            var placedCount = model.YourShips.Count(s => s.IsPlaced);
            var totalCount = model.YourShips.Count;
            
            if (model.PlacementSecondsLeft > 0)
            {
                return $"Placement: {placedCount}/{totalCount} ships placed ({model.PlacementSecondsLeft}s remaining)";
            }
            
            return $"Placement: drag ships from palette below ({placedCount}/{totalCount})";
        }

        public void HandleBoardClick(Point cell, GameModel model)
        {
            // In placement, clicks might be used for ship placement
            // Actual handling is done through drag-and-drop
        }

        public void HandleShipPlacement(IShip ship, Point position, GameModel model)
        {
            if (CanPlaceShip(model) && model.CanPlaceShip(ship, position))
            {
                // Use internal method to actually place the ship
                model.PlaceShipInternal(ship, position);
                
                // Update status message
                var placedCount = model.YourShips.Count(s => s.IsPlaced);
                var totalCount = model.YourShips.Count;
                model.CurrentStatus = $"Placed {placedCount}/{totalCount} ships";
            }
        }

        public void HandleMinePlacement(Point position, GameModel model)
        {
            // Not allowed during ship placement
        }

        public void HandleFire(Point target, GameModel model)
        {
            // Not allowed during placement
        }

        public void Update(GameModel model)
        {
            // Update placement timer if needed
            // Timer logic is handled in Form1, but we can update status here
            if (model.PlacementSecondsLeft > 0)
            {
                var placedCount = model.YourShips.Count(s => s.IsPlaced);
                var totalCount = model.YourShips.Count;
                model.CurrentStatus = $"Placement: {placedCount}/{totalCount} ships ({model.PlacementSecondsLeft}s)";
            }
        }
    }
}

