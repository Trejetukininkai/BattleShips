using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{

    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    public class PlaceShipCommand : ICommand
    {
        private readonly GameModel _game;
        private readonly IShip _ship;
        private readonly Point _position;
        private Point _previousPosition;
        private bool _wasPlacedBefore;

        public PlaceShipCommand(GameModel game, IShip ship, Point position)
        {
            _game = game;
            _ship = ship;
            _position = position;
        }

        public void Execute()
        {
            // Store old state for undo
            _previousPosition = _ship.Position;
            _wasPlacedBefore = _ship.IsPlaced;

            if (_game.CanPlaceShip(_ship, _position))
            {
                _ship.Position = _position;
                _ship.IsPlaced = true;
                if (!_game.YourShips.Contains(_ship))
                    _game.YourShips.Add(_ship);
            }

            _game.CurrentStatus = $"Placed ship at {_position}.";
            _game.OnModelPropertyChanged(nameof(_game.YourShips));
        }

        public void Undo()
        {
            _ship.IsPlaced = _wasPlacedBefore;
            _ship.Position = _previousPosition;
            _game.CurrentStatus = $"Undid placement of last ship.";
            _game.OnModelPropertyChanged(nameof(_game.YourShips));
        }
    }
}
