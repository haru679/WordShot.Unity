using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordShot.Stages
{
    /// <summary>
    /// どのステージをクリアしたか（＝どこまで解放したか）。PlayerPrefs に保存し、次に起動したときも続きから遊べる。
    /// </summary>
    public static class StageProgress
    {
        const string SaveKey = "Stage.Cleared";   // クリアしたステージの ID を「,」でつないで保存する

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
                cleared = new HashSet<string>(saved.Split(',', StringSplitOptions.RemoveEmptyEntries));
                return cleared;
            }
        }

        public static bool IsCleared(StageData stage) => stage != null && Cleared.Contains(stage.StageId);

        /// <summary>解放の条件（requiredClears）を全部クリアしていれば true</summary>
        public static bool IsUnlocked(StageData stage)
        {
            if (stage == null) return false;
            return stage.RequiredClears.All(required => required == null || IsCleared(required));
        }

        public static void MarkCleared(StageData stage)
        {
            if (stage == null || !Cleared.Add(stage.StageId)) return;
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
