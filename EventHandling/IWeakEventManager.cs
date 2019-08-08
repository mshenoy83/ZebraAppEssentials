using System;

namespace AppEssentials.EventHandling
{
    public interface IWeakEventManager
    {
        void AddEventHandler(string eventName, EventHandler value);
        void AddEventHandler<TEventArgs>(string eventName, EventHandler<TEventArgs> value) where TEventArgs : EventArgs;
        void HandleEvent(object sender, object args, string eventName);
        void RemoveEventHandler(string eventName, EventHandler value);
        void RemoveEventHandler<TEventArgs>(string eventName, EventHandler<TEventArgs> value) where TEventArgs : EventArgs;
    }
}