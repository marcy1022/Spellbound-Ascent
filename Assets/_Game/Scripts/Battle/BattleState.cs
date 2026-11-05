using System;
using System.Collections.Generic;

namespace SpellboundAscent.Battle
{
    public class BattleState
    {
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int Block { get; set; }
        public int Energy { get; set; }
        public int MaxEnergy { get; set; }
        public Dictionary<string, int> StatusEffects { get; private set; } = new();

        public event Action<int> OnDamageTaken;
        public event Action<int> OnBlockChanged;
        public event Action<int> OnEnergyChanged;
        public event Action<int> OnHpChanged;

        public bool IsDead => CurrentHp <= 0;

        public void Init(int maxHp, int currentHp, int maxEnergy)
        {
            MaxHp = maxHp;
            CurrentHp = currentHp;
            MaxEnergy = maxEnergy;
            Energy = maxEnergy;
            Block = 0;
            StatusEffects.Clear();
        }

        public void TakeDamage(int amount)
        {
            int afterBlock = amount - Block;
            Block = Math.Max(0, Block - amount);
            OnBlockChanged?.Invoke(Block);

            if (afterBlock > 0)
            {
                CurrentHp = Math.Max(0, CurrentHp - afterBlock);
                OnDamageTaken?.Invoke(afterBlock);
                OnHpChanged?.Invoke(CurrentHp);
            }
        }

        public void Heal(int amount)
        {
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp);
        }

        public void AddBlock(int amount)
        {
            Block += amount;
            OnBlockChanged?.Invoke(Block);
        }

        public void ResetBlock()
        {
            Block = 0;
            OnBlockChanged?.Invoke(Block);
        }

        public bool SpendEnergy(int amount)
        {
            if (Energy < amount) return false;
            Energy -= amount;
            OnEnergyChanged?.Invoke(Energy);
            return true;
        }

        public void AddEnergy(int amount)
        {
            Energy = Math.Min(Energy + amount, 10);
            OnEnergyChanged?.Invoke(Energy);
        }

        public void ResetEnergy()
        {
            Energy = MaxEnergy;
            OnEnergyChanged?.Invoke(Energy);
        }
    }
}
