using UnityEngine;

namespace ChoNoi.Presentation.NPC
{
    public class NpcTradeTarget : MonoBehaviour
    {
        [SerializeField] private string npcDisplayName = "Thuong Lai";
        [SerializeField] private float interactionRadius = 3f;

        public string NpcDisplayName => npcDisplayName;
        public float InteractionRadius => interactionRadius;

        public void Configure(string displayName, float radius)
        {
            npcDisplayName = displayName;
            interactionRadius = radius;
        }
    }
}
