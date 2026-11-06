using System;
using System.Collections.Generic;
using UnityEngine;
using SpellboundAscent.Card;
using SpellboundAscent.Core;
using SpellboundAscent.Utils;

namespace SpellboundAscent.Battle
{
    public class RewardManager
    {
        public struct BattleReward
        {
            public int gold;
            public List<CardData> cardChoices;
            public Relic.RelicData relic;
            public PotionSlot potion;
        }

        public BattleReward GenerateReward(string enemyType, int act)
        {
            var balance = GameManager.Instance.Balance;
            var reward = new BattleReward();

            reward.gold = RollGold(enemyType, act, balance);
            reward.cardChoices = RollCardChoices(3, balance);

            if (enemyType == "elite")
                reward.relic = RollRelic();

            reward.potion = RollPotion(enemyType, balance);

            return reward;
        }

        int RollGold(string enemyType, int act, BalanceConfig balance)
        {

            return enemyType switch
            {
                "elite" => UnityEngine.Random.Range(balance.goldReward.elite[0], balance.goldReward.elite[1] + 1),
                "boss" => UnityEngine.Random.Range(balance.goldReward.boss[0], balance.goldReward.boss[1] + 1),
                _ => GetNormalGold(act, balance)
            };
        }

        List<CardData> RollCardChoices(int count, BalanceConfig balance)
        {
            var run = GameManager.Instance.CurrentRun;
            var pool = GameManager.Instance.GetCardPool(run.playerClass);
            pool.RemoveAll(c => c.rarity == "starter" || c.rarity == "curse");

            var result = new List<CardData>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                string rarity = RollRarity(balance.cardRewardRarity);
                var filtered = pool.FindAll(c => c.rarity == rarity);

                if (filtered.Count == 0)
                    filtered = pool;

                int idx = UnityEngine.Random.Range(0, filtered.Count);
                var card = filtered[idx];
                result.Add(card);
                pool.Remove(card);
            }
            return result;
        }

        string RollRarity(CardRewardRarity rates)
        {
            float roll = UnityEngine.Random.Range(0f, 1f);
            if (roll < rates.rare) return "rare";
            if (roll < rates.rare + rates.uncommon) return "uncommon";
            return "common";
        }

        Relic.RelicData RollRelic()
        {
            var relics = Relic.RelicManager.Instance?.GetRandomRelics(1);
            return relics is { Count: > 0 } ? relics[0] : null;
        }

        PotionSlot RollPotion(string enemyType, BalanceConfig balance)
        {
            var run = GameManager.Instance.CurrentRun;
            if (run.potions.Count >= Constants.MAX_POTION_SLOTS) return null;

            float dropRate = enemyType switch
            {
                "elite" => balance.potionDrop.eliteBattle,
                "boss" => balance.potionDrop.bossBattle,
                _ => balance.potionDrop.normalBattle
            };

            if (UnityEngine.Random.Range(0f, 1f) > dropRate) return null;

            string rarity = RollPotionRarity(enemyType);
            var potionData = GameManager.Instance.GetRandomPotion(rarity);
            if (potionData == null)
                potionData = GameManager.Instance.GetRandomPotion();
            return potionData?.ToSlot();
        }

        string RollPotionRarity(string enemyType)
        {
            float roll = UnityEngine.Random.Range(0f, 1f);
            return enemyType switch
            {
                "boss" => roll < 0.4f ? "rare" : roll < 0.8f ? "uncommon" : "common",
                "elite" => roll < 0.2f ? "rare" : roll < 0.6f ? "uncommon" : "common",
                _ => roll < 0.05f ? "rare" : roll < 0.3f ? "uncommon" : "common"
            };
        }

        static int GetNormalGold(int act, BalanceConfig balance)
        {
            var range = balance.goldReward.normal.Get(act);
            return range != null ? UnityEngine.Random.Range(range[0], range[1] + 1) : 10;
        }
    }
}
