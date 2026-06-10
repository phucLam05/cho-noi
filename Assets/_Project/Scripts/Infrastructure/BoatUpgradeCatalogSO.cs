using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChoNoiMienTay.Infrastructure
{
    [Serializable]
    public class StorageUpgradeTier
    {
        public string displayName;
        public int costMoney;
        public float capacityBonus;
    }

    [Serializable]
    public class EngineUpgradeTier
    {
        public string displayName;
        public int costMoney;
        public float thrustMultiplier = 1f;
    }

    [Serializable]
    public class RoofUpgradeTier
    {
        public string displayName;
        public int costMoney;
    }

    [Serializable]
    public class BambooPoleUpgradeTier
    {
        public string displayName;
        public int costMoney;
        public float hagglingBonusRatio;
    }

    [CreateAssetMenu(fileName = "BoatUpgradeCatalog", menuName = "ChoNoi/Data/Boat Upgrade Catalog")]
    public class BoatUpgradeCatalogSO : ScriptableObject
    {
        [SerializeField] private List<StorageUpgradeTier> storageTiers = new List<StorageUpgradeTier>();
        [SerializeField] private List<EngineUpgradeTier> engineTiers = new List<EngineUpgradeTier>();
        [SerializeField] private List<RoofUpgradeTier> roofTiers = new List<RoofUpgradeTier>();
        [SerializeField] private List<BambooPoleUpgradeTier> bambooPoleTiers = new List<BambooPoleUpgradeTier>();

        public IReadOnlyList<StorageUpgradeTier> StorageTiers => storageTiers;
        public IReadOnlyList<EngineUpgradeTier> EngineTiers => engineTiers;
        public IReadOnlyList<RoofUpgradeTier> RoofTiers => roofTiers;
        public IReadOnlyList<BambooPoleUpgradeTier> BambooPoleTiers => bambooPoleTiers;

        public StorageUpgradeTier GetStorageTier(int level) =>
            level >= 0 && level < storageTiers.Count ? storageTiers[level] : null;

        public EngineUpgradeTier GetEngineTier(int level) =>
            level >= 0 && level < engineTiers.Count ? engineTiers[level] : null;

        public RoofUpgradeTier GetRoofTier(int level) =>
            level >= 0 && level < roofTiers.Count ? roofTiers[level] : null;

        public BambooPoleUpgradeTier GetBambooPoleTier(int level) =>
            level >= 0 && level < bambooPoleTiers.Count ? bambooPoleTiers[level] : null;
    }
}
