using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordShot.Data
{
    /// <summary>
    /// 翻訳。日本語の文字をキーにして、今の言語の文字を引く。
    /// </summary>
    /// <remarks>
    /// 翻訳ファイル：Assets/Data/Resources/Localization/（言語）.json（例：en.json、ko.json、zh-Hans.json）。
    /// 同じ言葉は、どのボスで出ても同じ翻訳になる。
    /// 言語を足すときは、翻訳ファイルを1つ置くだけでよい（遊べる言語の一覧に自動で出る。プログラムは変えない）。
    /// </remarks>
    public static class Localization
    {
        public const string Folder = "Localization";

        static Dictionary<string, string> table;
        static string tableLanguage;
        static List<LanguageInfo> languages;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            table = null;
            tableLanguage = null;
            languages = null;
        }

        /// <summary>
        /// 遊べる言語の一覧。日本語が先頭で、そのあとに翻訳ファイルがある言語が並ぶ。言語を選ぶ画面で使う
        /// </summary>
        public static IReadOnlyList<LanguageInfo> AvailableLanguages => languages ??= FindLanguages();

        public static bool IsAvailable(string language) => AvailableLanguages.Any(l => l.Code == language);

        // 言語の名前はファイル名（en.json なら "en"）。画面に出す名前はファイルの "displayName"
        static List<LanguageInfo> FindLanguages()
        {
            var result = new List<LanguageInfo> { new LanguageInfo(GameLanguage.Japanese, "日本語") };
            foreach (TextAsset file in Resources.LoadAll<TextAsset>(Folder).OrderBy(f => f.name))
            {
                if (file.name == GameLanguage.Japanese) continue;

                LocalizationFile data = null;
                try
                {
                    data = JsonUtility.FromJson<LocalizationFile>(file.text);
                }
                catch (Exception)
                {
                    // 書き間違いは LoadTable（データのチェック）で知らせる。一覧には名前だけで出す
                }
                string displayName = string.IsNullOrEmpty(data?.displayName) ? file.name : data.displayName;
                result.Add(new LanguageInfo(file.name, displayName, data?.fallbackFonts));
            }
            return result;
        }

        /// <summary>
        /// 今の言語での文字。日本語版ならそのまま返す。翻訳がなければ null
        /// </summary>
        public static string Translate(string japanese)
        {
            if (string.IsNullOrEmpty(japanese) || GameLanguage.IsJapanese) return japanese;

            if (tableLanguage != GameLanguage.Current)
            {
                tableLanguage = GameLanguage.Current;
                table = LoadTable(tableLanguage, problems: null);
            }
            return table.TryGetValue(japanese, out string translated) ? translated : null;
        }

        /// <summary>今の言語での文字。翻訳がなければ日本語のまま返す</summary>
        public static string TranslateOrSame(string japanese) => Translate(japanese) ?? japanese;

        /// <summary>
        /// 翻訳ファイルを読んで、日本語 → 翻訳の表にする。ファイルがなければ空の表。
        /// problems を渡すと、ファイルの書き間違いをそこに入れる（データのチェック用）
        /// </summary>
        public static Dictionary<string, string> LoadTable(string language, DataReport problems)
        {
            var result = new Dictionary<string, string>();
            string fileName = $"{Folder}/{language}.json";

            var file = Resources.Load<TextAsset>($"{Folder}/{language}");
            if (file == null)
            {
                if (problems != null) problems.Error(fileName, "翻訳ファイルがありません");
                else Debug.LogWarning($"[Localization] {fileName} がありません。日本語のまま表示します");
                return result;
            }

            LocalizationFile data;
            try
            {
                data = JsonUtility.FromJson<LocalizationFile>(file.text);
            }
            catch (Exception e)
            {
                ReportOrLog(problems, fileName, $"JSON の書き方が正しくありません（{e.Message}）");
                return result;
            }

            foreach (LocalizationEntry entry in data.entries ?? Array.Empty<LocalizationEntry>())
            {
                if (string.IsNullOrEmpty(entry.ja) || string.IsNullOrEmpty(entry.text))
                {
                    ReportOrLog(problems, fileName, $"「ja」か「text」が空の行があります（ja：{entry.ja}）");
                    continue;
                }
                if (result.ContainsKey(entry.ja))
                {
                    ReportOrLog(problems, fileName, $"「{entry.ja}」の翻訳が2つあります。最初のものを使います");
                    continue;
                }
                result.Add(entry.ja, entry.text);
            }
            return result;
        }

        static void ReportOrLog(DataReport problems, string fileName, string message)
        {
            if (problems != null) problems.Error(fileName, message);
            else Debug.LogError($"[Localization] {fileName}：{message}");
        }
    }

    /// <summary>言語1つ（言語の名前と、画面に出す名前）</summary>
    public class LanguageInfo
    {
        /// <summary>言語の名前。翻訳ファイルの名前と同じ。例：en、ko、zh-Hans</summary>
        public string Code { get; }

        /// <summary>言語を選ぶ画面に出す名前（その言語で書く）。例：English、한국어、简体中文</summary>
        public string DisplayName { get; }

        /// <summary>この言語の文字を出すときに使う、パソコンに入っているフォントの名前（前から順に探す）</summary>
        public string[] FallbackFonts { get; }

        public LanguageInfo(string code, string displayName, string[] fallbackFonts = null)
        {
            Code = code;
            DisplayName = displayName;
            FallbackFonts = fallbackFonts ?? Array.Empty<string>();
        }
    }

    /// <summary>翻訳ファイル（Localization/（言語）.json）の中身</summary>
    [Serializable]
    public class LocalizationFile
    {
        public int version = 1;

        /// <summary>言語の名前。ファイル名と同じにする。例：en</summary>
        public string language;

        /// <summary>言語を選ぶ画面に出す名前（その言語で書く）。例：English</summary>
        public string displayName;

        /// <summary>
        /// この言語の文字（ハングルや簡体字など）を出すときに使う、パソコンに入っているフォントの名前。前から順に探す。
        /// 例：["Malgun Gothic"]。日本語のフォントで出せる言語なら空でよい
        /// </summary>
        public string[] fallbackFonts = Array.Empty<string>();

        public LocalizationEntry[] entries = Array.Empty<LocalizationEntry>();
    }

    /// <summary>翻訳1つ</summary>
    [Serializable]
    public class LocalizationEntry
    {
        /// <summary>日本語（ボスのデータに書いた文字と同じにする）</summary>
        public string ja;

        /// <summary>翻訳</summary>
        public string text;
    }
}
