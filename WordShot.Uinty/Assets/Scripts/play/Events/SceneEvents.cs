using System.Collections.Generic;
using UnityEngine;

namespace WordShot.Events
{
    /// <summary>
    /// シーンに置くと、そのシーンにいる間だけ有効な「いつ・何をするか」を追加できる。
    /// シーンの中のオブジェクト（UI・Animator など）を動かしたい演出はここに書く。
    /// </summary>
    public class SceneEvents : MonoBehaviour
    {
        [Tooltip("このシーンだけで使うデータファイル（共通のものは Resources/Events に置けば不要）")]
        [SerializeField] List<StateEventData> dataLists = new List<StateEventData>();

        [Tooltip("このシーンの中で使う「いつ・何をするか」")]
        [SerializeField] List<EventSetup> setups = new List<EventSetup>();

        void OnEnable() => EventManager.Register(this);
        void OnDisable() => EventManager.Unregister(this);

        public IEnumerable<EventSetup> AllSetups()
        {
            foreach (StateEventData data in dataLists)
            {
                if (data == null) continue;
                foreach (EventSetup setup in data.setups) yield return setup;
            }
            foreach (EventSetup setup in setups) yield return setup;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            foreach (EventSetup setup in setups) setup?.RefreshAutoMemo();
        }
#endif
    }
}
