using System.IO;
using UnityEditor;
using UnityEngine;

namespace WordShot.Saves.EditorTools
{
    /// <summary>
    /// 開発用：セーブデータ（save.json）を見る・消すメニュー
    /// </summary>
    public static class SaveDataMenu
    {
        [MenuItem("WordShot/セーブデータ/フォルダを開く")]
        static void OpenFolder()
        {
            string path = SaveSystem.FilePath;
            if (File.Exists(path)) EditorUtility.RevealInFinder(path);
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                EditorUtility.RevealInFinder(Path.GetDirectoryName(path));
            }
        }

        [MenuItem("WordShot/セーブデータ/消す（最初から遊ぶ）")]
        static void Delete()
        {
            if (!EditorUtility.DisplayDialog("セーブデータを消す",
                    $"セーブデータを消して、最初から遊べる状態にします。元には戻せません。\n\n{SaveSystem.FilePath}",
                    "消す", "やめる"))
                return;

            SaveSystem.DeleteAll();
            Debug.Log("[Save] セーブデータを消しました");
        }

        // 遊んでいる途中に消すと、画面の表示とずれるので、再生していないときだけ
        [MenuItem("WordShot/セーブデータ/消す（最初から遊ぶ）", true)]
        static bool CanDelete() => !EditorApplication.isPlaying;
    }
}
