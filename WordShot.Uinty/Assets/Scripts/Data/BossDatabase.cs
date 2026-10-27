using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordShot.Data
{
    /// <summary>
    /// ボスのデータを読み込んで、名前（id）で引けるようにする。
    /// </summary>
    /// <remarks>
    /// Assets/Data/Resources/Bosses/ にある JSON を、最初に使ったときに全部読む。
    /// ボスを増やすときは、このフォルダに JSON を1つ足すだけでよい（プログラムは変えない）。
    /// 書き間違いのあるファイルは読み込まず、Console に理由を出す。
    /// </remarks>
    public static class BossDatabase
    {
        public const string Folder = "Bosses";

        static Dictionary<string, BossData> bosses;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => bosses = null;

        public static IReadOnlyCollection<BossData> All
        {
            get
            {
                EnsureLoaded();
                return bosses.Values;
            }
        }

        /// <summary>id でボスを探す。見つからなければ null（Console に理由を出す）</summary>
        public static BossData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureLoaded();
            if (bosses.TryGetValue(id, out BossData boss)) return boss;

            Debug.LogWarning($"[BossDatabase] 「{id}」というボスのデータがありません（Resources/{Folder}/{id}.json）");
            return null;
        }

        static void EnsureLoaded()
        {
            if (bosses != null) return;

            var report = new DataReport();
            bosses = LoadAll(report);

            foreach (string error in report.Errors) Debug.LogError($"[BossDatabase] {error}");
            foreach (string warning in report.Warnings) Debug.LogWarning($"[BossDatabase] {warning}");
        }

        /// <summary>
        /// フォルダの JSON を全部読む。エラーのあるファイルは入れない。チェック結果は report に入る
        /// </summary>
        public static Dictionary<string, BossData> LoadAll(DataReport report)
        {
            var result = new Dictionary<string, BossData>();
            foreach (TextAsset file in Resources.LoadAll<TextAsset>(Folder))
            {
                string fileName = $"{Folder}/{file.name}.json";
                int errorsBefore = report.Errors.Count;

                BossData boss = Parse(file.text, fileName, report);
                if (boss == null || report.Errors.Count > errorsBefore) continue;

                if (result.ContainsKey(boss.id))
                {
                    report.Error(fileName, $"id「{boss.id}」が、ほかのファイルと同じです");
                    continue;
                }
                result.Add(boss.id, boss);
            }
            return result;
        }

        /// <summary>JSON の文字を BossData にして、中身をチェックする。読めなければ null</summary>
        public static BossData Parse(string json, string fileName, DataReport report)
        {
            BossData boss;
            try
            {
                boss = JsonUtility.FromJson<BossData>(json);
            }
            catch (Exception e)
            {
                report.Error(fileName, $"JSON の書き方が正しくありません（{e.Message}）");
                return null;
            }

            if (boss == null)
            {
                report.Error(fileName, "中身が空です");
                return null;
            }

            Validate(boss, fileName, report);
            return boss;
        }

        // ---- チェック ----

        static void Validate(BossData boss, string fileName, DataReport report)
        {
            if (boss.version < 2)
                report.Error(fileName, $"version が {boss.version} の古い形です。難易度ごとの値（difficulties）を書いて version を {BossData.CurrentVersion} にしてください");
            else if (boss.version != BossData.CurrentVersion)
                report.Warning(fileName, $"version が {boss.version} です（今の形は {BossData.CurrentVersion}）");

            if (string.IsNullOrEmpty(boss.id)) report.Error(fileName, "id が空です");
            else if (!boss.id.All(c => c < 128 && (char.IsLetterOrDigit(c) || c == '_')))
                report.Error(fileName, $"id「{boss.id}」は半角の英数字と _ だけで書いてください");
            else if (!fileName.EndsWith($"/{boss.id}.json"))
                report.Warning(fileName, $"id「{boss.id}」とファイル名が違います。そろえると探しやすくなります");

            if (string.IsNullOrEmpty(boss.name)) report.Error(fileName, "name（ボスの名前）が空です");

            ValidateDifficulties(boss, fileName, report);

            if (boss.attacks == null || boss.attacks.Length == 0) report.Warning(fileName, "attacks（攻撃）が1つもありません");
            else
            {
                for (int i = 0; i < boss.attacks.Length; i++)
                {
                    if (string.IsNullOrEmpty(boss.attacks[i]?.type))
                        report.Error(fileName, $"attacks の {i + 1}つ目：type（攻撃の種類）が空です");
                }
            }
        }

        // 難易度は easy / normal / hard を1つずつ。それぞれ HP・間隔・速さ・妨害・お題をチェックする
        static void ValidateDifficulties(BossData boss, string fileName, DataReport report)
        {
            var seen = new HashSet<Difficulty>();
            foreach (BossDifficultyData data in boss.difficulties ?? Array.Empty<BossDifficultyData>())
            {
                if (data == null) continue;
                if (!DifficultyExtensions.TryParse(data.level, out Difficulty difficulty))
                {
                    report.Error(fileName, $"difficulties の level「{data.level}」は、easy / normal / hard のどれかにしてください");
                    continue;
                }
                if (!seen.Add(difficulty))
                {
                    report.Error(fileName, $"難易度「{difficulty.ToKey()}」が2つあります");
                    continue;
                }

                string where = $"{difficulty.ToKey()}";
                if (data.hp <= 0) report.Error(fileName, $"{where}：hp が {data.hp} です。1 以上にしてください");
                if (data.attackInterval <= 0f) report.Error(fileName, $"{where}：attackInterval が {data.attackInterval} です。0 より大きくしてください");
                if (data.attackSpeed <= 0f) report.Error(fileName, $"{where}：attackSpeed が {data.attackSpeed} です。0 より大きくしてください");
                if (!DifficultyExtensions.TryParse(data.interference, out Interference _))
                    report.Error(fileName, $"{where}：interference「{data.interference}」は、none / normal / strong のどれかにしてください");

                if (data.words == null || data.words.Length == 0) report.Error(fileName, $"{where}：words（お題）が1つもありません");
                else
                {
                    for (int i = 0; i < data.words.Length; i++) ValidateWord(data.words[i], $"{where}：words の {i + 1}つ目", fileName, report);
                }
            }

            foreach (Difficulty difficulty in DifficultyExtensions.All)
            {
                if (!seen.Contains(difficulty)) report.Error(fileName, $"難易度「{difficulty.ToKey()}」の値（difficulties）がありません");
            }
        }

        static void ValidateWord(WordData word, string where, string fileName, DataReport report)
        {
            if (word == null)
            {
                report.Error(fileName, $"{where}が空です");
                return;
            }
            if (string.IsNullOrEmpty(word.text)) report.Error(fileName, $"{where}：text（表記）が空です");
            if (string.IsNullOrEmpty(word.reading))
            {
                report.Error(fileName, $"{where}：reading（読み）が空です");
                return;
            }

            // 読みはひらがなと「ー」だけ（カタカナや漢字があると、ローマ字が作れない）
            string notHiragana = new string(word.reading.Where(c => !IsHiragana(c)).Distinct().ToArray());
            if (notHiragana.Length > 0)
            {
                report.Error(fileName, $"{where}（{word.text}）：reading「{word.reading}」に、ひらがな以外の文字「{notHiragana}」があります");
                return;
            }

            // タイピングの仕組み（RomajiEngine）で、実際に打ち方が作れるかを確かめる
            List<string> patterns = RomajiEngine.GenerateAllPatterns(RomajiEngine.Parse(word.reading));
            if (patterns.Count == 0 || patterns.Any(p => p.Any(c => !(c >= 'a' && c <= 'z') && c != '-')))
            {
                report.Error(fileName, $"{where}（{word.text}）：reading「{word.reading}」からローマ字の打ち方が作れません");
            }
        }

        static bool IsHiragana(char c) => (c >= 'ぁ' && c <= 'ゖ') || c == 'ー';
    }
}
