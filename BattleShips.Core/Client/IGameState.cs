using System.Drawing;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: Interface for game state objects.
    /// Each state encapsulates behavior specific to that game phase.
    /// </summary>
    public interface IGameState
    {
        /// <summary>
        /// Gets the state type (for backward compatibility with AppState enum)
        /// </summary>
        AppState StateType { get; }

        /// <summary>
        /// Called when entering this state
        /// </summary>
        void OnEnter(GameModel model);

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        void OnExit(GameModel model);

        /// <summary>
        /// Checks if ship placement is allowed in this state
        /// </summary>
        bool CanPlaceShip(GameModel model);

        /// <summary>
        /// Checks if mine placement is allowed in this state
        /// </summary>
        bool CanPlaceMine(GameModel model);

        /// <summary>
        /// Checks if firing is allowed in this state
        /// </summary>
        bool CanFire(GameModel model);

        /// <summary>
        /// Checks if power-up activation is allowed in this state
        /// </summary>
        bool CanActivatePowerUp(GameModel model);

        /// <summary>
        /// Gets the default status message for this state
        /// </summary>
        string GetStatusMessage(GameModel model);

        /// <summary>
        /// Handles mouse click on board in this state
        /// </summary>
        void HandleBoardClick(Point cell, GameModel model);

        /// <summary>
        /// Handles ship placement attempt in this state
        /// </summary>
        void HandleShipPlacement(IShip ship, Point position, GameModel model);

        /// <summary>
        /// Handles mine placement attempt in this state
        /// </summary>
        void HandleMinePlacement(Point position, GameModel model);

        /// <summary>
        /// Handles fire action attempt in this state
        /// </summary>
        void HandleFire(Point target, GameModel model);

        /// <summary>
        /// Updates state-specific logic (called each frame/tick)
        /// </summary>
        void Update(GameModel model);
    }
}

