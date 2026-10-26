using System;
using System.Collections.Generic;

namespace WordShot.Saves
{
    /// <summary>
    /// セーブデータの中身（ゲームの進み具合）。JSON にして save.json に書く。
    /// </summary>
    /// <remarks>
    /// 項目を足すときは、ここに変数を足すだけでよい（前のセーブデータもそのまま読める。足した項目は初期値になる）。
    /// 項目の名前や意味を変えるときは CurrentVersion を上げ、SaveSystem で古い形を直す。
    /// 音量や言語は「設定」なので、ここではなく PlayerPrefs に保存している。
    /// </remarks>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>最後にセーブした日時（確認用）</summary>
        public string savedAt;

        /// <summary>クリアしたステージと難易度（例：stage_a:easy）。StageProgress が使う</summary>
        public List<string> clearedStages = new List<string>();
    }
}
