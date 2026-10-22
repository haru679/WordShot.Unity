namespace WordShot.Events
{
    // ==========================================================================================
    // 最初から用意しているフラグの種類。
    // ゲームとシーンは自動で設定される。それ以外は、担当の人が必要な場所で GameFlags.Set(...) を呼ぶ。
    //
    // 【フラグを足すとき】
    // ・値を足す … 下の enum に名前を足すだけ（順番はどこでもよい。保存は名前なので、ずれない）
    // ・種類を足す … どのファイルでもよいので、[FlagGroup("名前")] を付けた enum を書く
    //   ※ 名前を変えると、インスペクターで選んだものが「見つからない」になるので、変えるときは声をかける
    // ==========================================================================================

    /// <summary>ゲーム全体の状態（GameFlow から自動で入る）</summary>
    [FlagGroup("ゲーム", Order = 0)]
    public enum GameState { Playing, Paused, GameOver, Cleared }

    /// <summary>今いるシーン（シーンを読み込んだときに自動で入る）</summary>
    [FlagGroup("シーン", Order = 1)]
    public enum SceneState { Title, Loading, StageSelect, Stage, Result, Other }

    /// <summary>プレイヤー（田場）</summary>
    [FlagGroup("プレイヤー", Order = 10)]
    public enum PlayerState { Idle, Move, Jump, Land, Attack, Damage, Dead }

    /// <summary>ボス（田場）。Windup は攻撃前の予備動作</summary>
    [FlagGroup("ボス", Order = 11)]
    public enum BossState { Entry, Idle, Windup, Attack, Damage, Dead }

    /// <summary>タイピング（松井）</summary>
    [FlagGroup("タイピング", Order = 20)]
    public enum TypingState { WordStart, KeyCorrect, KeyMiss, MissLimit, WordComplete }

    /// <summary>メニューなど UI の操作（松井）</summary>
    [FlagGroup("UI", Order = 30)]
    public enum UIState { Move, Decide, Cancel, OpenMenu, CloseMenu }
}
