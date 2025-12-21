using System.Drawing;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// VISITOR PATTERN: Interface for visitors that perform operations on game elements.
    /// Allows adding new operations without modifying element classes.
    /// </summary>
    public interface IGameElementVisitor
    {
        /// <summary>
        /// Visits a ship element
        /// </summary>
        void VisitShip(IShip ship, GameModel model, Rectangle boardRect);

        /// <summary>
        /// Visits a mine element
        /// </summary>
        void VisitMine(NavalMine mine, GameModel model, Rectangle boardRect);

        /// <summary>
        /// Visits a hit marker (opponent's shot on your board)
        /// </summary>
        void VisitOpponentHit(Point hitCell, bool isShipHit, GameModel model, Rectangle boardRect);

        /// <summary>
        /// Visits a fired shot marker (your shot on opponent's board)
        /// </summary>
        void VisitFiredShot(Point shotCell, bool isHit, GameModel model, Rectangle boardRect);

        /// <summary>
        /// Visits an animated disaster cell
        /// </summary>
        void VisitAnimatedCell(Point cell, GameModel model, Rectangle boardRect);

        /// <summary>
        /// Visits a dragged ship preview
        /// </summary>
        void VisitDraggedShip(IShip ship, bool isValid, GameModel model, Rectangle boardRect);
    }
}

