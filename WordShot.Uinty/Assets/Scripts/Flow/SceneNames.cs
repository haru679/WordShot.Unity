namespace WordShot.Flow
{
    /// <summary>
    /// シーン名の定数。打ち間違いを防ぐため、シーン名の文字列はここにだけ書く。
    /// </summary>
    public static class SceneNames
    {
        public const string Title = "TitleScene";
        public const string Result = "ResultScene";

        /// <summary>
        /// フィールドを歩いてドアからステージを選ぶシーン。
        /// ビルド設定に入っていなければ、代わりにタイトルへ戻る。
        /// </summary>
        public const string StageSelect = "StageSelectScene";

        /// <summary>
        /// シーンを切り替える間に挟むロード画面。
        /// ビルド設定に入っていなければ、ロード画面なしで切り替える。
        /// </summary>
        public const string Loading = "LoadingScene";

        /// <summary>
        /// ステージとして扱わないシーン。
        /// </summary>
        public static readonly string[] NonStageScenes = { Title, Result, StageSelect, Loading, "SampleScene" };
    }
}
