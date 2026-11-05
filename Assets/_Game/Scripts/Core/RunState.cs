using System;
using System.Collections.Generic;
using SpellboundAscent.Card;
using SpellboundAscent.Relic;

namespace SpellboundAscent.Core
{
    [Serializable]
    public class RunState
    {
        public string playerClass;
        public int maxHp;
        public int currentHp;
        public int gold;
        public int currentAct;
        public int currentFloor;
        public List<CardInstance> deck = new();
        public List<RelicData> relics = new();
        public List<PotionSlot> potions = new();
        public int seed;

        public bool IsAlive => currentHp > 0;
        public float HpPercent => maxHp > 0 ? (float)currentHp / maxHp : 0f;

        public void TakeDamage(int amount)
        {
            currentHp = Math.Max(0, currentHp - amount);
        }

        public void Heal(int amount)
        {
            currentHp = Math.Min(maxHp, currentHp + amount);
        }

        public void HealPercent(float percent)
        {
            int amount = (int)(maxHp * percent);
            Heal(amount);
        }

        public void AddGold(int amount)
        {
            gold += amount;
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;
            gold -= amount;
            return true;
        }

        public void AddCard(CardInstance card)
        {
            deck.Add(card);
        }

        public void RemoveCard(CardInstance card)
        {
            deck.Remove(card);
        }

        public bool AddPotion(PotionSlot potion)
        {
            if (potions.Count >= Utils.Constants.MAX_POTION_SLOTS) return false;
            potions.Add(potion);
            return true;
        }
    }

    [Serializable]
    public class PotionSlot
    {
        public string id;
        public string name;
        public string nameKo;
        public List<Card.EffectData> effects = new();
    }
}
