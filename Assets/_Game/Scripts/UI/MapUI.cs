using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Card;
using SpellboundAscent.Map;
using SpellboundAscent.Core;

namespace SpellboundAscent.UI
{
    public class MapUI : MonoBehaviour
    {
        public event Action<MapNode> OnNodeSelected;

        Canvas _canvas;
        RectTransform _root;
        RectTransform _scrollContent;
        ScrollRect _scrollRect;
        List<List<MapNode>> _map;
        List<NodeButton> _nodeButtons = new();
        Dictionary<string, Image> _connectionLines = new();
        int _currentFloor;
        GameObject _deckViewPanel;

        struct NodeButton
        {
            public RectTransform rt;
            public MapNode node;
            public Image image;
            public Image glow;
            public Color baseColor;
        }

        public void ShowMap(List<List<MapNode>> map, int currentFloor)
        {
            _map = map;
            _currentFloor = currentFloor;
            BuildCanvas();
            BuildBackground();
            BuildScrollArea();
            DrawMap();
            DrawConnections();
            HighlightReachableRoutes();
            BuildHeader();
            ScrollToFloor(currentFloor);
        }

        public void Hide()
        {
            if (_deckViewPanel != null) Destroy(_deckViewPanel);
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        void BuildCanvas()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var go = new GameObject("MapCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 15;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            _root = go.GetComponent<RectTransform>();
        }

        void BuildBackground()
        {
            var bg = MakeRect(_root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = bg.gameObject.AddComponent<Image>();
            img.color = new Color(0.06f, 0.05f, 0.1f);
            img.raycastTarget = false;
        }

        void BuildHeader()
        {
            var run = GameManager.Instance.CurrentRun;
            var header = MakeRect(_root, "Header",
                new Vector2(0, 0.92f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var bg = header.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.15f, 0.95f);

            MakeTMP(header, "Title", $"{run.currentAct}막 - 지도",
                new Vector2(0.02f, 0.1f), new Vector2(0.5f, 0.9f), 36,
                new Color(1, 0.85f, 0.2f));

            MakeTMP(header, "HP", $"HP: {run.currentHp}/{run.maxHp}",
                new Vector2(0.5f, 0.5f), new Vector2(0.8f, 0.9f), 24, Color.white);

            MakeTMP(header, "Gold", $"골드: {run.gold}",
                new Vector2(0.5f, 0.1f), new Vector2(0.8f, 0.5f), 24,
                new Color(1, 0.85f, 0.2f));

            MakeTMP(header, "Floor", $"{run.currentFloor}/15층",
                new Vector2(0.8f, 0.1f), new Vector2(0.98f, 0.9f), 22,
                new Color(0.7f, 0.7f, 0.7f));

            var deckBtn = MakeRect(_root, "DeckBtn",
                new Vector2(0.02f, 0.005f), new Vector2(0.12f, 0.035f), Vector2.zero, Vector2.zero);
            var deckImg = deckBtn.gameObject.AddComponent<Image>();
            deckImg.color = new Color(0.2f, 0.2f, 0.35f, 0.9f);
            var db = deckBtn.gameObject.AddComponent<Button>();
            db.targetGraphic = deckImg;
            db.onClick.AddListener(ToggleDeckView);
            MakeTMP(deckBtn, "Txt", $"덱 ({run.deck.Count})",
                Vector2.zero, Vector2.one, 18, Color.white);

            var cardCodexBtn = MakeRect(_root, "CardCodexBtn",
                new Vector2(0.13f, 0.005f), new Vector2(0.23f, 0.035f), Vector2.zero, Vector2.zero);
            cardCodexBtn.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.15f, 0.3f, 0.9f);
            var ccb = cardCodexBtn.gameObject.AddComponent<Button>();
            ccb.targetGraphic = cardCodexBtn.GetComponent<Image>();
            ccb.onClick.AddListener(() => CardCodexUI.Show());
            MakeTMP(cardCodexBtn, "Txt", "카드",
                Vector2.zero, Vector2.one, 16, new Color(0.8f, 0.7f, 1f));

            var potCodexBtn = MakeRect(_root, "PotCodexBtn",
                new Vector2(0.24f, 0.005f), new Vector2(0.34f, 0.035f), Vector2.zero, Vector2.zero);
            potCodexBtn.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.22f, 0.15f, 0.9f);
            var pcb = potCodexBtn.gameObject.AddComponent<Button>();
            pcb.targetGraphic = potCodexBtn.GetComponent<Image>();
            pcb.onClick.AddListener(() => PotionCodexUI.Show());
            MakeTMP(potCodexBtn, "Txt", "물약",
                Vector2.zero, Vector2.one, 16, new Color(0.5f, 1f, 0.6f));

            var relCodexBtn = MakeRect(_root, "RelCodexBtn",
                new Vector2(0.35f, 0.005f), new Vector2(0.45f, 0.035f), Vector2.zero, Vector2.zero);
            relCodexBtn.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.2f, 0.9f);
            var rcb = relCodexBtn.gameObject.AddComponent<Button>();
            rcb.targetGraphic = relCodexBtn.GetComponent<Image>();
            rcb.onClick.AddListener(() => RelicCodexUI.Show());
            MakeTMP(relCodexBtn, "Txt", "유물",
                Vector2.zero, Vector2.one, 16, new Color(1f, 0.7f, 0.9f));
        }

        void ToggleDeckView()
        {
            if (_deckViewPanel != null)
            {
                Destroy(_deckViewPanel);
                _deckViewPanel = null;
                return;
            }

            var run = GameManager.Instance.CurrentRun;
            if (run == null) return;

            _deckViewPanel = new GameObject("DeckView");
            var canvas = _deckViewPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = _deckViewPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            _deckViewPanel.AddComponent<GraphicRaycaster>();
            var root = _deckViewPanel.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            MakeTMP(root, "Title", $"DECK ({run.deck.Count} cards)",
                new Vector2(0.1f, 0.92f), new Vector2(0.9f, 0.98f), 36, new Color(1, 0.85f, 0.2f));

            var closeBtn = MakeRect(root, "Close",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.07f), Vector2.zero, Vector2.zero);
            var closeImg = closeBtn.gameObject.AddComponent<Image>();
            closeImg.color = new Color(0.5f, 0.15f, 0.15f);
            var cb = closeBtn.gameObject.AddComponent<Button>();
            cb.targetGraphic = closeImg;
            cb.onClick.AddListener(() => { Destroy(_deckViewPanel); _deckViewPanel = null; });
            MakeTMP(closeBtn, "Txt", "닫기", Vector2.zero, Vector2.one, 24, Color.white);

            var scrollContainer = MakeRect(root, "ScrollContainer",
                new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero);
            var scrollRect = scrollContainer.gameObject.AddComponent<ScrollRect>();
            scrollContainer.gameObject.AddComponent<Image>().color = Color.clear;

            var viewport = MakeRect(scrollContainer, "Viewport",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = MakeRect(viewport, "Content",
                new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            int cols = 3;
            float cardW = 320f;
            float cardH = 200f;
            float padX = 20f;
            float padY = 10f;

            int rows = Mathf.CeilToInt(run.deck.Count / (float)cols);
            float totalH = rows * (cardH + padY) + padY;
            content.sizeDelta = new Vector2(0, totalH);

            for (int i = 0; i < run.deck.Count; i++)
            {
                var card = run.deck[i];
                int col = i % cols;
                int row = i / cols;

                float x = padX + col * (cardW + padX);
                float y = -padY - row * (cardH + padY);

                var cardRT = new GameObject($"Card{i}", typeof(RectTransform)).GetComponent<RectTransform>();
                cardRT.SetParent(content, false);
                cardRT.anchorMin = new Vector2(0, 1);
                cardRT.anchorMax = new Vector2(0, 1);
                cardRT.pivot = new Vector2(0, 1);
                cardRT.anchoredPosition = new Vector2(x, y);
                cardRT.sizeDelta = new Vector2(cardW, cardH);

                Color cardColor = card.Data.type switch
                {
                    "attack" => new Color(0.5f, 0.15f, 0.15f, 0.9f),
                    "skill" => new Color(0.15f, 0.25f, 0.5f, 0.9f),
                    "power" => new Color(0.35f, 0.15f, 0.5f, 0.9f),
                    _ => new Color(0.3f, 0.3f, 0.3f, 0.9f)
                };
                cardRT.gameObject.AddComponent<Image>().color = cardColor;

                string upgStr = card.Upgraded ? " <color=#44ff44>+</color>" : "";
                MakeTMP(cardRT, "Cost", card.Cost.ToString(),
                    new Vector2(0, 0.7f), new Vector2(0.2f, 1f), 28,
                    new Color(1, 0.85f, 0.2f));

                string mCardName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;
                var mNameT = MakeTMP(cardRT, "Name", mCardName + upgStr,
                    new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.7f), 24, Color.white);
                mNameT.fontStyle = FontStyles.Bold;

                MakeTMP(cardRT, "Desc", card.ActiveDescriptionKo,
                    new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.45f), 17,
                    new Color(0.8f, 0.8f, 0.8f));
            }
        }

        void BuildScrollArea()
        {
            var scrollContainer = MakeRect(_root, "ScrollContainer",
                new Vector2(0, 0), new Vector2(1, 0.92f), Vector2.zero, Vector2.zero);

            var scrollBg = scrollContainer.gameObject.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.01f);

            _scrollRect = scrollContainer.gameObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            _scrollRect.scrollSensitivity = 30f;

            var viewport = MakeRect(scrollContainer, "Viewport",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();

            float floorHeight = 120f;
            float totalHeight = (_map.Count + 2) * floorHeight;

            _scrollContent = MakeRect(viewport, "Content",
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero,
                new Vector2(0, totalHeight));
            _scrollContent.pivot = new Vector2(0.5f, 0);

            _scrollRect.content = _scrollContent;
            _scrollRect.viewport = viewport;
        }

        void DrawMap()
        {
            float floorHeight = 120f;
            float padding = 60f;

            for (int f = 0; f < _map.Count; f++)
            {
                var floor = _map[f];
                float y = padding + f * floorHeight;
                int displayFloor = f + 1;

                var floorLabel = MakeRect(_scrollContent, $"F{f}Label",
                    new Vector2(0, 0), new Vector2(0, 0),
                    new Vector2(40, y), new Vector2(60, 30));
                var labelTmp = floorLabel.gameObject.AddComponent<TextMeshProUGUI>();
                labelTmp.text = $"{displayFloor}";
                labelTmp.fontSize = 18;
                labelTmp.color = new Color(0.5f, 0.5f, 0.5f);
                labelTmp.alignment = TextAlignmentOptions.Center;
                labelTmp.raycastTarget = false;

                float colWidth = 800f / 7f;

                for (int c = 0; c < floor.Count; c++)
                {
                    var node = floor[c];
                    float x = 140f + node.vCol * colWidth + colWidth / 2f + NodeJitterX(f, node.vCol);
                    float ny = y + NodeJitterY(f, node.vCol);

                    var nodeRT = MakeRect(_scrollContent, $"N{f}_{c}",
                        new Vector2(0, 0), new Vector2(0, 0),
                        new Vector2(x, ny), new Vector2(80, 80));

                    var img = nodeRT.gameObject.AddComponent<Image>();
                    img.color = GetNodeColor(node);

                    string icon = GetNodeIcon(node.type);
                    MakeFixedTMP(nodeRT, "Icon", icon,
                        new Vector2(0, 4), new Vector2(70, 28), 22, Color.white);

                    MakeFixedTMP(nodeRT, "Type", node.type.ToString(),
                        new Vector2(0, -16), new Vector2(70, 20), 13, new Color(0.85f, 0.85f, 0.85f));

                    Image glowImg = null;
                    Color baseCol = img.color;

                    if (node.visited)
                    {
                        img.color *= new Color(0.5f, 0.5f, 0.5f, 0.7f);
                        baseCol = img.color;
                    }
                    else if (node.available)
                    {
                        var btn = nodeRT.gameObject.AddComponent<Button>();
                        btn.targetGraphic = img;
                        var captured = node;
                        btn.onClick.AddListener(() => OnNodeClicked(captured));

                        var glow = MakeRect(nodeRT, "Glow",
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            Vector2.zero, new Vector2(96, 96));
                        glow.SetAsFirstSibling();
                        glowImg = glow.gameObject.AddComponent<Image>();
                        glowImg.color = new Color(1, 0.85f, 0.2f, 0.4f);
                        glowImg.raycastTarget = false;

                        var trigger = nodeRT.gameObject.AddComponent<EventTrigger>();
                        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                        var capturedNode = node;
                        enterEntry.callback.AddListener(_ => OnNodeHoverEnter(capturedNode));
                        trigger.triggers.Add(enterEntry);

                        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                        exitEntry.callback.AddListener(_ => OnNodeHoverExit(capturedNode));
                        trigger.triggers.Add(exitEntry);
                    }
                    else
                    {
                        img.color *= new Color(0.6f, 0.6f, 0.6f, 0.5f);
                        baseCol = img.color;
                    }

                    _nodeButtons.Add(new NodeButton
                    {
                        rt = nodeRT, node = node, image = img,
                        glow = glowImg, baseColor = baseCol
                    });
                }
            }
        }

        void DrawConnections()
        {
            _connectionLines.Clear();
            float floorHeight = 120f;
            float padding = 60f;

            float colWidth = 800f / 7f;

            for (int f = 0; f < _map.Count - 1; f++)
            {
                var floor = _map[f];
                var nextFloor = _map[f + 1];
                float y1 = padding + f * floorHeight;
                float y2 = padding + (f + 1) * floorHeight;

                for (int c = 0; c < floor.Count; c++)
                {
                    var node = floor[c];
                    float x1 = 140f + node.vCol * colWidth + colWidth / 2f + NodeJitterX(f, node.vCol);
                    float ay1 = y1 + NodeJitterY(f, node.vCol);

                    foreach (int target in node.connections)
                    {
                        if (target >= nextFloor.Count) continue;
                        var tNode = nextFloor[target];
                        float x2 = 140f + tNode.vCol * colWidth + colWidth / 2f + NodeJitterX(f + 1, tNode.vCol);
                        float ay2 = y2 + NodeJitterY(f + 1, tNode.vCol);

                        bool isAvailableRoute = node.available && !node.visited;
                        float lineWidth = isAvailableRoute ? 4f : 3f;

                        var line = MakeRect(_scrollContent, $"L{f}_{c}_{target}",
                            new Vector2(0, 0), new Vector2(0, 0),
                            new Vector2((x1 + x2) / 2f, (ay1 + ay2) / 2f),
                            new Vector2(lineWidth, Vector2.Distance(new Vector2(x1, ay1), new Vector2(x2, ay2))));

                        float angle = Mathf.Atan2(x2 - x1, ay2 - ay1) * Mathf.Rad2Deg;
                        line.localRotation = Quaternion.Euler(0, 0, -angle);

                        var lineImg = line.gameObject.AddComponent<Image>();
                        if (node.visited)
                            lineImg.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                        else if (isAvailableRoute)
                            lineImg.color = new Color(0.6f, 0.5f, 0.2f, 0.5f);
                        else
                            lineImg.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);

                        lineImg.raycastTarget = false;
                        line.SetAsFirstSibling();

                        _connectionLines[$"{f}_{c}_{target}"] = lineImg;
                    }
                }
            }
        }

        void ScrollToFloor(int floor)
        {
            StartCoroutine(ScrollToFloorCoroutine(floor));
        }

        IEnumerator ScrollToFloorCoroutine(int floor)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            if (_scrollRect == null || _scrollContent == null) yield break;
            float floorHeight = 120f;
            float padding = 60f;
            float contentHeight = _scrollContent.sizeDelta.y;
            var viewportRT = _scrollRect.viewport;
            if (viewportRT == null) yield break;

            float viewHeight = viewportRT.rect.height;
            float maxScroll = contentHeight - viewHeight;
            if (maxScroll <= 0) yield break;

            float floorCenterY = padding + floor * floorHeight;
            float scrollTarget = floorCenterY - viewHeight * 0.5f;
            float normalized = Mathf.Clamp01(scrollTarget / maxScroll);
            _scrollRect.verticalNormalizedPosition = normalized;
        }

        void HighlightReachableRoutes()
        {
            var reachableColumns = new HashSet<int>();
            int startFloor = -1;

            for (int f = 0; f < _map.Count; f++)
            {
                for (int c = 0; c < _map[f].Count; c++)
                {
                    if (_map[f][c].available && !_map[f][c].visited)
                    {
                        reachableColumns.Add(c);
                        startFloor = f;
                    }
                }
                if (reachableColumns.Count > 0) break;
            }

            if (startFloor < 0) return;

            var reachableNodeKeys = new HashSet<string>();

            for (int f = startFloor; f < _map.Count - 1; f++)
            {
                var nextColumns = new HashSet<int>();

                foreach (int c in reachableColumns)
                {
                    if (c >= _map[f].Count) continue;
                    var node = _map[f][c];

                    foreach (int target in node.connections)
                    {
                        nextColumns.Add(target);
                        reachableNodeKeys.Add($"{f + 1}_{target}");

                        if (!node.visited && !node.available)
                        {
                            string key = $"{f}_{c}_{target}";
                            if (_connectionLines.TryGetValue(key, out var lineImg))
                            {
                                lineImg.color = new Color(0.7f, 0.55f, 0.15f, 0.5f);
                                lineImg.rectTransform.sizeDelta = new Vector2(3.5f, lineImg.rectTransform.sizeDelta.y);
                            }
                        }
                    }
                }

                reachableColumns = nextColumns;
            }

            foreach (var nb in _nodeButtons)
            {
                if (nb.node.visited || nb.node.available) continue;
                if (reachableNodeKeys.Contains($"{nb.node.floor}_{nb.node.column}"))
                {
                    var c = nb.baseColor;
                    nb.image.color = new Color(
                        Mathf.Min(c.r * 1.5f, 1f),
                        Mathf.Min(c.g * 1.5f, 1f),
                        Mathf.Min(c.b * 1.5f, 1f),
                        Mathf.Min(c.a * 2f, 0.85f));
                }
            }
        }

        void OnNodeHoverEnter(MapNode node)
        {
            foreach (int target in node.connections)
            {
                string key = $"{node.floor}_{node.column}_{target}";
                if (_connectionLines.TryGetValue(key, out var lineImg))
                {
                    lineImg.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                    lineImg.rectTransform.sizeDelta = new Vector2(6f, lineImg.rectTransform.sizeDelta.y);
                }

                if (node.floor + 1 < _map.Count && target < _map[node.floor + 1].Count)
                {
                    var targetNode = _map[node.floor + 1][target];
                    foreach (var nb in _nodeButtons)
                    {
                        if (nb.node == targetNode)
                        {
                            nb.image.color = nb.baseColor * 1.5f;
                            break;
                        }
                    }
                }
            }

            foreach (var nb in _nodeButtons)
            {
                if (nb.node == node && nb.glow != null)
                {
                    nb.glow.color = new Color(1, 0.85f, 0.2f, 0.7f);
                    nb.glow.rectTransform.sizeDelta = new Vector2(104, 104);
                    break;
                }
            }
        }

        void OnNodeHoverExit(MapNode node)
        {
            foreach (int target in node.connections)
            {
                string key = $"{node.floor}_{node.column}_{target}";
                if (_connectionLines.TryGetValue(key, out var lineImg))
                {
                    lineImg.color = new Color(0.6f, 0.5f, 0.2f, 0.5f);
                    lineImg.rectTransform.sizeDelta = new Vector2(4f, lineImg.rectTransform.sizeDelta.y);
                }

                if (node.floor + 1 < _map.Count && target < _map[node.floor + 1].Count)
                {
                    var targetNode = _map[node.floor + 1][target];
                    foreach (var nb in _nodeButtons)
                    {
                        if (nb.node == targetNode)
                        {
                            nb.image.color = nb.baseColor;
                            break;
                        }
                    }
                }
            }

            foreach (var nb in _nodeButtons)
            {
                if (nb.node == node && nb.glow != null)
                {
                    nb.glow.color = new Color(1, 0.85f, 0.2f, 0.4f);
                    nb.glow.rectTransform.sizeDelta = new Vector2(96, 96);
                    break;
                }
            }
        }

        void OnNodeClicked(MapNode node)
        {
            if (!node.available) return;
            node.visited = true;

            foreach (var n in _map[node.floor])
                n.available = false;

            if (node.floor + 1 < _map.Count)
            {
                foreach (int conn in node.connections)
                {
                    if (conn < _map[node.floor + 1].Count)
                        _map[node.floor + 1][conn].available = true;
                }
            }

            Hide();
            OnNodeSelected?.Invoke(node);
        }

        Color GetNodeColor(MapNode node)
        {
            return node.type switch
            {
                NodeType.Normal => new Color(0.25f, 0.2f, 0.35f),
                NodeType.Elite => new Color(0.6f, 0.2f, 0.2f),
                NodeType.Rest => new Color(0.15f, 0.4f, 0.2f),
                NodeType.Shop => new Color(0.5f, 0.4f, 0.1f),
                NodeType.Event => new Color(0.2f, 0.3f, 0.5f),
                NodeType.Boss => new Color(0.7f, 0.15f, 0.15f),
                _ => new Color(0.3f, 0.3f, 0.3f)
            };
        }

        string GetNodeIcon(NodeType type)
        {
            return type switch
            {
                NodeType.Normal => "[!]",
                NodeType.Elite => "[!!]",
                NodeType.Rest => "[R]",
                NodeType.Shop => "[$]",
                NodeType.Event => "[?]",
                NodeType.Boss => "[BOSS]",
                _ => "[?]"
            };
        }

        float NodeJitterX(int floor, int vCol)
        {
            return (Mathf.PerlinNoise(floor * 7.3f, vCol * 13.7f) - 0.5f) * 40f;
        }

        float NodeJitterY(int floor, int vCol)
        {
            return (Mathf.PerlinNoise(floor * 11.1f + 100f, vCol * 5.9f + 100f) - 0.5f) * 30f;
        }

        RectTransform MakeRect(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        TextMeshProUGUI MakeTMP(RectTransform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, int size, Color color)
        {
            var rt = MakeRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            FontManager.ApplyTo(tmp);
            return tmp;
        }

        TextMeshProUGUI MakeFixedTMP(RectTransform parent, string name, string text,
            Vector2 pos, Vector2 size, int fontSize, Color color)
        {
            var rt = MakeRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            FontManager.ApplyTo(tmp);
            return tmp;
        }
    }
}
