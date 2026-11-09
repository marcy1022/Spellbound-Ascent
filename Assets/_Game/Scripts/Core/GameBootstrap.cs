using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpellboundAscent.Battle;

namespace SpellboundAscent.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        static string _bootLog = "";

        void Awake()
        {
            try
            {
                Log("Bootstrap Awake start");
                EnsureManager<FontManager>("FontManager");
                Log("FontManager OK");
                EnsureManager<GameManager>("GameManager");
                Log("GameManager OK");
                EnsureManager<BattleManager>("BattleManager");
                Log("BattleManager OK");
                EnsureManager<Relic.RelicManager>("RelicManager");
                Log("RelicManager OK");
                EnsureManager<GameFlowManager>("GameFlowManager");
                Log("GameFlowManager OK");
            }
            catch (System.Exception e)
            {
                Log($"Awake FAILED: {e.Message}\n{e.StackTrace}");
                ShowErrorScreen($"Init Error:\n{e.Message}");
            }
        }

        IEnumerator Start()
        {
            yield return null;
            try
            {
                Log("Calling ShowTitle");
                GameFlowManager.Instance.ShowTitle();
                Log("ShowTitle OK");
            }
            catch (System.Exception e)
            {
                Log($"ShowTitle FAILED: {e.Message}\n{e.StackTrace}");
                ShowErrorScreen($"ShowTitle Error:\n{e.Message}");
            }
        }

        T EnsureManager<T>(string name) where T : MonoBehaviour
        {
            var existing = FindAnyObjectByType<T>();
            if (existing != null) return existing;
            var go = new GameObject(name);
            DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }

        static void Log(string msg)
        {
            Debug.Log($"[Bootstrap] {msg}");
            _bootLog += msg + "\n";
        }

        void ShowErrorScreen(string error)
        {
            var go = new GameObject("ErrorCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            var root = go.GetComponent<RectTransform>();

            var bgRT = new GameObject("Bg", typeof(RectTransform)).GetComponent<RectTransform>();
            bgRT.SetParent(root, false);
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.gameObject.AddComponent<Image>().color = new Color(0.1f, 0, 0);

            var txtRT = new GameObject("Txt", typeof(RectTransform)).GetComponent<RectTransform>();
            txtRT.SetParent(root, false);
            txtRT.anchorMin = new Vector2(0.05f, 0.05f);
            txtRT.anchorMax = new Vector2(0.95f, 0.95f);
            txtRT.sizeDelta = Vector2.zero;
            var tmp = txtRT.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{error}\n\n--- Boot Log ---\n{_bootLog}";
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
        }
    }
}
