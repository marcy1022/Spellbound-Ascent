using System;
using System.Collections.Generic;
using SpellboundAscent.Utils;

namespace SpellboundAscent.Battle
{
    public class StatusEffectSystem
    {
        public event Action<string, string, int> OnStatusApplied;
        public event Action<string, string> OnStatusRemoved;
        public event Action<string, int> OnStatusDamage;

        public void ApplyStatus(Dictionary<string, int> effects, string key, int amount, string targetName = "")
        {
            if (!effects.ContainsKey(key))
                effects[key] = 0;

            effects[key] += amount;

            if (key == StatusKeys.Thorns)
                effects[key] = Math.Min(effects[key], Constants.MAX_THORNS_STACK);

            OnStatusApplied?.Invoke(targetName, key, effects[key]);
        }

        public int GetStatus(Dictionary<string, int> effects, string key)
        {
            return effects.TryGetValue(key, out int val) ? val : 0;
        }

        public void TickTurnStart(Dictionary<string, int> effects, string targetName, Action<int> onDamage, Action<int> onHeal = null)
        {
            // Thorns: -1 per turn start
            if (effects.TryGetValue(StatusKeys.Thorns, out int thorns) && thorns > 0)
            {
                effects[StatusKeys.Thorns] = thorns - 1;
                if (effects[StatusKeys.Thorns] <= 0)
                {
                    effects.Remove(StatusKeys.Thorns);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Thorns);
                }
            }

            // Haste: draw extra cards, then remove all
            if (effects.TryGetValue(StatusKeys.Haste, out int haste) && haste > 0)
            {
                effects.Remove(StatusKeys.Haste);
                OnStatusRemoved?.Invoke(targetName, StatusKeys.Haste);
            }

            // Regen: heal = stacks, then -1
            if (effects.TryGetValue(StatusKeys.Regen, out int regen) && regen > 0)
            {
                onHeal?.Invoke(regen);
                effects[StatusKeys.Regen] = regen - 1;
                if (effects[StatusKeys.Regen] <= 0)
                {
                    effects.Remove(StatusKeys.Regen);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Regen);
                }
            }
        }

        public void TickTurnEnd(Dictionary<string, int> effects, string targetName, Action<int> onDamage, bool skipGrowthDecay = false)
        {
            // Burn: damage = stacks, then -1
            if (effects.TryGetValue(StatusKeys.Burn, out int burn) && burn > 0)
            {
                onDamage?.Invoke(burn);
                OnStatusDamage?.Invoke(targetName, burn);
                effects[StatusKeys.Burn] = burn - 1;
                if (effects[StatusKeys.Burn] <= 0)
                {
                    effects.Remove(StatusKeys.Burn);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Burn);
                }
            }

            // Poison: damage = stacks, then halve (ceil)
            if (effects.TryGetValue(StatusKeys.Poison, out int poison) && poison > 0)
            {
                onDamage?.Invoke(poison);
                OnStatusDamage?.Invoke(targetName, poison);
                effects[StatusKeys.Poison] = (poison + 1) / 2;
                if (effects[StatusKeys.Poison] <= 0)
                {
                    effects.Remove(StatusKeys.Poison);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Poison);
                }
            }

            // Weak: -1 per turn end
            if (effects.TryGetValue(StatusKeys.Weak, out int weak) && weak > 0)
            {
                effects[StatusKeys.Weak] = weak - 1;
                if (effects[StatusKeys.Weak] <= 0)
                {
                    effects.Remove(StatusKeys.Weak);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Weak);
                }
            }

            // Vulnerable: -1 per turn end
            if (effects.TryGetValue(StatusKeys.Vulnerable, out int vuln) && vuln > 0)
            {
                effects[StatusKeys.Vulnerable] = vuln - 1;
                if (effects[StatusKeys.Vulnerable] <= 0)
                {
                    effects.Remove(StatusKeys.Vulnerable);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Vulnerable);
                }
            }

            // Root: -1 per turn end
            if (effects.TryGetValue(StatusKeys.Root, out int root) && root > 0)
            {
                effects[StatusKeys.Root] = root - 1;
                if (effects[StatusKeys.Root] <= 0)
                {
                    effects.Remove(StatusKeys.Root);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Root);
                }
            }

            if (!skipGrowthDecay && effects.TryGetValue(StatusKeys.Growth, out int growth) && growth > 0)
            {
                effects[StatusKeys.Growth] = growth - 1;
                if (effects[StatusKeys.Growth] <= 0)
                {
                    effects.Remove(StatusKeys.Growth);
                    OnStatusRemoved?.Invoke(targetName, StatusKeys.Growth);
                }
            }
        }

        public float GetDamageMultiplier(Dictionary<string, int> attackerStatus, Dictionary<string, int> targetStatus)
        {
            float mult = 1f;

            // Weak: -25% damage dealt
            if (attackerStatus.TryGetValue(StatusKeys.Weak, out int weak) && weak > 0)
                mult *= 0.75f;

            // Vulnerable: +50% damage taken
            if (targetStatus.TryGetValue(StatusKeys.Vulnerable, out int vuln) && vuln > 0)
                mult *= 1.5f;

            return mult;
        }

        public int CalculateDamage(int baseDamage, int strength, Dictionary<string, int> attackerStatus, Dictionary<string, int> targetStatus)
        {
            float damage = baseDamage + strength;
            damage *= GetDamageMultiplier(attackerStatus, targetStatus);

            // Root: flat damage reduction on attacker
            if (attackerStatus.TryGetValue(StatusKeys.Root, out int root) && root > 0)
                damage -= root;

            return Math.Max(0, (int)damage);
        }

        public int CalculateBlock(int baseBlock, int dexterity)
        {
            return Math.Max(0, baseBlock + dexterity);
        }
    }
}
