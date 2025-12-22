using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Concrete mediator that coordinates communication between components
    /// </summary>
    public sealed class GameMediator : IGameMediator
    {
        private GameModel _gameModel;
        private object _form;
        private object _sfxService;
        private readonly Dictionary<string, Action<object>> _eventHandlers;

        public GameMediator()
        {
            _eventHandlers = new Dictionary<string, Action<object>>();
            InitializeDefaultHandlers();
        }

        private void InitializeDefaultHandlers()
        {
            // Default event handlers for common events
            _eventHandlers["GameStarted"] = data => NotifyForm("GameStarted", data);
            _eventHandlers["ShipHit"] = data => NotifySFX("PlayHitSound", data);
            _eventHandlers["ShipMissed"] = data => NotifySFX("PlayMissSound", data);
            _eventHandlers["ShipDestroyed"] = data =>
            {
                NotifyForm("ShipDestroyed", data);
                NotifySFX("PlayDestroySound", data);
            };
            _eventHandlers["GameOver"] = data =>
            {
                NotifyForm("GameOver", data);
                NotifySFX("PlayGameOverSound", data);
            };
        }

        public void RegisterForm(object form)
        {
            _form = form;
            Console.WriteLine("[Mediator] Form registered");
        }

        public void RegisterGameModel(GameModel model)
        {
            _gameModel = model;
            Console.WriteLine("[Mediator] GameModel registered");

            // Subscribe to GameModel events
            if (_gameModel != null)
            {
                // Note: In a real implementation, you'd wire up events here
                // For now, we'll handle them through method calls
            }
        }

        public void RegisterSFXService(object sfxService)
        {
            _sfxService = sfxService;
            Console.WriteLine("[Mediator] SFX Service registered");
        }

        public void NotifyForm(string eventType, object data = null)
        {
            Console.WriteLine($"[Mediator] Notifying Form: {eventType}");

            if (_form == null)
            {
                Console.WriteLine("[Mediator] Warning: Form not registered");
                return;
            }

            // In a real implementation, you would call specific methods on the form
            // For now, we'll log the notification
            switch (eventType)
            {
                case "GameStarted":
                    Console.WriteLine($"[Mediator→Form] Game started with data: {data}");
                    break;
                case "ShipDestroyed":
                    Console.WriteLine($"[Mediator→Form] Ship destroyed: {data}");
                    break;
                case "GameOver":
                    Console.WriteLine($"[Mediator→Form] Game over: {data}");
                    break;
                default:
                    Console.WriteLine($"[Mediator→Form] Unknown event: {eventType}");
                    break;
            }
        }

        public void NotifyGameModel(string eventType, object data = null)
        {
            Console.WriteLine($"[Mediator] Notifying GameModel: {eventType}");

            if (_gameModel == null)
            {
                Console.WriteLine("[Mediator] Warning: GameModel not registered");
                return;
            }

            // Handle events from Form to GameModel
            switch (eventType)
            {
                case "CellClicked":
                    if (data is Tuple<int, int> coordinates)
                    {
                        var (x, y) = coordinates;
                        Console.WriteLine($"[Mediator→GameModel] Cell clicked at ({x},{y})");
                        // In real implementation: _gameModel.ProcessCellClick(x, y);
                    }
                    break;
                case "ShipPlaced":
                    Console.WriteLine($"[Mediator→GameModel] Ships placed");
                    // In real implementation: _gameModel.ShipsPlaced(data as List<Point>);
                    break;
                default:
                    Console.WriteLine($"[Mediator→GameModel] Unknown event: {eventType}");
                    break;
            }
        }

        public void NotifySFX(string soundEvent, object data = null)
        {
            Console.WriteLine($"[Mediator] Notifying SFX Service: {soundEvent}");

            if (_sfxService == null)
            {
                Console.WriteLine("[Mediator] Warning: SFX Service not registered");
                return;
            }

            // In a real implementation, you would call specific methods on the SFX service
            switch (soundEvent)
            {
                case "PlayHitSound":
                    Console.WriteLine("[Mediator→SFX] Playing hit sound");
                    break;
                case "PlayMissSound":
                    Console.WriteLine("[Mediator→SFX] Playing miss sound");
                    break;
                case "PlayDestroySound":
                    Console.WriteLine("[Mediator→SFX] Playing destroy sound");
                    break;
                case "PlayGameOverSound":
                    Console.WriteLine("[Mediator→SFX] Playing game over sound");
                    break;
                default:
                    Console.WriteLine($"[Mediator→SFX] Unknown sound event: {soundEvent}");
                    break;
            }
        }

        // Helper method to send notifications through the chain
        public void SendNotification(string fromComponent, string eventType, object data = null)
        {
            Console.WriteLine($"[Mediator] {fromComponent} → {eventType}");

            // Handle the event based on its type
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType](data);
            }
            else
            {
                // Default routing based on event type
                if (eventType.StartsWith("Play"))
                {
                    NotifySFX(eventType, data);
                }
                else if (eventType.Contains("Click") || eventType.Contains("Place"))
                {
                    NotifyGameModel(eventType, data);
                }
                else
                {
                    NotifyForm(eventType, data);
                }
            }
        }
    }
}
