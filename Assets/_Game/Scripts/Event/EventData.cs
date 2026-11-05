using System;
using System.Collections.Generic;

namespace SpellboundAscent.Event
{
    [Serializable]
    public class EventData
    {
        public string id;
        public string name;
        public string nameKo;
        public List<int> act = new();
        public string text;
        public string textKo;
        public List<EventChoice> choices = new();
    }

    [Serializable]
    public class EventChoice
    {
        public string label;
        public string labelKo;
        public EventCondition condition;
        public List<Card.EffectData> effects = new();
        public string resultText;
        public string resultTextKo;
    }

    [Serializable]
    public class EventCondition
    {
        public string type;
        public float value;
    }
}
