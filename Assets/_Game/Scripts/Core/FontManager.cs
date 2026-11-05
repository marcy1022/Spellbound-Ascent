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
            var font = Resources.Load<Font>("Fonts/NotoSansKR-VF");
            if (font == null)
            {
                Debug.LogWarning("[FontManager] Korean font not found at Resources/Fonts/NotoSansKR-VF");
                return;
            }

            KoreanFont = TMP_FontAsset.CreateFontAsset(font);
            if (KoreanFont == null)
            {
                Debug.LogWarning("[FontManager] Failed to create TMP_FontAsset from Korean font");
                return;
            }

            KoreanFont.name = "NotoSansKR-Dynamic";

            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                defaultFont.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                if (!defaultFont.fallbackFontAssetTable.Contains(KoreanFont))
                    defaultFont.fallbackFontAssetTable.Add(KoreanFont);
                Debug.Log("[FontManager] Korean font added as fallback to default TMP font");
            }
            else
            {
                Debug.Log("[FontManager] Korean font loaded (no default TMP font found, will apply manually)");
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
