using System.Drawing;
using System.Linq;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: MineSelection state - player is selecting and placing mines
    /// </summary>
    public class MineSelectionState : IGameState
    {
        public AppState StateType => AppState.MineSelection;

        public void OnEnter(GameModel model)
        {
            model.CurrentStatus = "Place your mine(s) on your board";
        }

        public void OnExit(GameModel model)
        {
            model.SelectedMineCategory = null;
        }

        public bool CanPlaceShip(GameModel model) => false;
        public bool CanPlaceMine(GameModel model) => true;
        public bool CanFire(GameModel model) => false;
        public bool CanActivatePowerUp(GameModel model) => false;

        public string GetStatusMessage(GameModel model)
        {
            if (model.SelectedMineCategory != null)
            {
                return $"Place {model.SelectedMineCategory} mine on your board";
            }
            return "Select a mine category, then place it on your board";
        }

        public void HandleBoardClick(Point cell, GameModel model)
        {
            // In mine selection, clicks on own board place mines
            if (CanPlaceMine(model))
            {
                HandleMinePlacement(cell, model);
            }
        }

        public void HandleShipPlacement(IShip ship, Point position, GameModel model)
        {
            // Not allowed during mine placement
        }

        public void HandleMinePlacement(Point position, GameModel model)
        {
            if (!CanPlaceMine(model))
            {
                return;
            }

            if (model.SelectedMineCategory == null)
            {
                model.CurrentStatus = "Please select a mine category first";
                return;
            }

            // Check if cell already has a mine
            if (model.YourMines.Any(m => m.Position == position))
            {
                model.CurrentStatus = "Cell already has a mine";
                return;
            }

            // Call internal method to actually place the mine
            model.PlaceMineInternal(position);
            model.CurrentStatus = $"Mine placed. Select another category to place more mines.";
        }

        public void HandleFire(Point target, GameModel model)
        {
            // Not allowed during mine placement
        }

        public void Update(GameModel model)
        {
            // Update status based on selection
            if (model.SelectedMineCategory != null)
            {
                model.CurrentStatus = $"Place {model.SelectedMineCategory} mine on your board";
            }
        }
    }
}

