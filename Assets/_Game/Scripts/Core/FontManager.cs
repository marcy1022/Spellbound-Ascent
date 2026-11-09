using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpellboundAscent.Core
{
    public class FontManager : MonoBehaviour
    {
        public static FontManager Instance { get; private set; }
        public static TMP_FontAsset KoreanFont { get; private set; }
        public static TMP_FontAsset KoreanFontBold { get; private set; }

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
                KoreanFontBold = TryCreateFont("Fonts/Pretendard-ExtraBold", "Pretendard-ExtraBold");
                KoreanFont = TryCreateFont("Fonts/Pretendard-Bold", "Pretendard-Bold");

                if (KoreanFont == null)
                    KoreanFont = KoreanFontBold;
                if (KoreanFont == null)
                    KoreanFont = TryCreateFont("Fonts/NotoSansKR-VF", "NotoSansKR-Dynamic");

                if (KoreanFont == null)
                {
                    Debug.LogWarning("[FontManager] No Korean font found");
                    return;
                }

                if (KoreanFontBold == null)
                    KoreanFontBold = KoreanFont;

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
                Debug.LogWarning($"[FontManager] Font loading failed: {e.Message}");
            }
        }

        static TMP_FontAsset TryCreateFont(string path, string label)
        {
            var font = Resources.Load<Font>(path);
            if (font == null) return null;
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 4096, 4096);
            if (asset != null) asset.name = label;
            return asset;
        }

        public static void ApplyTo(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;

            bool wantBold = (tmp.fontStyle & FontStyles.Bold) != 0;
            var targetFont = wantBold && KoreanFontBold != null ? KoreanFontBold : KoreanFont;
            if (targetFont == null) return;

            tmp.font = targetFont;
            if (wantBold && KoreanFontBold != null)
                tmp.fontStyle &= ~FontStyles.Bold;
        }
    }
}
