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
        public string condition;
        public float conditionValue;
        public string formula;
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
                bool hasFormula = !string.IsNullOrEmpty(eff.formula);
                string condPrefix = "";
                if (!string.IsNullOrEmpty(eff.condition))
                    condPrefix = ConditionDesc(eff.condition, (int)eff.conditionValue, ko) + " ";

                switch (eff.type)
                {
                    case "damage":
                        if (hasFormula)
                        {
                            string fd = FormulaDesc(eff.formula, ko);
                            parts.Add(condPrefix + (multiHit > 0
                                ? (ko ? $"{fd} 데미지를 {multiHit}회 반복." : $"Deal {fd} damage {multiHit} times.")
                                : (ko ? $"{fd} 데미지." : $"Deal {fd} damage.")));
                        }
                        else
                        {
                            parts.Add(condPrefix + (multiHit > 0
                                ? (ko ? $"{v} 데미지를 {multiHit}회 반복." : $"Deal {v} damage {multiHit} times.")
                                : (ko ? $"{v} 데미지." : $"Deal {v} damage.")));
                        }
                        break;
                    case "damageAll":
                        if (hasFormula)
                        {
                            string fd = FormulaDesc(eff.formula, ko);
                            parts.Add(condPrefix + (ko ? $"적 전체 {fd} 데미지." : $"Deal {fd} damage to ALL enemies."));
                        }
                        else
                        {
                            parts.Add(condPrefix + (ko ? $"적 전체 {v} 데미지." : $"Deal {v} damage to ALL enemies."));
                        }
                        break;
                    case "block":
                        if (hasFormula)
                        {
                            string fd = FormulaDesc(eff.formula, ko);
                            parts.Add(condPrefix + (ko ? $"블록 {fd} 획득." : $"Gain {fd} Block."));
                        }
                        else
                        {
                            parts.Add(condPrefix + (ko ? $"블록 {v} 획득." : $"Gain {v} Block."));
                        }
                        break;
                    case "heal":
                        parts.Add(condPrefix + (ko ? $"HP {v} 회복." : $"Heal {v} HP."));
                        break;
                    case "draw":
                        parts.Add(condPrefix + (ko ? $"카드 {v}장 드로우." : $"Draw {v} cards."));
                        break;
                    case "energy":
                        parts.Add(condPrefix + (ko ? $"에너지 {v}." : $"Gain {v} Energy."));
                        break;
                    case "applyBurn":
                        parts.Add(condPrefix + (ko ? $"화상 {v} 부여." : $"Apply {v} Burn."));
                        break;
                    case "applyPoison":
                        parts.Add(condPrefix + (ko ? $"독 {v} 부여." : $"Apply {v} Poison."));
                        break;
                    case "applyWeak":
                        parts.Add(condPrefix + (ko ? $"약화 {v} 부여." : $"Apply {v} Weak."));
                        break;
                    case "applyVulnerable":
                        parts.Add(condPrefix + (ko ? $"취약 {v} 부여." : $"Apply {v} Vulnerable."));
                        break;
                    case "applyStrength":
                        parts.Add(condPrefix + (ko ? $"힘 {v} 획득." : $"Gain {v} Strength."));
                        break;
                    case "applyDexterity":
                        parts.Add(condPrefix + (ko ? $"민첩 {v} 획득." : $"Gain {v} Dexterity."));
                        break;
                    case "applyThorns":
                        parts.Add(condPrefix + (ko ? $"가시 {v} 획득." : $"Gain {v} Thorns."));
                        break;
                    case "gainEmber":
                        parts.Add(condPrefix + (ko ? $"잔불 {v} 획득." : $"Gain {v} Ember."));
                        break;
                    case "detonateEmber":
                        parts.Add(condPrefix + (ko ? $"모든 잔불 소비. 잔불×{v} 데미지." : $"Consume all Ember. Deal Ember×{v} damage."));
                        break;
                    case "detonateEmberAll":
                        parts.Add(condPrefix + (ko ? $"모든 잔불 소비. 적 전체 잔불×{v} 데미지." : $"Consume all Ember. Deal Ember×{v} to ALL."));
                        break;
                    case "selfDamage":
                        parts.Add(condPrefix + (ko ? $"HP {v} 소모." : $"Lose {v} HP."));
                        break;
                    case "applyCombustion":
                        parts.Add(condPrefix + (ko ? $"턴 종료 시 잔불만큼 적 전체 데미지." : $"End of turn: deal Ember as damage to ALL."));
                        break;
                    case "gainGrowth":
                        parts.Add(condPrefix + (ko ? $"성장 {v} 획득." : $"Gain {v} Growth."));
                        break;
                    case "applyRegen":
                        parts.Add(condPrefix + (ko ? $"재생 {v} 획득." : $"Gain {v} Regen."));
                        break;
                    case "applyRoot":
                        parts.Add(condPrefix + (ko ? $"속박 {v} 부여." : $"Apply {v} Root."));
                        break;
                    case "applyHaste":
                        parts.Add(condPrefix + (ko ? $"가속 {v} 획득." : $"Gain {v} Haste."));
                        break;
                    case "multiHit": break;
                    case "costReduction": break;
                }
            }
            if (exhaust) parts.Add(ko ? "소멸." : "Exhaust.");
            return string.Join(" ", parts);
        }

        public string GetBattleDescKo(int strength, int dexterity, bool isWeak,
            Dictionary<string, int> playerStatus, int handSize, int discardSize)
        {
            var parts = new List<string>();
            int multiHit = 0;
            foreach (var eff in ActiveEffects)
                if (eff.type == "multiHit") multiHit = (int)eff.value;

            foreach (var eff in ActiveEffects)
            {
                bool hasFormula = !string.IsNullOrEmpty(eff.formula);
                string condPrefix = "";
                if (!string.IsNullOrEmpty(eff.condition))
                    condPrefix = ConditionDesc(eff.condition, (int)eff.conditionValue, true) + " ";

                int baseVal = hasFormula
                    ? ResolveFormula(eff.formula, playerStatus, handSize, discardSize)
                    : (int)eff.value;
                int cardVal = (int)eff.value;

                switch (eff.type)
                {
                    case "damage":
                    {
                        int dmg = Math.Max(0, (int)((baseVal + strength) * (isWeak ? 0.75f : 1f)));
                        string s = ColorNum(dmg, cardVal, hasFormula);
                        parts.Add(condPrefix + (multiHit > 0 ? $"{s} 데미지를 {multiHit}회 반복." : $"{s} 데미지."));
                        break;
                    }
                    case "damageAll":
                    {
                        int dmg = Math.Max(0, (int)((baseVal + strength) * (isWeak ? 0.75f : 1f)));
                        string s = ColorNum(dmg, cardVal, hasFormula);
                        parts.Add($"{condPrefix}적 전체 {s} 데미지.");
                        break;
                    }
                    case "block":
                    {
                        int blk = Math.Max(0, baseVal + dexterity);
                        string s = ColorNum(blk, cardVal, hasFormula);
                        parts.Add($"{condPrefix}블록 {s} 획득.");
                        break;
                    }
                    case "heal":
                        parts.Add($"{condPrefix}HP {baseVal} 회복.");
                        break;
                    case "draw":
                        parts.Add($"{condPrefix}카드 {baseVal}장 드로우.");
                        break;
                    case "energy":
                        parts.Add($"{condPrefix}에너지 {baseVal}.");
                        break;
                    case "applyBurn":
                        parts.Add($"{condPrefix}화상 {baseVal} 부여.");
                        break;
                    case "applyPoison":
                        parts.Add($"{condPrefix}독 {baseVal} 부여.");
                        break;
                    case "applyWeak":
                        parts.Add($"{condPrefix}약화 {baseVal} 부여.");
                        break;
                    case "applyVulnerable":
                        parts.Add($"{condPrefix}취약 {baseVal} 부여.");
                        break;
                    case "applyStrength":
                        parts.Add($"{condPrefix}힘 {baseVal} 획득.");
                        break;
                    case "applyDexterity":
                        parts.Add($"{condPrefix}민첩 {baseVal} 획득.");
                        break;
                    case "applyThorns":
                        parts.Add($"{condPrefix}가시 {baseVal} 획득.");
                        break;
                    case "gainEmber":
                        parts.Add($"{condPrefix}잔불 {baseVal} 획득.");
                        break;
                    case "detonateEmber":
                    {
                        int ember = playerStatus != null && playerStatus.TryGetValue("ember", out int e) ? e : 0;
                        int dmg = ember * baseVal;
                        parts.Add($"{condPrefix}모든 잔불 소비. <color=#ff8800>{dmg}</color> 데미지.");
                        break;
                    }
                    case "detonateEmberAll":
                    {
                        int ember = playerStatus != null && playerStatus.TryGetValue("ember", out int e) ? e : 0;
                        int dmg = ember * baseVal;
                        parts.Add($"{condPrefix}모든 잔불 소비. 적 전체 <color=#ff8800>{dmg}</color> 데미지.");
                        break;
                    }
                    case "selfDamage":
                        parts.Add($"{condPrefix}HP {baseVal} 소모.");
                        break;
                    case "applyCombustion":
                        parts.Add($"{condPrefix}턴 종료 시 잔불만큼 적 전체 데미지.");
                        break;
                    case "gainGrowth":
                        parts.Add($"{condPrefix}성장 {baseVal} 획득.");
                        break;
                    case "applyRegen":
                        parts.Add($"{condPrefix}재생 {baseVal} 획득.");
                        break;
                    case "applyRoot":
                        parts.Add($"{condPrefix}속박 {baseVal} 부여.");
                        break;
                    case "applyHaste":
                        parts.Add($"{condPrefix}가속 {baseVal} 획득.");
                        break;
                    case "multiHit": break;
                    case "costReduction": break;
                }
            }
            if (Data.exhaust) parts.Add("소멸.");
            return string.Join(" ", parts);
        }

        static string ColorNum(int actual, int baseValue, bool isFormula)
        {
            if (isFormula)
                return $"<color=#88ccff>{actual}</color>";
            if (actual > baseValue)
                return $"<color=#44ff44>{actual}</color>";
            if (actual < baseValue)
                return $"<color=#ff4444>{actual}</color>";
            return actual.ToString();
        }

        static string ConditionDesc(string condition, int value, bool ko)
        {
            return condition switch
            {
                "emberAbove" => ko ? $"<color=#ff8800>잔불 {value}↑:</color>" : $"If Ember >= {value}:",
                "growthAbove" => ko ? $"<color=#22cc44>성장 {value}↑:</color>" : $"If Growth >= {value}:",
                "targetRooted" => ko ? "<color=#886622>속박 시:</color>" : "If Rooted:",
                "targetBurned" => ko ? "<color=#ff6633>화상 시:</color>" : "If Burning:",
                "lowHp" => ko ? $"<color=#ff4444>HP {value}% 이하:</color>" : $"If HP <= {value}%:",
                _ => ""
            };
        }

        static string FormulaDesc(string formula, bool ko)
        {
            string desc = formula;
            if (ko)
            {
                desc = desc.Replace("{ember}", "잔불");
                desc = desc.Replace("{growth}", "성장");
                desc = desc.Replace("{strength}", "힘");
                desc = desc.Replace("{handSize}", "패 수");
                desc = desc.Replace("{discardPileSize}", "버린 카드 수");
            }
            else
            {
                desc = desc.Replace("{ember}", "Ember");
                desc = desc.Replace("{growth}", "Growth");
                desc = desc.Replace("{strength}", "Strength");
                desc = desc.Replace("{handSize}", "Hand");
                desc = desc.Replace("{discardPileSize}", "Discard");
            }
            desc = desc.Replace("*", "×");
            return desc;
        }

        static int ResolveFormula(string formula, Dictionary<string, int> playerStatus, int handSize, int discardSize)
        {
            string expr = formula;
            expr = ReplaceVar(expr, "ember", playerStatus != null && playerStatus.TryGetValue("ember", out int e) ? e : 0);
            expr = ReplaceVar(expr, "growth", playerStatus != null && playerStatus.TryGetValue("growth", out int g) ? g : 0);
            expr = ReplaceVar(expr, "strength", playerStatus != null && playerStatus.TryGetValue("strength", out int s) ? s : 0);
            expr = ReplaceVar(expr, "handSize", handSize);
            expr = ReplaceVar(expr, "discardPileSize", discardSize);
            return EvalSimple(expr);
        }

        static string ReplaceVar(string expr, string varName, int value)
        {
            return expr.Replace($"{{{varName}}}", value.ToString());
        }

        static int EvalSimple(string expr)
        {
            try
            {
                expr = expr.Trim();
                int mulIdx = expr.IndexOf('*');
                if (mulIdx > 0)
                {
                    int a = int.Parse(expr.Substring(0, mulIdx).Trim());
                    int b = int.Parse(expr.Substring(mulIdx + 1).Trim());
                    return a * b;
                }
                int addIdx = expr.IndexOf('+');
                if (addIdx > 0)
                {
                    int a = int.Parse(expr.Substring(0, addIdx).Trim());
                    int b = int.Parse(expr.Substring(addIdx + 1).Trim());
                    return a + b;
                }
                return int.Parse(expr);
            }
            catch { return 0; }
        }
    }
}
