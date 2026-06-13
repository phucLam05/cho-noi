using System;
using System.Collections.Generic;
using UnityEngine;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoiMienTay.Data
{
    [Serializable]
    public class BargainingItemEconomyEntry
    {
        public ItemData item;
        public int minPriceVariation = -2000;
        public int maxPriceVariation = 4000;
        public int startingInventoryAmount = 2;
    }

    [Serializable]
    public class BargainingNpcProfile
    {
        public string npcId;
        public string displayName;
        public Sprite avatar;
        [Range(0.5f, 1.5f)] public float openingPriceMultiplier = 0.9f;
        [Range(0.5f, 1.5f)] public float maxAcceptPriceMultiplier = 1.05f;
    }

    [CreateAssetMenu(fileName = "BargainingEconomyConfig", menuName = "ChoNoi/Data/Bargaining Economy Config")]
    public class BargainingEconomyConfig : ScriptableObject
    {
        [Header("Negotiation Rules")]
        [SerializeField] private int staminaCostPerNegotiation = 8;
        [SerializeField] private int offerStep = 500;

        [Header("Prototype Data")]
        [SerializeField] private List<BargainingItemEconomyEntry> agriculturalItems = new List<BargainingItemEconomyEntry>();
        [SerializeField] private List<BargainingNpcProfile> npcProfiles = new List<BargainingNpcProfile>();

        public int StaminaCostPerNegotiation => Mathf.Max(1, staminaCostPerNegotiation);
        public int OfferStep => Mathf.Max(100, offerStep);
        public IReadOnlyList<BargainingItemEconomyEntry> AgriculturalItems => agriculturalItems;
        public IReadOnlyList<BargainingNpcProfile> NpcProfiles => npcProfiles;

        public BargainingItemEconomyEntry FindItemEntry(ItemData item)
        {
            if (item == null)
            {
                return null;
            }

            return agriculturalItems.Find(entry => entry != null && entry.item == item);
        }

        public BargainingNpcProfile FindNpcProfile(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return null;
            }

            return npcProfiles.Find(profile => profile != null &&
                string.Equals(profile.npcId, npcId, StringComparison.OrdinalIgnoreCase));
        }

        public void ReplacePrototypeData(
            int negotiationStaminaCost,
            int negotiationOfferStep,
            List<BargainingItemEconomyEntry> itemEntries,
            List<BargainingNpcProfile> profiles)
        {
            staminaCostPerNegotiation = negotiationStaminaCost;
            offerStep = negotiationOfferStep;
            agriculturalItems = itemEntries ?? new List<BargainingItemEconomyEntry>();
            npcProfiles = profiles ?? new List<BargainingNpcProfile>();
        }
    }
}
