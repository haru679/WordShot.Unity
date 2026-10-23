using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace WordShot.Data
{
    /// <summary>
    /// 言語ごとの予備のフォント（フォールバック）をつなぐ。韓国語や中国語など、日本語のフォントにない文字を出すため。
    /// </summary>
    /// <remarks>
    /// 翻訳ファイルの "fallbackFonts" に書いた、パソコンに入っているフォントを使う（例：Malgun Gothic）。
    /// 今の言語のフォントを一番先に、ほかの言語のフォントをうしろに並べる（言語ボタンの「한국어」なども出せるように）。
    /// フォントのファイルをゲームに入れないので、容量は増えないが、そのフォントがないパソコンでは表示できない。
    /// 製品版では、Noto Sans KR / SC などのフォントをゲームに入れて差し替える。
    /// </remarks>
    public static class LanguageFonts
    {
        static readonly Dictionary<string, TMP_FontAsset> created = new Dictionary<string, TMP_FontAsset>();
        static readonly HashSet<string> warned = new HashSet<string>();

        // このプロジェクトはドメインのリロードが無効なので、再生のたびに作り直す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            created.Clear();
            warned.Clear();
            GameLanguage.Changed += Apply;
            Application.quitting += RemoveAll;
            Apply();
        }

        /// <summary>今の言語に合わせて、予備のフォントを並べ直す</summary>
        public static void Apply()
        {
            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks == null) return;

            fallbacks.RemoveAll(font => created.ContainsValue(font));

            var currentFirst = Localization.AvailableLanguages
                .OrderBy(language => language.Code == GameLanguage.Current ? 0 : 1);
            int insertAt = 0;
            foreach (LanguageInfo language in currentFirst)
            {
                bool isCurrent = language.Code == GameLanguage.Current;
                foreach (string family in language.FallbackFonts)
                {
                    TMP_FontAsset font = GetOrCreate(family);
                    if (font == null || fallbacks.Contains(font)) continue;

                    // 今の言語のフォントは、日本語のフォントより先に使う
                    if (isCurrent) fallbacks.Insert(insertAt++, font);
                    else fallbacks.Add(font);
                }
            }
        }

        static TMP_FontAsset GetOrCreate(string family)
        {
            if (created.TryGetValue(family, out TMP_FontAsset font)) return font;

            font = TMP_FontAsset.CreateFontAsset(family, "Regular");
            if (font == null)
            {
                if (warned.Add(family)) Debug.LogWarning($"[LanguageFonts] このパソコンにフォント「{family}」がありません。その言語の文字が □ になります");
                return null;
            }
            font.name = $"{family} (PC)";
            created.Add(family, font);
            return font;
        }

        // エディタでは TMP Settings がファイルなので、再生を止めたときに、作ったフォントを一覧から外して元に戻す
        static void RemoveAll()
        {
            TMP_Settings.fallbackFontAssets?.RemoveAll(font => font == null || created.ContainsValue(font));
            GameLanguage.Changed -= Apply;
            Application.quitting -= RemoveAll;
        }
    }
}
