using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Battle;
using SpellboundAscent.Card;
using SpellboundAscent.Core;
using SpellboundAscent.Relic;
using SpellboundAscent.Utils;

namespace SpellboundAscent.UI
{
    public class RewardUI : MonoBehaviour
    {
        public event Action OnRewardComplete;

        Canvas _canvas;
        RectTransform _root;
        RectTransform _cardChoicePanel;
        List<CardData> _cardChoices;
        List<Image> _cardImages = new();
        List<Color> _cardBaseColors = new();
        int _selectedIndex = -1;
        Button _confirmBtn;
        TextMeshProUGUI _confirmText;
        TextMeshProUGUI _selectionLabel;
        RectTransform _tooltipPanel;

        public void ShowReward(RewardManager.BattleReward reward)
        {
            BuildCanvas();
            BuildBackground();

            MakeTMP(_root, "Title", "보상",
                new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.92f), 48,
                new Color(1, 0.85f, 0.2f)).alignment = TextAlignmentOptions.Center;

            float y = 0.74f;

            var run = GameManager.Instance.CurrentRun;
            run.AddGold(reward.gold);
            MakeRewardRow(y, $"골드: +{reward.gold}  (합계: {run.gold})",
                new Color(1, 0.85f, 0.2f));
            y -= 0.08f;

            if (reward.potion != null)
            {
                if (run.AddPotion(reward.potion))
                {
                    string potName = !string.IsNullOrEmpty(reward.potion.nameKo) ? reward.potion.nameKo : reward.potion.name;
                    var potRow = MakeRewardRow(y, $"       {potName}", new Color(0.3f, 0.9f, 0.4f));
                    var potSprite = SpriteManager.GetPotion(reward.potion.id);
                    if (potSprite != null)
                    {
                        var potIcon = MakeRect(potRow, "Icon",
                            new Vector2(0.02f, 0.1f), new Vector2(0.12f, 0.9f), Vector2.zero, Vector2.zero);
                        var potImg = potIcon.gameObject.AddComponent<Image>();
                        potImg.sprite = potSprite;
                        potImg.preserveAspect = true;
                        potImg.raycastTarget = false;
                    }
                    potRow.GetComponent<Image>().raycastTarget = true;
                    var potBtn = potRow.gameObject.AddComponent<Button>();
                    potBtn.targetGraphic = potRow.GetComponent<Image>();
                    var capturedPotion = reward.potion;
                    potBtn.onClick.AddListener(() => ShowPotionTooltip(capturedPotion));
                    MakeTMP(potRow, "Hint", "?",
                        new Vector2(0.9f, 0), new Vector2(1f, 1), 22,
                        new Color(0.5f, 0.8f, 0.5f)).alignment = TextAlignmentOptions.Center;
                    y -= 0.08f;
                }
            }

            if (reward.relic != null)
            {
                string relName = !string.IsNullOrEmpty(reward.relic.nameKo) ? reward.relic.nameKo : reward.relic.name;
                var relRow = MakeRewardRow(y, $"       {relName}", new Color(0.8f, 0.6f, 1f));
                var relSprite = SpriteManager.GetRelic(reward.relic.id);
                if (relSprite != null)
                {
                    var relIcon = MakeRect(relRow, "Icon",
                        new Vector2(0.02f, 0.1f), new Vector2(0.12f, 0.9f), Vector2.zero, Vector2.zero);
                    var relImg = relIcon.gameObject.AddComponent<Image>();
                    relImg.sprite = relSprite;
                    relImg.preserveAspect = true;
                    relImg.raycastTarget = false;
                }
                relRow.GetComponent<Image>().raycastTarget = true;
                var relBtn = relRow.gameObject.AddComponent<Button>();
                relBtn.targetGraphic = relRow.GetComponent<Image>();
                var capturedRelic = reward.relic;
                relBtn.onClick.AddListener(() => ShowRelicTooltip(capturedRelic));
                MakeTMP(relRow, "Hint", "?",
                    new Vector2(0.9f, 0), new Vector2(1f, 1), 22,
                    new Color(0.7f, 0.5f, 1f)).alignment = TextAlignmentOptions.Center;
                y -= 0.08f;
            }

            if (reward.cardChoices != null && reward.cardChoices.Count > 0)
            {
                _cardChoices = reward.cardChoices;

                MakeTMP(_root, "PickLabel", "카드를 선택하세요:",
                    new Vector2(0.05f, y - 0.02f), new Vector2(0.95f, y + 0.04f), 28, Color.white)
                    .alignment = TextAlignmentOptions.Center;
                y -= 0.06f;

                BuildCardChoices(y);
                BuildBottomButtons();
            }
            else
            {
                BuildSimpleProceed();
            }
        }

        void BuildCardChoices(float topY)
        {
            _cardChoicePanel = MakeRect(_root, "Cards",
                new Vector2(0.02f, topY - 0.32f), new Vector2(0.98f, topY),
                Vector2.zero, Vector2.zero);

            int cnt = _cardChoices.Count;
            float cw = 0.9f / cnt;
            float sx = (1f - cw * cnt) / 2f;

            for (int i = 0; i < cnt; i++)
            {
                var card = _cardChoices[i];
                float x = sx + i * cw;

                var cardRT = MakeRect(_cardChoicePanel, $"Card{i}",
                    new Vector2(x + 0.01f, 0.02f), new Vector2(x + cw - 0.01f, 0.98f),
                    Vector2.zero, Vector2.zero);

                var img = cardRT.gameObject.AddComponent<Image>();
                var rwdFrameSprite = SpriteManager.GetCardFrame(card.type);
                Color bg;
                if (rwdFrameSprite != null)
                {
                    img.sprite = rwdFrameSprite;
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    bg = Color.white;
                }
                else
                {
                    bg = card.type switch
                    {
                        "attack" => new Color(0.5f, 0.15f, 0.15f, 0.95f),
                        "skill" => new Color(0.15f, 0.25f, 0.5f, 0.95f),
                        "power" => new Color(0.35f, 0.15f, 0.5f, 0.95f),
                        _ => new Color(0.3f, 0.3f, 0.3f, 0.95f)
                    };
                    img.color = bg;
                }
                _cardImages.Add(img);
                _cardBaseColors.Add(bg);

                var btn = cardRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                int idx = i;
                btn.onClick.AddListener(() => OnCardClicked(idx));

                float rwdYOff = card.type switch { "skill" => 0.01f, "power" => -0.015f, _ => 0f };
                var rwdGemRT = MakeRect(cardRT, "CostGem",
                    new Vector2(0.02f, 0.73f + rwdYOff), new Vector2(0.35f, 0.95f + rwdYOff), Vector2.zero, Vector2.zero);
                var rwdGemImg = rwdGemRT.gameObject.AddComponent<Image>();
                var rwdGemSprite = SpriteManager.GetCostGem(card.cost);
                if (rwdGemSprite != null) { rwdGemImg.sprite = rwdGemSprite; rwdGemImg.color = Color.white; }
                else { rwdGemImg.color = new Color(0.15f, 0.1f, 0.25f, 0.9f); }
                rwdGemImg.raycastTarget = false;
                var rwdCostT = MakeTMP(rwdGemRT, "CostNum", card.cost.ToString(),
                    Vector2.zero, Vector2.one, 26, Color.white);
                rwdCostT.alignment = TextAlignmentOptions.Center;
                rwdCostT.outlineWidth = 0.3f;
                rwdCostT.outlineColor = Color.black;

                var rarityColor = card.rarity switch
                {
                    "rare" => new Color(1, 0.85f, 0.2f),
                    "uncommon" => new Color(0.4f, 0.7f, 1f),
                    _ => Color.white
                };
                string rarityKo = card.rarity switch { "common" => "일반", "uncommon" => "고급", "rare" => "희귀", _ => card.rarity };
                var rarityT = MakeTMP(cardRT, "Rarity", rarityKo,
                    new Vector2(0.7f, 0.88f), new Vector2(0.98f, 0.96f), 20, rarityColor);
                rarityT.alignment = TextAlignmentOptions.MidlineRight;
                rarityT.raycastTarget = false;

                string rCardName = !string.IsNullOrEmpty(card.nameKo) ? card.nameKo : card.name;
                var rNameT = MakeTMP(cardRT, "Name", rCardName,
                    new Vector2(0.14f, 0.55f), new Vector2(0.86f, 0.75f), 28, Color.white);
                rNameT.fontStyle = FontStyles.Bold;
                rNameT.raycastTarget = false;

                string typeKo = card.type switch { "attack" => "공격", "skill" => "스킬", "power" => "파워", _ => card.type };
                var typeT = MakeTMP(cardRT, "Type", typeKo,
                    new Vector2(0.14f, 0.48f), new Vector2(0.86f, 0.55f), 20,
                    new Color(0.7f, 0.7f, 0.7f));
                typeT.raycastTarget = false;

                string rCardDesc = CardInstance.BuildEffectDesc(card.effects, card.exhaust, true);
                var descT = MakeTMP(cardRT, "Desc", rCardDesc,
                    new Vector2(0.14f, 0.10f), new Vector2(0.86f, 0.46f), 22,
                    new Color(0.9f, 0.9f, 0.9f));
                descT.alignment = TextAlignmentOptions.TopLeft;
                descT.raycastTarget = false;
            }
        }

        void OnCardClicked(int index)
        {
            if (_selectedIndex == index)
            {
                _selectedIndex = -1;
            }
            else
            {
                _selectedIndex = index;
            }
            RefreshCardHighlights();
            RefreshConfirmButton();
        }

        void RefreshCardHighlights()
        {
            for (int i = 0; i < _cardImages.Count; i++)
            {
                if (i == _selectedIndex)
                {
                    _cardImages[i].color = new Color(0.2f, 0.65f, 0.2f, 1f);
                    _cardImages[i].transform.localScale = Vector3.one * 1.05f;
                }
                else
                {
                    _cardImages[i].color = _cardBaseColors[i];
                    _cardImages[i].transform.localScale = Vector3.one;
                }
            }
        }

        void RefreshConfirmButton()
        {
            if (_selectedIndex >= 0)
            {
                var selectedCard = _cardChoices[_selectedIndex];
                string selName = !string.IsNullOrEmpty(selectedCard.nameKo) ? selectedCard.nameKo : selectedCard.name;
                _confirmText.text = $"{selName} 획득";
                _confirmBtn.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.2f);
                _selectionLabel.text = $"선택: {selName}";
            }
            else
            {
                _confirmText.text = "계속";
                _confirmBtn.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.2f);
                _selectionLabel.text = "카드 미선택";
            }
        }

        void BuildBottomButtons()
        {
            _selectionLabel = MakeTMP(_root, "SelLabel", "카드 미선택",
                new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.2f), 22,
                new Color(0.7f, 0.7f, 0.7f));
            _selectionLabel.alignment = TextAlignmentOptions.Center;

            // Deck view button
            var deckRT = MakeRect(_root, "DeckBtn",
                new Vector2(0.05f, 0.04f), new Vector2(0.25f, 0.13f), Vector2.zero, Vector2.zero);
            var deckImg = deckRT.gameObject.AddComponent<Image>();
            deckImg.color = new Color(0.2f, 0.2f, 0.35f, 0.9f);
            var deckBtn = deckRT.gameObject.AddComponent<Button>();
            deckBtn.targetGraphic = deckImg;
            deckBtn.onClick.AddListener(ShowDeckOverlay);
            MakeTMP(deckRT, "Txt", "덱",
                Vector2.zero, Vector2.one, 24, new Color(0.7f, 0.8f, 1f)).alignment = TextAlignmentOptions.Center;

            // Skip button
            var skipRT = MakeRect(_root, "Skip",
                new Vector2(0.27f, 0.04f), new Vector2(0.48f, 0.13f), Vector2.zero, Vector2.zero);
            var skipImg = skipRT.gameObject.AddComponent<Image>();
            skipImg.color = new Color(0.35f, 0.25f, 0.25f, 0.9f);
            var skipBtn = skipRT.gameObject.AddComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            skipBtn.onClick.AddListener(OnSkipClicked);
            MakeTMP(skipRT, "Txt", "건너뛰기",
                Vector2.zero, Vector2.one, 24, Color.white).alignment = TextAlignmentOptions.Center;

            // Confirm button
            var confirmRT = MakeRect(_root, "Confirm",
                new Vector2(0.5f, 0.04f), new Vector2(0.95f, 0.13f), Vector2.zero, Vector2.zero);
            var confirmImg = confirmRT.gameObject.AddComponent<Image>();
            confirmImg.color = new Color(0.15f, 0.55f, 0.2f);
            _confirmBtn = confirmRT.gameObject.AddComponent<Button>();
            _confirmBtn.targetGraphic = confirmImg;
            _confirmBtn.onClick.AddListener(OnConfirmClicked);
            _confirmText = MakeTMP(confirmRT, "Txt", "계속",
                Vector2.zero, Vector2.one, 26, Color.white);
            _confirmText.alignment = TextAlignmentOptions.Center;
        }

        void ShowDeckOverlay()
        {
            var run = GameManager.Instance.CurrentRun;
            if (run == null) return;

            var overlay = new GameObject("DeckOverlay", typeof(RectTransform));
            var overlayCanvas = overlay.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 30;
            var scaler = overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            overlay.AddComponent<GraphicRaycaster>();

            var overlayRoot = overlay.GetComponent<RectTransform>();

            var bg = MakeRect(overlayRoot, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.04f, 0.08f, 0.97f);
            bgImg.raycastTarget = false;

            MakeTMP(overlayRoot, "Title", $"내 덱 ({run.deck.Count}장)",
                new Vector2(0.1f, 0.92f), new Vector2(0.9f, 0.97f), 36,
                new Color(1, 0.85f, 0.2f)).alignment = TextAlignmentOptions.Center;

            // Close button
            var closeRT = MakeRect(overlayRoot, "Close",
                new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), Vector2.zero, Vector2.zero);
            var closeImg = closeRT.gameObject.AddComponent<Image>();
            closeImg.color = new Color(0.4f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeRT.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() => Destroy(overlay));
            MakeTMP(closeRT, "Txt", "닫기",
                Vector2.zero, Vector2.one, 28, Color.white).alignment = TextAlignmentOptions.Center;

            // Scroll area
            var scrollContainer = MakeRect(overlayRoot, "Scroll",
                new Vector2(0.02f, 0.09f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero);
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
            float cardW = 220f;
            float cardH = 308f;
            float padX = 20f;
            float padY = 12f;

            var deck = run.deck;
            int rows = Mathf.CeilToInt(deck.Count / (float)cols);
            float totalH = rows * (cardH + padY) + padY;
            content.sizeDelta = new Vector2(0, totalH);

            for (int i = 0; i < deck.Count; i++)
            {
                var card = deck[i];
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

                var codexCardImg = cardRT.gameObject.AddComponent<Image>();
                var codexFrameSprite = SpriteManager.GetCardFrame(card.Data.type);
                if (codexFrameSprite != null)
                {
                    codexCardImg.sprite = codexFrameSprite;
                    codexCardImg.type = Image.Type.Simple;
                    codexCardImg.color = Color.white;
                }
                else
                {
                    Color cardColor = card.Data.type switch
                    {
                        "attack" => new Color(0.5f, 0.15f, 0.15f, 0.9f),
                        "skill" => new Color(0.15f, 0.25f, 0.5f, 0.9f),
                        "power" => new Color(0.35f, 0.15f, 0.5f, 0.9f),
                        _ => new Color(0.3f, 0.3f, 0.3f, 0.9f)
                    };
                    codexCardImg.color = cardColor;
                }

                string upgStr = card.Upgraded ? " <color=#44ff44>+</color>" : "";

                float dkYOff = card.Data.type switch { "skill" => 0.01f, "power" => -0.015f, _ => 0f };
                var dkGemRT = MakeRect(cardRT, "CostGem",
                    new Vector2(0.02f, 0.73f + dkYOff), new Vector2(0.35f, 0.95f + dkYOff), Vector2.zero, Vector2.zero);
                var dkGemImg = dkGemRT.gameObject.AddComponent<Image>();
                var dkGemSprite = SpriteManager.GetCostGem(card.Cost);
                if (dkGemSprite != null) { dkGemImg.sprite = dkGemSprite; dkGemImg.color = Color.white; }
                else { dkGemImg.color = new Color(0.15f, 0.1f, 0.25f, 0.9f); }
                dkGemImg.raycastTarget = false;
                var dkCostT = MakeTMP(dkGemRT, "CostNum", card.Cost.ToString(),
                    Vector2.zero, Vector2.one, 22, Color.white);
                dkCostT.alignment = TextAlignmentOptions.Center;
                dkCostT.outlineWidth = 0.3f;
                dkCostT.outlineColor = Color.black;

                string cardName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;
                var nameT = MakeTMP(cardRT, "Name", cardName + upgStr,
                    new Vector2(0.14f, 0.55f), new Vector2(0.86f, 0.75f), 22, Color.white);
                nameT.fontStyle = FontStyles.Bold;
                nameT.raycastTarget = false;

                string desc = card.ActiveDescriptionKo;
                var descT = MakeTMP(cardRT, "Desc", desc,
                    new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.53f), 20,
                    new Color(0.82f, 0.82f, 0.82f));
                descT.alignment = TextAlignmentOptions.TopLeft;
                descT.raycastTarget = false;
            }
        }

        void OnSkipClicked()
        {
            Destroy(_canvas.gameObject);
            OnRewardComplete?.Invoke();
        }

        void OnConfirmClicked()
        {
            if (_selectedIndex >= 0)
            {
                var card = _cardChoices[_selectedIndex];
                GameManager.Instance.CurrentRun.AddCard(new CardInstance(card));
            }
            Destroy(_canvas.gameObject);
            OnRewardComplete?.Invoke();
        }

        void BuildSimpleProceed()
        {
            var rt = MakeRect(_root, "Proceed",
                new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.2f), Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.15f, 0.5f, 0.2f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                Destroy(_canvas.gameObject);
                OnRewardComplete?.Invoke();
            });
            MakeTMP(rt, "Txt", "계속",
                Vector2.zero, Vector2.one, 30, Color.white).alignment = TextAlignmentOptions.Center;
        }

        void DismissTooltip()
        {
            if (_tooltipPanel != null)
            {
                Destroy(_tooltipPanel.gameObject);
                _tooltipPanel = null;
            }
        }

        void ShowRelicTooltip(RelicData relic)
        {
            DismissTooltip();

            _tooltipPanel = MakeRect(_root, "Tooltip",
                new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);
            var bg = _tooltipPanel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.14f, 0.98f);

            var outline = _tooltipPanel.gameObject.AddComponent<Outline>();
            Color borderColor = relic.rarity switch
            {
                "rare" => new Color(1f, 0.85f, 0.2f, 0.6f),
                "uncommon" => new Color(0.4f, 0.7f, 1f, 0.5f),
                "boss" => new Color(0.9f, 0.3f, 0.3f, 0.6f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.4f)
            };
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2, -2);

            string name = !string.IsNullOrEmpty(relic.nameKo) ? relic.nameKo : relic.name;
            var nameT = MakeTMP(_tooltipPanel, "Name", name,
                new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.96f), 30, Color.white);
            nameT.fontStyle = FontStyles.Bold;
            nameT.alignment = TextAlignmentOptions.MidlineLeft;

            string rarityKo = relic.rarity switch
            {
                "common" => "일반", "uncommon" => "고급", "rare" => "희귀", "boss" => "보스", _ => relic.rarity
            };
            Color tagColor = relic.rarity switch
            {
                "rare" => new Color(1, 0.85f, 0.2f),
                "uncommon" => new Color(0.5f, 0.75f, 1f),
                "boss" => new Color(1f, 0.4f, 0.4f),
                _ => new Color(0.55f, 0.55f, 0.55f)
            };
            MakeTMP(_tooltipPanel, "Rarity", rarityKo,
                new Vector2(0.04f, 0.65f), new Vector2(0.96f, 0.78f), 20, tagColor)
                .alignment = TextAlignmentOptions.MidlineLeft;

            string desc = !string.IsNullOrEmpty(relic.descriptionKo) ? relic.descriptionKo : relic.description;
            if (string.IsNullOrEmpty(desc))
                desc = CardInstance.BuildEffectDesc(relic.effects, false, true);
            var descT = MakeTMP(_tooltipPanel, "Desc", desc,
                new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.65f), 22,
                new Color(0.85f, 0.85f, 0.85f));
            descT.alignment = TextAlignmentOptions.TopLeft;

            var closeRT = MakeRect(_tooltipPanel, "Close",
                new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.18f), Vector2.zero, Vector2.zero);
            closeRT.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeRT.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeRT.GetComponent<Image>();
            closeBtn.onClick.AddListener(DismissTooltip);
            MakeTMP(closeRT, "Txt", "닫기",
                Vector2.zero, Vector2.one, 22, Color.white).alignment = TextAlignmentOptions.Center;
        }

        void ShowPotionTooltip(PotionSlot potion)
        {
            DismissTooltip();

            _tooltipPanel = MakeRect(_root, "Tooltip",
                new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.68f), Vector2.zero, Vector2.zero);
            var bg = _tooltipPanel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.1f, 0.06f, 0.98f);

            var outline = _tooltipPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.8f, 0.4f, 0.5f);
            outline.effectDistance = new Vector2(2, -2);

            string name = !string.IsNullOrEmpty(potion.nameKo) ? potion.nameKo : potion.name;
            var nameT = MakeTMP(_tooltipPanel, "Name", name,
                new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.96f), 30,
                new Color(0.3f, 1f, 0.5f));
            nameT.fontStyle = FontStyles.Bold;
            nameT.alignment = TextAlignmentOptions.MidlineLeft;

            string desc = CardInstance.BuildEffectDesc(potion.effects, false, true);
            var descT = MakeTMP(_tooltipPanel, "Desc", desc,
                new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.75f), 22,
                new Color(0.85f, 0.85f, 0.85f));
            descT.alignment = TextAlignmentOptions.TopLeft;

            var closeRT = MakeRect(_tooltipPanel, "Close",
                new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.2f), Vector2.zero, Vector2.zero);
            closeRT.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeRT.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeRT.GetComponent<Image>();
            closeBtn.onClick.AddListener(DismissTooltip);
            MakeTMP(closeRT, "Txt", "닫기",
                Vector2.zero, Vector2.one, 22, Color.white).alignment = TextAlignmentOptions.Center;
        }

        void BuildCanvas()
        {
            var go = new GameObject("RewardCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;
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
            img.color = new Color(0.05f, 0.04f, 0.08f, 0.95f);
            img.raycastTarget = true;
        }

        RectTransform MakeRewardRow(float y, string text, Color color)
        {
            var row = MakeRect(_root, "Row",
                new Vector2(0.1f, y - 0.05f), new Vector2(0.9f, y + 0.02f), Vector2.zero, Vector2.zero);
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.1f, 0.18f, 0.9f);
            bg.raycastTarget = false;
            MakeTMP(row, "Txt", text, new Vector2(0.05f, 0), new Vector2(0.95f, 1), 28, color);
            return row;
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
            tmp.fontSize = Mathf.Max(size, 20);
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            FontManager.ApplyTo(tmp);
            return tmp;
        }
    }
}
