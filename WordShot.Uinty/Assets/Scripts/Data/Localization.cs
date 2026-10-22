using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordShot.Data
{
    /// <summary>
    /// 翻訳。日本語の文字をキーにして、今の言語の文字を引く。
    /// </summary>
    /// <remarks>
    /// 翻訳ファイル：Assets/Data/Resources/Localization/（言語）.json（例：en.json）。
    /// 同じ言葉は、どのボスで出ても同じ翻訳になる。言語を足すときは、ファイルを1つ足すだけでよい。
    /// </remarks>
    public static class Localization
    {
        public const string Folder = "Localization";

        static Dictionary<string, string> table;
        static string tableLanguage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            table = null;
            tableLanguage = null;
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

    /// <summary>翻訳ファイル（Localization/（言語）.json）の中身</summary>
    [Serializable]
    public class LocalizationFile
    {
        public int version = 1;
        public string language;
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
