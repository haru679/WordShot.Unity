using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace WordShot.Data.EditorTools
{
    /// <summary>
    /// エディタからボスのデータと翻訳ファイルを読み書きする（ボスデータ編集の画面が使う）。
    /// </summary>
    public static class BossDataFiles
    {
        public const string BossFolder = "Assets/Data/Resources/" + BossDatabase.Folder;
        public const string LocalizationFolder = "Assets/Data/Resources/" + Localization.Folder;

        static readonly Encoding Utf8 = new UTF8Encoding(false);

        public static string PathFor(string id) => $"{BossFolder}/{id}.json";

        /// <summary>ボスの JSON ファイルの一覧（名前順）</summary>
        public static string[] ListBossPaths()
        {
            if (!Directory.Exists(BossFolder)) return Array.Empty<string>();
            return Directory.GetFiles(BossFolder, "*.json")
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path)
                .ToArray();
        }

        /// <summary>ファイルを読んでチェックする。JSON が壊れていて読めなければ null</summary>
        public static BossData Load(string path, DataReport report)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            return BossDatabase.Parse(json, ReportName(path), report);
        }

        /// <summary>今の中身をチェックする（保存する前の確認用）</summary>
        public static DataReport Validate(string path, BossData boss)
        {
            var report = new DataReport();
            BossDatabase.Parse(JsonUtility.ToJson(boss), ReportName(path), report);
            return report;
        }

        public static void Save(string path, BossData boss)
        {
            boss.version = BossData.CurrentVersion;
            File.WriteAllText(path, JsonUtility.ToJson(boss, true) + "\n", Utf8);
            AssetDatabase.ImportAsset(path);
        }

        /// <summary>新しいボスのひな形</summary>
        public static BossData CreateTemplate(string id) => new BossData
        {
            version = BossData.CurrentVersion,
            id = id,
            name = "",
            words = new[] { new WordData { text = "", reading = "" } },
            attacks = new[] { new AttackData { type = "bullet" } },
        };

        /// <summary>id として使えるか（半角英数字と _、まだないもの）</summary>
        public static bool IsNewValidId(string id) =>
            !string.IsNullOrEmpty(id)
            && id.All(c => c < 128 && (char.IsLetterOrDigit(c) || c == '_'))
            && !File.Exists(PathFor(id));

        // ---- 翻訳 ----

        /// <summary>翻訳ファイルがある言語（日本語以外）。言語の名前順</summary>
        public static LanguageInfo[] ListTranslationLanguages()
        {
            if (!Directory.Exists(LocalizationFolder)) return Array.Empty<LanguageInfo>();
            return Directory.GetFiles(LocalizationFolder, "*.json")
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Where(code => code != GameLanguage.Japanese)
                .OrderBy(code => code)
                .Select(code => new LanguageInfo(code, LoadTranslations(code).displayName ?? code))
                .ToArray();
        }

        /// <summary>言語の名前として使えるか（英字と - だけ、まだないもの）。例：ko、zh-Hans</summary>
        public static bool IsNewValidLanguage(string code) =>
            !string.IsNullOrEmpty(code)
            && code != GameLanguage.Japanese
            && code.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '-')
            && !File.Exists(TranslationPath(code));

        /// <summary>新しい言語の、空の翻訳ファイルを作る</summary>
        public static void CreateLanguage(string code, string displayName)
        {
            SaveTranslations(new LocalizationFile { language = code, displayName = displayName });
        }

        /// <summary>翻訳ファイルを読む。並び順もそのまま（保存するときに順番を崩さないため）。なければ空</summary>
        public static LocalizationFile LoadTranslations(string language)
        {
            string path = TranslationPath(language);
            LocalizationFile file = File.Exists(path)
                ? JsonUtility.FromJson<LocalizationFile>(File.ReadAllText(path, Encoding.UTF8))
                : null;

            file ??= new LocalizationFile();
            file.language = language;
            file.entries ??= Array.Empty<LocalizationEntry>();
            return file;
        }

        /// <summary>翻訳を保存する。翻訳が空になった行は消す</summary>
        public static void SaveTranslations(LocalizationFile file)
        {
            file.version = 1;
            file.entries = file.entries.Where(e => !string.IsNullOrEmpty(e.ja) && !string.IsNullOrEmpty(e.text)).ToArray();

            string path = TranslationPath(file.language);
            Directory.CreateDirectory(LocalizationFolder);
            File.WriteAllText(path, JsonUtility.ToJson(file, true) + "\n", Utf8);
            AssetDatabase.ImportAsset(path);
        }

        static string TranslationPath(string language) => $"{LocalizationFolder}/{language}.json";

        // ---- 画面の文字 ----

        static readonly Regex TranslateCall = new Regex(@"Translate(?:OrSame)?\(\s*""((?:[^""\\]|\\.)*)""\s*\)");

        /// <summary>
        /// 翻訳が必要な画面の文字（日本語）を集める。
        /// シーンとプレハブの LocalizedText・スクリプトの Localization.Translate("…") / TranslateOrSame("…")・ステージの名前。
        /// </summary>
        public static List<string> CollectUiTexts()
        {
            var result = new List<string>();
            void Add(string text)
            {
                if (!string.IsNullOrWhiteSpace(text) && !result.Contains(text)) result.Add(text);
            }

            foreach (string path in Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
                         .Where(path => path.EndsWith(".unity") || path.EndsWith(".prefab")))
            {
                foreach (string line in File.ReadLines(path))
                {
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("japanese: ")) Add(ParseYamlString(trimmed.Substring("japanese: ".Length)));
                }
            }

            // エディタ用のスクリプト（Editor フォルダ）はゲームの画面に出ないので、調べない
            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                         .Where(path => !path.Replace('\\', '/').Contains("/Editor/")))
            {
                foreach (Match match in TranslateCall.Matches(File.ReadAllText(path, Encoding.UTF8)))
                    Add(Regex.Unescape(match.Groups[1].Value));
            }

            foreach (string guid in AssetDatabase.FindAssets("t:StageData"))
            {
                var stage = AssetDatabase.LoadAssetAtPath<Stages.StageData>(AssetDatabase.GUIDToAssetPath(guid));
                Add(new SerializedObject(stage).FindProperty("displayName").stringValue);
            }
            return result;
        }

        /// <summary>全部のボスの名前とお題の表記（画面の文字と分けるため）</summary>
        public static HashSet<string> CollectBossTexts()
        {
            var result = new HashSet<string>();
            foreach (string path in ListBossPaths())
            {
                BossData boss = Load(path, new DataReport());
                if (boss == null) continue;
                result.Add(boss.name);
                foreach (WordData word in boss.words) result.Add(word.text);
            }
            return result;
        }

        // Unity のシーンやプレハブに書かれた文字（"ス…" のような書き方もある）を、ふつうの文字にする
        static string ParseYamlString(string value)
        {
            value = value.Trim();
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"') return Regex.Unescape(value.Substring(1, value.Length - 2));
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'') return value.Substring(1, value.Length - 2).Replace("''", "'");
            return value;
        }

        static string ReportName(string path) => $"{BossDatabase.Folder}/{Path.GetFileName(path)}";
    }
}
