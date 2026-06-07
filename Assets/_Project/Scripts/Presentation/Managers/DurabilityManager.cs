using UnityEngine;
using ChoNoi.Domain;

namespace ChoNoi.Presentation
{
    public class DurabilityManager : MonoBehaviour, IDurabilityProvider
    {
        [SerializeField] private float maxDurability = 100f;
        [SerializeField] private float currentDurability = 100f;

        public float MaxDurability => maxDurability;
        public float CurrentDurability => currentDurability;

        public float GetDurabilityRatio() => Mathf.Clamp01(currentDurability / maxDurability);

        public void ReduceDurability(float amount)
        {
            if (amount > 0)
            {
                currentDurability = Mathf.Clamp(currentDurability - amount, 0, maxDurability);
            }
        }

        public void RepairDurability(float amount)
        {
            if (amount > 0)
            {
                currentDurability = Mathf.Clamp(currentDurability + amount, 0, maxDurability);
                Debug.Log($"[DurabilityManager] Đã phục hồi {amount} độ bền. Hiện tại: {currentDurability}/{maxDurability}");
            }
        }

        public void LoadDurability(float savedDurability)
        {
            currentDurability = Mathf.Clamp(savedDurability, 0, maxDurability);
        }
    }
}
