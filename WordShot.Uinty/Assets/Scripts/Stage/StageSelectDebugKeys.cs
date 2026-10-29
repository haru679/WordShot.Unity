using UnityEngine;
using UnityEngine.InputSystem;
using WordShot.Data;

namespace WordShot.Stages
{
    /// <summary>
    /// ステージセレクトの開発用キー。エディタと開発用ビルドでだけ動く。
    /// F5：クリア状況をリセット（最初の状態に戻す）　F6：置いてある扉のステージと難易度を全部解放する
    /// （セーブデータごと消すときは F11。どの画面でも使える：SaveDebugKeys）
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
                    // かんたん・ふつうをクリア扱いにすると、全部のステージと全部の難易度が遊べる
                    StageProgress.MarkCleared(door.Stage, Difficulty.Easy);
                    StageProgress.MarkCleared(door.Stage, Difficulty.Normal);
                }
                Debug.Log("[StageSelect] 全部のステージと難易度を解放しました");
            }
        }
#endif
    }
}
