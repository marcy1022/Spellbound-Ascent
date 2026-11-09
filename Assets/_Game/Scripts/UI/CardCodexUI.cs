using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Card;
using SpellboundAscent.Core;
using SpellboundAscent.Utils;

namespace SpellboundAscent.UI
{
    public static class CardCodexUI
    {
        static readonly string[] ClassOrder = { "pyromancer", "warden", "chronomancer", "necromancer", "neutral" };
        static readonly Dictionary<string, string> ClassNames = new()
        {
            { "pyromancer", "화염술사" },
            { "warden", "수호자" },
            { "chronomancer", "시간술사" },
            { "necromancer", "사령술사" },
            { "neutral", "중립" }
        };
        static readonly Dictionary<string, Color> ClassColors = new()
        {
            { "pyromancer", new Color(0.9f, 0.3f, 0.2f) },
            { "warden", new Color(0.3f, 0.8f, 0.4f) },
            { "chronomancer", new Color(0.4f, 0.6f, 1f) },
            { "necromancer", new Color(0.7f, 0.3f, 0.9f) },
            { "neutral", new Color(0.6f, 0.6f, 0.6f) }
        };

        static RectTransform _root;
        static RectTransform _tabContent;
        static List<Image> _tabBtnImages = new();
        static List<TextMeshProUGUI> _tabBtnTexts = new();
        static int _activeTab;

        static string _typeFilter = "all";
        static string _sortMode = "cost";
        static bool _showUpgraded;

        static List<Image> _filterBtnImages = new();
        static List<TextMeshProUGUI> _filterBtnTexts = new();
        static List<Image> _sortBtnImages = new();
        static List<TextMeshProUGUI> _sortBtnTexts = new();
        static Image _upgToggleImg;
        static TextMeshProUGUI _upgToggleText;
        static TextMeshProUGUI _countText;

        public static void Show()
        {
            _typeFilter = "all";
            _sortMode = "cost";
            _showUpgraded = false;

            var go = new GameObject("CardCodex");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            _root = go.GetComponent<RectTransform>();

            var bg = MakeRect(_root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.98f);
            bg.gameObject.GetComponent<Image>().raycastTarget = false;

            // Header
            MakeTMP(_root, "Title", "카드 도감",
                new Vector2(0.04f, 0.955f), new Vector2(0.5f, 0.995f), 34,
                new Color(1, 0.85f, 0.2f));

            _countText = MakeTMP(_root, "Count", "",
                new Vector2(0.5f, 0.955f), new Vector2(0.74f, 0.995f), 20,
                new Color(0.6f, 0.6f, 0.6f));
            _countText.alignment = TextAlignmentOptions.MidlineRight;

            var closeRT = MakeRect(_root, "Close",
                new Vector2(0.78f, 0.958f), new Vector2(0.97f, 0.992f), Vector2.zero, Vector2.zero);
            closeRT.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeRT.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeRT.gameObject.GetComponent<Image>();
            closeBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(go));
            MakeTMP(closeRT, "Txt", "닫기", Vector2.zero, Vector2.one, 22, Color.white)
                .alignment = TextAlignmentOptions.Center;

            BuildClassTabs();
            BuildFilterBar();

            _activeTab = 0;
            SwitchTab(0);
        }

        static void BuildClassTabs()
        {
            _tabBtnImages.Clear();
            _tabBtnTexts.Clear();

            var tabBar = MakeRect(_root, "TabBar",
                new Vector2(0.02f, 0.91f), new Vector2(0.98f, 0.955f), Vector2.zero, Vector2.zero);

            float tabW = 1f / ClassOrder.Length;
            for (int i = 0; i < ClassOrder.Length; i++)
            {
                string cls = ClassOrder[i];
                float x = i * tabW;

                var tabRT = MakeRect(tabBar, $"Tab_{cls}",
                    new Vector2(x + 0.004f, 0.06f), new Vector2(x + tabW - 0.004f, 0.94f),
                    Vector2.zero, Vector2.zero);

                var tabImg = tabRT.gameObject.AddComponent<Image>();
                _tabBtnImages.Add(tabImg);

                var tabBtn = tabRT.gameObject.AddComponent<Button>();
                tabBtn.targetGraphic = tabImg;
                int idx = i;
                tabBtn.onClick.AddListener(() => SwitchTab(idx));

                var pool = GameManager.Instance.GetCardPool(cls);
                int count = pool.FindAll(c => c.@class == cls && c.rarity != "starter").Count;

                var txt = MakeTMP(tabRT, "Txt", $"{ClassNames[cls]}\n<size=16>{count}장</size>",
                    Vector2.zero, Vector2.one, 20, Color.white);
                txt.alignment = TextAlignmentOptions.Center;
                _tabBtnTexts.Add(txt);
            }
        }

        static void BuildFilterBar()
        {
            _filterBtnImages.Clear();
            _filterBtnTexts.Clear();
            _sortBtnImages.Clear();
            _sortBtnTexts.Clear();

            var filterBar = MakeRect(_root, "FilterBar",
                new Vector2(0.02f, 0.835f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero);
            var filterBg = filterBar.gameObject.AddComponent<Image>();
            filterBg.color = new Color(0.08f, 0.07f, 0.12f, 0.95f);
            filterBg.raycastTarget = false;

            // Type filters
            string[] types = { "all", "attack", "skill", "power" };
            string[] typeLabels = { "전체", "공격", "스킬", "파워" };
            for (int i = 0; i < types.Length; i++)
            {
                float x = 0.01f + i * 0.145f;
                var btnRT = MakeRect(filterBar, $"Filter_{types[i]}",
                    new Vector2(x, 0.08f), new Vector2(x + 0.14f, 0.92f),
                    Vector2.zero, Vector2.zero);
                var btnImg = btnRT.gameObject.AddComponent<Image>();
                _filterBtnImages.Add(btnImg);
                var btn = btnRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                string t = types[i];
                btn.onClick.AddListener(() => { _typeFilter = t; RefreshFilterVisuals(); RebuildCards(); });
                var txt = MakeTMP(btnRT, "Txt", typeLabels[i],
                    Vector2.zero, Vector2.one, 22, Color.white);
                txt.alignment = TextAlignmentOptions.Center;
                _filterBtnTexts.Add(txt);
            }

            // Sort buttons
            var sortLabel = MakeTMP(filterBar, "SortLabel", "정렬:",
                new Vector2(0.59f, 0), new Vector2(0.67f, 1), 20, new Color(0.5f, 0.5f, 0.5f));
            sortLabel.alignment = TextAlignmentOptions.Center;

            string[] sorts = { "cost", "rarity", "name" };
            string[] sortLabels = { "비용", "등급", "이름" };
            for (int i = 0; i < sorts.Length; i++)
            {
                float x = 0.67f + i * 0.11f;
                var btnRT = MakeRect(filterBar, $"Sort_{sorts[i]}",
                    new Vector2(x, 0.08f), new Vector2(x + 0.105f, 0.92f),
                    Vector2.zero, Vector2.zero);
                var btnImg = btnRT.gameObject.AddComponent<Image>();
                _sortBtnImages.Add(btnImg);
                var btn = btnRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                string s = sorts[i];
                btn.onClick.AddListener(() => { _sortMode = s; RefreshFilterVisuals(); RebuildCards(); });
                var txt = MakeTMP(btnRT, "Txt", sortLabels[i],
                    Vector2.zero, Vector2.one, 20, Color.white);
                txt.alignment = TextAlignmentOptions.Center;
                _sortBtnTexts.Add(txt);
            }

            // Upgrade toggle
            var upgRT = MakeRect(_root, "UpgToggle",
                new Vector2(0.02f, 0.793f), new Vector2(0.98f, 0.833f), Vector2.zero, Vector2.zero);
            _upgToggleImg = upgRT.gameObject.AddComponent<Image>();
            var upgBtn = upgRT.gameObject.AddComponent<Button>();
            upgBtn.targetGraphic = _upgToggleImg;
            upgBtn.onClick.AddListener(() => { _showUpgraded = !_showUpgraded; RefreshFilterVisuals(); RebuildCards(); });
            _upgToggleText = MakeTMP(upgRT, "Txt", "",
                Vector2.zero, Vector2.one, 20, Color.white);
            _upgToggleText.alignment = TextAlignmentOptions.Center;

            _tabContent = MakeRect(_root, "TabContent",
                new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.79f), Vector2.zero, Vector2.zero);

            RefreshFilterVisuals();
        }

        static void RefreshFilterVisuals()
        {
            string[] types = { "all", "attack", "skill", "power" };
            for (int i = 0; i < _filterBtnImages.Count; i++)
            {
                bool active = types[i] == _typeFilter;
                _filterBtnImages[i].color = active
                    ? new Color(0.3f, 0.25f, 0.5f, 0.95f)
                    : new Color(0.15f, 0.13f, 0.2f, 0.8f);
                _filterBtnTexts[i].color = active ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            }

            string[] sorts = { "cost", "rarity", "name" };
            for (int i = 0; i < _sortBtnImages.Count; i++)
            {
                bool active = sorts[i] == _sortMode;
                _sortBtnImages[i].color = active
                    ? new Color(0.25f, 0.35f, 0.25f, 0.95f)
                    : new Color(0.15f, 0.13f, 0.2f, 0.8f);
                _sortBtnTexts[i].color = active ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            }

            if (_upgToggleImg != null)
            {
                _upgToggleImg.color = _showUpgraded
                    ? new Color(0.15f, 0.4f, 0.2f, 0.95f)
                    : new Color(0.12f, 0.1f, 0.18f, 0.95f);
                _upgToggleText.text = _showUpgraded
                    ? "강화 상태 보기: ON  (강화+ 수치 표시 중)"
                    : "강화 상태 보기: OFF  (기본 수치 표시 중)";
                _upgToggleText.color = _showUpgraded
                    ? new Color(0.4f, 1f, 0.5f)
                    : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        static void SwitchTab(int index)
        {
            _activeTab = index;
            for (int i = 0; i < _tabBtnImages.Count; i++)
            {
                bool active = i == index;
                string cls = ClassOrder[i];
                Color c = ClassColors[cls];
                _tabBtnImages[i].color = active
                    ? new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.95f)
                    : new Color(0.12f, 0.1f, 0.18f, 0.9f);
                _tabBtnTexts[i].color = active ? Color.white : new Color(0.45f, 0.45f, 0.45f);
            }
            RebuildCards();
        }

        static void RebuildCards()
        {
            if (_tabContent == null) return;

            foreach (Transform child in _tabContent)
                UnityEngine.Object.Destroy(child.gameObject);

            string className = ClassOrder[_activeTab];
            var allCards = GameManager.Instance.GetCardPool(className);
            var filtered = allCards.FindAll(c =>
                c.@class == className &&
                c.rarity != "starter" &&
                (_typeFilter == "all" || c.type == _typeFilter));

            filtered.Sort((a, b) =>
            {
                switch (_sortMode)
                {
                    case "cost":
                        if (a.cost != b.cost) return a.cost.CompareTo(b.cost);
                        return RarityOrder(a.rarity).CompareTo(RarityOrder(b.rarity));
                    case "rarity":
                        int ra = RarityOrder(a.rarity), rb = RarityOrder(b.rarity);
                        if (ra != rb) return ra.CompareTo(rb);
                        return a.cost.CompareTo(b.cost);
                    case "name":
                        string na = !string.IsNullOrEmpty(a.nameKo) ? a.nameKo : a.name;
                        string nb = !string.IsNullOrEmpty(b.nameKo) ? b.nameKo : b.name;
                        return string.Compare(na, nb, StringComparison.Ordinal);
                    default:
                        return 0;
                }
            });

            if (_countText != null)
                _countText.text = $"{filtered.Count}장";

            BuildCardGrid(filtered);
        }

        static void BuildCardGrid(List<CardData> cards)
        {
            var scrollContainer = MakeRect(_tabContent, "Scroll",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
            scrollRect.scrollSensitivity = 40f;

            int cols = 3;
            float padX = 6f;
            float padY = 8f;
            float totalW = 1060f;
            float cardW = (totalW - padX * (cols + 1)) / cols;
            float cardH = cardW * 1.4f;

            int rows = Mathf.CeilToInt(cards.Count / (float)cols);
            float totalH = rows * (cardH + padY) + padY;
            content.sizeDelta = new Vector2(0, totalH);

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                int col = i % cols;
                int row = i / cols;

                float x = padX + col * (cardW + padX);
                float y = -padY - row * (cardH + padY);

                var cardRT = new GameObject($"Card_{card.id}", typeof(RectTransform)).GetComponent<RectTransform>();
                cardRT.SetParent(content, false);
                cardRT.anchorMin = new Vector2(0, 1);
                cardRT.anchorMax = new Vector2(0, 1);
                cardRT.pivot = new Vector2(0, 1);
                cardRT.anchoredPosition = new Vector2(x, y);
                cardRT.sizeDelta = new Vector2(cardW, cardH);

                var codexImg = cardRT.gameObject.AddComponent<Image>();
                var codexFrame = SpriteManager.GetCardFrame(card.type);
                if (codexFrame != null)
                {
                    codexImg.sprite = codexFrame;
                    codexImg.type = Image.Type.Simple;
                    codexImg.color = Color.white;
                }
                else
                {
                    Color bgColor = card.type switch
                    {
                        "attack" => new Color(0.28f, 0.1f, 0.1f, 0.92f),
                        "skill" => new Color(0.1f, 0.15f, 0.3f, 0.92f),
                        "power" => new Color(0.22f, 0.1f, 0.3f, 0.92f),
                        _ => new Color(0.18f, 0.18f, 0.18f, 0.92f)
                    };
                    codexImg.color = bgColor;
                }

                bool showUpg = _showUpgraded && card.upgradeEffects != null && card.upgradeEffects.Count > 0;
                bool hasCostReduce = showUpg && card.upgradeEffects.Exists(e => e.type == "costReduction");
                int displayCost = hasCostReduce ? Mathf.Max(0, card.cost - 1) : card.cost;
                Color costColor = hasCostReduce ? new Color(0.3f, 1f, 0.4f) : Color.white;

                float gemYOff = card.type switch { "skill" => 0.01f, "power" => -0.015f, _ => 0f };
                var costGem = MakeRect(cardRT, "CostGem",
                    new Vector2(0.02f, 0.73f + gemYOff), new Vector2(0.35f, 0.95f + gemYOff), Vector2.zero, Vector2.zero);
                var costGemImg = costGem.gameObject.AddComponent<Image>();
                var gemSprite = SpriteManager.GetCostGem(displayCost);
                if (gemSprite != null)
                {
                    costGemImg.sprite = gemSprite;
                    costGemImg.color = Color.white;
                }
                else
                {
                    costGemImg.color = new Color(0.15f, 0.1f, 0.25f, 0.9f);
                }
                costGemImg.raycastTarget = false;

                var costText = MakeTMP(costGem, "Cost", displayCost.ToString(),
                    Vector2.zero, Vector2.one, 22, costColor);
                costText.alignment = TextAlignmentOptions.Center;
                costText.outlineWidth = 0.3f;
                costText.outlineColor = Color.black;

                string nameStr = !string.IsNullOrEmpty(card.nameKo) ? card.nameKo : card.name;
                string suffix = showUpg ? " <color=#44ff44>+</color>" : "";

                var nameT = MakeTMP(cardRT, "Name", nameStr + suffix,
                    new Vector2(0.14f, 0.55f), new Vector2(0.86f, 0.75f), 22, Color.white);
                nameT.fontStyle = FontStyles.Bold;
                nameT.alignment = TextAlignmentOptions.Center;
                nameT.enableWordWrapping = false;
                nameT.overflowMode = TextOverflowModes.Ellipsis;

                string typeKo = card.type switch
                {
                    "attack" => "공격", "skill" => "스킬", "power" => "파워", _ => card.type
                };
                string rarityKo = card.rarity switch
                {
                    "common" => "일반", "uncommon" => "고급", "rare" => "희귀", _ => card.rarity
                };
                Color rarityTextColor = card.rarity switch
                {
                    "rare" => new Color(1, 0.85f, 0.2f),
                    "uncommon" => new Color(0.5f, 0.75f, 1f),
                    _ => new Color(0.55f, 0.55f, 0.55f)
                };
                var typeTag = MakeTMP(cardRT, "TypeRarity", $"{typeKo} · {rarityKo}",
                    new Vector2(0.14f, 0.48f), new Vector2(0.86f, 0.55f), 20, rarityTextColor);
                typeTag.alignment = TextAlignmentOptions.Center;

                var effects = showUpg ? card.upgradeEffects : card.effects;
                string desc = CardInstance.BuildEffectDesc(effects, card.exhaust, true);
                Color descColor = showUpg ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.82f, 0.82f, 0.82f);

                var descT = MakeTMP(cardRT, "Desc", desc,
                    new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.46f), 20, descColor);
                descT.alignment = TextAlignmentOptions.Center;
            }
        }

        static int RarityOrder(string r) => r switch
        {
            "starter" => 0, "common" => 1, "uncommon" => 2, "rare" => 3, _ => 4
        };

        static RectTransform MakeRect(RectTransform parent, string name,
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

        static TextMeshProUGUI MakeTMP(RectTransform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, int size, Color color)
        {
            var rt = MakeRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = Mathf.Max(size, 20);
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            FontManager.ApplyTo(tmp);
            return tmp;
        }
    }
}
