using System;
using System.Collections.Generic;

namespace SpellboundAscent.Enemy
{
    [Serializable]
    public class EnemyData
    {
        public string id;
        public string name;
        public string nameKo;
        public int act;
        public string type;
        public HpRange hp;
        public List<MoveData> moves = new();
        public string pattern;
        public PatternData patternData;
    }

    [Serializable]
    public class HpRange
    {
        public int min;
        public int max;
    }

    [Serializable]
    public class MoveData
    {
        public string id;
        public string intent;
        public List<Card.EffectData> effects = new();
    }

    [Serializable]
    public class PatternData
    {
        public List<string> sequence;
        public bool loop;
        public List<float> weights;
        public List<ConditionData> conditions;
    }

    [Serializable]
    public class ConditionData
    {
        public string type;
        public float value;
        public string then;
    }

    public class EnemyInstance
    {
        public EnemyData Data { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; set; }
        public int Block { get; set; }
        public Dictionary<string, int> StatusEffects { get; private set; } = new();
        public int PatternIndex { get; set; }
        public string CurrentPhase { get; set; }

        public bool IsDead => CurrentHp <= 0;
        public float HpPercent => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;

        public EnemyInstance(EnemyData data)
        {
            Data = data;
            MaxHp = UnityEngine.Random.Range(data.hp.min, data.hp.max + 1);
            CurrentHp = MaxHp;
        }

        public void TakeDamage(int amount)
        {
            int afterBlock = amount - Block;
            Block = System.Math.Max(0, Block - amount);
            if (afterBlock > 0)
                CurrentHp = System.Math.Max(0, CurrentHp - afterBlock);
        }

        public void Heal(int amount)
        {
            CurrentHp = System.Math.Min(MaxHp, CurrentHp + amount);
        }

        public int GetStatus(string key)
        {
            return StatusEffects.TryGetValue(key, out int val) ? val : 0;
        }

        public void SetStatus(string key, int value)
        {
            if (value <= 0)
                StatusEffects.Remove(key);
            else
                StatusEffects[key] = value;
        }

        public void AddStatus(string key, int amount)
        {
            SetStatus(key, GetStatus(key) + amount);
        }
    }
}
