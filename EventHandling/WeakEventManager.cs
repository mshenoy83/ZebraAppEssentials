﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AppEssentials.EventHandling
{
    internal class WeakEventManager : IWeakEventManager
    {
        private readonly Dictionary<string, List<Tuple<WeakReference, MethodInfo>>> _eventHandlers
            = new Dictionary<string, List<Tuple<WeakReference, MethodInfo>>>();

        public void AddEventHandler<TEventArgs>(string eventName, EventHandler<TEventArgs> value)
            where TEventArgs : EventArgs
        {
            BuildEventHandler(eventName, value.Target, value.GetMethodInfo());
        }

        public void AddEventHandler(string eventName, EventHandler value)
        {
            BuildEventHandler(eventName, value.Target, value.GetMethodInfo());
        }

        private void BuildEventHandler(string eventName, object handlerTarget, MethodInfo methodInfo)
        {
            List<Tuple<WeakReference, MethodInfo>> target;
            if (!_eventHandlers.TryGetValue(eventName, out target))
            {
                target = new List<Tuple<WeakReference, MethodInfo>>();
                _eventHandlers.Add(eventName, target);
            }

            target.Add(Tuple.Create(new WeakReference(handlerTarget), methodInfo));
        }

        public void HandleEvent(object sender, object args, string eventName)
        {
            var toRaise = new List<Tuple<object, MethodInfo>>();

            List<Tuple<WeakReference, MethodInfo>> target;
            if (_eventHandlers.TryGetValue(eventName, out target))
            {
                foreach (var tuple in target.ToList())
                {
                    var o = tuple.Item1.Target;

                    if (o == null)
                        target.Remove(tuple);
                    else
                        toRaise.Add(Tuple.Create(o, tuple.Item2));
                }
            }

            foreach (var tuple in toRaise)
                tuple.Item2.Invoke(tuple.Item1, new[] { sender, args });
        }

        public void RemoveEventHandler<TEventArgs>(string eventName, EventHandler<TEventArgs> value)
    where TEventArgs : EventArgs
        {
            RemoveEventHandlerImpl(eventName, value.Target, value.GetMethodInfo());
        }

        public void RemoveEventHandler(string eventName, EventHandler value)
        {
            RemoveEventHandlerImpl(eventName, value.Target, value.GetMethodInfo());
        }

        private void RemoveEventHandlerImpl(string eventName, object handlerTarget, MemberInfo methodInfo)
        {
            List<Tuple<WeakReference, MethodInfo>> target;
            if (_eventHandlers.TryGetValue(eventName, out target))
            {
                foreach (var tuple in target.Where(t => t.Item1.Target == handlerTarget &&
                    t.Item2.Name == methodInfo.Name).ToList())
                    target.Remove(tuple);
            }
        }
    }
}
