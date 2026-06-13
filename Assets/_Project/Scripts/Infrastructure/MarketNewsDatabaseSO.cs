using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChoNoiMienTay.Infrastructure
{
    [Serializable]
    public class MarketNewsEntry
    {
        public int dayNumber = 1;
        public string npcName;
        public Sprite npcAvatar;
        [TextArea(2, 4)] public string headline;
        [TextArea(3, 6)] public string marketRumor;
    }

    [CreateAssetMenu(fileName = "MarketNewsDatabase", menuName = "ChoNoi/Data/Market News Database")]
    public class MarketNewsDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<MarketNewsEntry> entries = new List<MarketNewsEntry>();

        public IReadOnlyList<MarketNewsEntry> Entries => entries;

        public MarketNewsEntry GetEntryForDay(int dayNumber)
        {
            if (entries.Count == 0)
            {
                return null;
            }

            MarketNewsEntry exact = entries.Find(entry => entry != null && entry.dayNumber == dayNumber);
            if (exact != null)
            {
                return exact;
            }

            int loopIndex = Mathf.Abs(dayNumber - 1) % entries.Count;
            return entries[loopIndex];
        }
    }
}
