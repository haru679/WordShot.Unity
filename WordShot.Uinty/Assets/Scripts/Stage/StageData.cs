using System;
using UnityEngine;
using WordShot.Data;

namespace WordShot.Stages
{
    /// <summary>
    /// ステージ1つぶんの情報（名前・読み込むシーン・解放の条件）。1ステージにつき1ファイル作る。
    /// </summary>
    /// <remarks>
    /// 作り方：Project ウィンドウで右クリック → Create → WordShot → Stage Data。
    /// 置き場所：Assets/Data/Stages
    /// </remarks>
    [CreateAssetMenu(menuName = "WordShot/Stage Data", fileName = "Stage_")]
    public class StageData : ScriptableObject
    {
        [Tooltip("クリア状況の保存に使う名前。あとから変えると、そのステージのクリア記録が消える")]
        [SerializeField] string stageId;

        [Tooltip("画面に出す名前")]
        [SerializeField] string displayName;

        [Tooltip("読み込むシーンの名前（ビルド設定に登録しておく）")]
        [SerializeField] string sceneName;

        [Tooltip("ここに入れたステージを全部クリアすると解放される。空なら最初から遊べる")]
        [SerializeField] StageData[] requiredClears = Array.Empty<StageData>();

        [Tooltip("このステージで戦うボスの id（Assets/Data/Resources/Bosses/（id）.json）")]
        [SerializeField] string bossId;

        [Tooltip("ON にすると、まだ遊べないステージになる。解放の条件を満たしても解放せず、扉に「近日公開！」と出す")]
        [SerializeField] bool comingSoon;

        public string StageId => stageId;
        public bool ComingSoon => comingSoon;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string SceneName => sceneName;
        public StageData[] RequiredClears => requiredClears;
        public string BossId => bossId;

        /// <summary>このステージで戦うボスのデータ。bossId が空か、見つからなければ null</summary>
        public BossData Boss => BossDatabase.Find(bossId);

#if UNITY_EDITOR
        // 新しく作ったとき、名前が空ならファイル名を入れておく
        void OnValidate()
        {
            if (string.IsNullOrEmpty(stageId)) stageId = name;
        }
#endif
    }
}
