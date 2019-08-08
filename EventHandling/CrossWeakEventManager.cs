using System;
using System.Threading;

namespace AppEssentials.EventHandling
{
    public class CrossWeakEventManager
    {
        static Lazy<IWeakEventManager> implementation = new Lazy<IWeakEventManager>(() => CreateWeakEventManager(), LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Current settings to use
        /// </summary>
        public static IWeakEventManager Current
        {
            get
            {
                return implementation.Value;
            }
        }

        static IWeakEventManager CreateWeakEventManager()
        {
            return new WeakEventManager();
        }

    }
}
