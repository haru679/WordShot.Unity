using System.Linq;
using UnityEditor;
using UnityEngine;
using WordShot.Stages;

namespace WordShot.Data.EditorTools
{
    /// <summary>
    /// データを書いたあとに、間違いがないかまとめて調べるメニュー。
    /// メニュー「WordShot > データをチェック」から実行し、結果は Console に出る。
    /// </summary>
    /// <remarks>
    /// 調べること：ボスの JSON の書き間違い・お題の読みからローマ字が作れるか・
    /// 英語などの翻訳が足りているか・ステージの bossId に合うボスがあるか。
    /// </remarks>
    public static class DataCheckMenu
    {
        // 翻訳を調べる言語（日本語以外）。言語を足したらここにも足す
        static readonly string[] TranslatedLanguages = { GameLanguage.English };

        [MenuItem("WordShot/データをチェック")]
        public static void Run() => Check(logResult: true);

        /// <summary>チェックして結果を返す（テストからも使う）</summary>
        public static DataReport Check(bool logResult)
        {
            var report = new DataReport();
            var bosses = BossDatabase.LoadAll(report);

            // 翻訳：ボスの名前とお題の表記に、それぞれの言語の翻訳があるか
            foreach (string language in TranslatedLanguages)
            {
                var table = Localization.LoadTable(language, report);
                string fileName = $"{Localization.Folder}/{language}.json";
                foreach (BossData boss in bosses.Values)
                {
                    string[] needed = new[] { boss.name }.Concat(boss.words.Select(w => w.text)).Distinct().ToArray();
                    string[] missing = needed.Where(text => !table.ContainsKey(text)).ToArray();
                    if (missing.Length > 0)
                        report.Warning(fileName, $"{boss.id} の翻訳がありません：{string.Join("、", missing)}（ルビは日本語の読みのまま出ます）");
                }
            }

            // ステージ：bossId に合うボスがあるか
            foreach (string guid in AssetDatabase.FindAssets("t:StageData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (string.IsNullOrEmpty(stage.BossId))
                    report.Warning(path, "bossId が空です（まだボスが決まっていないステージ）");
                else if (!bosses.ContainsKey(stage.BossId))
                    report.Error(path, $"bossId「{stage.BossId}」のボスがありません");
            }

            if (logResult) Log(report, bosses.Count);
            return report;
        }

        static void Log(DataReport report, int bossCount)
        {
            string summary = $"[データをチェック] ボス {bossCount}体を読み込みました。エラー {report.Errors.Count}件、注意 {report.Warnings.Count}件";
            if (report.HasErrors) Debug.LogError($"{summary}\n{report}");
            else if (report.Warnings.Count > 0) Debug.LogWarning($"{summary}\n{report}");
            else Debug.Log($"{summary}\n問題はありません");
        }
    }
}
