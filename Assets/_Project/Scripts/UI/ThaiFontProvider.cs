using UnityEngine;
using UnityEngine.UI;

namespace BangkokLastSurvivor.UI
{
    /// <summary>
    /// Makes sure every uGUI Text under this object uses a font that contains Thai glyphs.
    /// If <see cref="thaiFont"/> is assigned (recommended: import Sarabun / Noto Sans Thai .ttf), it is used.
    /// Otherwise a system font is loaded at runtime (Windows: Leelawadee UI / Tahoma, Android: Noto Sans Thai).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ThaiFontProvider : MonoBehaviour
    {
        [Tooltip("Optional project font with Thai support. Leave empty to use an OS font.")]
        [SerializeField] private Font thaiFont;

        private static readonly string[] OsFontCandidates =
        {
            "Leelawadee UI", "Leelawadee", "Tahoma", "Noto Sans Thai", "NotoSansThai",
            "Noto Sans Thai UI", "Droid Sans Thai", "Thonburi", "Sukhumvit Set", "Arial Unicode MS"
        };

        private static Font _cachedOsFont;

        public static Font GetThaiFont(Font preferred = null)
        {
            if (preferred != null) return preferred;
            if (_cachedOsFont != null) return _cachedOsFont;
            _cachedOsFont = Font.CreateDynamicFontFromOSFont(OsFontCandidates, 32);
            return _cachedOsFont;
        }

        private void Awake()
        {
            Font font = GetThaiFont(thaiFont);
            if (font == null) return;
            foreach (Text text in GetComponentsInChildren<Text>(true))
                text.font = font;
        }
    }
}
