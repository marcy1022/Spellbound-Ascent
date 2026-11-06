using System;

namespace SpellboundAscent.Utils
{
    [Serializable]
    public class BalanceConfig
    {
        public EnergyConfig energy;
        public DrawConfig draw;
        public BlockConfig block;
        public StartDeckConfig startDeck;
        public ClassHPConfig classHP;
        public int masterSkillCooldown;
        public CardPowerBudget cardPowerBudget;
        public EnemyHPConfig enemyHP;
        public GoldRewardConfig goldReward;
        public ShopPrices shopPrices;
        public PotionDrop potionDrop;
        public CardRewardRarity cardRewardRarity;
        public RestSiteConfig restSite;
        public StatusEffectsConfig statusEffects;
    }

    [Serializable] public class EnergyConfig { public int basePerTurn; public int max; }
    [Serializable] public class DrawConfig { public int basePerTurn; public int maxHand; }
    [Serializable] public class BlockConfig { public bool resetOnTurnStart; }
    [Serializable] public class StartDeckConfig { public int size; public int strikeCount; public int defendCount; public int uniqueCount; }

    [Serializable]
    public class ClassHPConfig
    {
        public int pyromancer;
        public int warden;
        public int chronomancer;
        public int necromancer;

        public int Get(string className) => className switch
        {
            "pyromancer" => pyromancer,
            "warden" => warden,
            "chronomancer" => chronomancer,
            "necromancer" => necromancer,
            _ => 75
        };
    }

    [Serializable]
    public class CardPowerBudget
    {
        public int damagePerEnergy;
        public int blockPerEnergy;
        public RarityMultiplier rarityMultiplier;
        public float[] upgradeMultiplier;
        public int maxZeroCostPerClass;
    }

    [Serializable]
    public class RarityMultiplier
    {
        public float[] common;
        public float[] uncommon;
        public float[] rare;
    }

    [Serializable]
    public class EnemyHPConfig
    {
        public ScalingConfig scaling;
        public ActRange normalRange;
        public ActRange eliteRange;
        public ActRange bossHP;
    }

    [Serializable] public class ScalingConfig { public float actMultiplier; public float floorMultiplier; }

    [Serializable]
    public class ActRange
    {
        public int[] act1;
        public int[] act2;
        public int[] act3;

        public int[] Get(int act) => act switch
        {
            1 => act1,
            2 => act2,
            3 => act3,
            _ => act1
        };
    }

    [Serializable]
    public class GoldRewardConfig
    {
        public ActRange normal;
        public int[] elite;
        public int[] boss;
    }

    [Serializable]
    public class ShopPrices
    {
        public int[] cardCommon;
        public int[] cardUncommon;
        public int[] cardRare;
        public int cardRemoval;
        public int cardRemovalActIncrease;
        public int relicCommon;
        public int relicUncommon;
        public int relicRare;
        public int[] potion;
    }

    [Serializable]
    public class PotionDrop
    {
        public float normalBattle;
        public float eliteBattle;
        public float bossBattle;
        public int maxSlots;
    }

    [Serializable]
    public class CardRewardRarity
    {
        public float common;
        public float uncommon;
        public float rare;
    }

    [Serializable] public class RestSiteConfig { public float healPercent; public int upgradeCount; }

    [Serializable]
    public class StatusEffectsConfig
    {
        public ThornsConfig thorns;
        public BurnConfig burn;
        public PoisonConfig poison;
        public WeakConfig weak;
        public VulnerableConfig vulnerable;
    }

    [Serializable] public class ThornsConfig { public int maxStack; public int decayPerTurn; }
    [Serializable] public class BurnConfig { public bool ignoresBlock; public int decayOnProc; }
    [Serializable] public class PoisonConfig { public bool ignoresBlock; public string decayType; }
    [Serializable] public class WeakConfig { public float damageReduction; public int decayPerTurn; }
    [Serializable] public class VulnerableConfig { public float damageIncrease; public int decayPerTurn; }
}
