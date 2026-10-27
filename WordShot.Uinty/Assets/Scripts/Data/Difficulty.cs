namespace WordShot.Data
{
    /// <summary>
    /// 難易度。かんたんをクリアすると、ふつう（と次のステージ）が解放される。ふつうをクリアすると、むずかしいが解放される。
    /// </summary>
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
    }

    /// <summary>
    /// 妨害の強さ（企画書 仕様④）。かんたん＝なし、ふつう＝あり、むずかしい＝強い、が目安。
    /// </summary>
    public enum Interference
    {
        None,
        Normal,
        Strong,
    }

    public static class DifficultyExtensions
    {
        public static readonly Difficulty[] All = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };

        /// <summary>JSON に書く名前（easy / normal / hard）</summary>
        public static string ToKey(this Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => "easy",
            Difficulty.Normal => "normal",
            _ => "hard",
        };

        /// <summary>JSON に書いた名前から難易度にする。知らない名前なら false</summary>
        public static bool TryParse(string key, out Difficulty difficulty)
        {
            foreach (Difficulty candidate in All)
            {
                if (candidate.ToKey() == key)
                {
                    difficulty = candidate;
                    return true;
                }
            }
            difficulty = Difficulty.Easy;
            return false;
        }

        /// <summary>今の言語での名前（かんたん / ふつう / むずかしい）</summary>
        public static string DisplayName(this Difficulty difficulty) => difficulty switch
        {
            // 翻訳が必要な文字を編集画面が自動で集められるよう、文字は TranslateOrSame の中に直接書く
            Difficulty.Easy => Localization.TranslateOrSame("かんたん"),
            Difficulty.Normal => Localization.TranslateOrSame("ふつう"),
            _ => Localization.TranslateOrSame("むずかしい"),
        };

        /// <summary>JSON に書く名前（none / normal / strong）</summary>
        public static string ToKey(this Interference interference) => interference switch
        {
            Interference.None => "none",
            Interference.Normal => "normal",
            _ => "strong",
        };

        public static bool TryParse(string key, out Interference interference)
        {
            foreach (Interference candidate in new[] { Interference.None, Interference.Normal, Interference.Strong })
            {
                if (candidate.ToKey() == key)
                {
                    interference = candidate;
                    return true;
                }
            }
            interference = Interference.None;
            return false;
        }
    }
}
