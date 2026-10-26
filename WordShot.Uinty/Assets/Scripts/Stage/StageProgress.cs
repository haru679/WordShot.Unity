using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordShot.Data;

namespace WordShot.Stages
{
    /// <summary>
    /// どのステージをどの難易度でクリアしたか（＝どこまで解放したか）。PlayerPrefs に保存し、次に起動したときも続きから遊べる。
    /// </summary>
    /// <remarks>
    /// 解放のルール：
    /// ・ステージの「かんたん」… requiredClears のステージを全部「かんたん」以上でクリアすると解放（空なら最初から）
    /// ・「ふつう」… そのステージの「かんたん」をクリアすると解放
    /// ・「むずかしい」… そのステージの「ふつう」をクリアすると解放
    /// </remarks>
    public static class StageProgress
    {
        const string SaveKey = "Stage.Cleared";   // 「ステージID:難易度」を「,」でつないで保存する（例：stage_a:easy,stage_a:normal）

        static HashSet<string> cleared;

        /// <summary>クリア状況が変わったとき。ステージセレクトの扉が見た目を変えるのに使う</summary>
        public static event Action Changed;

        // このプロジェクトはドメインのリロードが無効なので、再生のたびに読み直す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cleared = null;
            Changed = null;
        }

        static HashSet<string> Cleared
        {
            get
            {
                if (cleared != null) return cleared;

                string saved = PlayerPrefs.GetString(SaveKey, "");
                // 難易度を足す前に保存したもの（「stage_a」だけ）は、「かんたん」をクリアしたことにする
                cleared = new HashSet<string>(saved.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Contains(':') ? entry : $"{entry}:{Difficulty.Easy.ToKey()}"));
                return cleared;
            }
        }

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
            if (stage == null) return false;
            return stage.RequiredClears.All(required => required == null || IsCleared(required, Difficulty.Easy));
        }

        /// <summary>その難易度が遊べるか（ひとつ下の難易度をクリアしていれば解放）</summary>
        public static bool IsUnlocked(StageData stage, Difficulty difficulty)
        {
            if (!IsUnlocked(stage)) return false;
            return difficulty == Difficulty.Easy || IsCleared(stage, difficulty - 1);
        }

        public static void MarkCleared(StageData stage, Difficulty difficulty)
        {
            if (stage == null || !Cleared.Add(Key(stage, difficulty))) return;
            Save();
        }

        /// <summary>開発用：全部クリアしていない状態に戻す</summary>
        public static void ResetAll()
        {
            Cleared.Clear();
            Save();
        }

        static void Save()
        {
            PlayerPrefs.SetString(SaveKey, string.Join(",", Cleared));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
