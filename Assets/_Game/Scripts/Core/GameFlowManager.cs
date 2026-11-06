using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Battle;
using SpellboundAscent.Card;
using SpellboundAscent.Event;
using SpellboundAscent.Map;
using SpellboundAscent.Relic;
using SpellboundAscent.UI;

namespace SpellboundAscent.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        List<List<MapNode>> _currentMap;
        MapUI _mapUI;
        RewardUI _rewardUI;
        string _lastEnemyType;
        bool _wasBossBattle;
        string _lastRemovedCardName;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ShowTitle()
        {
            var go = new GameObject("TitleCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.08f);

            var titleT = MakeTMP(root, "Title", "SPELLBOUND\nASCENT",
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.78f), 72,
                new Color(1, 0.85f, 0.2f));
            titleT.alignment = TextAlignmentOptions.Center;
            titleT.fontStyle = FontStyles.Bold;

            var subT = MakeTMP(root, "Sub", "로그라이크 덱빌더",
                new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.45f), 30,
                new Color(0.6f, 0.6f, 0.7f));
            subT.alignment = TextAlignmentOptions.Center;

            bool hasSave = SaveLoadManager.HasSave();

            if (hasSave)
            {
                var continueBtn = MakeRect(root, "Continue",
                    new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.33f), Vector2.zero, Vector2.zero);
                var contImg = continueBtn.gameObject.AddComponent<Image>();
                contImg.color = new Color(0.15f, 0.3f, 0.55f);
                var cb = continueBtn.gameObject.AddComponent<Button>();
                cb.targetGraphic = contImg;
                cb.onClick.AddListener(() =>
                {
                    Destroy(go);
                    LoadAndContinue();
                });
                var contT = MakeTMP(continueBtn, "Txt", "이어하기",
                    Vector2.zero, Vector2.one, 32, Color.white);
                contT.alignment = TextAlignmentOptions.Center;

                var newBtn = MakeRect(root, "NewGame",
                    new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.23f), Vector2.zero, Vector2.zero);
                var newImg = newBtn.gameObject.AddComponent<Image>();
                newImg.color = new Color(0.3f, 0.3f, 0.3f);
                var nb = newBtn.gameObject.AddComponent<Button>();
                nb.targetGraphic = newImg;
                nb.onClick.AddListener(() =>
                {
                    SaveLoadManager.DeleteSave();
                    Destroy(go);
                    ShowClassSelect();
                });
                var newT = MakeTMP(newBtn, "Txt", "새 게임",
                    Vector2.zero, Vector2.one, 26, new Color(0.7f, 0.7f, 0.7f));
                newT.alignment = TextAlignmentOptions.Center;

                var cardCBtn = MakeRect(root, "CardCodex",
                    new Vector2(0.03f, 0.06f), new Vector2(0.33f, 0.13f), Vector2.zero, Vector2.zero);
                cardCBtn.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.35f);
                var ccb1 = cardCBtn.gameObject.AddComponent<Button>();
                ccb1.targetGraphic = cardCBtn.GetComponent<Image>();
                ccb1.onClick.AddListener(() => CardCodexUI.Show());
                MakeTMP(cardCBtn, "Txt", "카드 도감",
                    Vector2.zero, Vector2.one, 22, new Color(0.7f, 0.8f, 1f)).alignment = TextAlignmentOptions.Center;

                var potCBtn = MakeRect(root, "PotionCodex",
                    new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.13f), Vector2.zero, Vector2.zero);
                potCBtn.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.22f, 0.15f);
                var pcb1 = potCBtn.gameObject.AddComponent<Button>();
                pcb1.targetGraphic = potCBtn.GetComponent<Image>();
                pcb1.onClick.AddListener(() => PotionCodexUI.Show());
                MakeTMP(potCBtn, "Txt", "물약 도감",
                    Vector2.zero, Vector2.one, 22, new Color(0.5f, 1f, 0.6f)).alignment = TextAlignmentOptions.Center;

                var relCBtn = MakeRect(root, "RelicCodex",
                    new Vector2(0.67f, 0.06f), new Vector2(0.97f, 0.13f), Vector2.zero, Vector2.zero);
                relCBtn.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.2f);
                var rcb1 = relCBtn.gameObject.AddComponent<Button>();
                rcb1.targetGraphic = relCBtn.GetComponent<Image>();
                rcb1.onClick.AddListener(() => RelicCodexUI.Show());
                MakeTMP(relCBtn, "Txt", "유물 도감",
                    Vector2.zero, Vector2.one, 22, new Color(1f, 0.7f, 0.9f)).alignment = TextAlignmentOptions.Center;
            }
            else
            {
                var tapT = MakeTMP(root, "Tap", "화면을 터치하세요",
                    new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.22f), 26,
                    new Color(0.5f, 0.5f, 0.5f));
                tapT.alignment = TextAlignmentOptions.Center;

                var cardCBtn2 = MakeRect(root, "CardCodex",
                    new Vector2(0.03f, 0.06f), new Vector2(0.33f, 0.13f), Vector2.zero, Vector2.zero);
                cardCBtn2.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.35f);
                var ccb2 = cardCBtn2.gameObject.AddComponent<Button>();
                ccb2.targetGraphic = cardCBtn2.GetComponent<Image>();
                ccb2.onClick.AddListener(() => CardCodexUI.Show());
                MakeTMP(cardCBtn2, "Txt", "카드 도감",
                    Vector2.zero, Vector2.one, 22, new Color(0.7f, 0.8f, 1f)).alignment = TextAlignmentOptions.Center;

                var potCBtn2 = MakeRect(root, "PotionCodex",
                    new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.13f), Vector2.zero, Vector2.zero);
                potCBtn2.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.22f, 0.15f);
                var pcb2 = potCBtn2.gameObject.AddComponent<Button>();
                pcb2.targetGraphic = potCBtn2.GetComponent<Image>();
                pcb2.onClick.AddListener(() => PotionCodexUI.Show());
                MakeTMP(potCBtn2, "Txt", "물약 도감",
                    Vector2.zero, Vector2.one, 22, new Color(0.5f, 1f, 0.6f)).alignment = TextAlignmentOptions.Center;

                var relCBtn2 = MakeRect(root, "RelicCodex",
                    new Vector2(0.67f, 0.06f), new Vector2(0.97f, 0.13f), Vector2.zero, Vector2.zero);
                relCBtn2.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.2f);
                var rcb2 = relCBtn2.gameObject.AddComponent<Button>();
                rcb2.targetGraphic = relCBtn2.GetComponent<Image>();
                rcb2.onClick.AddListener(() => RelicCodexUI.Show());
                MakeTMP(relCBtn2, "Txt", "유물 도감",
                    Vector2.zero, Vector2.one, 22, new Color(1f, 0.7f, 0.9f)).alignment = TextAlignmentOptions.Center;

                var tapBtn = MakeRect(root, "TapArea",
                    new Vector2(0, 0.14f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                var tapImg = tapBtn.gameObject.AddComponent<Image>();
                tapImg.color = new Color(0, 0, 0, 0);
                tapImg.raycastTarget = true;
                var btn = tapBtn.gameObject.AddComponent<Button>();
                btn.targetGraphic = tapImg;
                btn.onClick.AddListener(() =>
                {
                    Destroy(go);
                    ShowClassSelect();
                });
            }
        }

        public void ShowClassSelect()
        {
            var go = new GameObject("ClassSelectCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f);

            MakeTMP(root, "Title", "SPELLBOUND ASCENT",
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), 46,
                new Color(1, 0.85f, 0.2f));

            MakeTMP(root, "Sub", "클래스를 선택하세요",
                new Vector2(0.1f, 0.83f), new Vector2(0.9f, 0.88f), 26,
                new Color(0.7f, 0.7f, 0.7f));

            var balance = GameManager.Instance.Balance;

            BuildClassCard(root, go, "pyromancer",
                "Pyromancer", "화염술사",
                balance.classHP.Get("pyromancer"),
                new Color(0.6f, 0.15f, 0.1f),
                "잔불 축적 → 폭발 버스트 데미지\n특기: 잔불, 기폭, 자기희생\n마스터: 점화 (잔불×2 전체 데미지)",
                new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.82f));

            BuildClassCard(root, go, "warden",
                "Warden", "수호자",
                balance.classHP.Get("warden"),
                new Color(0.1f, 0.4f, 0.15f),
                "턴마다 성장하는 방어 스케일링\n특기: 성장, 가시, 재생, 속박\n마스터: 철벽 (블록 2배 + 신속)",
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.60f));

            BuildClassCard(root, go, "chronomancer",
                "Chronomancer", "시간술사",
                balance.classHP.Get("chronomancer"),
                new Color(0.15f, 0.2f, 0.5f),
                "드로우/에너지 조작으로 대량 콤보\n특기: 가속, 시간 정지, 핸드 활용\n마스터: 되감기 (카드 복귀 + 에너지)",
                new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.40f));

            BuildClassCard(root, go, "necromancer",
                "Necromancer", "사령술사",
                balance.classHP.Get("necromancer"),
                new Color(0.35f, 0.1f, 0.35f),
                "독과 자기희생으로 고위험 고수익\n특기: 독, 생명 흡수, 자해 카드\n마스터: 영혼 수확 (독 회수 + 힘)",
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.20f));
        }

        void BuildClassCard(RectTransform root, GameObject canvas, string classId,
            string name, string nameKo, int hp, Color color, string desc,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var card = MakeRect(root, $"Class_{classId}",
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = color;

            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = cardImg;

            var colors = btn.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;

            btn.onClick.AddListener(() =>
            {
                Destroy(canvas);
                StartRun(classId);
            });

            var portraitArea = MakeRect(card, "Portrait",
                new Vector2(0.01f, 0.05f), new Vector2(0.24f, 0.95f), Vector2.zero, Vector2.zero);
            var portraitBg = portraitArea.gameObject.AddComponent<Image>();
            portraitBg.color = new Color(0, 0, 0, 0.3f);
            portraitBg.raycastTarget = false;
            BuildClassPortrait(portraitArea, classId);

            var classNameT = MakeTMP(card, "Name", $"{name}  /  {nameKo}",
                new Vector2(0.26f, 0.68f), new Vector2(0.72f, 0.95f), 30, Color.white);
            classNameT.fontStyle = FontStyles.Bold;

            MakeTMP(card, "HP", $"HP: {hp}",
                new Vector2(0.76f, 0.68f), new Vector2(0.98f, 0.95f), 26,
                new Color(0.4f, 1f, 0.4f));

            var descTMP = MakeTMP(card, "Desc", desc,
                new Vector2(0.26f, 0.05f), new Vector2(0.98f, 0.65f), 20,
                new Color(0.85f, 0.85f, 0.85f));
            descTMP.alignment = TextAlignmentOptions.TopLeft;
        }

        void BuildClassPortrait(RectTransform parent, string classId)
        {
            switch (classId)
            {
                case "pyromancer":
                    BuildPyromancerPortrait(parent);
                    break;
                case "warden":
                    BuildWardenPortrait(parent);
                    break;
                case "chronomancer":
                    BuildChronomancerPortrait(parent);
                    break;
                case "necromancer":
                    BuildNecromancerPortrait(parent);
                    break;
            }
        }

        void BuildPyromancerPortrait(RectTransform parent)
        {
            var flame1 = MakeRect(parent, "Flame1",
                new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.75f), Vector2.zero, Vector2.zero);
            var f1 = flame1.gameObject.AddComponent<Image>();
            f1.color = new Color(1f, 0.4f, 0.05f, 0.9f);
            f1.raycastTarget = false;
            flame1.localRotation = Quaternion.Euler(0, 0, 45);

            var flame2 = MakeRect(parent, "Flame2",
                new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.85f), Vector2.zero, Vector2.zero);
            var f2 = flame2.gameObject.AddComponent<Image>();
            f2.color = new Color(1f, 0.7f, 0.1f, 0.85f);
            f2.raycastTarget = false;
            flame2.localRotation = Quaternion.Euler(0, 0, 45);

            var core = MakeRect(parent, "Core",
                new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.65f), Vector2.zero, Vector2.zero);
            var c = core.gameObject.AddComponent<Image>();
            c.color = new Color(1f, 0.95f, 0.5f, 0.9f);
            c.raycastTarget = false;

            var spark1 = MakeRect(parent, "Spark1",
                new Vector2(0.1f, 0.7f), new Vector2(0.25f, 0.85f), Vector2.zero, Vector2.zero);
            var s1 = spark1.gameObject.AddComponent<Image>();
            s1.color = new Color(1f, 0.5f, 0.1f, 0.7f);
            s1.raycastTarget = false;
            spark1.localRotation = Quaternion.Euler(0, 0, 30);

            var spark2 = MakeRect(parent, "Spark2",
                new Vector2(0.7f, 0.75f), new Vector2(0.85f, 0.9f), Vector2.zero, Vector2.zero);
            var s2 = spark2.gameObject.AddComponent<Image>();
            s2.color = new Color(1f, 0.6f, 0.15f, 0.6f);
            s2.raycastTarget = false;
            spark2.localRotation = Quaternion.Euler(0, 0, -20);

            MakeTMP(parent, "Icon", "FIRE",
                new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.2f), 16,
                new Color(1, 0.6f, 0.2f, 0.7f)).raycastTarget = false;
        }

        void BuildWardenPortrait(RectTransform parent)
        {
            var trunk = MakeRect(parent, "Trunk",
                new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.45f), Vector2.zero, Vector2.zero);
            var t = trunk.gameObject.AddComponent<Image>();
            t.color = new Color(0.4f, 0.25f, 0.1f, 0.9f);
            t.raycastTarget = false;

            var canopy1 = MakeRect(parent, "Canopy1",
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.75f), Vector2.zero, Vector2.zero);
            var c1 = canopy1.gameObject.AddComponent<Image>();
            c1.color = new Color(0.15f, 0.6f, 0.2f, 0.85f);
            c1.raycastTarget = false;

            var canopy2 = MakeRect(parent, "Canopy2",
                new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.zero);
            var c2 = canopy2.gameObject.AddComponent<Image>();
            c2.color = new Color(0.1f, 0.5f, 0.15f, 0.9f);
            c2.raycastTarget = false;

            var leaf1 = MakeRect(parent, "Leaf1",
                new Vector2(0.05f, 0.6f), new Vector2(0.25f, 0.8f), Vector2.zero, Vector2.zero);
            var l1 = leaf1.gameObject.AddComponent<Image>();
            l1.color = new Color(0.2f, 0.7f, 0.25f, 0.6f);
            l1.raycastTarget = false;
            leaf1.localRotation = Quaternion.Euler(0, 0, 15);

            var leaf2 = MakeRect(parent, "Leaf2",
                new Vector2(0.75f, 0.65f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
            var l2 = leaf2.gameObject.AddComponent<Image>();
            l2.color = new Color(0.2f, 0.7f, 0.25f, 0.6f);
            l2.raycastTarget = false;
            leaf2.localRotation = Quaternion.Euler(0, 0, -15);

            MakeTMP(parent, "Icon", "NATURE",
                new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.2f), 16,
                new Color(0.3f, 0.9f, 0.4f, 0.7f)).raycastTarget = false;
        }

        void BuildChronomancerPortrait(RectTransform parent)
        {
            var outerRing = MakeRect(parent, "Ring",
                new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            var or = outerRing.gameObject.AddComponent<Image>();
            or.color = new Color(0.3f, 0.45f, 0.9f, 0.8f);
            or.raycastTarget = false;

            var innerRing = MakeRect(parent, "Inner",
                new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.82f), Vector2.zero, Vector2.zero);
            var ir = innerRing.gameObject.AddComponent<Image>();
            ir.color = new Color(0.08f, 0.1f, 0.25f, 0.9f);
            ir.raycastTarget = false;

            var hand1 = MakeRect(parent, "Hand1",
                new Vector2(0.47f, 0.5f), new Vector2(0.53f, 0.78f), Vector2.zero, Vector2.zero);
            var h1 = hand1.gameObject.AddComponent<Image>();
            h1.color = new Color(0.5f, 0.7f, 1f, 0.9f);
            h1.raycastTarget = false;

            var hand2 = MakeRect(parent, "Hand2",
                new Vector2(0.5f, 0.5f), new Vector2(0.72f, 0.58f), Vector2.zero, Vector2.zero);
            var h2 = hand2.gameObject.AddComponent<Image>();
            h2.color = new Color(0.5f, 0.7f, 1f, 0.9f);
            h2.raycastTarget = false;

            var center = MakeRect(parent, "Center",
                new Vector2(0.44f, 0.49f), new Vector2(0.56f, 0.61f), Vector2.zero, Vector2.zero);
            var ct = center.gameObject.AddComponent<Image>();
            ct.color = new Color(0.6f, 0.8f, 1f, 0.95f);
            ct.raycastTarget = false;

            var glow1 = MakeRect(parent, "Glow1",
                new Vector2(0.05f, 0.75f), new Vector2(0.2f, 0.9f), Vector2.zero, Vector2.zero);
            var g1 = glow1.gameObject.AddComponent<Image>();
            g1.color = new Color(0.4f, 0.5f, 1f, 0.4f);
            g1.raycastTarget = false;
            glow1.localRotation = Quaternion.Euler(0, 0, 45);

            MakeTMP(parent, "Icon", "TIME",
                new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.2f), 16,
                new Color(0.5f, 0.7f, 1f, 0.7f)).raycastTarget = false;
        }

        void BuildNecromancerPortrait(RectTransform parent)
        {
            var skull = MakeRect(parent, "Skull",
                new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.9f), Vector2.zero, Vector2.zero);
            var sk = skull.gameObject.AddComponent<Image>();
            sk.color = new Color(0.75f, 0.65f, 0.85f, 0.85f);
            sk.raycastTarget = false;

            var jaw = MakeRect(parent, "Jaw",
                new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.45f), Vector2.zero, Vector2.zero);
            var jw = jaw.gameObject.AddComponent<Image>();
            jw.color = new Color(0.65f, 0.55f, 0.75f, 0.8f);
            jw.raycastTarget = false;

            var eyeL = MakeRect(parent, "EyeL",
                new Vector2(0.22f, 0.6f), new Vector2(0.42f, 0.8f), Vector2.zero, Vector2.zero);
            var el = eyeL.gameObject.AddComponent<Image>();
            el.color = new Color(0.15f, 0f, 0.2f, 0.95f);
            el.raycastTarget = false;

            var eyeR = MakeRect(parent, "EyeR",
                new Vector2(0.58f, 0.6f), new Vector2(0.78f, 0.8f), Vector2.zero, Vector2.zero);
            var er = eyeR.gameObject.AddComponent<Image>();
            er.color = new Color(0.15f, 0f, 0.2f, 0.95f);
            er.raycastTarget = false;

            var pupilL = MakeRect(parent, "PupilL",
                new Vector2(0.28f, 0.65f), new Vector2(0.36f, 0.75f), Vector2.zero, Vector2.zero);
            var pl = pupilL.gameObject.AddComponent<Image>();
            pl.color = new Color(0.6f, 0.1f, 0.8f, 0.9f);
            pl.raycastTarget = false;

            var pupilR = MakeRect(parent, "PupilR",
                new Vector2(0.64f, 0.65f), new Vector2(0.72f, 0.75f), Vector2.zero, Vector2.zero);
            var pr = pupilR.gameObject.AddComponent<Image>();
            pr.color = new Color(0.6f, 0.1f, 0.8f, 0.9f);
            pr.raycastTarget = false;

            var aura1 = MakeRect(parent, "Aura1",
                new Vector2(0f, 0.1f), new Vector2(0.2f, 0.35f), Vector2.zero, Vector2.zero);
            var a1 = aura1.gameObject.AddComponent<Image>();
            a1.color = new Color(0.5f, 0.1f, 0.6f, 0.4f);
            a1.raycastTarget = false;

            var aura2 = MakeRect(parent, "Aura2",
                new Vector2(0.8f, 0.15f), new Vector2(1f, 0.4f), Vector2.zero, Vector2.zero);
            var a2 = aura2.gameObject.AddComponent<Image>();
            a2.color = new Color(0.5f, 0.1f, 0.6f, 0.4f);
            a2.raycastTarget = false;

            MakeTMP(parent, "Icon", "DEATH",
                new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.2f), 16,
                new Color(0.7f, 0.5f, 0.9f, 0.7f)).raycastTarget = false;
        }

        void LoadAndContinue()
        {
            if (SaveLoadManager.Load(out var run, out var map))
            {
                GameManager.Instance.RestoreRun(run);
                _currentMap = map;
                ShowMap();
            }
            else
            {
                ShowClassSelect();
            }
        }

        public void StartRun(string playerClass)
        {
            GameManager.Instance.StartNewRun(playerClass);
            GenerateMap();
            ShowMap();
        }

        void GenerateMap()
        {
            var run = GameManager.Instance.CurrentRun;
            var gen = new MapGenerator();
            _currentMap = gen.Generate(run.currentAct, run.seed);
        }

        void ShowMap()
        {
            if (_mapUI != null) Destroy(_mapUI);

            SaveLoadManager.Save(GameManager.Instance.CurrentRun, _currentMap);

            var go = new GameObject("MapUI");
            _mapUI = go.AddComponent<MapUI>();
            _mapUI.OnNodeSelected += OnNodeSelected;

            var run = GameManager.Instance.CurrentRun;
            _mapUI.ShowMap(_currentMap, run.currentFloor);
        }

        void OnNodeSelected(MapNode node)
        {
            var run = GameManager.Instance.CurrentRun;
            run.currentFloor = node.floor + 1;

            switch (node.type)
            {
                case NodeType.Normal:
                    StartBattle("normal");
                    break;
                case NodeType.Elite:
                    StartBattle("elite");
                    break;
                case NodeType.Boss:
                    StartBattle("boss");
                    break;
                case NodeType.Rest:
                    DoRestSite();
                    break;
                case NodeType.Shop:
                    DoShop();
                    break;
                case NodeType.Event:
                    DoEvent();
                    break;
                default:
                    ShowMap();
                    break;
            }
        }

        void StartBattle(string enemyType)
        {
            _lastEnemyType = enemyType;
            var run = GameManager.Instance.CurrentRun;

            var picked = new List<Enemy.EnemyData>();
            var scales = new List<float>();
            var ccImmunes = new List<bool>();

            float ccChance = run.currentFloor <= 3 ? 0f : run.currentFloor <= 7 ? 0.12f : 0.2f;

            if (enemyType == "normal")
            {
                var enemies = GameManager.Instance.GetEnemies(run.currentAct, "normal");
                if (enemies.Count == 0) return;

                int count = RollNormalEnemyCount(run.currentFloor);
                float s = count switch { 2 => 0.6f, 3 => 0.47f, _ => 1f };

                for (int i = 0; i < count; i++)
                {
                    picked.Add(enemies[Random.Range(0, enemies.Count)]);
                    scales.Add(s);
                    ccImmunes.Add(Random.value < ccChance);
                }
            }
            else if (enemyType == "elite")
            {
                var elites = GameManager.Instance.GetEnemies(run.currentAct, "elite");
                if (elites.Count == 0)
                    elites = GameManager.Instance.GetEnemies(run.currentAct, "normal");

                picked.Add(elites[Random.Range(0, elites.Count)]);
                scales.Add(1f);
                ccImmunes.Add(Random.value < 0.35f);

                if (Random.value < 0.35f)
                {
                    var normals = GameManager.Instance.GetEnemies(run.currentAct, "normal");
                    if (normals.Count > 0)
                    {
                        picked.Add(normals[Random.Range(0, normals.Count)]);
                        scales.Add(0.3f);
                        ccImmunes.Add(false);
                    }
                }
            }
            else if (enemyType == "boss")
            {
                var bosses = GameManager.Instance.GetEnemies(run.currentAct, "boss");
                if (bosses.Count == 0)
                    bosses = GameManager.Instance.GetEnemies(run.currentAct, "normal");

                picked.Add(bosses[Random.Range(0, bosses.Count)]);
                scales.Add(1f);
                ccImmunes.Add(true);
            }

            BattleManager.Instance.OnBattleEnded -= OnBattleEnded;
            BattleManager.Instance.OnBattleEnded += OnBattleEnded;
            BattleManager.Instance.StartBattle(picked, scales, ccImmunes);
        }

        int RollNormalEnemyCount(int floor)
        {
            float roll = Random.value;
            if (floor <= 1) return 1;
            if (floor <= 4) return roll < 0.5f ? 1 : 2;
            if (floor <= 7) return roll < 0.25f ? 1 : roll < 0.75f ? 2 : 3;
            return roll < 0.15f ? 1 : roll < 0.55f ? 2 : 3;
        }

        void OnBattleEnded(bool victory)
        {
            BattleManager.Instance.OnBattleEnded -= OnBattleEnded;

            if (!victory)
            {
                ShowGameOver();
                return;
            }

            var run = GameManager.Instance.CurrentRun;
            _wasBossBattle = _lastEnemyType == "boss";

            if (_wasBossBattle && run.currentAct >= 3)
            {
                ShowVictoryScreen();
                return;
            }

            ShowReward();
        }

        void ShowReward()
        {
            var rewardMgr = new RewardManager();
            var run = GameManager.Instance.CurrentRun;
            var reward = rewardMgr.GenerateReward(_lastEnemyType ?? "normal", run.currentAct);

            var go = new GameObject("RewardUI");
            _rewardUI = go.AddComponent<RewardUI>();
            _rewardUI.OnRewardComplete += OnRewardComplete;
            _rewardUI.ShowReward(reward);
        }

        void OnRewardComplete()
        {
            if (_rewardUI != null) Destroy(_rewardUI.gameObject);
            DestroyBattleCanvas();

            if (_wasBossBattle)
            {
                _wasBossBattle = false;
                ShowActTransition();
            }
            else
            {
                ShowMap();
            }
        }

        void DoRestSite()
        {
            var run = GameManager.Instance.CurrentRun;
            ShowRestUI(run);
        }

        void ShowRestUI(RunState run)
        {
            var go = new GameObject("RestCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.1f, 0.05f, 0.95f);

            MakeTMP(root, "Title", "휴식처",
                new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.85f), 48,
                new Color(0.4f, 1f, 0.4f));

            MakeTMP(root, "HpInfo", $"HP: {run.currentHp}/{run.maxHp}",
                new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.7f), 28, Color.white);

            var balance = GameManager.Instance.Balance;
            int healAmount = (int)(run.maxHp * balance.restSite.healPercent);

            var restBtn = MakeRect(root, "Rest",
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.58f), Vector2.zero, Vector2.zero);
            var restImg = restBtn.gameObject.AddComponent<Image>();
            restImg.color = new Color(0.15f, 0.5f, 0.2f);
            var rb = restBtn.gameObject.AddComponent<Button>();
            rb.targetGraphic = restImg;
            rb.onClick.AddListener(() =>
            {
                run.HealPercent(balance.restSite.healPercent);
                Destroy(go);
                ShowMap();
            });
            MakeTMP(restBtn, "Txt", $"휴식 (HP {healAmount} 회복)",
                Vector2.zero, Vector2.one, 28, Color.white);

            bool hasUpgradeable = false;
            foreach (var c in run.deck)
            {
                if (!c.Upgraded) { hasUpgradeable = true; break; }
            }

            var upgBtn = MakeRect(root, "Upgrade",
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.43f), Vector2.zero, Vector2.zero);
            var upgImg = upgBtn.gameObject.AddComponent<Image>();
            upgImg.color = hasUpgradeable
                ? new Color(0.4f, 0.3f, 0.15f)
                : new Color(0.2f, 0.2f, 0.2f);
            var ub = upgBtn.gameObject.AddComponent<Button>();
            ub.targetGraphic = upgImg;
            ub.interactable = hasUpgradeable;
            ub.onClick.AddListener(() =>
            {
                Destroy(go);
                ShowUpgradeUI(run);
            });
            MakeTMP(upgBtn, "Txt", "카드 업그레이드",
                Vector2.zero, Vector2.one, 28,
                hasUpgradeable ? new Color(1, 0.85f, 0.2f) : new Color(0.5f, 0.5f, 0.5f));
        }

        void ShowUpgradeUI(RunState run)
        {
            var go = new GameObject("UpgradeCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            MakeTMP(root, "Title", "업그레이드할 카드를 선택하세요",
                new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.98f), 32, new Color(1, 0.85f, 0.2f));

            var cancelBtn = MakeRect(root, "Cancel",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.07f), Vector2.zero, Vector2.zero);
            var cancelImg = cancelBtn.gameObject.AddComponent<Image>();
            cancelImg.color = new Color(0.5f, 0.15f, 0.15f);
            var cb = cancelBtn.gameObject.AddComponent<Button>();
            cb.targetGraphic = cancelImg;
            cb.onClick.AddListener(() => { Destroy(go); ShowRestUI(run); });
            MakeTMP(cancelBtn, "Txt", "돌아가기", Vector2.zero, Vector2.one, 24, Color.white);

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
            float cardH = 220f;
            float padX = 20f;
            float padY = 10f;

            int total = run.deck.Count;
            int rows = Mathf.CeilToInt(total / (float)cols);
            float totalH = rows * (cardH + padY) + padY;
            content.sizeDelta = new Vector2(0, totalH);

            for (int i = 0; i < total; i++)
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

                var cardImg = cardRT.gameObject.AddComponent<Image>();
                cardImg.color = cardColor;

                string upgStr = card.Upgraded ? " <color=#44ff44>+</color>" : "";
                MakeTMP(cardRT, "Cost", card.Cost.ToString(),
                    new Vector2(0, 0.75f), new Vector2(0.2f, 1f), 28,
                    new Color(1, 0.85f, 0.2f)).raycastTarget = false;

                string cardName = !string.IsNullOrEmpty(card.Data.nameKo) ? card.Data.nameKo : card.Data.name;
                var ugNameT = MakeTMP(cardRT, "Name", cardName + upgStr,
                    new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.75f), 24, Color.white);
                ugNameT.fontStyle = FontStyles.Bold;
                ugNameT.raycastTarget = false;

                string currentDesc = card.ActiveDescriptionKo;
                if (!card.Upgraded && card.Data.upgradeEffects.Count > 0)
                {
                    string upgDesc = CardInstance.BuildEffectDesc(card.Data.upgradeEffects, card.Data.exhaust, true);
                    MakeTMP(cardRT, "Desc", currentDesc,
                        new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.5f), 15,
                        new Color(0.7f, 0.7f, 0.7f)).raycastTarget = false;
                    var upgDescT = MakeTMP(cardRT, "UpgDesc", $"<color=#44ff44>▶</color> {upgDesc}",
                        new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f), 15,
                        new Color(0.3f, 1f, 0.4f));
                    upgDescT.alignment = TextAlignmentOptions.TopLeft;
                    upgDescT.raycastTarget = false;
                }
                else
                {
                    MakeTMP(cardRT, "Desc", currentDesc,
                        new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.5f), 17,
                        new Color(0.8f, 0.8f, 0.8f)).raycastTarget = false;
                }

                if (!card.Upgraded)
                {
                    var btn = cardRT.gameObject.AddComponent<Button>();
                    btn.targetGraphic = cardImg;
                    var captured = card;
                    btn.onClick.AddListener(() =>
                    {
                        captured.Upgrade();
                        Destroy(go);
                        ShowMap();
                    });
                }
                else
                {
                    cardImg.color *= new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
            }
        }

        void DoShop()
        {
            var go = new GameObject("ShopUI");
            var shop = go.AddComponent<ShopUI>();
            shop.OnShopClosed += () => { Destroy(go); ShowMap(); };
            shop.Show();
        }

        void DoEvent()
        {
            var run = GameManager.Instance.CurrentRun;
            var eventData = GameManager.Instance.GetRandomEvent(run.currentAct);
            if (eventData == null)
            {
                ShowMap();
                return;
            }

            ShowEventUI(eventData);
        }

        void ShowEventUI(EventData eventData)
        {
            var go = new GameObject("EventCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.95f);

            string evtTitle = !string.IsNullOrEmpty(eventData.nameKo) ? eventData.nameKo : eventData.name;
            var titleT = MakeTMP(root, "Title", evtTitle,
                new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.82f), 42,
                new Color(0.4f, 0.6f, 1f));
            titleT.fontStyle = FontStyles.Bold;

            string evtText = !string.IsNullOrEmpty(eventData.textKo) ? eventData.textKo : eventData.text;
            var descTMP = MakeTMP(root, "Desc", evtText,
                new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.68f), 26, Color.white);
            descTMP.alignment = TextAlignmentOptions.TopLeft;

            var run = GameManager.Instance.CurrentRun;
            float btnY = 0.42f;

            for (int i = 0; i < eventData.choices.Count; i++)
            {
                var choice = eventData.choices[i];

                bool meetsCondition = true;
                if (choice.condition != null)
                {
                    meetsCondition = choice.condition.type switch
                    {
                        "minGold" => run.gold >= (int)choice.condition.value,
                        "minHp" => run.currentHp >= (int)choice.condition.value,
                        _ => true
                    };
                }

                float y0 = btnY - i * 0.09f;
                float y1 = y0 + 0.07f;

                var btnRT = MakeRect(root, $"Choice{i}",
                    new Vector2(0.08f, y0), new Vector2(0.92f, y1), Vector2.zero, Vector2.zero);
                var btnImg = btnRT.gameObject.AddComponent<Image>();
                btnImg.color = meetsCondition
                    ? new Color(0.15f, 0.2f, 0.4f)
                    : new Color(0.2f, 0.2f, 0.2f);
                var btn = btnRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.interactable = meetsCondition;

                var capturedChoice = choice;
                btn.onClick.AddListener(() =>
                {
                    ResolveEventChoice(capturedChoice, run);
                    Destroy(go);
                    string resText = !string.IsNullOrEmpty(capturedChoice.resultTextKo) ? capturedChoice.resultTextKo : capturedChoice.resultText;
                    if (!string.IsNullOrEmpty(_lastRemovedCardName))
                        resText += $"\n\n<color=#ff8844>제거된 카드: {_lastRemovedCardName}</color>";
                    ShowEventResult(resText);
                });

                string choiceLabel = !string.IsNullOrEmpty(choice.labelKo) ? choice.labelKo : choice.label;
                MakeTMP(btnRT, "Txt", choiceLabel,
                    Vector2.zero, Vector2.one, 26,
                    meetsCondition ? Color.white : new Color(0.5f, 0.5f, 0.5f));
            }
        }

        void ResolveEventChoice(EventChoice choice, RunState run)
        {
            _lastRemovedCardName = null;
            foreach (var effect in choice.effects)
            {
                int value = (int)effect.value;
                switch (effect.type)
                {
                    case "heal":
                        run.Heal(value);
                        break;
                    case "damage":
                        run.TakeDamage(value);
                        break;
                    case "gainGold":
                        run.AddGold(value);
                        break;
                    case "loseGold":
                        run.SpendGold(value);
                        break;
                    case "maxHpUp":
                        run.maxHp += value;
                        run.Heal(value);
                        break;
                    case "maxHpDown":
                        run.maxHp = Mathf.Max(1, run.maxHp - value);
                        if (run.currentHp > run.maxHp) run.currentHp = run.maxHp;
                        break;
                    case "upgradeRandom":
                        UpgradeRandomCard(run, value);
                        break;
                    case "removeWeakest":
                        _lastRemovedCardName = RemoveWeakestCard(run);
                        break;
                }
            }
        }

        void UpgradeRandomCard(RunState run, int count)
        {
            var upgradeable = run.deck.FindAll(c => !c.Upgraded);
            for (int i = 0; i < count && upgradeable.Count > 0; i++)
            {
                int idx = Random.Range(0, upgradeable.Count);
                upgradeable[idx].Upgrade();
                upgradeable.RemoveAt(idx);
            }
        }

        string RemoveWeakestCard(RunState run)
        {
            if (run.deck.Count <= 4) return null;
            CardInstance weakest = null;
            foreach (var c in run.deck)
            {
                if (c.Data.rarity == "starter" && !c.Upgraded)
                {
                    weakest = c;
                    break;
                }
            }
            if (weakest == null && run.deck.Count > 0)
                weakest = run.deck[Random.Range(0, run.deck.Count)];

            if (weakest != null)
            {
                string name = !string.IsNullOrEmpty(weakest.Data.nameKo)
                    ? weakest.Data.nameKo : weakest.Data.name;
                run.RemoveCard(weakest);
                return name;
            }
            return null;
        }

        void ShowEventResult(string resultText)
        {
            var go = new GameObject("EventResultCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 22;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.95f);

            var resultTMP = MakeTMP(root, "Result", resultText,
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.65f), 28, new Color(0.8f, 1f, 0.6f));
            resultTMP.alignment = TextAlignmentOptions.Center;

            var run = GameManager.Instance.CurrentRun;
            MakeTMP(root, "HP", $"HP: {run.currentHp}/{run.maxHp}  골드: {run.gold}",
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.43f), 24, Color.white);

            var btnRT = MakeRect(root, "Continue",
                new Vector2(0.3f, 0.22f), new Vector2(0.7f, 0.32f), Vector2.zero, Vector2.zero);
            var btnImg = btnRT.gameObject.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.3f, 0.5f);
            var btn = btnRT.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => { Destroy(go); ShowMap(); });
            MakeTMP(btnRT, "Txt", "계속", Vector2.zero, Vector2.one, 28, Color.white);
        }

        void ShowActTransition()
        {
            var run = GameManager.Instance.CurrentRun;
            int completedAct = run.currentAct;

            var go = new GameObject("ActTransitionCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.06f, 0.98f);

            Color actColor = completedAct switch
            {
                1 => new Color(0.3f, 0.7f, 1f),
                2 => new Color(1f, 0.6f, 0.2f),
                _ => new Color(1f, 0.85f, 0.2f)
            };

            MakeTMP(root, "Complete", $"{completedAct}막",
                new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), 72, actColor);

            MakeTMP(root, "Label", "클리어!",
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.64f), 42,
                new Color(0.8f, 0.8f, 0.8f));

            string nextActName = (completedAct + 1) switch
            {
                2 => "저주받은 심연",
                3 => "첨탑의 정상",
                _ => "알 수 없음"
            };
            MakeTMP(root, "Next", $"{completedAct + 1}막 진입: {nextActName}",
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.54f), 26,
                new Color(0.6f, 0.6f, 0.6f));

            MakeTMP(root, "Stats",
                $"HP: {run.currentHp}/{run.maxHp}   골드: {run.gold}   덱: {run.deck.Count}장   유물: {run.relics.Count}개",
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.44f), 22, Color.white);

            var continueBtn = MakeRect(root, "Continue",
                new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.32f), Vector2.zero, Vector2.zero);
            var continueImg = continueBtn.gameObject.AddComponent<Image>();
            continueImg.color = new Color(0.15f, 0.3f, 0.55f);
            var cb = continueBtn.gameObject.AddComponent<Button>();
            cb.targetGraphic = continueImg;
            cb.onClick.AddListener(() =>
            {
                Destroy(go);
                ShowTreasureRoom();
            });
            MakeTMP(continueBtn, "Txt", "보물방 열기",
                Vector2.zero, Vector2.one, 28, new Color(1, 0.85f, 0.2f));
        }

        void ShowTreasureRoom()
        {
            var run = GameManager.Instance.CurrentRun;
            var relicChoices = RelicManager.Instance.GetRandomRelics(3, "rare");

            var go = new GameObject("TreasureCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.05f, 0.02f, 0.98f);

            MakeTMP(root, "Title", "보물방",
                new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.93f), 48,
                new Color(1, 0.85f, 0.2f));

            MakeTMP(root, "Sub", "보상으로 레어 유물 하나를 선택하세요",
                new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.82f), 24,
                new Color(0.7f, 0.7f, 0.7f));

            if (relicChoices.Count == 0)
            {
                MakeTMP(root, "Empty", "획득 가능한 유물이 없습니다...",
                    new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), 28,
                    new Color(0.5f, 0.5f, 0.5f));

                var skipBtn = MakeRect(root, "Skip",
                    new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.3f), Vector2.zero, Vector2.zero);
                var skipImg = skipBtn.gameObject.AddComponent<Image>();
                skipImg.color = new Color(0.3f, 0.3f, 0.3f);
                var sb = skipBtn.gameObject.AddComponent<Button>();
                sb.targetGraphic = skipImg;
                sb.onClick.AddListener(() => { Destroy(go); AdvanceToNextAct(); });
                MakeTMP(skipBtn, "Txt", "계속", Vector2.zero, Vector2.one, 26, Color.white);
                return;
            }

            float cardHeight = 0.18f;
            float startY = 0.72f - (3 - relicChoices.Count) * 0.03f;

            for (int i = 0; i < relicChoices.Count; i++)
            {
                var relic = relicChoices[i];
                float y0 = startY - (i + 1) * (cardHeight + 0.02f);
                float y1 = y0 + cardHeight;

                var relicCard = MakeRect(root, $"Relic{i}",
                    new Vector2(0.06f, y0), new Vector2(0.94f, y1), Vector2.zero, Vector2.zero);
                var relicImg = relicCard.gameObject.AddComponent<Image>();
                relicImg.color = new Color(0.18f, 0.12f, 0.06f);

                var btn = relicCard.gameObject.AddComponent<Button>();
                btn.targetGraphic = relicImg;

                var colors = btn.colors;
                colors.highlightedColor = new Color(0.3f, 0.22f, 0.1f);
                colors.pressedColor = new Color(0.15f, 0.1f, 0.05f);
                btn.colors = colors;

                var capturedRelic = relic;
                btn.onClick.AddListener(() =>
                {
                    RelicManager.Instance.AddRelic(capturedRelic);
                    Debug.Log($"[Treasure] Acquired relic: {capturedRelic.name}");
                    Destroy(go);
                    AdvanceToNextAct();
                });

                Color rarityColor = new Color(1, 0.85f, 0.2f);

                string relicName = !string.IsNullOrEmpty(relic.nameKo) ? relic.nameKo : relic.name;
                var relNameT = MakeTMP(relicCard, "Name", relicName,
                    new Vector2(0.04f, 0.6f), new Vector2(0.7f, 0.95f), 30, rarityColor);
                relNameT.fontStyle = FontStyles.Bold;
                relNameT.alignment = TextAlignmentOptions.Left;

                MakeTMP(relicCard, "NameEn", relic.name,
                    new Vector2(0.7f, 0.6f), new Vector2(0.96f, 0.95f), 20,
                    new Color(0.6f, 0.5f, 0.3f)).alignment = TextAlignmentOptions.Right;

                string relicDesc = !string.IsNullOrEmpty(relic.descriptionKo) ? relic.descriptionKo : relic.description;
                var descTMP = MakeTMP(relicCard, "Desc", relicDesc,
                    new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.58f), 22,
                    new Color(0.85f, 0.85f, 0.85f));
                descTMP.alignment = TextAlignmentOptions.Left;
            }

            var passBtn = MakeRect(root, "Skip",
                new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.14f), Vector2.zero, Vector2.zero);
            var passImg = passBtn.gameObject.AddComponent<Image>();
            passImg.color = new Color(0.3f, 0.2f, 0.2f);
            var pb = passBtn.gameObject.AddComponent<Button>();
            pb.targetGraphic = passImg;
            pb.onClick.AddListener(() => { Destroy(go); AdvanceToNextAct(); });
            MakeTMP(passBtn, "Txt", "건너뛰기", Vector2.zero, Vector2.one, 24,
                new Color(0.7f, 0.7f, 0.7f));
        }

        void AdvanceToNextAct()
        {
            var run = GameManager.Instance.CurrentRun;
            run.currentAct++;
            run.currentFloor = 0;
            GenerateMap();
            ShowMap();
        }

        void ShowGameOver()
        {
            SaveLoadManager.DeleteSave();
            DestroyBattleCanvas();
            var run = GameManager.Instance.CurrentRun;

            var go = new GameObject("GameOverCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.02f, 0.02f, 0.95f);

            MakeTMP(root, "Title", "게임 오버",
                new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.8f), 64,
                new Color(1, 0.3f, 0.3f));

            string className = run.playerClass switch { "pyromancer" => "화염술사", "warden" => "수호자", "chronomancer" => "시간술사", "necromancer" => "사령술사", _ => run.playerClass };
            MakeTMP(root, "Stats",
                $"클래스: {className}\n{run.currentAct}막 - {run.currentFloor}층\n골드: {run.gold}\n덱: {run.deck.Count}장\n유물: {run.relics.Count}개",
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.58f), 28, Color.white);

            var retryBtn = MakeRect(root, "Retry",
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.3f), Vector2.zero, Vector2.zero);
            var retryImg = retryBtn.gameObject.AddComponent<Image>();
            retryImg.color = new Color(0.5f, 0.15f, 0.15f);
            var rb = retryBtn.gameObject.AddComponent<Button>();
            rb.targetGraphic = retryImg;
            rb.onClick.AddListener(() => { Destroy(go); ShowClassSelect(); });
            MakeTMP(retryBtn, "Txt", "다시 도전", Vector2.zero, Vector2.one, 32, Color.white);
        }

        void ShowVictoryScreen()
        {
            SaveLoadManager.DeleteSave();
            DestroyBattleCanvas();
            var run = GameManager.Instance.CurrentRun;

            var go = new GameObject("VictoryCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bg = MakeRect(root, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.02f, 0.95f);

            MakeTMP(root, "Title", "승리!",
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.85f), 72,
                new Color(1, 0.85f, 0.2f));

            MakeTMP(root, "Sub", "3개의 막을 모두 정복했습니다!",
                new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.65f), 28,
                new Color(0.8f, 1f, 0.5f));

            string vClassName = run.playerClass switch { "pyromancer" => "화염술사", "warden" => "수호자", "chronomancer" => "시간술사", "necromancer" => "사령술사", _ => run.playerClass };
            MakeTMP(root, "Stats",
                $"클래스: {vClassName}\nHP: {run.currentHp}/{run.maxHp}\n골드: {run.gold}\n덱: {run.deck.Count}장\n유물: {run.relics.Count}개",
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.55f), 28, Color.white);

            var newBtn = MakeRect(root, "NewRun",
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.3f), Vector2.zero, Vector2.zero);
            var newImg = newBtn.gameObject.AddComponent<Image>();
            newImg.color = new Color(0.15f, 0.5f, 0.2f);
            var nb = newBtn.gameObject.AddComponent<Button>();
            nb.targetGraphic = newImg;
            nb.onClick.AddListener(() => { Destroy(go); ShowClassSelect(); });
            MakeTMP(newBtn, "Txt", "새 게임", Vector2.zero, Vector2.one, 32, Color.white);
        }

        void DestroyBattleCanvas()
        {
            var battleCanvas = GameObject.Find("BattleCanvas");
            if (battleCanvas != null) Destroy(battleCanvas);
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
