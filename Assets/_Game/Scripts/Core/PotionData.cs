using System;
using System.Collections.Generic;
using SpellboundAscent.Card;

namespace SpellboundAscent.Core
{
    [Serializable]
    public class PotionData
    {
        public string id;
        public string name;
        public string nameKo;
        public string rarity;
        public string description;
        public string descriptionKo;
        public List<EffectData> effects = new();

        public PotionSlot ToSlot()
        {
            return new PotionSlot
            {
                id = id,
                name = name,
                nameKo = nameKo,
                effects = new List<EffectData>(effects)
            };
        }
    }
}
