using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordShot.Data;
using WordShot.Saves;

namespace WordShot.Stages
{
    /// <summary>
    /// どのステージをどの難易度でクリアしたか（＝どこまで解放したか）。セーブデータ（SaveSystem）に保存し、次に起動したときも続きから遊べる。
    /// </summary>
    /// <remarks>
    /// 解放のルール：
    /// ・ステージの「かんたん」… requiredClears のステージを全部「かんたん」以上でクリアすると解放（空なら最初から）
    /// ・「ふつう」… そのステージの「かんたん」をクリアすると解放
    /// ・「むずかしい」… そのステージの「ふつう」をクリアすると解放
    /// クリア状況が変わると、オートセーブする（「セーブ中...」が出る）。
    /// </remarks>
    public static class StageProgress
    {
        /// <summary>クリア状況が変わったとき。ステージセレクトの扉が見た目を変えるのに使う</summary>
        public static event Action Changed;

        // このプロジェクトはドメインのリロードが無効なので、再生のたびに読み直す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
        }

        // セーブデータの「クリアしたステージ」の一覧（「ステージID:難易度」の形。例：stage_a:easy）
        static List<string> Cleared => SaveSystem.Data.clearedStages;

        static string Key(StageData stage, Difficulty difficulty) => $"{stage.StageId}:{difficulty.ToKey()}";

        /// <summary>その難易度でクリアしたか</summary>
        public static bool IsCleared(StageData stage, Difficulty difficulty) => stage != null && Cleared.Contains(Key(stage, difficulty));

        /// <summary>どれかの難易度でクリアしたか</summary>
        public static bool IsCleared(StageData stage) => DifficultyExtensions.All.Any(d => IsCleared(stage, d));

        /// <summary>クリアした一番むずかしい難易度。まだクリアしていなければ null</summary>
        public static Difficulty? BestCleared(StageData stage) =>
            DifficultyExtensions.All.Reverse().Where(d => IsCleared(stage, d)).Select(d => (Difficulty?)d).FirstOrDefault();

        /// <summary>ステージそのもの（扉）が解放されているか ＝「かんたん」が遊べるか</summary>
        public static bool IsUnlocked(StageData stage)
        {
            if (stage == null || stage.ComingSoon) return false;   // 近日公開のステージは、条件を満たしても解放しない
            return MeetsRequirements(stage);
        }

        /// <summary>解放の条件（requiredClears を全部クリア）を満たしているか。近日公開のステージでも調べる</summary>
        public static bool MeetsRequirements(StageData stage)
        {
            if (stage == null) return false;
            return stage.RequiredClears.All(required => required == null || IsCleared(required, Difficulty.Easy));
        }

        /// <summary>その難易度が遊べるか（ひとつ下の難易度をクリアしていれば解放）</summary>
        public static bool IsUnlocked(StageData stage, Difficulty difficulty)
        {
            if (!IsUnlocked(stage)) return false;
            return difficulty == Difficulty.Easy || IsCleared(stage, difficulty - 1);
        }

        /// <summary>クリアを記録する。初めてのクリアならオートセーブする</summary>
        public static void MarkCleared(StageData stage, Difficulty difficulty)
        {
            if (stage == null) return;
            string key = Key(stage, difficulty);
            if (Cleared.Contains(key)) return;
            Cleared.Add(key);
            Save();
        }

        /// <summary>開発用：全部クリアしていない状態に戻す</summary>
        public static void ResetAll()
        {
            if (Cleared.Count == 0) return;
            Cleared.Clear();
            Save();
        }

        /// <summary>セーブデータを消したあとなどに呼ぶ。扉などの見た目を、今のセーブデータに合わせ直す</summary>
        public static void Refresh() => Changed?.Invoke();

        static void Save()
        {
            SaveSystem.RequestSave();
            Changed?.Invoke();
        }
    }
}
