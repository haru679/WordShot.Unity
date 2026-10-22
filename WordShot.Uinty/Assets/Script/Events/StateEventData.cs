using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WordShot.Events
{
    /// <summary>「いつ」「何をするか」の組み合わせ1つぶん</summary>
    [Serializable]
    public class EventSetup
    {
        [Tooltip("一覧に出る名前。空にしておくと「いつ → 何をするか」が自動で入る")]
        public string memo;

        [Tooltip("いつ（このフラグが設定されたとき）")]
        public FlagRef when;

        [Tooltip("さらに、このときだけ（空でよい。例：シーン/Stage）")]
        public List<FlagRef> onlyWhile = new List<FlagRef>();

        [Tooltip("何をするか（上から順に実行する）")]
        [SerializeReference] public List<GameEventAction> actions = new List<GameEventAction>();

        [Tooltip("再生中にチェックを入れると、条件に関係なくすぐ実行する（確認用）")]
        public bool testPlay;

        public bool IsTriggeredBy(Type changedType)
        {
            if (!when.IsTriggeredBy(changedType) || !when.IsCurrent()) return false;
            if (onlyWhile != null)
            {
                foreach (FlagRef condition in onlyWhile)
                {
                    if (!condition.IsEmpty && !condition.IsCurrent()) return false;
                }
            }
            return true;
        }

        public void Execute()
        {
            if (actions == null) return;
            foreach (GameEventAction action in actions)
            {
                if (action == null) continue;
                try
                {
                    action.Execute();
                }
                catch (Exception e)
                {
                    // 1つの処理が失敗しても、残りの処理は続ける
                    Debug.LogException(e);
                }
            }
        }

#if UNITY_EDITOR
        const string AutoMark = "▶ ";

        /// <summary>メモが空か自動で入れたものなら、「いつ → 何をするか」で書き直す</summary>
        public void RefreshAutoMemo()
        {
            if (!string.IsNullOrEmpty(memo) && !memo.StartsWith(AutoMark)) return;

            var names = new List<string>();
            if (actions != null)
            {
                foreach (GameEventAction action in actions)
                {
                    if (action == null) continue;
                    string path = action.GetType().GetCustomAttribute<MenuNameAttribute>()?.Path ?? action.GetType().Name;
                    names.Add(path.Substring(path.LastIndexOf('/') + 1));
                }
            }
            string label = AutoMark + when.Label + (names.Count > 0 ? " → " + string.Join("・", names) : "");
            if (memo != label) memo = label;
        }
#endif
    }

    /// <summary>
    /// 「いつ・何をするか」の一覧（データファイル）。全シーン共通の演出や音をここにまとめる。
    /// </summary>
    /// <remarks>
    /// メニューの Tools → WordShot → 共通イベントを開く で、Resources/Events に自動で作られる。
    /// Resources/Events に置いたものは、どのシーンでも自動で読み込まれる。
    /// </remarks>
    [CreateAssetMenu(menuName = "WordShot/State Event Data", fileName = "NewStateEventData")]
    public class StateEventData : ScriptableObject
    {
        public List<EventSetup> setups = new List<EventSetup>();

#if UNITY_EDITOR
        void OnValidate()
        {
            foreach (EventSetup setup in setups) setup?.RefreshAutoMemo();
        }
#endif
    }
}
