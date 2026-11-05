using System;
using System.Collections.Generic;
using UnityEngine;
using SpellboundAscent.Core;

namespace SpellboundAscent.Relic
{
    public class RelicManager : MonoBehaviour
    {
        public static RelicManager Instance { get; private set; }

        public event Action<RelicData> OnRelicAcquired;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddRelic(RelicData relic)
        {
            GameManager.Instance.CurrentRun.relics.Add(relic);
            OnRelicAcquired?.Invoke(relic);
        }

        public bool HasRelic(string relicId)
        {
            var relics = GameManager.Instance.CurrentRun.relics;
            foreach (var r in relics)
            {
                if (r.id == relicId) return true;
            }
            return false;
        }

        public List<RelicData> GetRandomRelics(int count, string rarity = null)
        {
            var pool = GameManager.Instance.GetRelics(rarity, GameManager.Instance.CurrentRun.playerClass);
            var owned = GameManager.Instance.CurrentRun.relics;

            pool.RemoveAll(r => owned.Exists(o => o.id == r.id));

            var result = new List<RelicData>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
