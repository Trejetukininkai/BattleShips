using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BattleShips.Core.Server.Memento
{
    /// <summary>
    /// Caretaker class for managing game mementos.
    /// Stores mementos in memory and provides save/load functionality.
    /// Maps player names to their saved games for reconnection.
    /// </summary>
    public class GameCaretaker
    {
        private static readonly Lazy<GameCaretaker> _instance = new(() => new GameCaretaker());
        public static GameCaretaker Instance => _instance.Value;

        // Store mementos in memory by game ID
        private readonly ConcurrentDictionary<string, GameMemento> _savedGames = new();

        // Map player names to game IDs for reconnection
        private readonly ConcurrentDictionary<string, string> _playerNameToGameId = new();

        // Directory for persistent storage
        private readonly string _savePath;

        private GameCaretaker()
        {
            _savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SavedGames");
            Directory.CreateDirectory(_savePath);
            Console.WriteLine($"[GameCaretaker] Initialized with save path: {_savePath}");
        }

        /// <summary>
        /// Save a game memento
        /// </summary>
        public void SaveGame(GameMemento memento)
        {
            if (memento == null)
            {
                Console.WriteLine("[GameCaretaker] Cannot save null memento");
                return;
            }

            _savedGames[memento.GameId] = memento;

            // Map player names to game ID
            if (!string.IsNullOrEmpty(memento.PlayerAName))
            {
                _playerNameToGameId[memento.PlayerAName] = memento.GameId;
            }
            if (!string.IsNullOrEmpty(memento.PlayerBName))
            {
                _playerNameToGameId[memento.PlayerBName] = memento.GameId;
            }

            Console.WriteLine($"[GameCaretaker] Saved game {memento.GameId} with players: {memento.PlayerAName}, {memento.PlayerBName}");

            // Optionally persist to disk
            PersistToDisk(memento);
        }

        /// <summary>
        /// Retrieve a game memento by game ID
        /// SECURE: Returns a defensive copy to prevent external modification
        /// </summary>
        public GameMemento? GetGame(string gameId)
        {
            if (_savedGames.TryGetValue(gameId, out var memento))
            {
                Console.WriteLine($"[GameCaretaker] Retrieved game {gameId} (returning defensive copy)");
                return CreateDefensiveCopy(memento);
            }

            // Try loading from disk
            var loadedMemento = LoadFromDisk(gameId);
            if (loadedMemento != null)
            {
                _savedGames[gameId] = loadedMemento;
                Console.WriteLine($"[GameCaretaker] Loaded game {gameId} from disk (returning defensive copy)");
                return CreateDefensiveCopy(loadedMemento);
            }

            Console.WriteLine($"[GameCaretaker] Game {gameId} not found");
            return null;
        }

        /// <summary>
        /// Find a saved game by player name
        /// SECURE: Returns a defensive copy to prevent external modification
        /// </summary>
        public GameMemento? GetGameByPlayerName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
                return null;

            if (_playerNameToGameId.TryGetValue(playerName, out var gameId))
            {
                return GetGame(gameId); // Already returns defensive copy
            }

            // Search through all saved games
            foreach (var memento in _savedGames.Values)
            {
                if (memento.PlayerAName == playerName || memento.PlayerBName == playerName)
                {
                    _playerNameToGameId[playerName] = memento.GameId;
                    return CreateDefensiveCopy(memento);
                }
            }

            // Try loading from disk
            var savedFiles = Directory.GetFiles(_savePath, "*.json");
            foreach (var file in savedFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var memento = JsonSerializer.Deserialize<GameMemento>(json);
                    if (memento != null && (memento.PlayerAName == playerName || memento.PlayerBName == playerName))
                    {
                        _savedGames[memento.GameId] = memento;
                        _playerNameToGameId[playerName] = memento.GameId;
                        return CreateDefensiveCopy(memento);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameCaretaker] Error loading file {file}: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Remove a saved game
        /// </summary>
        public void RemoveGame(string gameId)
        {
            if (_savedGames.TryRemove(gameId, out var memento))
            {
                // Remove player name mappings
                if (!string.IsNullOrEmpty(memento.PlayerAName))
                {
                    _playerNameToGameId.TryRemove(memento.PlayerAName, out _);
                }
                if (!string.IsNullOrEmpty(memento.PlayerBName))
                {
                    _playerNameToGameId.TryRemove(memento.PlayerBName, out _);
                }

                // Delete from disk
                DeleteFromDisk(gameId);

                Console.WriteLine($"[GameCaretaker] Removed game {gameId}");
            }
        }

        /// <summary>
        /// Get all saved games
        /// </summary>
        public List<GameMemento> GetAllSavedGames()
        {
            return _savedGames.Values.ToList();
        }

        /// <summary>
        /// Clear all saved games
        /// </summary>
        public void ClearAll()
        {
            _savedGames.Clear();
            _playerNameToGameId.Clear();
            Console.WriteLine("[GameCaretaker] Cleared all saved games");
        }

        /// <summary>
        /// Persist memento to disk
        /// </summary>
        private void PersistToDisk(GameMemento memento)
        {
            try
            {
                var filePath = Path.Combine(_savePath, $"{memento.GameId}.json");
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(memento, options);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"[GameCaretaker] Persisted game {memento.GameId} to disk");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameCaretaker] Failed to persist game {memento.GameId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Load memento from disk
        /// </summary>
        private GameMemento? LoadFromDisk(string gameId)
        {
            try
            {
                var filePath = Path.Combine(_savePath, $"{gameId}.json");
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<GameMemento>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameCaretaker] Failed to load game {gameId} from disk: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Creates a defensive deep copy of a memento using JSON serialization
        /// This prevents external code from modifying the stored memento
        /// </summary>
        private GameMemento CreateDefensiveCopy(GameMemento original)
        {
            try
            {
                // Use JSON serialization for deep cloning
                var json = JsonSerializer.Serialize(original);
                var copy = JsonSerializer.Deserialize<GameMemento>(json);

                if (copy == null)
                {
                    Console.WriteLine($"[GameCaretaker] WARNING: Failed to create defensive copy, returning original");
                    return original;
                }

                return copy;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameCaretaker] ERROR creating defensive copy: {ex.Message}");
                return original;
            }
        }

        /// <summary>
        /// Delete memento from disk
        /// </summary>
        private void DeleteFromDisk(string gameId)
        {
            try
            {
                var filePath = Path.Combine(_savePath, $"{gameId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"[GameCaretaker] Deleted game {gameId} from disk");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameCaretaker] Failed to delete game {gameId} from disk: {ex.Message}");
            }
        }
    }
}
