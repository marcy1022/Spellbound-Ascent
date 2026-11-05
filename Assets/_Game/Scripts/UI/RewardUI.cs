using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Battle;
using SpellboundAscent.Card;
using SpellboundAscent.Core;

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
                    MakeRewardRow(y, $"포션: {potName}", new Color(0.3f, 0.9f, 0.4f));
                    y -= 0.08f;
                }
            }

            if (reward.relic != null)
            {
                string relName = !string.IsNullOrEmpty(reward.relic.nameKo) ? reward.relic.nameKo : reward.relic.name;
                MakeRewardRow(y, $"유물: {relName}", new Color(0.8f, 0.6f, 1f));
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
                new Vector2(0.02f, topY - 0.42f), new Vector2(0.98f, topY),
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

                Color bg = card.type switch
                {
                    "attack" => new Color(0.5f, 0.15f, 0.15f, 0.95f),
                    "skill" => new Color(0.15f, 0.25f, 0.5f, 0.95f),
                    "power" => new Color(0.35f, 0.15f, 0.5f, 0.95f),
                    _ => new Color(0.3f, 0.3f, 0.3f, 0.95f)
                };

                var img = cardRT.gameObject.AddComponent<Image>();
                img.color = bg;
                _cardImages.Add(img);
                _cardBaseColors.Add(bg);

                var btn = cardRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                int idx = i;
                btn.onClick.AddListener(() => OnCardClicked(idx));

                MakeTMP(cardRT, "Cost", card.cost.ToString(),
                    new Vector2(0, 0.88f), new Vector2(0.25f, 1f), 32,
                    new Color(1, 0.85f, 0.2f)).alignment = TextAlignmentOptions.Center;

                var rarityColor = card.rarity switch
                {
                    "rare" => new Color(1, 0.85f, 0.2f),
                    "uncommon" => new Color(0.4f, 0.7f, 1f),
                    _ => Color.white
                };
                string rarityKo = card.rarity switch { "common" => "일반", "uncommon" => "고급", "rare" => "희귀", _ => card.rarity };
                MakeTMP(cardRT, "Rarity", rarityKo,
                    new Vector2(0.3f, 0.88f), new Vector2(1f, 1f), 18, rarityColor);

                string rCardName = !string.IsNullOrEmpty(card.nameKo) ? card.nameKo : card.name;
                var rNameT = MakeTMP(cardRT, "Name", rCardName,
                    new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.88f), 28, Color.white);
                rNameT.fontStyle = FontStyles.Bold;

                string typeKo = card.type switch { "attack" => "공격", "skill" => "스킬", "power" => "파워", _ => card.type };
                MakeTMP(cardRT, "Type", typeKo,
                    new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.64f), 20,
                    new Color(0.7f, 0.7f, 0.7f));

                string rCardDesc = CardInstance.BuildEffectDesc(card.effects, card.exhaust, true);
                var descT = MakeTMP(cardRT, "Desc", rCardDesc,
                    new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.54f), 21,
                    new Color(0.9f, 0.9f, 0.9f));
                descT.alignment = TextAlignmentOptions.TopLeft;
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

            // Skip button
            var skipRT = MakeRect(_root, "Skip",
                new Vector2(0.05f, 0.04f), new Vector2(0.35f, 0.13f), Vector2.zero, Vector2.zero);
            var skipImg = skipRT.gameObject.AddComponent<Image>();
            skipImg.color = new Color(0.35f, 0.25f, 0.25f, 0.9f);
            var skipBtn = skipRT.gameObject.AddComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            skipBtn.onClick.AddListener(OnSkipClicked);
            MakeTMP(skipRT, "Txt", "건너뛰기",
                Vector2.zero, Vector2.one, 26, Color.white).alignment = TextAlignmentOptions.Center;

            // Confirm button
            var confirmRT = MakeRect(_root, "Confirm",
                new Vector2(0.4f, 0.04f), new Vector2(0.95f, 0.13f), Vector2.zero, Vector2.zero);
            var confirmImg = confirmRT.gameObject.AddComponent<Image>();
            confirmImg.color = new Color(0.15f, 0.55f, 0.2f);
            _confirmBtn = confirmRT.gameObject.AddComponent<Button>();
            _confirmBtn.targetGraphic = confirmImg;
            _confirmBtn.onClick.AddListener(OnConfirmClicked);
            _confirmText = MakeTMP(confirmRT, "Txt", "계속",
                Vector2.zero, Vector2.one, 26, Color.white);
            _confirmText.alignment = TextAlignmentOptions.Center;
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
            img.raycastTarget = false;
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
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            FontManager.ApplyTo(tmp);
            return tmp;
        }
    }
}
