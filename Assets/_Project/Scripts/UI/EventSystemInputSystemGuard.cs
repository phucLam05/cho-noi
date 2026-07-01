using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ChoNoi.UI
{
    /// <summary>
    /// Ensures scenes created with legacy UGUI input modules still run when the
    /// project uses the new Input System package.
    /// </summary>
    public static class EventSystemInputSystemGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void FixEventSystems()
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (EventSystem eventSystem in eventSystems)
            {
                if (eventSystem == null)
                    continue;

                StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                    Object.Destroy(legacyModule);

                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
