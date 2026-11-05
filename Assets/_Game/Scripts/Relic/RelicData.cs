using System;
using System.Collections.Generic;

namespace SpellboundAscent.Relic
{
    [Serializable]
    public class RelicData
    {
        public string id;
        public string name;
        public string nameKo;
        public string rarity;
        public string @class;
        public string trigger;
        public ConditionInfo condition;
        public List<Card.EffectData> effects = new();
        public string description;
        public string descriptionKo;
        public string flavorText;
    }

    [Serializable]
    public class ConditionInfo
    {
        public string type;
        public float value;
    }
}
