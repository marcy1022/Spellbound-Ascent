using System;
using System.Collections.Generic;
using UnityEngine;
using SpellboundAscent.Card;
using SpellboundAscent.Event;
using SpellboundAscent.Utils;

namespace SpellboundAscent.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState CurrentRun { get; private set; }
        public BalanceConfig Balance { get; private set; }

        public event Action OnRunStarted;
        public event Action OnRunEnded;
        public event Action<int> OnActChanged;

        Dictionary<string, List<CardData>> _cardPool = new();
        List<Enemy.EnemyData> _enemyPool = new();
        List<Relic.RelicData> _relicPool = new();
        List<EventData> _eventPool = new();
        List<PotionData> _potionPool = new();

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGameData();
        }

        void LoadGameData()
        {
            Balance = JsonLoader.LoadBalanceConfig();
            Debug.Log($"[GM] Balance loaded: energy={Balance?.energy?.basePerTurn}");

            foreach (string cls in new[] { "pyromancer", "warden", "chronomancer", "necromancer", "neutral" })
            {
                var cards = JsonLoader.LoadCards(cls);
                Debug.Log($"[GM] {cls} cards: {cards.Count}");
                if (cards.Count > 0)
                    _cardPool[cls] = cards;
            }

            for (int act = 1; act <= Constants.TOTAL_ACTS; act++)
            {
                var enemies = JsonLoader.LoadEnemies(act);
                Debug.Log($"[GM] Act{act} enemies: {enemies.Count}");
                _enemyPool.AddRange(enemies);
            }

            _relicPool = JsonLoader.LoadRelics();

            _eventPool = JsonLoader.LoadEvents();

            _potionPool = JsonLoader.LoadPotions();
            Debug.Log($"[GM] Total: cards={_cardPool.Count} pools, enemies={_enemyPool.Count}, relics={_relicPool.Count}, events={_eventPool.Count}, potions={_potionPool.Count}");
        }

        public void RestoreRun(RunState run)
        {
            CurrentRun = run;
            OnRunStarted?.Invoke();
        }

        public void StartNewRun(string playerClass)
        {
            CurrentRun = new RunState
            {
                playerClass = playerClass,
                currentAct = 1,
                currentFloor = 0,
                gold = 99,
                seed = UnityEngine.Random.Range(0, int.MaxValue)
            };

            int hp = Balance.classHP.Get(playerClass);
            CurrentRun.maxHp = hp;
            CurrentRun.currentHp = hp;

            BuildStarterDeck(playerClass);
            OnRunStarted?.Invoke();
        }

        void BuildStarterDeck(string playerClass)
        {
            if (!_cardPool.TryGetValue(playerClass, out var classCards)) return;

            var starters = classCards.FindAll(c => c.rarity == "starter");
            foreach (var card in starters)
            {
                CurrentRun.AddCard(new CardInstance(card));
            }
        }

        public void AdvanceAct()
        {
            if (CurrentRun.currentAct >= Constants.TOTAL_ACTS)
            {
                EndRun(true);
                return;
            }
            CurrentRun.currentAct++;
            CurrentRun.currentFloor = 0;
            OnActChanged?.Invoke(CurrentRun.currentAct);
        }

        public void EndRun(bool victory)
        {
            OnRunEnded?.Invoke();
        }

        public List<CardData> GetCardPool(string playerClass, string rarity = null)
        {
            var result = new List<CardData>();

            if (_cardPool.TryGetValue(playerClass, out var classCards))
                result.AddRange(classCards);
            if (_cardPool.TryGetValue("neutral", out var neutralCards))
                result.AddRange(neutralCards);

            if (rarity != null)
                result.RemoveAll(c => c.rarity != rarity);

            return result;
        }

        public List<Enemy.EnemyData> GetEnemies(int act, string type)
        {
            return _enemyPool.FindAll(e => e.act == act && e.type == type);
        }

        public EventData GetRandomEvent(int act)
        {
            var filtered = _eventPool.FindAll(e => e.act.Contains(act));
            if (filtered.Count == 0) return null;
            return filtered[UnityEngine.Random.Range(0, filtered.Count)];
        }

        public List<Relic.RelicData> GetRelics(string rarity = null, string forClass = null)
        {
            return _relicPool.FindAll(r =>
                (rarity == null || r.rarity == rarity) &&
                (forClass == null || r.@class == "any" || r.@class == forClass));
        }

        public PotionData GetRandomPotion(string rarity = null)
        {
            var pool = rarity != null
                ? _potionPool.FindAll(p => p.rarity == rarity)
                : _potionPool;
            if (pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        public List<PotionData> GetPotionPool()
        {
            return _potionPool;
        }
    }
}
