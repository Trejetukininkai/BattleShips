using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShips.Core.Server
{
    /// <summary>
    /// Singleton class that manages all game instances and player connections.
    /// Provides thread-safe operations for game state management.
    /// </summary>
    public sealed class GameManager
    {
        // Thread-safe lazy initialization
        private static readonly Lazy<GameManager> _instance = new(() => new GameManager());
        
        /// <summary>
        /// Gets the singleton instance of GameManager
        /// </summary>
        public static GameManager Instance => _instance.Value;

        // Thread-safe collections for managing games and players
        private readonly ConcurrentDictionary<string, GameInstance> _games = new();
        private readonly ConcurrentDictionary<string, string> _playerGame = new(); // connectionId -> gameId
        private readonly object _lock = new();

        // Private constructor to prevent external instantiation
        private GameManager()
        {
            Console.WriteLine("[GameManager] Singleton instance created");
        }

        #region Game Management

        /// <summary>
        /// Gets a game instance by ID
        /// </summary>
        public GameInstance? GetGame(string gameId)
        {
            return _games.TryGetValue(gameId, out var game) ? game : null;
        }

        /// <summary>
        /// Gets the game ID for a specific player connection
        /// </summary>
        public string? GetPlayerGameId(string connectionId)
        {
            return _playerGame.TryGetValue(connectionId, out var gameId) ? gameId : null;
        }

        /// <summary>
        /// Gets the game instance for a specific player connection
        /// </summary>
        public GameInstance? GetPlayerGame(string connectionId)
        {
            var gameId = GetPlayerGameId(connectionId);
            return gameId != null ? GetGame(gameId) : null;
        }

        /// <summary>
        /// Assigns a player to a game (creates new game if needed)
        /// </summary>
        public GameInstance AssignPlayerToGame(string connectionId)
        {
            lock (_lock)
            {
                // Try to find an existing waiting game (has one player and not started)
                var waitingGame = _games.Values.FirstOrDefault(g => !g.HasSecondPlayer && !g.Started);
                
                if (waitingGame == null)
                {
                    // Create new game
                    var gameId = Guid.NewGuid().ToString("N");
                    waitingGame = new GameInstance(gameId)
                    {
                        PlayerA = connectionId,
                        // Initialize game mode so disaster countdown / generator exists
                        GameMode = new GameMode(shipCount: 10, boardX: Board.Size, boardY: Board.Size)
                    };
                    
                    Console.WriteLine($"[GameManager] Created new game {gameId} for player {connectionId}");
                    _games[gameId] = waitingGame;
                }
                else
                {
                    // Join existing game as Player B
                    waitingGame.PlayerB = connectionId;
                    Console.WriteLine($"[GameManager] Player {connectionId} joined game {waitingGame.Id} as Player B");
                }

                // Map player to game
                _playerGame[connectionId] = waitingGame.Id;
                return waitingGame;
            }
        }

        /// <summary>
        /// Removes a player from their current game
        /// </summary>
        public (GameInstance? game, string? gameId) RemovePlayer(string connectionId)
        {
            if (!_playerGame.TryRemove(connectionId, out var gameId))
            {
                return (null, null);
            }

            if (!_games.TryGetValue(gameId, out var game))
            {
                return (null, gameId);
            }

            lock (_lock)
            {
                // Remove player from game
                if (game.PlayerA == connectionId)
                {
                    game.PlayerA = null;
                }
                else if (game.PlayerB == connectionId)
                {
                    game.PlayerB = null;
                }

                // Clean up empty games
                if (!game.HasFirstPlayer && !game.HasSecondPlayer)
                {
                    _games.TryRemove(gameId, out _);
                    Console.WriteLine($"[GameManager] Removed empty game {gameId}");
                }
                else
                {
                    Console.WriteLine($"[GameManager] Player {connectionId} left game {gameId}");
                }
            }

            return (game, gameId);
        }

        /// <summary>
        /// Removes a game completely
        /// </summary>
        public void RemoveGame(string gameId, GameInstance game)
        {
            _games.TryRemove(gameId, out _);
            
            // Remove player mappings
            if (game.PlayerA != null)
            {
                _playerGame.TryRemove(game.PlayerA, out _);
            }
            if (game.PlayerB != null)
            {
                _playerGame.TryRemove(game.PlayerB, out _);
            }
            
            Console.WriteLine($"[GameManager] Completely removed game {gameId}");
        }

        #endregion

        #region Game State Queries

        /// <summary>
        /// Gets all active games
        /// </summary>
        public IEnumerable<GameInstance> GetAllGames()
        {
            return _games.Values.ToList(); // Return a snapshot
        }

        /// <summary>
        /// Gets all waiting games (games with only one player)
        /// </summary>
        public IEnumerable<GameInstance> GetWaitingGames()
        {
            return _games.Values.Where(g => g.HasFirstPlayer && !g.HasSecondPlayer && !g.Started).ToList();
        }

        /// <summary>
        /// Gets all active games (games with two players that have started)
        /// </summary>
        public IEnumerable<GameInstance> GetActiveGames()
        {
            return _games.Values.Where(g => g.HasFirstPlayer && g.HasSecondPlayer && g.Started).ToList();
        }

        /// <summary>
        /// Gets the total number of games
        /// </summary>
        public int TotalGames => _games.Count;

        /// <summary>
        /// Gets the total number of connected players
        /// </summary>
        public int TotalPlayers => _playerGame.Count;

        #endregion

        #region Utility Methods

        /// <summary>
        /// Validates if a player is in a valid game
        /// </summary>
        public bool ValidatePlayerInGame(string connectionId, out GameInstance? game, out string? gameId)
        {
            gameId = GetPlayerGameId(connectionId);
            game = gameId != null ? GetGame(gameId) : null;
            return game != null;
        }

        /// <summary>
        /// Gets game statistics for debugging
        /// </summary>
        public string GetStatistics()
        {
            var waitingGames = GetWaitingGames().Count();
            var activeGames = GetActiveGames().Count();
            
            return $"Total Games: {TotalGames}, Active: {activeGames}, Waiting: {waitingGames}, Players: {TotalPlayers}";
        }

        /// <summary>
        /// Finds a game by any criteria (for debugging/admin purposes)
        /// </summary>
        public GameInstance? FindGame(Func<GameInstance, bool> predicate)
        {
            return _games.Values.FirstOrDefault(predicate);
        }

        #endregion
    }
}
