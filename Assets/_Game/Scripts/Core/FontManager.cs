using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpellboundAscent.Core
{
    public class FontManager : MonoBehaviour
    {
        public static FontManager Instance { get; private set; }
        public static TMP_FontAsset KoreanFont { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            LoadKoreanFont();
        }

        void LoadKoreanFont()
        {
            try
            {
                var font = Resources.Load<Font>("Fonts/NotoSansKR-VF");
                if (font == null)
                {
                    font = Resources.Load<Font>("Fonts/NotoSansKR-Regular");
                    if (font == null)
                    {
                        Debug.LogWarning("[FontManager] Korean font not found");
                        return;
                    }
                }

                KoreanFont = TMP_FontAsset.CreateFontAsset(font);
                if (KoreanFont == null)
                {
                    Debug.LogWarning("[FontManager] Failed to create TMP_FontAsset");
                    return;
                }

                KoreanFont.name = "NotoSansKR-Dynamic";

                var defaultFont = TMP_Settings.defaultFontAsset;
                if (defaultFont != null)
                {
                    defaultFont.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                    if (!defaultFont.fallbackFontAssetTable.Contains(KoreanFont))
                        defaultFont.fallbackFontAssetTable.Add(KoreanFont);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FontManager] Font loading failed (WebGL?): {e.Message}");
            }
        }

        public static void ApplyTo(TextMeshProUGUI tmp)
        {
            if (KoreanFont == null || tmp == null) return;
            if (tmp.font != null && tmp.font.fallbackFontAssetTable != null
                && tmp.font.fallbackFontAssetTable.Contains(KoreanFont)) return;

            if (tmp.font != null)
            {
                tmp.font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                if (!tmp.font.fallbackFontAssetTable.Contains(KoreanFont))
                    tmp.font.fallbackFontAssetTable.Add(KoreanFont);
            }
            else
            {
                tmp.font = KoreanFont;
            }
        }
    }
}
