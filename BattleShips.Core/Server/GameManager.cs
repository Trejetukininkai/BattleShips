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
    /// Now supports proxy pattern for games.
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
        private readonly ConcurrentDictionary<string, VirtualProxy> _virtualProxies = new(); // Track virtual proxies
        private readonly ConcurrentDictionary<string, GameInstanceProxy> _activeProxies = new(); // Active proxies (Security/Logging)
        private readonly ConcurrentDictionary<string, string> _playerGame = new(); // connectionId -> gameId
        private readonly ConcurrentDictionary<string, string> _proxyTypes = new(); // gameId -> proxy type for logging
        private readonly object _lock = new();
        private readonly Random _random = new();

        // Private constructor to prevent external instantiation
        private GameManager()
        {
            Console.WriteLine("[GameManager] Singleton instance created");
        }

        #region Game Management

        /// <summary>
        /// Gets a game instance by ID - returns proxy if available, otherwise raw instance
        /// </summary>
        public GameInstance? GetGame(string gameId)
        {
            // Try to get the active proxy first (LoggingProxy or SecurityProxy)
            if (_activeProxies.TryGetValue(gameId, out var proxy))
            {
                return proxy.GetWrappedInstance(); // Return the wrapped instance through the proxy
            }

            // Fall back to raw game instance
            return _games.TryGetValue(gameId, out var game) ? game : null;
        }

        /// <summary>
        /// Gets the proxy wrapper for a game by ID (for use by server code)
        /// </summary>
        public GameInstanceProxy? GetGameProxy(string gameId)
        {
            // Return the active proxy if available
            if (_activeProxies.TryGetValue(gameId, out var proxy))
            {
                return proxy;
            }

            // No proxy available, return null
            return null;
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
        /// Gets the proxy wrapper for a player's game (for use by server code to enable logging)
        /// </summary>
        public GameInstanceProxy? GetPlayerGameProxy(string connectionId)
        {
            var gameId = GetPlayerGameId(connectionId);
            return gameId != null ? GetGameProxy(gameId) : null;
        }

        /// <summary>
        /// Assigns a player to a game (creates new game if needed)
        /// Uses VirtualProxy initially, then wraps with SecurityProxy or LoggingProxy when both players join
        /// </summary>
        public GameInstance AssignPlayerToGame(string connectionId)
        {
            lock (_lock)
            {
                // Try to find an existing waiting game (has one player and not started)
                var waitingGame = _games.Values.FirstOrDefault(g => !g.HasSecondPlayer && !g.Started);

                if (waitingGame == null)
                {
                    // Create new game using VirtualProxy for lazy initialization
                    var gameId = Guid.NewGuid().ToString("N");
                    var virtualProxy = new VirtualProxy(gameId);
                    virtualProxy.PlayerA = connectionId;

                    // Get the wrapped GameInstance to initialize game mode
                    var wrappedGame = virtualProxy.GetWrappedInstance();
                    wrappedGame.GameMode = new GameMode(shipCount: 10, boardX: Board.Size, boardY: Board.Size);

                    Console.WriteLine($"[GameManager] Created VirtualProxy game {gameId} for player {connectionId}");
                    _games[gameId] = wrappedGame;
                    _virtualProxies[gameId] = virtualProxy;
                    _proxyTypes[gameId] = "VirtualProxy";

                    waitingGame = wrappedGame;
                }
                else
                {
                    // Join existing game as Player B
                    waitingGame.PlayerB = connectionId;
                    var gameId = waitingGame.Id;

                    Console.WriteLine($"[GameManager] Player {connectionId} joined game {gameId} as Player B");

                    // Both players connected - randomly choose Security or Logging proxy
                    if (_virtualProxies.TryGetValue(gameId, out var virtualProxy))
                    {
                        // Trigger VirtualProxy initialization by setting PlayerB
                        virtualProxy.PlayerB = connectionId;

                        // 50/50 chance to wrap with SecurityProxy or LoggingProxy
                        bool useSecurity = _random.Next(2) == 0;
                        // bool useSecurity = false;
                        // bool useSecurity = true;

                        if (useSecurity)
                        {
                            // Wrap with SecurityProxy for PlayerA
                            var securityProxyA = new SecurityProxy(waitingGame, waitingGame.PlayerA!);
                            _activeProxies[gameId] = securityProxyA;
                            Console.WriteLine($"[GameManager] Wrapped game {gameId} with SecurityProxy");
                            _proxyTypes[gameId] = "VirtualProxy → SecurityProxy";
                        }
                        else
                        {
                            // Wrap with LoggingProxy - save to gameLogs folder
                            var loggingProxy = new LoggingProxy(waitingGame, $"gameLogs/game_{gameId}_log.txt");
                            _activeProxies[gameId] = loggingProxy;

                            // IMPORTANT: Enable logging on the GameInstance itself
                            waitingGame.EnableLogging($"gameLogs/game_{gameId}_log.txt");

                            Console.WriteLine($"[GameManager] Wrapped game {gameId} with LoggingProxy (logging to gameLogs/game_{gameId}_log.txt)");
                            _proxyTypes[gameId] = "VirtualProxy → LoggingProxy";
                        }
                    }
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
        /// Validates if a player is in a valid game and returns the proxy wrapper for logging
        /// Use this instead of ValidatePlayerInGame when you want enhanced logging
        /// </summary>
        public bool ValidatePlayerInGameWithProxy(string connectionId, out GameInstance? game, out GameInstanceProxy? proxy, out string? gameId)
        {
            gameId = GetPlayerGameId(connectionId);
            game = gameId != null ? GetGame(gameId) : null;
            proxy = gameId != null ? GetGameProxy(gameId) : null;
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

        /// <summary>
        /// Gets the proxy type being used for a game (for debugging)
        /// </summary>
        public string GetProxyType(string gameId)
        {
            return _proxyTypes.TryGetValue(gameId, out var type) ? type : "None";
        }

        /// <summary>
        /// Gets the active proxy for a game (if exists), otherwise returns null
        /// </summary>
        public GameInstanceProxy? GetActiveProxy(string gameId)
        {
            return _activeProxies.TryGetValue(gameId, out var proxy) ? proxy : null;
        }

        #endregion
    }
}
