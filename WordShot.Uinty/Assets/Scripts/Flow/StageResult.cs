namespace WordShot.Flow
{
    /// <summary>
    /// 1回のステージの結果。ステージ → リザルト画面へ渡す。
    /// </summary>
    public class StageResult
    {
        /// <summary>遊んだステージのシーン名（リトライに使う）</summary>
        public string StageScene { get; }

        /// <summary>クリアしたか（false ならゲームオーバー）</summary>
        public bool IsCleared { get; }

        /// <summary>ステージ開始からの経過時間（秒）</summary>
        public float PlayTime { get; }

        public StageResult(string stageScene, bool isCleared, float playTime)
        {
            StageScene = stageScene;
            IsCleared = isCleared;
            PlayTime = playTime;
        }
    }
}
