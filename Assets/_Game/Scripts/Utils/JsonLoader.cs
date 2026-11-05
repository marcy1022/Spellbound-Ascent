using System.Collections.Generic;
using UnityEngine;

namespace SpellboundAscent.Utils
{
    public static class JsonLoader
    {
        public static T Load<T>(string resourcePath)
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogWarning($"[JsonLoader] Resource not found: {resourcePath}");
                return default;
            }
            return JsonUtility.FromJson<T>(textAsset.text);
        }

        public static BalanceConfig LoadBalanceConfig()
        {
            return Load<BalanceConfig>("Data/Balance/balance_config");
        }

        public static List<Card.CardData> LoadCards(string className)
        {
            var textAsset = Resources.Load<TextAsset>($"Data/Cards/{className}_cards");
            if (textAsset == null) return new List<Card.CardData>();
            string wrapped = "{\"items\":" + textAsset.text + "}";
            var wrapper = JsonUtility.FromJson<CardListWrapper>(wrapped);
            return wrapper?.items ?? new List<Card.CardData>();
        }

        public static List<Enemy.EnemyData> LoadEnemies(int act)
        {
            var textAsset = Resources.Load<TextAsset>($"Data/Enemies/act{act}_enemies");
            if (textAsset == null) return new List<Enemy.EnemyData>();
            string wrapped = "{\"items\":" + textAsset.text + "}";
            var wrapper = JsonUtility.FromJson<EnemyListWrapper>(wrapped);
            return wrapper?.items ?? new List<Enemy.EnemyData>();
        }

        public static List<Relic.RelicData> LoadRelics()
        {
            var textAsset = Resources.Load<TextAsset>("Data/Relics/relics");
            if (textAsset == null) return new List<Relic.RelicData>();
            string wrapped = "{\"items\":" + textAsset.text + "}";
            var wrapper = JsonUtility.FromJson<RelicListWrapper>(wrapped);
            return wrapper?.items ?? new List<Relic.RelicData>();
        }

        public static List<Event.EventData> LoadEvents()
        {
            var textAsset = Resources.Load<TextAsset>("Data/Events/events");
            if (textAsset == null) return new List<Event.EventData>();
            string wrapped = "{\"items\":" + textAsset.text + "}";
            var wrapper = JsonUtility.FromJson<EventListWrapper>(wrapped);
            return wrapper?.items ?? new List<Event.EventData>();
        }

        [System.Serializable] class CardListWrapper { public List<Card.CardData> items; }
        [System.Serializable] class EnemyListWrapper { public List<Enemy.EnemyData> items; }
        [System.Serializable] class RelicListWrapper { public List<Relic.RelicData> items; }
        [System.Serializable] class EventListWrapper { public List<Event.EventData> items; }
    }
}
