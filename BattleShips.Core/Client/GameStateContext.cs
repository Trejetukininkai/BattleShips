using System;
using System.Collections.Generic;
using System.Drawing;
using BattleShips.Core;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// STATE PATTERN: Context class that manages state transitions and delegates behavior to current state.
    /// </summary>
    public class GameStateContext
    {
        private readonly Dictionary<AppState, IGameState> _states = new();
        private IGameState? _currentState;
        private GameModel? _model;

        public GameStateContext(GameModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            
            // Initialize all states
            _states[AppState.Menu] = new MenuState();
            _states[AppState.Waiting] = new WaitingState();
            _states[AppState.Placement] = new PlacementState();
            _states[AppState.Playing] = new PlayingState();
            _states[AppState.GameOver] = new GameOverState();
            _states[AppState.MineSelection] = new MineSelectionState();

            // Set initial state
            TransitionTo(AppState.Menu);
        }

        /// <summary>
        /// Gets the current state type
        /// </summary>
        public AppState CurrentStateType => _currentState?.StateType ?? AppState.Menu;

        /// <summary>
        /// Gets the current state instance
        /// </summary>
        public IGameState? CurrentState => _currentState;

        /// <summary>
        /// Transitions to a new state
        /// </summary>
        public void TransitionTo(AppState newState)
        {
            if (!_states.TryGetValue(newState, out var state))
            {
                throw new ArgumentException($"Unknown state: {newState}");
            }

            if (_currentState != null && _currentState.StateType == newState)
            {
                return; // Already in this state
            }

            _currentState?.OnExit(_model!);
            _currentState = state;
            _currentState.OnEnter(_model!);
        }

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public bool CanPlaceShip() => _currentState?.CanPlaceShip(_model!) ?? false;

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public bool CanPlaceMine() => _currentState?.CanPlaceMine(_model!) ?? false;

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public bool CanFire() => _currentState?.CanFire(_model!) ?? false;

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public bool CanActivatePowerUp() => _currentState?.CanActivatePowerUp(_model!) ?? false;

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public string GetStatusMessage() => _currentState?.GetStatusMessage(_model!) ?? "";

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public void HandleBoardClick(Point cell) => _currentState?.HandleBoardClick(cell, _model!);

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public void HandleShipPlacement(IShip ship, Point position) => 
            _currentState?.HandleShipPlacement(ship, position, _model!);

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public void HandleMinePlacement(Point position) => 
            _currentState?.HandleMinePlacement(position, _model!);

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public void HandleFire(Point target) => _currentState?.HandleFire(target, _model!);

        /// <summary>
        /// Delegates to current state
        /// </summary>
        public void Update() => _currentState?.Update(_model!);
    }
}

