using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SpellboundAscent.Card;
using SpellboundAscent.Map;
using SpellboundAscent.Relic;

namespace SpellboundAscent.Core
{
    public static class SaveLoadManager
    {
        static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public static void Save(RunState run, List<List<MapNode>> map)
        {
            var data = new SaveData
            {
                playerClass = run.playerClass,
                maxHp = run.maxHp,
                currentHp = run.currentHp,
                gold = run.gold,
                currentAct = run.currentAct,
                currentFloor = run.currentFloor,
                seed = run.seed
            };

            foreach (var card in run.deck)
                data.cards.Add(new SavedCard { id = card.Data.id, upgraded = card.Upgraded });

            foreach (var relic in run.relics)
                data.relicIds.Add(relic.id);

            foreach (var potion in run.potions)
            {
                var sp = new SavedPotion { id = potion.id, name = potion.name, nameKo = potion.nameKo };
                foreach (var eff in potion.effects)
                    sp.effects.Add(new SavedEffect { type = eff.type, value = eff.value });
                data.potions.Add(sp);
            }

            if (map != null)
            {
                foreach (var floor in map)
                {
                    var sf = new SavedFloor();
                    foreach (var node in floor)
                    {
                        sf.nodes.Add(new SavedNode
                        {
                            floor = node.floor,
                            column = node.column,
                            vCol = node.vCol,
                            type = (int)node.type,
                            connections = new List<int>(node.connections),
                            visited = node.visited
                        });
                    }
                    data.mapFloors.Add(sf);
                }
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] Saved to {SavePath}");
        }

        public static bool Load(out RunState run, out List<List<MapNode>> map)
        {
            run = null;
            map = null;

            if (!File.Exists(SavePath)) return false;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return false;

                run = new RunState
                {
                    playerClass = data.playerClass,
                    maxHp = data.maxHp,
                    currentHp = data.currentHp,
                    gold = data.gold,
                    currentAct = data.currentAct,
                    currentFloor = data.currentFloor,
                    seed = data.seed
                };

                var allCards = new Dictionary<string, CardData>();
                foreach (string cls in new[] { "pyromancer", "warden", "chronomancer", "necromancer", "neutral" })
                {
                    var pool = GameManager.Instance.GetCardPool(cls);
                    foreach (var c in pool)
                    {
                        if (!allCards.ContainsKey(c.id))
                            allCards[c.id] = c;
                    }
                }

                foreach (var sc in data.cards)
                {
                    if (allCards.TryGetValue(sc.id, out var cardData))
                    {
                        var instance = new CardInstance(cardData);
                        if (sc.upgraded) instance.Upgrade();
                        run.deck.Add(instance);
                    }
                    else
                    {
                        Debug.LogWarning($"[Load] Card not found: {sc.id}");
                    }
                }

                var relicPool = GameManager.Instance.GetRelics();
                foreach (var relicId in data.relicIds)
                {
                    var relic = relicPool.Find(r => r.id == relicId);
                    if (relic != null) run.relics.Add(relic);
                    else Debug.LogWarning($"[Load] Relic not found: {relicId}");
                }

                foreach (var sp in data.potions)
                {
                    var slot = new PotionSlot
                    {
                        id = sp.id,
                        name = sp.name,
                        nameKo = sp.nameKo
                    };
                    foreach (var se in sp.effects)
                        slot.effects.Add(new EffectData { type = se.type, value = se.value });
                    run.potions.Add(slot);
                }

                map = new List<List<MapNode>>();
                foreach (var sf in data.mapFloors)
                {
                    var floor = new List<MapNode>();
                    foreach (var sn in sf.nodes)
                    {
                        floor.Add(new MapNode
                        {
                            floor = sn.floor,
                            column = sn.column,
                            vCol = sn.vCol,
                            type = (NodeType)sn.type,
                            connections = new List<int>(sn.connections),
                            visited = sn.visited,
                            available = false
                        });
                    }
                    map.Add(floor);
                }

                RecalculateAvailable(map, run.currentFloor);

                Debug.Log($"[Load] Loaded: {run.playerClass} Act{run.currentAct} Floor{run.currentFloor} Deck:{run.deck.Count}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Load] Failed: {e.Message}");
                return false;
            }
        }

        static void RecalculateAvailable(List<List<MapNode>> map, int currentFloor)
        {
            if (map == null || map.Count == 0) return;

            if (currentFloor <= 0)
            {
                foreach (var node in map[0])
                    node.available = true;
                return;
            }

            int prevFloor = currentFloor - 1;
            if (prevFloor >= map.Count) return;

            foreach (var node in map[prevFloor])
            {
                if (!node.visited) continue;
                if (prevFloor + 1 < map.Count)
                {
                    foreach (int conn in node.connections)
                    {
                        if (conn < map[prevFloor + 1].Count && !map[prevFloor + 1][conn].visited)
                            map[prevFloor + 1][conn].available = true;
                    }
                }
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[Save] Save deleted");
            }
        }

        [Serializable]
        class SaveData
        {
            public string playerClass;
            public int maxHp;
            public int currentHp;
            public int gold;
            public int currentAct;
            public int currentFloor;
            public int seed;
            public List<SavedCard> cards = new();
            public List<string> relicIds = new();
            public List<SavedPotion> potions = new();
            public List<SavedFloor> mapFloors = new();
        }

        [Serializable]
        class SavedCard
        {
            public string id;
            public bool upgraded;
        }

        [Serializable]
        class SavedPotion
        {
            public string id;
            public string name;
            public string nameKo;
            public List<SavedEffect> effects = new();
        }

        [Serializable]
        class SavedEffect
        {
            public string type;
            public float value;
        }

        [Serializable]
        class SavedFloor
        {
            public List<SavedNode> nodes = new();
        }

        [Serializable]
        class SavedNode
        {
            public int floor;
            public int column;
            public int vCol;
            public int type;
            public List<int> connections = new();
            public bool visited;
        }
    }
}
