using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        /// <summary>翻訳ファイルを読む。並び順もそのまま返す（保存するときに順番を崩さないため）</summary>
        public static List<LocalizationEntry> LoadTranslations(string language)
        {
            string path = TranslationPath(language);
            if (!File.Exists(path)) return new List<LocalizationEntry>();

            var file = JsonUtility.FromJson<LocalizationFile>(File.ReadAllText(path, Encoding.UTF8));
            return (file?.entries ?? Array.Empty<LocalizationEntry>()).ToList();
        }

        /// <summary>翻訳を保存する。翻訳が空になった行は消す</summary>
        public static void SaveTranslations(string language, IEnumerable<LocalizationEntry> entries)
        {
            var file = new LocalizationFile
            {
                language = language,
                entries = entries.Where(e => !string.IsNullOrEmpty(e.ja) && !string.IsNullOrEmpty(e.text)).ToArray(),
            };
            string path = TranslationPath(language);
            Directory.CreateDirectory(LocalizationFolder);
            File.WriteAllText(path, JsonUtility.ToJson(file, true) + "\n", Utf8);
            AssetDatabase.ImportAsset(path);
        }

        static string TranslationPath(string language) => $"{LocalizationFolder}/{language}.json";

        static string ReportName(string path) => $"{BossDatabase.Folder}/{Path.GetFileName(path)}";
    }
}
