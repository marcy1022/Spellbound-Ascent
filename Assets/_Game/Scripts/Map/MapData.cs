using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpellboundAscent.Map
{
    public enum NodeType { Normal, Elite, Rest, Shop, Event, Boss, Treasure }

    [Serializable]
    public class MapNode
    {
        public int floor;
        public int column;
        public int vCol;
        public NodeType type;
        public List<int> connections = new();
        public bool visited;
        public bool available;
    }

    public class MapGenerator
    {
        const int FLOORS = 15;
        const int VCOLS = 7;
        const int PATH_COUNT = 6;

        public List<List<MapNode>> Generate(int act, int seed)
        {
            UnityEngine.Random.InitState(seed + act * 1000);

            var paths = new int[PATH_COUNT][];
            var startCols = PickStartColumns();

            for (int p = 0; p < PATH_COUNT; p++)
            {
                paths[p] = new int[FLOORS];
                paths[p][0] = startCols[p];

                for (int f = 1; f < FLOORS - 1; f++)
                {
                    int prev = paths[p][f - 1];
                    int center = VCOLS / 2;
                    int shift;

                    if (f >= FLOORS - 4)
                        shift = prev < center ? 1 : (prev > center ? -1 : 0);
                    else
                    {
                        float roll = UnityEngine.Random.value;
                        shift = roll < 0.34f ? -1 : (roll < 0.67f ? 0 : 1);
                    }

                    paths[p][f] = Mathf.Clamp(prev + shift, 0, VCOLS - 1);
                }

                paths[p][FLOORS - 1] = VCOLS / 2;
            }

            var map = new List<List<MapNode>>();
            var colMaps = new Dictionary<int, int>[FLOORS];

            for (int f = 0; f < FLOORS; f++)
            {
                var vCols = new SortedSet<int>();
                for (int p = 0; p < PATH_COUNT; p++)
                    vCols.Add(paths[p][f]);

                colMaps[f] = new Dictionary<int, int>();
                var floor = new List<MapNode>();
                int idx = 0;
                foreach (int vc in vCols)
                {
                    colMaps[f][vc] = idx;
                    floor.Add(new MapNode
                    {
                        floor = f,
                        column = idx,
                        vCol = vc,
                        type = AssignNodeType(f, act)
                    });
                    idx++;
                }
                map.Add(floor);
            }

            map[FLOORS - 1][0].type = NodeType.Boss;

            for (int p = 0; p < PATH_COUNT; p++)
            {
                for (int f = 0; f < FLOORS - 1; f++)
                {
                    int fromVC = paths[p][f];
                    int toVC = paths[p][f + 1];
                    int fromIdx = colMaps[f][fromVC];
                    int toIdx = colMaps[f + 1][toVC];

                    var node = map[f][fromIdx];
                    if (!node.connections.Contains(toIdx))
                        node.connections.Add(toIdx);

                    if (UnityEngine.Random.value < 0.3f)
                    {
                        int adjVC = toVC + (UnityEngine.Random.value < 0.5f ? -1 : 1);
                        adjVC = Mathf.Clamp(adjVC, 0, VCOLS - 1);
                        if (colMaps[f + 1].TryGetValue(adjVC, out int adjIdx))
                        {
                            if (!node.connections.Contains(adjIdx))
                                node.connections.Add(adjIdx);
                        }
                    }
                }
            }

            for (int f = 0; f < FLOORS - 1; f++)
                foreach (var node in map[f])
                    node.connections.Sort();

            foreach (var node in map[0])
                node.available = true;

            return map;
        }

        int[] PickStartColumns()
        {
            var starts = new int[PATH_COUNT];
            var used = new HashSet<int>();

            for (int p = 0; p < 4; p++)
            {
                int col;
                do { col = UnityEngine.Random.Range(0, VCOLS); } while (used.Contains(col));
                starts[p] = col;
                used.Add(col);
            }

            for (int p = 4; p < PATH_COUNT; p++)
                starts[p] = UnityEngine.Random.Range(0, VCOLS);

            return starts;
        }

        NodeType AssignNodeType(int floor, int act)
        {
            if (floor == 0) return NodeType.Normal;
            if (floor == 14) return NodeType.Boss;
            if (floor == 13) return NodeType.Rest;

            if (floor == 8)
                return UnityEngine.Random.value < 0.5f ? NodeType.Rest : NodeType.Shop;

            float roll = UnityEngine.Random.value;

            if (floor >= 5)
            {
                if (roll < 0.16f) return NodeType.Elite;
                if (roll < 0.38f) return NodeType.Event;
                if (roll < 0.50f) return NodeType.Rest;
                if (roll < 0.55f) return NodeType.Shop;
                return NodeType.Normal;
            }

            if (roll < 0.26f) return NodeType.Event;
            if (roll < 0.40f) return NodeType.Rest;
            if (roll < 0.46f) return NodeType.Shop;
            return NodeType.Normal;
        }
    }
}
