using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Card;
using SpellboundAscent.Core;
using SpellboundAscent.Utils;

namespace SpellboundAscent.UI
{
    public static class PotionCodexUI
    {
        public static void Show()
        {
            var potions = GameManager.Instance.GetPotionPool();
            if (potions == null || potions.Count == 0) return;

            var go = new GameObject("PotionCodex");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.98f);
            bg.gameObject.GetComponent<Image>().raycastTarget = false;

            MakeTMP(root, "Title", $"물약 도감 ({potions.Count}종)",
                new Vector2(0.04f, 0.94f), new Vector2(0.7f, 0.99f), 36,
                new Color(0.3f, 1f, 0.5f));

            var closeRT = MakeRect(root, "Close",
                new Vector2(0.78f, 0.948f), new Vector2(0.97f, 0.988f), Vector2.zero, Vector2.zero);
            closeRT.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeRT.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeRT.GetComponent<Image>();
            closeBtn.onClick.AddListener(() => Object.Destroy(go));
            MakeTMP(closeRT, "Txt", "닫기", Vector2.zero, Vector2.one, 22, Color.white)
                .alignment = TextAlignmentOptions.Center;

            MakeTMP(root, "Hint", "전투 중 에너지 소모 없이 사용. 최대 3슬롯 소지.",
                new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.945f), 18,
                new Color(0.5f, 0.5f, 0.5f));

            // Scroll
            var scrollContainer = MakeRect(root, "Scroll",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero);
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

            var sorted = new List<PotionData>(potions);
            sorted.Sort((a, b) =>
            {
                int ra = RarityOrder(a.rarity), rb = RarityOrder(b.rarity);
                if (ra != rb) return ra.CompareTo(rb);
                return string.Compare(a.id, b.id);
            });

            float cardH = 160f;
            float gap = 10f;
            float yOff = -gap;
            string currentRarity = "";

            foreach (var pot in sorted)
            {
                if (pot.rarity != currentRarity)
                {
                    currentRarity = pot.rarity;
                    string rarityKo = pot.rarity switch
                    {
                        "common" => "일반 (Common)",
                        "uncommon" => "고급 (Uncommon)",
                        "rare" => "희귀 (Rare)",
                        _ => pot.rarity
                    };

                    var labelRT = new GameObject("RarityLabel", typeof(RectTransform)).GetComponent<RectTransform>();
                    labelRT.SetParent(content, false);
                    labelRT.anchorMin = new Vector2(0, 1);
                    labelRT.anchorMax = new Vector2(1, 1);
                    labelRT.pivot = new Vector2(0.5f, 1);
                    labelRT.anchoredPosition = new Vector2(0, yOff);
                    labelRT.sizeDelta = new Vector2(0, 36);
                    labelRT.gameObject.AddComponent<Image>().color = pot.rarity switch
                    {
                        "rare" => new Color(0.3f, 0.25f, 0.1f, 0.7f),
                        "uncommon" => new Color(0.1f, 0.15f, 0.3f, 0.7f),
                        _ => new Color(0.12f, 0.12f, 0.12f, 0.7f)
                    };
                    labelRT.GetComponent<Image>().raycastTarget = false;

                    MakeTMP(labelRT, "Txt", rarityKo,
                        new Vector2(0.03f, 0), new Vector2(0.97f, 1), 20,
                        pot.rarity switch
                        {
                            "rare" => new Color(1, 0.85f, 0.2f),
                            "uncommon" => new Color(0.4f, 0.7f, 1f),
                            _ => new Color(0.65f, 0.65f, 0.65f)
                        }).fontStyle = FontStyles.Bold;

                    yOff -= 40f;
                }

                var potRT = new GameObject($"Pot_{pot.id}", typeof(RectTransform)).GetComponent<RectTransform>();
                potRT.SetParent(content, false);
                potRT.anchorMin = new Vector2(0, 1);
                potRT.anchorMax = new Vector2(1, 1);
                potRT.pivot = new Vector2(0.5f, 1);
                potRT.anchoredPosition = new Vector2(0, yOff);
                potRT.sizeDelta = new Vector2(-20, cardH);

                Color bgCol = pot.rarity switch
                {
                    "rare" => new Color(0.18f, 0.15f, 0.08f, 0.9f),
                    "uncommon" => new Color(0.08f, 0.12f, 0.2f, 0.9f),
                    _ => new Color(0.1f, 0.12f, 0.1f, 0.9f)
                };
                potRT.gameObject.AddComponent<Image>().color = bgCol;

                // Potion icon area
                var iconBg = MakeRect(potRT, "Icon",
                    new Vector2(0.02f, 0.2f), new Vector2(0.15f, 0.8f), Vector2.zero, Vector2.zero);
                iconBg.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.3f, 0.2f, 0.8f);
                iconBg.GetComponent<Image>().raycastTarget = false;
                MakeTMP(iconBg, "Ico", "P", Vector2.zero, Vector2.one, 28,
                    new Color(0.3f, 1f, 0.5f)).alignment = TextAlignmentOptions.Center;

                // Name
                string name = !string.IsNullOrEmpty(pot.nameKo) ? pot.nameKo : pot.name;
                var nameT = MakeTMP(potRT, "Name", name,
                    new Vector2(0.18f, 0.6f), new Vector2(0.96f, 0.95f), 26, Color.white);
                nameT.fontStyle = FontStyles.Bold;

                // Description
                string desc = !string.IsNullOrEmpty(pot.descriptionKo) ? pot.descriptionKo : pot.description;
                if (string.IsNullOrEmpty(desc))
                    desc = CardInstance.BuildEffectDesc(pot.effects, false, true);
                MakeTMP(potRT, "Desc", desc,
                    new Vector2(0.18f, 0.3f), new Vector2(0.96f, 0.62f), 19,
                    new Color(0.8f, 0.8f, 0.8f)).alignment = TextAlignmentOptions.TopLeft;

                // Effects detail
                string effectStr = BuildPotionEffects(pot.effects);
                MakeTMP(potRT, "Effects", effectStr,
                    new Vector2(0.18f, 0.04f), new Vector2(0.96f, 0.32f), 16,
                    new Color(0.5f, 0.7f, 0.55f));

                yOff -= (cardH + gap);
            }

            content.sizeDelta = new Vector2(0, Mathf.Abs(yOff) + gap);
        }

        static string BuildPotionEffects(List<EffectData> effects)
        {
            var parts = new List<string>();
            foreach (var e in effects)
            {
                int v = (int)e.value;
                switch (e.type)
                {
                    case "heal": parts.Add($"HP {v} 회복"); break;
                    case "block": parts.Add($"블록 {v}"); break;
                    case "damageAll": parts.Add($"적 전체 {v} 데미지"); break;
                    case "applyStrength": parts.Add($"힘 +{v}"); break;
                    case "applyDexterity": parts.Add($"민첩 +{v}"); break;
                    case "draw": parts.Add($"카드 {v}장 드로우"); break;
                    case "energy": parts.Add($"에너지 +{v}"); break;
                    case "applyPoison": parts.Add($"독 {v} (적 전체)"); break;
                    case "applyRegen": parts.Add($"재생 {v}"); break;
                    case "healFull": parts.Add("HP 전체 회복"); break;
                    default: parts.Add($"{e.type}: {v}"); break;
                }
            }
            return string.Join(" | ", parts);
        }

        static int RarityOrder(string r) => r switch
        {
            "common" => 0, "uncommon" => 1, "rare" => 2, _ => 3
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
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            FontManager.ApplyTo(tmp);
            return tmp;
        }
    }
}
