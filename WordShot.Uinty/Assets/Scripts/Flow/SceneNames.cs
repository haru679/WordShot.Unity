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
        /// フィールドを歩いてドアからステージを選ぶシーン（これから作る）。
        /// ビルド設定に入るまでは、代わりにタイトルへ戻る。
        /// </summary>
        public const string StageSelect = "StageSelectScene";

        /// <summary>
        /// ステージとして扱わないシーン。
        /// ビルド設定に入っていても、タイトル画面のステージ一覧には出さない。
        /// </summary>
        public static readonly string[] NonStageScenes = { Title, Result, StageSelect, "SampleScene" };
    }
}
