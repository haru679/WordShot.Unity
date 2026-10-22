using System;
using UnityEngine;

namespace WordShot.Data
{
    /// <summary>
    /// ゲームの言語の設定。PlayerPrefs に保存し、次に起動したときも同じ言語で始まる。
    /// </summary>
    /// <remarks>
    /// 言語は "ja"・"en" のような短い名前で表す。翻訳ファイルの名前（Localization/en.json）と同じにする。
    /// </remarks>
    public static class GameLanguage
    {
        public const string Japanese = "ja";
        public const string English = "en";

        const string SaveKey = "Language";

        static string current;

        /// <summary>言語が変わったとき。画面の文字を出し直すのに使う</summary>
        public static event Action Changed;

        // このプロジェクトはドメインのリロードが無効なので、再生のたびに読み直す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            Changed = null;
        }

        public static string Current => current ??= PlayerPrefs.GetString(SaveKey, Japanese);

        public static bool IsJapanese => Current == Japanese;

        public static void Set(string language)
        {
            if (string.IsNullOrEmpty(language) || language == Current) return;

            current = language;
            PlayerPrefs.SetString(SaveKey, language);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
