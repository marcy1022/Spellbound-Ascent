using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpellboundAscent.Card
{
    [Serializable]
    public class CardData
    {
        public string id;
        public string name;
        public string nameKo;
        public string @class;
        public string type;
        public int cost;
        public string rarity;
        public string targetType;
        public List<EffectData> effects = new();
        public List<EffectData> upgradeEffects = new();
        public List<string> keywords = new();
        public bool exhaust;
        public bool innate;
        public bool ethereal;
        public string description;
        public string descriptionKo;
        public string flavorText;
    }

    [Serializable]
    public class EffectData
    {
        public string type;
        public float value;
        public string enemyId;
        public string rarity;
        public bool giveReward;
    }

    public class CardInstance
    {
        public CardData Data { get; private set; }
        public bool Upgraded { get; private set; }
        public string UniqueId { get; private set; }

        public int Cost => Upgraded && Data.upgradeEffects.Count > 0
            ? Math.Max(Data.cost - (HasCostReduction() ? 1 : 0), 0)
            : Data.cost;

        public List<EffectData> ActiveEffects =>
            Upgraded && Data.upgradeEffects.Count > 0 ? Data.upgradeEffects : Data.effects;

        public string ActiveDescription => BuildEffectDesc(ActiveEffects, Data.exhaust, false);
        public string ActiveDescriptionKo => BuildEffectDesc(ActiveEffects, Data.exhaust, true);

        public CardInstance(CardData data)
        {
            Data = data;
            UniqueId = Guid.NewGuid().ToString("N")[..8];
        }

        public void Upgrade()
        {
            Upgraded = true;
        }

        bool HasCostReduction()
        {
            if (Data.upgradeEffects.Count == 0) return false;
            foreach (var eff in Data.upgradeEffects)
            {
                if (eff.type == "costReduction") return true;
            }
            return false;
        }

        public static string BuildEffectDesc(List<EffectData> effects, bool exhaust, bool ko)
        {
            var parts = new List<string>();
            int multiHit = 0;
            foreach (var eff in effects)
                if (eff.type == "multiHit") multiHit = (int)eff.value;

            foreach (var eff in effects)
            {
                int v = (int)eff.value;
                switch (eff.type)
                {
                    case "damage":
                        parts.Add(multiHit > 0
                            ? (ko ? $"{v} 데미지를 {multiHit}회 반복." : $"Deal {v} damage {multiHit} times.")
                            : (ko ? $"{v} 데미지." : $"Deal {v} damage."));
                        break;
                    case "damageAll":
                        parts.Add(ko ? $"적 전체 {v} 데미지." : $"Deal {v} damage to ALL enemies.");
                        break;
                    case "block":
                        parts.Add(ko ? $"블록 {v} 획득." : $"Gain {v} Block.");
                        break;
                    case "heal":
                        parts.Add(ko ? $"HP {v} 회복." : $"Heal {v} HP.");
                        break;
                    case "draw":
                        parts.Add(ko ? $"카드 {v}장 드로우." : $"Draw {v} cards.");
                        break;
                    case "energy":
                        parts.Add(ko ? $"에너지 {v}." : $"Gain {v} Energy.");
                        break;
                    case "applyBurn":
                        parts.Add(ko ? $"화상 {v} 부여." : $"Apply {v} Burn.");
                        break;
                    case "applyPoison":
                        parts.Add(ko ? $"독 {v} 부여." : $"Apply {v} Poison.");
                        break;
                    case "applyWeak":
                        parts.Add(ko ? $"약화 {v} 부여." : $"Apply {v} Weak.");
                        break;
                    case "applyVulnerable":
                        parts.Add(ko ? $"취약 {v} 부여." : $"Apply {v} Vulnerable.");
                        break;
                    case "applyStrength":
                        parts.Add(ko ? $"힘 {v} 획득." : $"Gain {v} Strength.");
                        break;
                    case "applyDexterity":
                        parts.Add(ko ? $"민첩 {v} 획득." : $"Gain {v} Dexterity.");
                        break;
                    case "applyThorns":
                        parts.Add(ko ? $"가시 {v} 획득." : $"Gain {v} Thorns.");
                        break;
                    case "multiHit": break;
                    case "costReduction": break;
                }
            }
            if (exhaust) parts.Add(ko ? "소멸." : "Exhaust.");
            return string.Join(" ", parts);
        }
    }
}
