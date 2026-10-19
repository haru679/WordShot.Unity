using UnityEngine;
using UnityEngine.InputSystem;

namespace WordShot.Stages
{
    /// <summary>
    /// ステージセレクトの開発用キー。エディタと開発用ビルドでだけ動く。
    /// F5：クリア状況をリセット（最初の状態に戻す）　F6：置いてある扉のステージを全部クリア扱いにする
    /// </summary>
    public class StageSelectDebugKeys : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                StageProgress.ResetAll();
                Debug.Log("[StageSelect] クリア状況をリセットしました");
            }
            else if (keyboard.f6Key.wasPressedThisFrame)
            {
                foreach (StageDoor door in FindObjectsByType<StageDoor>(FindObjectsInactive.Exclude))
                {
                    StageProgress.MarkCleared(door.Stage);
                }
                Debug.Log("[StageSelect] 全部のステージをクリア扱いにしました");
            }
        }
#endif
    }
}
