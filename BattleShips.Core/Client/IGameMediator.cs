using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Mediator interface for communication between GameModel, Form, and SFX Service
    /// </summary>
    public interface IGameMediator
    {
        // Notify Form about game events
        void NotifyForm(string eventType, object data = null);

        // Notify GameModel about UI events
        void NotifyGameModel(string eventType, object data = null);

        // Notify SFX Service about sound events
        void NotifySFX(string soundEvent, object data = null);

        public void SendNotification(string fromComponent, string eventType, object data = null);

        // Register components
        void RegisterForm(object form);
        void RegisterGameModel(GameModel model);
        void RegisterSFXService(object sfxService);
    }
}
