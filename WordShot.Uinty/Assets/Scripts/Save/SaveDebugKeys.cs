using UnityEngine;
using UnityEngine.InputSystem;
using WordShot.Stages;

namespace WordShot.Saves
{
    /// <summary>
    /// セーブデータの開発用キー。どの画面でも使える。ゲームの起動時に自動で作られる（シーンに置かなくてよい）。
    /// F11：セーブデータを消して、最初から遊べる状態にする（右下に「セーブデータを消しました」と出る）
    /// </summary>
    /// <remarks>
    /// エディタと開発用ビルドでだけ動く。製品版には入らない。
    /// </remarks>
    public class SaveDebugKeys : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateOnStartup()
        {
            var go = new GameObject("SaveDebugKeys");
            DontDestroyOnLoad(go);
            go.AddComponent<SaveDebugKeys>();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f11Key.wasPressedThisFrame) return;

            SaveSystem.DeleteAll();
            StageProgress.Refresh();   // ステージセレクトの扉の見た目を、消したあとの状態に合わせる
            Debug.Log($"[Save] セーブデータを消しました（{SaveSystem.FilePath}）");
        }
#endif
    }
}
