using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Battle;
using SpellboundAscent.Card;
using SpellboundAscent.Enemy;
using SpellboundAscent.Core;
using SpellboundAscent.Utils;

namespace SpellboundAscent.UI
{
    public class BattleUI : MonoBehaviour
    {
        Canvas _canvas;
        RectTransform _root;

        TextMeshProUGUI _playerHpText;
        Image _playerHpBar;
        TextMeshProUGUI _energyText;
        TextMeshProUGUI _blockText;
        TextMeshProUGUI _statusText;

        RectTransform _enemyArea;
        List<EnemyPanel> _enemyPanels = new();

        List<CardButton> _handButtons = new();
        RectTransform _handArea;

        Button _endTurnBtn;
        Button _masterSkillBtn;
        Image _masterSkillBg;
        TextMeshProUGUI _masterSkillText;
        List<Button> _potionBtns = new();
        List<TextMeshProUGUI> _potionLabels = new();

        TextMeshProUGUI _logText;
        List<string> _logLines = new();

        GameObject _deckViewPanel;

        // Drag state
        CardDragHandler _draggingCard;
        CardInstance _draggingCardInstance;
        Image _dragArrow;
        RectTransform _targetHighlight;
        int _hoveredEnemyIndex = -1;
        float _playThresholdY;

        void Start()
        {
            SubscribeEvents();
        }

        void BuildBattleCanvas()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            _enemyPanels.Clear();
            _handButtons.Clear();
            _logLines.Clear();

            BuildCanvas();
            BuildBackground();
            _enemyArea = MakeRect(_root, "EnemyArea",
                new Vector2(0, 0.55f), new Vector2(1, 0.92f), Vector2.zero, Vector2.zero);
            BuildLogArea();
            BuildPlayerHUD();
            _handArea = MakeRect(_root, "HandArea",
                new Vector2(0, 0.02f), new Vector2(1, 0.32f), Vector2.zero, Vector2.zero);
            BuildPotionBar();
            BuildEndTurnButton();
            BuildMasterSkillButton();
            BuildDeckViewButton();
            BuildDragIndicator();

            _playThresholdY = Screen.height * 0.4f;
        }

        void OnDestroy()
        {
            if (_deckViewPanel != null) Destroy(_deckViewPanel);

            var bm = BattleManager.Instance;
            if (bm == null) return;
            bm.OnBattleStarted -= OnBattleStarted;
            bm.OnPhaseChanged -= OnPhaseChanged;
            bm.OnCardPlayed -= OnCardPlayed;
            bm.OnEnemyActed -= OnEnemyActed;
            bm.OnBattleEnded -= OnBattleEnded;
            bm.OnEnemyDied -= OnEnemyDied;
            bm.OnCcImmune -= OnCcImmune;
        }

        // ======================== BUILD UI ========================

        void BuildCanvas()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var go = new GameObject("BattleCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
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
            img.color = new Color(0.08f, 0.06f, 0.12f);
            img.raycastTarget = false;
        }

        void BuildLogArea()
        {
            var area = MakeRect(_root, "LogArea",
                new Vector2(0.02f, 0.44f), new Vector2(0.98f, 0.55f), Vector2.zero, Vector2.zero);
            var bg = area.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);
            bg.raycastTarget = false;
            _logText = MakeTMP(area, "Log", "",
                new Vector2(0.02f, 0), new Vector2(0.98f, 1), 20, new Color(0.8f, 0.8f, 0.8f));
            _logText.alignment = TextAlignmentOptions.BottomLeft;
            _logText.overflowMode = TextOverflowModes.Truncate;
            _logText.raycastTarget = false;
        }

        void BuildPlayerHUD()
        {
            var hud = MakeRect(_root, "HUD",
                new Vector2(0, 0.35f), new Vector2(1, 0.44f), Vector2.zero, Vector2.zero);
            var hudBg = hud.gameObject.AddComponent<Image>();
            hudBg.color = new Color(0.1f, 0.08f, 0.15f, 0.9f);
            hudBg.raycastTarget = false;

            var hpBg = MakeRect(hud, "HpBg",
                new Vector2(0.02f, 0.55f), new Vector2(0.65f, 0.9f), Vector2.zero, Vector2.zero);
            hpBg.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.1f, 0.1f);

            var hpFill = MakeRect(hpBg, "HpFill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _playerHpBar = hpFill.gameObject.AddComponent<Image>();
            _playerHpBar.color = new Color(0.2f, 0.7f, 0.3f);

            _playerHpText = MakeTMP(hud, "HpTxt", "HP",
                new Vector2(0.02f, 0.1f), new Vector2(0.35f, 0.55f), 28, Color.white);
            _playerHpText.raycastTarget = false;

            _energyText = MakeTMP(hud, "Energy", "0",
                new Vector2(0.68f, 0.1f), new Vector2(0.82f, 0.9f), 36, new Color(1, 0.85f, 0.2f));
            _energyText.alignment = TextAlignmentOptions.Center;
            _energyText.raycastTarget = false;

            _blockText = MakeTMP(hud, "Block", "",
                new Vector2(0.36f, 0.1f), new Vector2(0.52f, 0.55f), 26, new Color(0.4f, 0.7f, 1f));
            _blockText.raycastTarget = false;

            _statusText = MakeTMP(hud, "Status", "",
                new Vector2(0.53f, 0.1f), new Vector2(0.67f, 0.55f), 20, new Color(0.7f, 0.7f, 1f));
            _statusText.raycastTarget = false;
        }

        void BuildEndTurnButton()
        {
            var btn = MakeRect(_root, "EndTurn",
                new Vector2(0.82f, 0.35f), new Vector2(0.98f, 0.44f), Vector2.zero, Vector2.zero);
            var img = btn.gameObject.AddComponent<Image>();
            img.color = new Color(0.6f, 0.15f, 0.15f);
            _endTurnBtn = btn.gameObject.AddComponent<Button>();
            _endTurnBtn.targetGraphic = img;
            _endTurnBtn.onClick.AddListener(OnEndTurnClicked);
            MakeTMP(btn, "Txt", "턴 종료",
                Vector2.zero, Vector2.one, 24, Color.white).alignment = TextAlignmentOptions.Center;
        }

        void BuildMasterSkillButton()
        {
            var bm = BattleManager.Instance;
            string skillName = bm.GetMasterSkillName();

            var btn = MakeRect(_root, "MasterSkill",
                new Vector2(0.82f, 0.445f), new Vector2(0.98f, 0.54f), Vector2.zero, Vector2.zero);
            _masterSkillBg = btn.gameObject.AddComponent<Image>();
            _masterSkillBg.color = new Color(0.15f, 0.12f, 0.4f);
            _masterSkillBtn = btn.gameObject.AddComponent<Button>();
            _masterSkillBtn.targetGraphic = _masterSkillBg;
            _masterSkillBtn.onClick.AddListener(OnMasterSkillClicked);

            _masterSkillText = MakeTMP(btn, "Txt", skillName,
                Vector2.zero, Vector2.one, 20, Color.white);
            _masterSkillText.alignment = TextAlignmentOptions.Center;
            FontManager.ApplyTo(_masterSkillText);

            UpdateMasterSkillButton();
        }

        void OnMasterSkillClicked()
        {
            var bm = BattleManager.Instance;
            if (bm == null || !bm.CanUseMasterSkill) return;

            bm.UseMasterSkill();
            AddLog($"<color=#a78bfa>★ {bm.GetMasterSkillName()} 발동!</color>");
            RefreshAll();
        }

        void UpdateMasterSkillButton()
        {
            if (_masterSkillBtn == null) return;
            var bm = BattleManager.Instance;
            if (bm == null) return;

            bool ready = bm.CanUseMasterSkill;
            _masterSkillBtn.interactable = ready;

            string skillName = bm.GetMasterSkillName();
            if (bm.MasterCooldown > 0)
            {
                _masterSkillText.text = $"{skillName}\n<size=16>({bm.MasterCooldown}턴)</size>";
                _masterSkillBg.color = new Color(0.15f, 0.12f, 0.2f);
            }
            else
            {
                _masterSkillText.text = $"★ {skillName}";
                _masterSkillBg.color = new Color(0.3f, 0.15f, 0.6f);
            }
        }

        void BuildDragIndicator()
        {
            var arrow = MakeRect(_root, "DragArrow",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 200));
            _dragArrow = arrow.gameObject.AddComponent<Image>();
            _dragArrow.color = new Color(1, 0.85f, 0.2f, 0.8f);
            _dragArrow.gameObject.SetActive(false);
            _dragArrow.raycastTarget = false;
        }

        void BuildPotionBar()
        {
            _potionBtns.Clear();
            _potionLabels.Clear();

            var bar = MakeRect(_root, "PotionBar",
                new Vector2(0.02f, 0.32f), new Vector2(0.7f, 0.35f), Vector2.zero, Vector2.zero);
            var barBg = bar.gameObject.AddComponent<Image>();
            barBg.color = new Color(0.08f, 0.06f, 0.12f, 0.7f);
            barBg.raycastTarget = false;

            for (int i = 0; i < Constants.MAX_POTION_SLOTS; i++)
            {
                float x0 = i / (float)Constants.MAX_POTION_SLOTS;
                float x1 = (i + 1) / (float)Constants.MAX_POTION_SLOTS;

                var slot = MakeRect(bar, $"Pot{i}",
                    new Vector2(x0 + 0.005f, 0.05f), new Vector2(x1 - 0.005f, 0.95f),
                    Vector2.zero, Vector2.zero);
                var slotImg = slot.gameObject.AddComponent<Image>();
                slotImg.color = new Color(0.15f, 0.3f, 0.15f, 0.8f);

                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = slotImg;

                int idx = i;
                btn.onClick.AddListener(() => OnPotionClicked(idx));

                var label = MakeTMP(slot, "Txt", "비어있음",
                    Vector2.zero, Vector2.one, 18, new Color(0.5f, 0.5f, 0.5f));
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;

                _potionBtns.Add(btn);
                _potionLabels.Add(label);
            }
        }

        void BuildDeckViewButton()
        {
            var btn = MakeRect(_root, "DeckBtn",
                new Vector2(0.02f, 0.005f), new Vector2(0.12f, 0.02f), Vector2.zero, Vector2.zero);
            var img = btn.gameObject.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.35f, 0.9f);
            var b = btn.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(ToggleDeckView);
            MakeTMP(btn, "Txt", "덱",
                Vector2.zero, Vector2.one, 16, Color.white).alignment = TextAlignmentOptions.Center;
        }

        void OnPotionClicked(int index)
        {
            if (BattleManager.Instance.Phase != BattlePhase.PlayerTurn) return;

            var run = GameManager.Instance.CurrentRun;
            if (index >= run.potions.Count) return;

            var potionData = run.potions[index];
            string potionName = !string.IsNullOrEmpty(potionData.nameKo) ? potionData.nameKo : potionData.name;
            if (BattleManager.Instance.UsePotion(index))
            {
                AddLog($"<color=#33ff88>{potionName} 사용</color>");
                RefreshAll();
                RefreshPotions();
            }
        }

        void RefreshPotions()
        {
            var run = GameManager.Instance?.CurrentRun;
            if (run == null) return;

            for (int i = 0; i < Constants.MAX_POTION_SLOTS; i++)
            {
                if (i < run.potions.Count)
                {
                    var pot = run.potions[i];
                    _potionLabels[i].text = !string.IsNullOrEmpty(pot.nameKo) ? pot.nameKo : pot.name;
                    _potionLabels[i].color = new Color(0.3f, 1f, 0.5f);
                    _potionBtns[i].interactable = true;
                }
                else
                {
                    _potionLabels[i].text = "비어있음";
                    _potionLabels[i].color = new Color(0.5f, 0.5f, 0.5f);
                    _potionBtns[i].interactable = false;
                }
            }
        }

        void ToggleDeckView()
        {
            if (_deckViewPanel != null)
            {
                Destroy(_deckViewPanel);
                _deckViewPanel = null;
                return;
            }
            ShowDeckView();
        }

        void ShowDeckView()
        {
            var deck = BattleManager.Instance?.Deck;
            if (deck == null) return;

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

            int totalCards = deck.DrawPile.Count + deck.Hand.Count + deck.DiscardPile.Count + deck.ExhaustPile.Count;
            MakeTMP(root, "Title", $"전투 덱 정보 ({totalCards}장)",
                new Vector2(0.05f, 0.93f), new Vector2(0.95f, 0.98f), 34, new Color(1, 0.85f, 0.2f));

            var closeBtn = MakeRect(root, "Close",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.07f), Vector2.zero, Vector2.zero);
            var closeImg = closeBtn.gameObject.AddComponent<Image>();
            closeImg.color = new Color(0.5f, 0.15f, 0.15f);
            var cb = closeBtn.gameObject.AddComponent<Button>();
            cb.targetGraphic = closeImg;
            cb.onClick.AddListener(() => { Destroy(_deckViewPanel); _deckViewPanel = null; });
            MakeTMP(closeBtn, "Txt", "닫기", Vector2.zero, Vector2.one, 24, Color.white);

            var scrollContainer = MakeRect(root, "ScrollContainer",
                new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);
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

            float yOff = 0f;

            yOff = AddDeckSection(root, content, yOff, "패 (Hand)", deck.Hand,
                new Color(0.2f, 0.35f, 0.2f), new Color(0.4f, 0.8f, 0.3f));

            var sortedDraw = new List<CardInstance>(deck.DrawPile);
            sortedDraw.Sort((a, b) =>
            {
                string na = !string.IsNullOrEmpty(a.Data.nameKo) ? a.Data.nameKo : a.Data.name;
                string nb = !string.IsNullOrEmpty(b.Data.nameKo) ? b.Data.nameKo : b.Data.name;
                return string.Compare(na, nb, StringComparison.Ordinal);
            });
            yOff = AddDeckSection(root, content, yOff, "드로우 더미 (Draw)", sortedDraw,
                new Color(0.15f, 0.2f, 0.35f), new Color(0.5f, 0.7f, 1f));

            yOff = AddDeckSection(root, content, yOff, "버린 카드 (Discard)", deck.DiscardPile,
                new Color(0.3f, 0.22f, 0.12f), new Color(0.9f, 0.7f, 0.3f));

            yOff = AddDeckSection(root, content, yOff, "소멸 (Exhaust)", deck.ExhaustPile,
                new Color(0.25f, 0.1f, 0.1f), new Color(0.7f, 0.3f, 0.3f));

            content.sizeDelta = new Vector2(0, yOff + 20f);
        }

        float AddDeckSection(RectTransform panelRoot, RectTransform content, float yStart,
            string title, List<CardInstance> cards, Color headerBg, Color headerText)
        {
            float headerH = 60f;
            float cardH = 200f;
            float cardW = 320f;
            float padX = 20f;
            float padY = 10f;
            int cols = 3;
            float y = yStart;

            var hdr = MakeRect(content, $"Hdr_{title}",
                new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, headerH));
            hdr.pivot = new Vector2(0.5f, 1f);
            hdr.anchoredPosition = new Vector2(0, -y);
            hdr.gameObject.AddComponent<Image>().color = headerBg;
            hdr.gameObject.GetComponent<Image>().raycastTarget = false;

            var hdrT = MakeTMP(hdr, "T", $"{title} — {cards.Count}장",
                new Vector2(0.03f, 0), new Vector2(0.97f, 1), 28, headerText);
            hdrT.fontStyle = FontStyles.Bold;
            hdrT.alignment = TextAlignmentOptions.MidlineLeft;
            hdrT.raycastTarget = false;

            y += headerH;

            if (cards.Count == 0)
            {
                var emptyT = MakeRect(content, $"Empty_{title}",
                    new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 50f));
                emptyT.pivot = new Vector2(0.5f, 1f);
                emptyT.anchoredPosition = new Vector2(0, -y);
                var et = MakeTMP(emptyT, "T", "  (없음)",
                    Vector2.zero, Vector2.one, 22, new Color(0.5f, 0.5f, 0.5f));
                et.alignment = TextAlignmentOptions.MidlineLeft;
                et.raycastTarget = false;
                return y + 50f;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                int col = i % cols;
                int row = i / cols;

                float cx = padX + col * (cardW + padX);
                float cy = -(y + padY + row * (cardH + padY));

                var cardRT = new GameObject($"C_{title}_{i}", typeof(RectTransform)).GetComponent<RectTransform>();
                cardRT.SetParent(content, false);
                cardRT.anchorMin = new Vector2(0, 1);
                cardRT.anchorMax = new Vector2(0, 1);
                cardRT.pivot = new Vector2(0, 1);
                cardRT.anchoredPosition = new Vector2(cx, cy);
                cardRT.sizeDelta = new Vector2(cardW, cardH);

                Color cardColor = card.Data.type switch
                {
                    "attack" => new Color(0.5f, 0.15f, 0.15f, 0.9f),
                    "skill" => new Color(0.15f, 0.25f, 0.5f, 0.9f),
                    "power" => new Color(0.35f, 0.15f, 0.5f, 0.9f),
                    _ => new Color(0.3f, 0.3f, 0.3f, 0.9f)
                };
                var cardImg = cardRT.gameObject.AddComponent<Image>();
                cardImg.color = cardColor;

                string upgStr = card.Upgraded ? " <color=#44ff44>+</color>" : "";
                MakeTMP(cardRT, "Cost", card.Cost.ToString(),
                    new Vector2(0, 0.7f), new Vector2(0.2f, 1f), 28,
                    new Color(1, 0.85f, 0.2f)).raycastTarget = false;

                string cName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;
                var cNameT = MakeTMP(cardRT, "Name", cName + upgStr,
                    new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.7f), 24, Color.white);
                cNameT.fontStyle = FontStyles.Bold;
                cNameT.raycastTarget = false;

                MakeTMP(cardRT, "Desc", card.ActiveDescriptionKo,
                    new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.45f), 17,
                    new Color(0.8f, 0.8f, 0.8f)).raycastTarget = false;

                var btn = cardRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = cardImg;
                var capturedCard = card;
                btn.onClick.AddListener(() => ShowCardTooltip(panelRoot, capturedCard));
            }

            int totalRows = Mathf.CeilToInt(cards.Count / (float)cols);
            y += padY + totalRows * (cardH + padY);

            return y;
        }

        void ShowCardTooltip(RectTransform panelRoot, CardInstance card)
        {
            var existing = panelRoot.Find("CardTooltip");
            if (existing != null) Destroy(existing.gameObject);

            var overlay = MakeRect(panelRoot, "CardTooltip",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);
            var overlayBtn = overlay.gameObject.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayImg;
            overlayBtn.onClick.AddListener(() => Destroy(overlay.gameObject));

            var popup = MakeRect(overlay, "Popup",
                new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.7f), Vector2.zero, Vector2.zero);

            Color cardBg = card.Data.type switch
            {
                "attack" => new Color(0.45f, 0.1f, 0.1f),
                "skill" => new Color(0.1f, 0.18f, 0.45f),
                "power" => new Color(0.3f, 0.1f, 0.45f),
                _ => new Color(0.25f, 0.25f, 0.25f)
            };
            popup.gameObject.AddComponent<Image>().color = cardBg;

            string upgStr = card.Upgraded ? " <color=#44ff44>+</color>" : "";
            string cName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;

            string rarityKo = card.Data.rarity switch
            {
                "common" => "일반",
                "uncommon" => "고급",
                "rare" => "희귀",
                _ => card.Data.rarity ?? ""
            };
            string typeKo = card.Data.type switch
            {
                "attack" => "공격",
                "skill" => "스킬",
                "power" => "파워",
                _ => card.Data.type ?? ""
            };

            var costT = MakeTMP(popup, "Cost", card.Cost.ToString(),
                new Vector2(0.02f, 0.8f), new Vector2(0.12f, 0.98f), 44,
                new Color(1, 0.85f, 0.2f));
            costT.fontStyle = FontStyles.Bold;
            costT.alignment = TextAlignmentOptions.Center;
            costT.raycastTarget = false;

            var nameT = MakeTMP(popup, "Name", cName + upgStr,
                new Vector2(0.12f, 0.82f), new Vector2(0.88f, 0.98f), 36, Color.white);
            nameT.fontStyle = FontStyles.Bold;
            nameT.alignment = TextAlignmentOptions.MidlineLeft;
            nameT.raycastTarget = false;

            string metaStr = $"{rarityKo} · {typeKo}";
            if (card.Data.exhaust) metaStr += " · <color=#ff6666>소멸</color>";
            var metaT = MakeTMP(popup, "Meta", metaStr,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.82f), 22, new Color(0.7f, 0.7f, 0.7f));
            metaT.alignment = TextAlignmentOptions.MidlineLeft;
            metaT.raycastTarget = false;

            var line = MakeRect(popup, "Line",
                new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.715f), Vector2.zero, Vector2.zero);
            line.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.2f);
            line.gameObject.GetComponent<Image>().raycastTarget = false;

            var descT = MakeTMP(popup, "Desc", card.ActiveDescriptionKo,
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.68f), 26, new Color(0.9f, 0.9f, 0.9f));
            descT.alignment = TextAlignmentOptions.TopLeft;
            descT.raycastTarget = false;

            MakeTMP(popup, "Hint", "(아무 곳이나 터치하여 닫기)",
                new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.1f), 18, new Color(0.5f, 0.5f, 0.5f)).raycastTarget = false;
        }

        // ======================== EVENTS ========================

        void SubscribeEvents()
        {
            var bm = BattleManager.Instance;
            if (bm == null) return;
            bm.OnBattleStarted += OnBattleStarted;
            bm.OnPhaseChanged += OnPhaseChanged;
            bm.OnCardPlayed += OnCardPlayed;
            bm.OnEnemyActed += OnEnemyActed;
            bm.OnBattleEnded += OnBattleEnded;
            bm.OnEnemyDied += OnEnemyDied;
            bm.OnCcImmune += OnCcImmune;
        }

        void OnBattleStarted()
        {
            BuildBattleCanvas();
            BuildEnemyPanels();
            RefreshAll();
            RefreshPotions();
            AddLog("전투 시작!");
        }

        void OnPhaseChanged(BattlePhase phase)
        {
            _endTurnBtn.interactable = phase == BattlePhase.PlayerTurn;
            if (phase == BattlePhase.PlayerTurn)
            {
                AddLog($"<color=#44ff44>--- {BattleManager.Instance.TurnNumber}턴 ---</color>");
                RefreshAll();
            }
        }

        void OnCardPlayed(CardInstance card) => RefreshAll();

        void OnEnemyActed(EnemyInstance enemy, MoveData move)
        {
            AddLog($"<color=#ff8844>{enemy.DisplayName}</color>: {IntentStr(move, enemy.StatScale)}");
            RefreshAll();
        }

        void OnEnemyDied(EnemyInstance enemy) =>
            AddLog($"<color=#ff5555>{enemy.DisplayName} 처치!</color>");

        void OnCcImmune(EnemyInstance enemy) =>
            AddLog($"<color=#ffaa33>{enemy.DisplayName}: 군중제어 면역!</color>");

        void OnBattleEnded(bool victory)
        {
            string title = victory ? "승리!" : "패배";
            Color c = victory ? new Color(0.1f, 0.3f, 0.1f, 0.95f) : new Color(0.3f, 0.05f, 0.05f, 0.95f);
            Color tc = victory ? new Color(1, 0.85f, 0.2f) : new Color(1, 0.3f, 0.3f);

            var ov = MakeRect(_root, "Overlay",
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f), Vector2.zero, Vector2.zero);
            ov.gameObject.AddComponent<Image>().color = c;
            MakeTMP(ov, "T", title,
                new Vector2(0, 0.5f), new Vector2(1, 0.9f), 56, tc).alignment = TextAlignmentOptions.Center;

            if (victory)
            {
                var run = GameManager.Instance.CurrentRun;
                MakeTMP(ov, "S", $"HP: {run.currentHp}/{run.maxHp}",
                    new Vector2(0, 0.1f), new Vector2(1, 0.5f), 28, Color.white)
                    .alignment = TextAlignmentOptions.Center;
            }

            AddLog(victory
                ? "<color=#44ff44>===== 승리 =====</color>"
                : "<color=#ff3333>===== 패배 =====</color>");
        }

        void OnEndTurnClicked()
        {
            _draggingCard = null;
            _draggingCardInstance = null;
            BattleManager.Instance?.EndPlayerTurn();
        }

        // ======================== ENEMY PANELS ========================

        void BuildEnemyPanels()
        {
            foreach (var p in _enemyPanels)
                if (p.root != null) Destroy(p.root.gameObject);
            _enemyPanels.Clear();

            var enemies = BattleManager.Instance.Enemies;
            float w = 1f / Mathf.Max(enemies.Count, 1);

            for (int i = 0; i < enemies.Count; i++)
            {
                float xMin = i * w + w * 0.05f;
                float xMax = (i + 1) * w - w * 0.05f;

                var panel = MakeRect(_enemyArea, $"E{i}",
                    new Vector2(xMin, 0.05f), new Vector2(xMax, 0.95f), Vector2.zero, Vector2.zero);
                var panelImg = panel.gameObject.AddComponent<Image>();
                panelImg.color = new Color(0.15f, 0.12f, 0.2f, 0.8f);

                var dropTarget = panel.gameObject.AddComponent<EnemyDropTarget>();
                dropTarget.EnemyIndex = i;
                dropTarget.OnEnemyHovered = OnEnemyHover;
                dropTarget.OnEnemyUnhovered = OnEnemyUnhover;

                Color nameColor = enemies[i].CcImmune ? new Color(1f, 0.75f, 0.3f) : Color.white;
                string enemyDisplayName = enemies[i].DisplayName;
                if (enemies[i].CcImmune) enemyDisplayName += " <size=18><color=#ffaa33>[군중제어 면역]</color></size>";
                MakeTMP(panel, "Name", enemyDisplayName,
                    new Vector2(0, 0.78f), new Vector2(1, 0.95f), 26, nameColor).raycastTarget = false;

                var hpBg = MakeRect(panel, "HpBg",
                    new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.76f), Vector2.zero, Vector2.zero);
                hpBg.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.1f, 0.1f);
                var hpFill = MakeRect(hpBg, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var hpImg = hpFill.gameObject.AddComponent<Image>();
                hpImg.color = new Color(0.8f, 0.2f, 0.2f);

                var hpT = MakeTMP(panel, "Hp", $"{enemies[i].CurrentHp}/{enemies[i].MaxHp}",
                    new Vector2(0, 0.58f), new Vector2(1, 0.68f), 24, Color.white);
                hpT.raycastTarget = false;
                var intT = MakeTMP(panel, "Intent", "",
                    new Vector2(0, 0.42f), new Vector2(1, 0.56f), 22, new Color(1, 0.8f, 0.4f));
                intT.raycastTarget = false;
                var stT = MakeTMP(panel, "St", "",
                    new Vector2(0, 0.02f), new Vector2(1, 0.4f), 18, new Color(0.7f, 0.7f, 1f));
                stT.raycastTarget = false;

                _enemyPanels.Add(new EnemyPanel
                {
                    root = panel, enemy = enemies[i], hpBar = hpImg,
                    hpText = hpT, intentText = intT, statusText = stT,
                    panelImage = panelImg
                });
            }
        }

        // ======================== HAND / CARDS ========================

        void RefreshHand()
        {
            foreach (var b in _handButtons)
                Destroy(b.root.gameObject);
            _handButtons.Clear();

            var deck = BattleManager.Instance?.Deck;
            if (deck == null) return;

            int cnt = deck.Hand.Count;
            if (cnt == 0) return;

            var ps = BattleManager.Instance.PlayerState;
            int str = ps.StatusEffects.TryGetValue(StatusKeys.Strength, out int sv) ? sv : 0;
            int dex = ps.StatusEffects.TryGetValue(StatusKeys.Dexterity, out int dv) ? dv : 0;
            bool weak = ps.StatusEffects.TryGetValue(StatusKeys.Weak, out int wv) && wv > 0;

            float cw = Mathf.Min(0.22f, 0.96f / cnt);
            float total = cw * cnt;
            float sx = (1f - total) / 2f;

            for (int i = 0; i < cnt; i++)
            {
                var card = deck.Hand[i];
                float x = sx + i * cw;

                var rt = MakeRect(_handArea, $"C{i}",
                    new Vector2(x + 0.004f, 0.02f), new Vector2(x + cw - 0.004f, 0.98f),
                    Vector2.zero, Vector2.zero);

                Color bg = card.Data.type switch
                {
                    "attack" => new Color(0.5f, 0.15f, 0.15f, 0.9f),
                    "skill" => new Color(0.15f, 0.25f, 0.5f, 0.9f),
                    "power" => new Color(0.35f, 0.15f, 0.5f, 0.9f),
                    _ => new Color(0.3f, 0.3f, 0.3f, 0.9f)
                };

                var img = rt.gameObject.AddComponent<Image>();
                img.color = bg;

                bool canPay = ps.Energy >= card.Cost;

                var drag = rt.gameObject.AddComponent<CardDragHandler>();
                drag.HandIndex = i;
                drag.OnDragStarted = OnCardDragStart;
                drag.OnDragging = OnCardDragging;
                drag.OnDragEnded = OnCardDragEnd;

                if (!canPay)
                {
                    var cg = rt.gameObject.GetComponent<CanvasGroup>();
                    if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0.5f;
                }

                var cost = MakeTMP(rt, "Cost", card.Cost.ToString(),
                    new Vector2(0, 0.82f), new Vector2(0.3f, 1f), 32,
                    canPay ? new Color(1, 0.85f, 0.2f) : new Color(1, 0.3f, 0.3f));
                cost.alignment = TextAlignmentOptions.Center;
                cost.raycastTarget = false;

                string handName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;
                var nameT = MakeTMP(rt, "Name", handName,
                    new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.82f), 24, Color.white);
                nameT.fontStyle = FontStyles.Bold;
                nameT.raycastTarget = false;

                string descText = card.GetBattleDescKo(str, dex, weak,
                    ps.StatusEffects, deck.Hand.Count, deck.DiscardPile.Count);
                var desc = MakeTMP(rt, "Desc", descText,
                    new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.55f), 19, new Color(0.85f, 0.85f, 0.85f));
                desc.alignment = TextAlignmentOptions.TopLeft;
                desc.raycastTarget = false;

                _handButtons.Add(new CardButton { root = rt, card = card, drag = drag });
            }
        }

        // ======================== DRAG HANDLERS ========================

        void OnCardDragStart(CardDragHandler handler)
        {
            if (BattleManager.Instance.Phase != BattlePhase.PlayerTurn) return;

            int idx = handler.HandIndex;
            var deck = BattleManager.Instance.Deck;
            if (idx >= deck.Hand.Count) return;

            _draggingCardInstance = deck.Hand[idx];

            if (BattleManager.Instance.PlayerState.Energy < _draggingCardInstance.Cost)
            {
                _draggingCardInstance = null;
                return;
            }

            _draggingCard = handler;

            if (_draggingCardInstance.Data.targetType == "single")
                AddLog($"적에게 드래그하여 사용: {_draggingCardInstance.Data.nameKo ?? _draggingCardInstance.Data.name}");
            else
                AddLog($"위로 드래그하여 사용: {_draggingCardInstance.Data.nameKo ?? _draggingCardInstance.Data.name}");
        }

        void OnCardDragging(CardDragHandler handler, PointerEventData eventData)
        {
            if (_draggingCardInstance == null) return;

            if (_draggingCardInstance.Data.targetType == "single")
            {
                UpdateDragArrow(handler, eventData);
                CheckEnemyUnderCursor(eventData);
            }
        }

        void OnCardDragEnd(CardDragHandler handler, PointerEventData eventData)
        {
            _dragArrow.gameObject.SetActive(false);
            ClearEnemyHighlight();

            if (_draggingCardInstance == null)
            {
                handler.ReturnToHand();
                return;
            }

            bool played = false;

            if (_draggingCardInstance.Data.targetType == "single")
            {
                int enemyIdx = GetEnemyAtPosition(eventData.position);
                if (enemyIdx >= 0)
                {
                    var target = BattleManager.Instance.Enemies[enemyIdx];
                    if (!target.IsDead)
                    {
                        played = BattleManager.Instance.TryPlayCard(_draggingCardInstance, target);
                        if (played)
                            AddLog($"<color=#ffcc44>{_draggingCardInstance.Data.nameKo ?? _draggingCardInstance.Data.name}</color> -> {target.DisplayName}");
                    }
                }
            }
            else
            {
                bool draggedUp = eventData.position.y > _playThresholdY;
                if (draggedUp)
                {
                    played = BattleManager.Instance.TryPlayCard(_draggingCardInstance, null);
                    if (played)
                        AddLog($"<color=#ffcc44>{_draggingCardInstance.Data.nameKo ?? _draggingCardInstance.Data.name} 사용</color>");
                }
            }

            _draggingCard = null;
            _draggingCardInstance = null;
            _hoveredEnemyIndex = -1;

            if (played)
                RefreshAll();
            else
                handler.ReturnToHand();
        }

        void UpdateDragArrow(CardDragHandler handler, PointerEventData eventData)
        {
            _dragArrow.gameObject.SetActive(true);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root, eventData.position, null, out Vector2 mouseLocal);

            var cardRT = handler.GetComponent<RectTransform>();
            Vector2 cardCenter = (Vector2)_root.InverseTransformPoint(cardRT.position);

            Vector2 dir = mouseLocal - cardCenter;
            float dist = dir.magnitude;

            var arrowRT = _dragArrow.rectTransform;
            arrowRT.anchoredPosition = (cardCenter + mouseLocal) / 2f;
            arrowRT.sizeDelta = new Vector2(6, dist);
            arrowRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        void CheckEnemyUnderCursor(PointerEventData eventData)
        {
            int idx = GetEnemyAtPosition(eventData.position);
            if (idx != _hoveredEnemyIndex)
            {
                ClearEnemyHighlight();
                _hoveredEnemyIndex = idx;
                if (idx >= 0 && idx < _enemyPanels.Count)
                    _enemyPanels[idx].panelImage.color = new Color(0.35f, 0.15f, 0.15f, 0.95f);
            }
        }

        void ClearEnemyHighlight()
        {
            if (_hoveredEnemyIndex >= 0 && _hoveredEnemyIndex < _enemyPanels.Count)
                _enemyPanels[_hoveredEnemyIndex].panelImage.color = new Color(0.15f, 0.12f, 0.2f, 0.8f);
            _hoveredEnemyIndex = -1;
        }

        int GetEnemyAtPosition(Vector2 screenPos)
        {
            var results = new List<RaycastResult>();
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var r in results)
            {
                var drop = r.gameObject.GetComponent<EnemyDropTarget>();
                if (drop != null) return drop.EnemyIndex;
            }
            return -1;
        }

        void OnEnemyHover(int index)
        {
            if (_draggingCardInstance == null) return;
            if (index >= 0 && index < _enemyPanels.Count)
                _enemyPanels[index].panelImage.color = new Color(0.35f, 0.15f, 0.15f, 0.95f);
        }

        void OnEnemyUnhover(int index)
        {
            if (index >= 0 && index < _enemyPanels.Count)
                _enemyPanels[index].panelImage.color = new Color(0.15f, 0.12f, 0.2f, 0.8f);
        }

        // ======================== REFRESH ========================

        void RefreshAll()
        {
            RefreshPlayerHUD();
            RefreshEnemies();
            RefreshHand();
            UpdateMasterSkillButton();
        }

        void RefreshPlayerHUD()
        {
            var ps = BattleManager.Instance?.PlayerState;
            if (ps == null) return;

            _playerHpText.text = $"HP {ps.CurrentHp}/{ps.MaxHp}";
            _playerHpBar.rectTransform.anchorMax = new Vector2(
                Mathf.Clamp01((float)ps.CurrentHp / ps.MaxHp), 1);
            _energyText.text = $"{ps.Energy}/{ps.MaxEnergy}";
            _blockText.text = ps.Block > 0 ? $"방어 {ps.Block}" : "";

            _statusText.text = FormatStatus(ps.StatusEffects);
        }

        void RefreshEnemies()
        {
            foreach (var p in _enemyPanels)
            {
                if (p.enemy.IsDead) { p.root.gameObject.SetActive(false); continue; }

                string blockStr = p.enemy.Block > 0 ? $"  [방어 {p.enemy.Block}]" : "";
                p.hpText.text = $"{p.enemy.CurrentHp}/{p.enemy.MaxHp}{blockStr}";
                p.hpBar.rectTransform.anchorMax = new Vector2(
                    Mathf.Clamp01((float)p.enemy.CurrentHp / p.enemy.MaxHp), 1);

                var ai = new EnemyAI();
                int savedIdx = p.enemy.PatternIndex;
                var move = ai.GetNextMove(p.enemy);
                p.enemy.PatternIndex = savedIdx;
                if (move != null) p.intentText.text = IntentStr(move, p.enemy.StatScale);

                p.statusText.text = FormatStatus(p.enemy.StatusEffects);
            }
        }

        // ======================== HELPERS ========================

        string FormatStatus(Dictionary<string, int> effects)
        {
            var parts = new List<string>();
            foreach (var kv in effects)
            {
                if (kv.Value <= 0) continue;
                string label = kv.Key switch
                {
                    "burn" => $"<color=#ff6633>화상 {kv.Value}</color>",
                    "poison" => $"<color=#33cc33>독 {kv.Value}</color>",
                    "weak" => $"<color=#cc99ff>약화 {kv.Value}</color>",
                    "vulnerable" => $"<color=#ff9944>취약 {kv.Value}</color>",
                    "strength" => $"<color=#ff4444>힘+{kv.Value}</color>",
                    "dexterity" => $"<color=#4488ff>민첩+{kv.Value}</color>",
                    "thorns" => $"<color=#88aaff>가시 {kv.Value}</color>",
                    "haste" => $"<color=#44ffcc>신속 {kv.Value}</color>",
                    "ember" => $"<color=#ff8800>잔불 {kv.Value}</color>",
                    "combustion" => $"<color=#ff4400>연소</color>",
                    "growth" => $"<color=#22cc44>성장 {kv.Value}</color>",
                    "regen" => $"<color=#44ff88>재생 {kv.Value}</color>",
                    "root" => $"<color=#886622>속박 {kv.Value}</color>",
                    _ => $"{kv.Key} {kv.Value}"
                };
                parts.Add(label);
            }
            return string.Join("\n", parts);
        }

        void AddLog(string msg)
        {
            _logLines.Add(msg);
            if (_logLines.Count > 4) _logLines.RemoveAt(0);
            if (_logText != null) _logText.text = string.Join("\n", _logLines);
        }

        string IntentStr(MoveData m, float statScale = 1f)
        {
            var p = new List<string>();
            foreach (var e in m.effects)
            {
                bool scaleThis = e.type == "damage" || e.type == "block" || e.type == "heal";
                int val = scaleThis
                    ? Mathf.Max(1, Mathf.RoundToInt(e.value * statScale))
                    : (int)e.value;
                p.Add(e.type switch
                {
                    "damage" => $"공격 {val}",
                    "block" => $"방어 {val}",
                    "applyBurn" => $"화상 {val}",
                    "applyWeak" => $"약화 {val}",
                    "applyVulnerable" => $"취약 {val}",
                    "applyStrength" => $"힘+{val}",
                    "heal" => $"회복 {val}",
                    "multiHit" => $"x{val}",
                    "purge" => "정화",
                    _ => e.type
                });
            }
            return string.Join(" ", p);
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
            FontManager.ApplyTo(tmp);
            return tmp;
        }

        class EnemyPanel
        {
            public RectTransform root;
            public EnemyInstance enemy;
            public Image hpBar;
            public Image panelImage;
            public TextMeshProUGUI hpText, intentText, statusText;
        }

        class CardButton
        {
            public RectTransform root;
            public CardInstance card;
            public CardDragHandler drag;
        }
    }
}
