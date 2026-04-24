using UnityEngine;

namespace Entitas.Unity
{
    public static class ContextObserverExtension
    {
        public static void CreateContextObserver(this IContext context)
        {
            var contextObserver = new GameObject().AddComponent<ContextObserverBehaviour>();
            contextObserver.Initialize(context);
            Object.DontDestroyOnLoad(contextObserver.gameObject);
        }

        public static ContextObserverBehaviour FindContextObserver(this IContext context)
        {
            foreach (var observer in Object.FindObjectsByType<ContextObserverBehaviour>(FindObjectsSortMode.None))
                if (observer.Context == context)
                    return observer;

            return null;
        }
    }
}
