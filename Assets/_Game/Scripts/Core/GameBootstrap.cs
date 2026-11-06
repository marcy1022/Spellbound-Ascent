using System.Collections;
using UnityEngine;
using SpellboundAscent.Battle;

namespace SpellboundAscent.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            EnsureManager<FontManager>("FontManager");
            EnsureManager<GameManager>("GameManager");
            EnsureManager<BattleManager>("BattleManager");
            EnsureManager<Relic.RelicManager>("RelicManager");
            EnsureManager<GameFlowManager>("GameFlowManager");
        }

        IEnumerator Start()
        {
            yield return null;
            GameFlowManager.Instance.ShowTitle();
        }

        T EnsureManager<T>(string name) where T : MonoBehaviour
        {
            var existing = FindAnyObjectByType<T>();
            if (existing != null) return existing;
            var go = new GameObject(name);
            DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }
    }
}
