using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Card;
using SpellboundAscent.Core;
using SpellboundAscent.Relic;
using SpellboundAscent.Utils;

namespace SpellboundAscent.UI
{
    public static class RelicCodexUI
    {
        static readonly Dictionary<string, string> ClassNames = new()
        {
            { "any", "공용" },
            { "pyromancer", "화염술사" },
            { "warden", "수호자" },
            { "chronomancer", "시간술사" },
            { "necromancer", "사령술사" }
        };
        static readonly Dictionary<string, string> TriggerNames = new()
        {
            { "battleStart", "전투 시작" },
            { "turnStart", "턴 시작" },
            { "onCardPlayed", "카드 사용" },
            { "onAttackPlayed", "공격 사용" },
            { "onSkillPlayed", "스킬 사용" },
            { "onKill", "적 처치" },
            { "onExhaust", "카드 소멸" },
            { "onTurnEnd", "턴 종료" },
            { "passive", "상시" },
            { "onDamaged", "피격" }
        };

        static string _classFilter = "all";
        static List<Image> _filterImages = new();
        static List<TextMeshProUGUI> _filterTexts = new();
        static RectTransform _contentArea;
        static RectTransform _root;
        static TextMeshProUGUI _countText;

        public static void Show()
        {
            _classFilter = "all";

            var relics = GameManager.Instance.GetRelics();
            if (relics == null || relics.Count == 0) return;

            var go = new GameObject("RelicCodex");
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
            bg.GetComponent<Image>().raycastTarget = false;

            MakeTMP(_root, "Title", "유물 도감",
                new Vector2(0.04f, 0.95f), new Vector2(0.5f, 0.995f), 36,
                new Color(0.8f, 0.6f, 1f));

            _countText = MakeTMP(_root, "Count", $"{relics.Count}종",
                new Vector2(0.5f, 0.955f), new Vector2(0.74f, 0.99f), 20,
                new Color(0.6f, 0.6f, 0.6f));
            _countText.alignment = TextAlignmentOptions.MidlineRight;

            var closeRT = MakeRect(_root, "Close",
                new Vector2(0.78f, 0.955f), new Vector2(0.97f, 0.99f), Vector2.zero, Vector2.zero);
            closeRT.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeRT.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeRT.GetComponent<Image>();
            closeBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(go));
            MakeTMP(closeRT, "Txt", "닫기", Vector2.zero, Vector2.one, 22, Color.white)
                .alignment = TextAlignmentOptions.Center;

            BuildFilterBar();

            _contentArea = MakeRect(_root, "ContentArea",
                new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.905f), Vector2.zero, Vector2.zero);

            RebuildList();
        }

        static void BuildFilterBar()
        {
            _filterImages.Clear();
            _filterTexts.Clear();

            var bar = MakeRect(_root, "FilterBar",
                new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
            bar.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.07f, 0.12f, 0.95f);
            bar.GetComponent<Image>().raycastTarget = false;

            string[] filters = { "all", "any", "pyromancer", "warden", "chronomancer", "necromancer" };
            string[] labels = { "전체", "공용", "화염", "수호", "시간", "사령" };

            float w = 1f / filters.Length;
            for (int i = 0; i < filters.Length; i++)
            {
                float x = i * w;
                var btnRT = MakeRect(bar, $"F_{filters[i]}",
                    new Vector2(x + 0.004f, 0.08f), new Vector2(x + w - 0.004f, 0.92f),
                    Vector2.zero, Vector2.zero);
                var btnImg = btnRT.gameObject.AddComponent<Image>();
                _filterImages.Add(btnImg);
                var btn = btnRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                string f = filters[i];
                btn.onClick.AddListener(() => { _classFilter = f; RefreshFilterVisuals(); RebuildList(); });
                var txt = MakeTMP(btnRT, "Txt", labels[i],
                    Vector2.zero, Vector2.one, 17, Color.white);
                txt.alignment = TextAlignmentOptions.Center;
                _filterTexts.Add(txt);
            }

            RefreshFilterVisuals();
        }

        static void RefreshFilterVisuals()
        {
            string[] filters = { "all", "any", "pyromancer", "warden", "chronomancer", "necromancer" };
            for (int i = 0; i < _filterImages.Count; i++)
            {
                bool active = filters[i] == _classFilter;
                _filterImages[i].color = active
                    ? new Color(0.35f, 0.2f, 0.45f, 0.95f)
                    : new Color(0.15f, 0.13f, 0.2f, 0.8f);
                _filterTexts[i].color = active ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            }
        }

        static void RebuildList()
        {
            if (_contentArea == null) return;
            foreach (Transform child in _contentArea)
                UnityEngine.Object.Destroy(child.gameObject);

            var allRelics = GameManager.Instance.GetRelics();
            var filtered = _classFilter == "all"
                ? new List<RelicData>(allRelics)
                : allRelics.FindAll(r => r.@class == _classFilter);

            filtered.Sort((a, b) =>
            {
                int ra = RarityOrder(a.rarity), rb = RarityOrder(b.rarity);
                if (ra != rb) return ra.CompareTo(rb);
                int ca = ClassOrder(a.@class), cb = ClassOrder(b.@class);
                if (ca != cb) return ca.CompareTo(cb);
                return string.Compare(a.id, b.id, StringComparison.Ordinal);
            });

            _countText.text = $"{filtered.Count}종";

            var scrollContainer = MakeRect(_contentArea, "Scroll",
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

            int cols = 2;
            float padX = 10f;
            float padY = 8f;
            float totalW = 1060f;
            float cardW = (totalW - padX * 3) / cols;
            float cardH = 180f;

            int rows = Mathf.CeilToInt(filtered.Count / (float)cols);
            float totalH = rows * (cardH + padY) + padY;
            content.sizeDelta = new Vector2(0, totalH);

            for (int i = 0; i < filtered.Count; i++)
            {
                var relic = filtered[i];
                int col = i % cols;
                int row = i / cols;

                float x = padX + col * (cardW + padX);
                float y = -padY - row * (cardH + padY);

                var relicRT = new GameObject($"Relic_{relic.id}", typeof(RectTransform)).GetComponent<RectTransform>();
                relicRT.SetParent(content, false);
                relicRT.anchorMin = new Vector2(0, 1);
                relicRT.anchorMax = new Vector2(0, 1);
                relicRT.pivot = new Vector2(0, 1);
                relicRT.anchoredPosition = new Vector2(x, y);
                relicRT.sizeDelta = new Vector2(cardW, cardH);

                Color bgColor = relic.rarity switch
                {
                    "rare" => new Color(0.2f, 0.15f, 0.08f, 0.92f),
                    "uncommon" => new Color(0.1f, 0.12f, 0.22f, 0.92f),
                    "boss" => new Color(0.22f, 0.08f, 0.08f, 0.92f),
                    _ => new Color(0.12f, 0.12f, 0.14f, 0.92f)
                };
                relicRT.gameObject.AddComponent<Image>().color = bgColor;

                Color borderColor = relic.rarity switch
                {
                    "rare" => new Color(1f, 0.85f, 0.2f, 0.5f),
                    "uncommon" => new Color(0.4f, 0.7f, 1f, 0.4f),
                    "boss" => new Color(0.9f, 0.3f, 0.3f, 0.5f),
                    _ => new Color(0.35f, 0.35f, 0.35f, 0.3f)
                };
                var outline = relicRT.gameObject.AddComponent<Outline>();
                outline.effectColor = borderColor;
                outline.effectDistance = new Vector2(2, -2);

                float textLeft = 0.04f;
                var relicSprite = SpriteManager.GetRelic(relic.id);
                if (relicSprite != null)
                {
                    var iconRT = MakeRect(relicRT, "Icon",
                        new Vector2(0.01f, 0.08f), new Vector2(0.24f, 0.95f), Vector2.zero, Vector2.zero);
                    var iconImg = iconRT.gameObject.AddComponent<Image>();
                    iconImg.sprite = relicSprite;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                    textLeft = 0.26f;
                }

                // Name
                string name = !string.IsNullOrEmpty(relic.nameKo) ? relic.nameKo : relic.name;
                var nameT = MakeTMP(relicRT, "Name", name,
                    new Vector2(textLeft, 0.72f), new Vector2(0.96f, 0.97f), 28, Color.white);
                nameT.fontStyle = FontStyles.Bold;
                nameT.enableAutoSizing = true;
                nameT.fontSizeMin = 20;
                nameT.fontSizeMax = 28;

                // Tags (rarity + class + trigger)
                string rarityKo = relic.rarity switch
                {
                    "common" => "일반", "uncommon" => "고급", "rare" => "희귀", "boss" => "보스", _ => relic.rarity
                };
                string classKo = ClassNames.TryGetValue(relic.@class, out var cn) ? cn : relic.@class;
                string triggerKo = TriggerNames.TryGetValue(relic.trigger, out var tn) ? tn : relic.trigger;

                Color tagColor = relic.rarity switch
                {
                    "rare" => new Color(1, 0.85f, 0.2f),
                    "uncommon" => new Color(0.5f, 0.75f, 1f),
                    "boss" => new Color(1f, 0.4f, 0.4f),
                    _ => new Color(0.55f, 0.55f, 0.55f)
                };
                MakeTMP(relicRT, "Tags", $"{rarityKo} · {classKo} · {triggerKo}",
                    new Vector2(textLeft, 0.58f), new Vector2(0.96f, 0.73f), 15, tagColor);

                // Description
                string desc = !string.IsNullOrEmpty(relic.descriptionKo) ? relic.descriptionKo : relic.description;
                if (string.IsNullOrEmpty(desc))
                    desc = CardInstance.BuildEffectDesc(relic.effects, false, true);
                var descT = MakeTMP(relicRT, "Desc", desc,
                    new Vector2(textLeft, 0.2f), new Vector2(0.96f, 0.58f), 17,
                    new Color(0.8f, 0.8f, 0.8f));
                descT.alignment = TextAlignmentOptions.TopLeft;

                // Condition
                if (relic.condition != null && !string.IsNullOrEmpty(relic.condition.type))
                {
                    string condStr = relic.condition.type switch
                    {
                        "deckSizeBelow" => $"덱 {(int)relic.condition.value}장 이하",
                        "hpBelow" => $"HP {(int)relic.condition.value}% 이하",
                        "emberAbove" => $"잔불 {(int)relic.condition.value} 이상",
                        _ => $"{relic.condition.type}: {relic.condition.value}"
                    };
                    MakeTMP(relicRT, "Cond", $"조건: {condStr}",
                        new Vector2(textLeft, 0.03f), new Vector2(0.96f, 0.2f), 14,
                        new Color(1f, 0.7f, 0.3f));
                }

                // Flavor text
                if (!string.IsNullOrEmpty(relic.flavorText) && relic.condition == null)
                {
                    MakeTMP(relicRT, "Flavor", $"\"{relic.flavorText}\"",
                        new Vector2(textLeft, 0.03f), new Vector2(0.96f, 0.2f), 13,
                        new Color(0.45f, 0.45f, 0.5f));
                }
            }
        }

        static int RarityOrder(string r) => r switch
        {
            "common" => 0, "uncommon" => 1, "rare" => 2, "boss" => 3, "shop" => 4, "event" => 5, _ => 6
        };

        static int ClassOrder(string c) => c switch
        {
            "any" => 0, "pyromancer" => 1, "warden" => 2, "chronomancer" => 3, "necromancer" => 4, _ => 5
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
