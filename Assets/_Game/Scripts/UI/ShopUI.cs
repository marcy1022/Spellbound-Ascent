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
    public class ShopUI : MonoBehaviour
    {
        public event Action OnShopClosed;

        Canvas _canvas;
        RectTransform _root;
        TextMeshProUGUI _goldText;
        List<ShopItem> _items = new();
        RectTransform _deckViewPanel;

        struct ShopItem
        {
            public RectTransform rt;
            public Button btn;
            public Image bg;
            public TextMeshProUGUI priceText;
            public int price;
            public bool sold;
            public Action onBuy;
        }

        public void Show()
        {
            BuildCanvas();
            BuildBackground();
            BuildHeader();
            BuildCardSection();
            BuildServiceSection();
            BuildLeaveButton();
        }

        void BuildCanvas()
        {
            var go = new GameObject("ShopCanvas");
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
            img.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);
            img.raycastTarget = false;
        }

        void BuildHeader()
        {
            var header = MakeRect(_root, "Header",
                new Vector2(0, 0.92f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var bg = header.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.1f, 0.06f, 0.95f);
            bg.raycastTarget = false;

            MakeTMP(header, "Title", "상점",
                new Vector2(0.02f, 0.1f), new Vector2(0.4f, 0.9f), 42,
                new Color(1, 0.85f, 0.2f));

            var run = GameManager.Instance.CurrentRun;
            _goldText = MakeTMP(header, "Gold", $"골드: {run.gold}",
                new Vector2(0.5f, 0.1f), new Vector2(0.98f, 0.9f), 32,
                new Color(1, 0.85f, 0.2f));
        }

        void BuildCardSection()
        {
            MakeTMP(_root, "CardsLabel", "-- 카드 --",
                new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.91f), 26,
                new Color(0.8f, 0.8f, 0.8f));

            var run = GameManager.Instance.CurrentRun;
            var balance = GameManager.Instance.Balance;
            var pool = GameManager.Instance.GetCardPool(run.playerClass);
            pool.RemoveAll(c => c.rarity == "starter" || c.rarity == "curse");

            var shopCards = new List<CardData>();
            string[] rarities = { "common", "common", "common", "uncommon", "uncommon", "rare" };

            foreach (string rarity in rarities)
            {
                var filtered = pool.FindAll(c => c.rarity == rarity);
                if (filtered.Count == 0) filtered = pool;
                if (filtered.Count == 0) continue;

                var card = filtered[UnityEngine.Random.Range(0, filtered.Count)];
                shopCards.Add(card);
                pool.Remove(card);
            }

            int cnt = shopCards.Count;
            float cw = 0.9f / Mathf.Max(cnt, 1);
            float sx = (1f - cw * cnt) / 2f;

            for (int i = 0; i < cnt; i++)
            {
                var card = shopCards[i];
                float x = sx + i * cw;

                var cardRT = MakeRect(_root, $"ShopCard{i}",
                    new Vector2(x + 0.005f, 0.52f), new Vector2(x + cw - 0.005f, 0.84f),
                    Vector2.zero, Vector2.zero);

                Color bgColor = card.type switch
                {
                    "attack" => new Color(0.45f, 0.12f, 0.12f, 0.95f),
                    "skill" => new Color(0.12f, 0.2f, 0.45f, 0.95f),
                    "power" => new Color(0.3f, 0.12f, 0.45f, 0.95f),
                    _ => new Color(0.25f, 0.25f, 0.25f, 0.95f)
                };

                var img = cardRT.gameObject.AddComponent<Image>();
                img.color = bgColor;

                MakeTMP(cardRT, "Cost", card.cost.ToString(),
                    new Vector2(0, 0.88f), new Vector2(0.3f, 1f), 28,
                    new Color(1, 0.85f, 0.2f)).alignment = TextAlignmentOptions.Center;

                string sCardName = !string.IsNullOrEmpty(card.nameKo) ? card.nameKo : card.name;
                var sNameT = MakeTMP(cardRT, "Name", sCardName,
                    new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.88f), 22, Color.white);
                sNameT.fontStyle = FontStyles.Bold;

                string sType = card.type switch { "attack" => "공격", "skill" => "스킬", "power" => "파워", _ => card.type };
                string sRarity = card.rarity switch { "common" => "일반", "uncommon" => "고급", "rare" => "희귀", _ => card.rarity };
                MakeTMP(cardRT, "Type", $"{sType} / {sRarity}",
                    new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.62f), 16,
                    new Color(0.7f, 0.7f, 0.7f));

                string sCardDesc = CardInstance.BuildEffectDesc(card.effects, card.exhaust, true);
                var descT = MakeTMP(cardRT, "Desc", sCardDesc,
                    new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.5f), 17,
                    new Color(0.85f, 0.85f, 0.85f));
                descT.alignment = TextAlignmentOptions.TopLeft;

                int price = GetCardPrice(card.rarity, balance);
                var priceT = MakeTMP(cardRT, "Price", $"{price}G",
                    new Vector2(0, 0), new Vector2(1, 0.15f), 22,
                    run.gold >= price ? new Color(1, 0.85f, 0.2f) : new Color(1, 0.3f, 0.3f));
                priceT.alignment = TextAlignmentOptions.Center;

                var btn = cardRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                int idx = _items.Count;
                var capturedCard = card;
                int capturedPrice = price;

                btn.onClick.AddListener(() => BuyCard(idx, capturedCard, capturedPrice));

                _items.Add(new ShopItem
                {
                    rt = cardRT, btn = btn, bg = img,
                    priceText = priceT, price = price, sold = false,
                    onBuy = () => BuyCard(idx, capturedCard, capturedPrice)
                });
            }
        }

        void BuildServiceSection()
        {
            MakeTMP(_root, "ServLabel", "-- 서비스 --",
                new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.51f), 26,
                new Color(0.8f, 0.8f, 0.8f));

            var run = GameManager.Instance.CurrentRun;
            var balance = GameManager.Instance.Balance;

            int removalPrice = balance.shopPrices.cardRemoval +
                (run.currentAct - 1) * balance.shopPrices.cardRemovalActIncrease;

            // Card Removal
            var remRT = MakeRect(_root, "Removal",
                new Vector2(0.05f, 0.32f), new Vector2(0.48f, 0.44f), Vector2.zero, Vector2.zero);
            var remImg = remRT.gameObject.AddComponent<Image>();
            remImg.color = new Color(0.4f, 0.15f, 0.15f, 0.9f);

            MakeTMP(remRT, "Label", "카드 제거",
                new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.9f), 22, Color.white);

            var remPriceT = MakeTMP(remRT, "Price", $"{removalPrice}G",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.4f), 22,
                run.gold >= removalPrice ? new Color(1, 0.85f, 0.2f) : new Color(1, 0.3f, 0.3f));

            var remBtn = remRT.gameObject.AddComponent<Button>();
            remBtn.targetGraphic = remImg;
            int remIdx = _items.Count;
            remBtn.onClick.AddListener(() => OpenCardRemoval(remIdx, removalPrice));

            _items.Add(new ShopItem
            {
                rt = remRT, btn = remBtn, bg = remImg,
                priceText = remPriceT, price = removalPrice, sold = false
            });

            // Potion
            int potionPrice = UnityEngine.Random.Range(
                balance.shopPrices.potion[0], balance.shopPrices.potion[1] + 1);

            var potRT = MakeRect(_root, "Potion",
                new Vector2(0.52f, 0.32f), new Vector2(0.95f, 0.44f), Vector2.zero, Vector2.zero);
            var potImg = potRT.gameObject.AddComponent<Image>();
            potImg.color = new Color(0.15f, 0.35f, 0.15f, 0.9f);

            MakeTMP(potRT, "Label", "체력 포션",
                new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.9f), 22, Color.white);

            var potPriceT = MakeTMP(potRT, "Price", $"{potionPrice}G",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.4f), 22,
                run.gold >= potionPrice ? new Color(1, 0.85f, 0.2f) : new Color(1, 0.3f, 0.3f));

            var potBtn = potRT.gameObject.AddComponent<Button>();
            potBtn.targetGraphic = potImg;
            int potIdx = _items.Count;
            int capturedPotPrice = potionPrice;
            potBtn.onClick.AddListener(() => BuyPotion(potIdx, capturedPotPrice));

            _items.Add(new ShopItem
            {
                rt = potRT, btn = potBtn, bg = potImg,
                priceText = potPriceT, price = potionPrice, sold = false
            });
        }

        void BuildLeaveButton()
        {
            var rt = MakeRect(_root, "Leave",
                new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.12f), Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.5f, 0.35f, 0.1f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                Destroy(_canvas.gameObject);
                OnShopClosed?.Invoke();
            });
            MakeTMP(rt, "Txt", "상점 나가기",
                Vector2.zero, Vector2.one, 28, Color.white).alignment = TextAlignmentOptions.Center;
        }

        void BuyCard(int itemIdx, CardData card, int price)
        {
            var item = _items[itemIdx];
            if (item.sold) return;

            var run = GameManager.Instance.CurrentRun;
            if (run.gold < price) return;

            ShowCardConfirmDialog(card, price, () =>
            {
                if (!run.SpendGold(price)) return;
                run.AddCard(new CardInstance(card));

                var it = _items[itemIdx];
                it.sold = true;
                it.bg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                it.btn.interactable = false;
                it.priceText.text = "판매됨";
                it.priceText.color = new Color(0.5f, 0.5f, 0.5f);
                _items[itemIdx] = it;
                RefreshGold();
            });
        }

        void BuyPotion(int itemIdx, int price)
        {
            var item = _items[itemIdx];
            if (item.sold) return;

            var run = GameManager.Instance.CurrentRun;
            if (!run.SpendGold(price)) return;

            var potion = new PotionSlot
            {
                id = "health_potion",
                name = "Health Potion",
                nameKo = "체력 포션",
                effects = new List<EffectData> { new() { type = "heal", value = 20 } }
            };

            if (!run.AddPotion(potion))
            {
                run.AddGold(price);
                return;
            }

            item.sold = true;
            item.bg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            item.btn.interactable = false;
            item.priceText.text = "판매됨";
            item.priceText.color = new Color(0.5f, 0.5f, 0.5f);
            _items[itemIdx] = item;

            RefreshGold();
        }

        void OpenCardRemoval(int itemIdx, int price)
        {
            var item = _items[itemIdx];
            if (item.sold) return;

            var run = GameManager.Instance.CurrentRun;
            if (run.gold < price) return;
            if (run.deck.Count <= 1) return;

            if (_deckViewPanel != null) Destroy(_deckViewPanel.gameObject);

            _deckViewPanel = MakeRect(_root, "DeckView",
                new Vector2(0.02f, 0.13f), new Vector2(0.98f, 0.31f), Vector2.zero, Vector2.zero);
            var panelBg = _deckViewPanel.gameObject.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.08f, 0.12f, 0.95f);

            MakeTMP(_deckViewPanel, "Label", "제거할 카드를 선택하세요:",
                new Vector2(0.02f, 0.82f), new Vector2(0.98f, 1f), 20, Color.white);

            int cnt = run.deck.Count;
            int cols = Mathf.Min(cnt, 6);
            int rows = Mathf.CeilToInt((float)cnt / cols);
            float cw = 0.95f / cols;
            float ch = 0.8f / rows;

            for (int i = 0; i < cnt; i++)
            {
                var card = run.deck[i];
                int col = i % cols;
                int row = rows - 1 - i / cols;

                float x = 0.025f + col * cw;
                float y = row * ch;

                var cardRT = MakeRect(_deckViewPanel, $"DC{i}",
                    new Vector2(x + 0.003f, y + 0.003f),
                    new Vector2(x + cw - 0.003f, y + ch - 0.003f),
                    Vector2.zero, Vector2.zero);

                Color bgColor = card.Data.type switch
                {
                    "attack" => new Color(0.4f, 0.1f, 0.1f, 0.9f),
                    "skill" => new Color(0.1f, 0.18f, 0.4f, 0.9f),
                    "power" => new Color(0.28f, 0.1f, 0.4f, 0.9f),
                    _ => new Color(0.2f, 0.2f, 0.2f, 0.9f)
                };

                var cImg = cardRT.gameObject.AddComponent<Image>();
                cImg.color = bgColor;

                string remName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;
                var remNameT = MakeTMP(cardRT, "Name", remName,
                    new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.95f), 17, Color.white);
                remNameT.fontStyle = FontStyles.Bold;

                MakeTMP(cardRT, "Cost", $"[{card.Cost}]",
                    new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.3f), 12,
                    new Color(1, 0.85f, 0.2f));

                var cBtn = cardRT.gameObject.AddComponent<Button>();
                cBtn.targetGraphic = cImg;
                var capturedCard = card;
                int capturedItemIdx = itemIdx;
                int capturedPrice = price;
                cBtn.onClick.AddListener(() =>
                    ConfirmRemoval(capturedItemIdx, capturedCard, capturedPrice));
            }
        }

        void ConfirmRemoval(int itemIdx, CardInstance card, int price)
        {
            ShowCardInstanceConfirmDialog(card, price, true, () =>
            {
                var run = GameManager.Instance.CurrentRun;
                if (!run.SpendGold(price)) return;

                run.RemoveCard(card);

                var item = _items[itemIdx];
                item.sold = true;
                item.bg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                item.btn.interactable = false;
                item.priceText.text = "완료";
                item.priceText.color = new Color(0.5f, 0.5f, 0.5f);
                _items[itemIdx] = item;

                if (_deckViewPanel != null) Destroy(_deckViewPanel.gameObject);
                RefreshGold();
            });
        }

        RectTransform _confirmDialog;

        void ShowCardConfirmDialog(CardData card, int price, System.Action onConfirm)
        {
            if (_confirmDialog != null) Destroy(_confirmDialog.gameObject);

            _confirmDialog = MakeRect(_root, "ConfirmDialog",
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
            var bg = _confirmDialog.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.1f, 0.98f);

            MakeTMP(_confirmDialog, "Title", "카드 구매 확인",
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f), 32,
                new Color(1, 0.85f, 0.2f));

            Color cardBg = card.type switch
            {
                "attack" => new Color(0.45f, 0.12f, 0.12f, 0.95f),
                "skill" => new Color(0.12f, 0.2f, 0.45f, 0.95f),
                "power" => new Color(0.3f, 0.12f, 0.45f, 0.95f),
                _ => new Color(0.25f, 0.25f, 0.25f, 0.95f)
            };

            var cardPanel = MakeRect(_confirmDialog, "Card",
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.82f), Vector2.zero, Vector2.zero);
            cardPanel.gameObject.AddComponent<Image>().color = cardBg;

            MakeTMP(cardPanel, "Cost", card.cost.ToString(),
                new Vector2(0, 0.85f), new Vector2(0.15f, 1f), 32,
                new Color(1, 0.85f, 0.2f)).alignment = TextAlignmentOptions.Center;

            string cardName = !string.IsNullOrEmpty(card.nameKo) ? card.nameKo : card.name;
            var nameT = MakeTMP(cardPanel, "Name", cardName,
                new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.88f), 28, Color.white);
            nameT.fontStyle = FontStyles.Bold;

            string typeKo = card.type switch { "attack" => "공격", "skill" => "스킬", "power" => "파워", _ => card.type };
            string rarityKo = card.rarity switch { "common" => "일반", "uncommon" => "고급", "rare" => "희귀", _ => card.rarity };
            MakeTMP(cardPanel, "Type", $"{typeKo} / {rarityKo}",
                new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.72f), 20,
                new Color(0.7f, 0.7f, 0.7f));

            string desc = CardInstance.BuildEffectDesc(card.effects, card.exhaust, true);
            var descT = MakeTMP(cardPanel, "Desc", desc,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.58f), 22,
                new Color(0.9f, 0.9f, 0.9f));
            descT.alignment = TextAlignmentOptions.TopLeft;

            MakeTMP(_confirmDialog, "Price", $"가격: {price}G",
                new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.3f), 26,
                new Color(1, 0.85f, 0.2f));

            var cancelRT = MakeRect(_confirmDialog, "Cancel",
                new Vector2(0.08f, 0.04f), new Vector2(0.46f, 0.18f), Vector2.zero, Vector2.zero);
            cancelRT.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f);
            var cancelBtn = cancelRT.gameObject.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelRT.GetComponent<Image>();
            cancelBtn.onClick.AddListener(() => { Destroy(_confirmDialog.gameObject); _confirmDialog = null; });
            MakeTMP(cancelRT, "Txt", "취소", Vector2.zero, Vector2.one, 26, Color.white);

            var confirmRT = MakeRect(_confirmDialog, "Confirm",
                new Vector2(0.54f, 0.04f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
            confirmRT.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.5f, 0.2f);
            var confirmBtn = confirmRT.gameObject.AddComponent<Button>();
            confirmBtn.targetGraphic = confirmRT.GetComponent<Image>();
            confirmBtn.onClick.AddListener(() =>
            {
                Destroy(_confirmDialog.gameObject);
                _confirmDialog = null;
                onConfirm?.Invoke();
            });
            MakeTMP(confirmRT, "Txt", "구매", Vector2.zero, Vector2.one, 26, Color.white);
        }

        void ShowCardInstanceConfirmDialog(CardInstance card, int price, bool isRemoval, System.Action onConfirm)
        {
            if (_confirmDialog != null) Destroy(_confirmDialog.gameObject);

            _confirmDialog = MakeRect(_root, "ConfirmDialog",
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
            var bg = _confirmDialog.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.1f, 0.98f);

            string titleText = isRemoval ? "카드 제거 확인" : "카드 구매 확인";
            MakeTMP(_confirmDialog, "Title", titleText,
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f), 32,
                new Color(1, 0.85f, 0.2f));

            Color cardBg = card.Data.type switch
            {
                "attack" => new Color(0.45f, 0.12f, 0.12f, 0.95f),
                "skill" => new Color(0.12f, 0.2f, 0.45f, 0.95f),
                "power" => new Color(0.3f, 0.12f, 0.45f, 0.95f),
                _ => new Color(0.25f, 0.25f, 0.25f, 0.95f)
            };

            var cardPanel = MakeRect(_confirmDialog, "Card",
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.82f), Vector2.zero, Vector2.zero);
            cardPanel.gameObject.AddComponent<Image>().color = cardBg;

            string upgStr = card.Upgraded ? " <color=#44ff44>+</color>" : "";
            MakeTMP(cardPanel, "Cost", card.Cost.ToString(),
                new Vector2(0, 0.85f), new Vector2(0.15f, 1f), 32,
                new Color(1, 0.85f, 0.2f)).alignment = TextAlignmentOptions.Center;

            string cardName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;
            var nameT = MakeTMP(cardPanel, "Name", cardName + upgStr,
                new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.88f), 28, Color.white);
            nameT.fontStyle = FontStyles.Bold;

            string typeKo = card.Data.type switch { "attack" => "공격", "skill" => "스킬", "power" => "파워", _ => card.Data.type };
            string rarityKo = card.Data.rarity switch { "common" => "일반", "uncommon" => "고급", "rare" => "희귀", "starter" => "기본", _ => card.Data.rarity };
            MakeTMP(cardPanel, "Type", $"{typeKo} / {rarityKo}",
                new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.72f), 20,
                new Color(0.7f, 0.7f, 0.7f));

            var descT = MakeTMP(cardPanel, "Desc", card.ActiveDescriptionKo,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.58f), 22,
                new Color(0.9f, 0.9f, 0.9f));
            descT.alignment = TextAlignmentOptions.TopLeft;

            string actionLabel = isRemoval ? "제거 비용" : "가격";
            MakeTMP(_confirmDialog, "Price", $"{actionLabel}: {price}G",
                new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.3f), 26,
                new Color(1, 0.85f, 0.2f));

            var cancelRT = MakeRect(_confirmDialog, "Cancel",
                new Vector2(0.08f, 0.04f), new Vector2(0.46f, 0.18f), Vector2.zero, Vector2.zero);
            cancelRT.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f);
            var cancelBtn = cancelRT.gameObject.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelRT.GetComponent<Image>();
            cancelBtn.onClick.AddListener(() => { Destroy(_confirmDialog.gameObject); _confirmDialog = null; });
            MakeTMP(cancelRT, "Txt", "취소", Vector2.zero, Vector2.one, 26, Color.white);

            string confirmLabel = isRemoval ? "제거" : "구매";
            var confirmRT = MakeRect(_confirmDialog, "Confirm",
                new Vector2(0.54f, 0.04f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
            confirmRT.gameObject.AddComponent<Image>().color = isRemoval
                ? new Color(0.5f, 0.15f, 0.15f)
                : new Color(0.15f, 0.5f, 0.2f);
            var confirmBtn = confirmRT.gameObject.AddComponent<Button>();
            confirmBtn.targetGraphic = confirmRT.GetComponent<Image>();
            confirmBtn.onClick.AddListener(() =>
            {
                Destroy(_confirmDialog.gameObject);
                _confirmDialog = null;
                onConfirm?.Invoke();
            });
            MakeTMP(confirmRT, "Txt", confirmLabel, Vector2.zero, Vector2.one, 26, Color.white);
        }

        void RefreshGold()
        {
            var run = GameManager.Instance.CurrentRun;
            _goldText.text = $"골드: {run.gold}";

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.sold) continue;
                item.priceText.color = run.gold >= item.price
                    ? new Color(1, 0.85f, 0.2f)
                    : new Color(1, 0.3f, 0.3f);
            }
        }

        int GetCardPrice(string rarity, BalanceConfig balance)
        {
            return rarity switch
            {
                "common" => UnityEngine.Random.Range(
                    balance.shopPrices.cardCommon[0], balance.shopPrices.cardCommon[1] + 1),
                "uncommon" => UnityEngine.Random.Range(
                    balance.shopPrices.cardUncommon[0], balance.shopPrices.cardUncommon[1] + 1),
                "rare" => UnityEngine.Random.Range(
                    balance.shopPrices.cardRare[0], balance.shopPrices.cardRare[1] + 1),
                _ => 50
            };
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
