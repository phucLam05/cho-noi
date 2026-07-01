using UnityEngine;

namespace ChoNoi.Presentation.NPC
{
    public enum InteractionTargetType
    {
        Bargain,
        Upgrade,
        News,
        Trade
    }

    public class NpcTradeTarget : MonoBehaviour
    {
        [SerializeField] private InteractionTargetType targetType = InteractionTargetType.Bargain;
        [SerializeField] private string npcDisplayName = "Thuong Lai";
        [SerializeField] private float interactionRadius = 3f;

        public InteractionTargetType TargetType => targetType;
        public string NpcDisplayName => npcDisplayName;
        public float InteractionRadius => interactionRadius;
        public bool HasTraded { get; set; } = false;

        public ChoNoiMienTay.Infrastructure.ItemData DesiredItem { get; set; }
        public int DesiredQuantity { get; set; }

        public void Configure(string displayName, float radius, InteractionTargetType type)
        {
            npcDisplayName = displayName;
            interactionRadius = radius;
            targetType = type;
        }

        public void Configure(string displayName, float radius)
        {
            npcDisplayName = displayName;
            interactionRadius = radius;
        }
    }
}
