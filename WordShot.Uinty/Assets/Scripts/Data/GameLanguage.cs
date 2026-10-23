using System;
using UnityEngine;

namespace WordShot.Data
{
    /// <summary>
    /// ゲームの言語の設定。PlayerPrefs に保存し、次に起動したときも同じ言語で始まる。
    /// </summary>
    /// <remarks>
    /// 言語は "ja"・"en"・"ko"・"zh-Hans" のような名前で表す。翻訳ファイルの名前（Localization/en.json）と同じにする。
    /// 初めて起動したときは、パソコンの言語に合う翻訳があればその言語、なければ日本語で始まる。
    /// 遊べる言語の一覧は Localization.AvailableLanguages。
    /// </remarks>
    public static class GameLanguage
    {
        public const string Japanese = "ja";

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

        public static string Current => current ??= LoadSaved();

        public static bool IsJapanese => Current == Japanese;

        /// <summary>言語を変えて保存する。翻訳ファイルがない言語は選べない</summary>
        public static void Set(string language)
        {
            if (string.IsNullOrEmpty(language) || language == Current) return;
            if (!Localization.IsAvailable(language))
            {
                Debug.LogWarning($"[GameLanguage] 「{language}」の翻訳ファイル（Localization/{language}.json）がないので選べません");
                return;
            }

            current = language;
            PlayerPrefs.SetString(SaveKey, language);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // 保存した言語。まだ選んでいない（初めての起動）か、その言語の翻訳ファイルがなくなっていたら、パソコンの言語に合わせる
        static string LoadSaved()
        {
            string saved = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(saved) && Localization.IsAvailable(saved)) return saved;

            string system = FromSystemLanguage(Application.systemLanguage);
            return system != null && Localization.IsAvailable(system) ? system : Japanese;
        }

        /// <summary>パソコンの言語設定を、言語の名前にする。対応を決めていない言語は null</summary>
        public static string FromSystemLanguage(SystemLanguage language) => language switch
        {
            SystemLanguage.Japanese => Japanese,
            SystemLanguage.English => "en",
            SystemLanguage.Korean => "ko",
            SystemLanguage.Chinese => "zh-Hans",
            SystemLanguage.ChineseSimplified => "zh-Hans",
            SystemLanguage.ChineseTraditional => "zh-Hant",
            SystemLanguage.French => "fr",
            SystemLanguage.German => "de",
            SystemLanguage.Spanish => "es",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.Italian => "it",
            SystemLanguage.Russian => "ru",
            _ => null,
        };
    }
}
