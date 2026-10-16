using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordShot.EditorTools;
using WordShot.Sounds.EditorTools;

namespace WordShot.Flow.EditorTools
{
    /// <summary>
    /// ポーズ画面とゲームオーバー表示のプレハブを作るエディタ用ツール。
    /// </summary>
    /// <remarks>
    /// 「WordShot > ポーズ画面とゲームオーバー表示を作成」：プレハブを作る（シーンは触らない）
    /// 「WordShot > 開いているシーンにポーズ画面とゲームオーバー表示を置く」：今開いているシーンに置く
    /// プレハブがすでにあれば作り直さない。作ったあとの見た目は、プレハブを開いて自由に直してよい。
    /// </remarks>
    public static class StageUIBuilder
    {
        const string PrefabFolder = "Assets/Prefabs/UI";
        const string PauseMenuPath = PrefabFolder + "/PauseMenu.prefab";
        const string StageOverlayPath = PrefabFolder + "/StageOverlay.prefab";

        // 重なり順。数字が大きいほど手前（暗転は GameFlow が一番手前に描く）
        const int StageOverlayOrder = 50;
        const int PauseMenuOrder = 100;

        [MenuItem("WordShot/ポーズ画面とゲームオーバー表示を作成")]
        public static void Build()
        {
            EditorUIFactory.EnsureFolder(PrefabFolder);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(StageOverlayPath) == null) CreateStageOverlay();
            else Debug.Log($"[StageUIBuilder] {StageOverlayPath} はすでにあるので作りません");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PauseMenuPath) == null) CreatePauseMenu();
            else Debug.Log($"[StageUIBuilder] {PauseMenuPath} はすでにあるので作りません");

            Debug.Log("[StageUIBuilder] 完了。プレハブをシーンに置くと使えます");
        }

        [MenuItem("WordShot/開いているシーンにポーズ画面とゲームオーバー表示を置く")]
        public static void PlaceInActiveScene()
        {
            Build();
            Scene scene = SceneManager.GetActiveScene();
            PlaceIfMissing<StageOverlay>(StageOverlayPath, scene);
            PlaceIfMissing<PauseMenu>(PauseMenuPath, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[StageUIBuilder] {scene.name} に置きました。シーンを保存してください");
        }

        static void PlaceIfMissing<T>(string prefabPath, Scene scene) where T : Component
        {
            bool exists = scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<T>(true) != null);
            if (exists) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
        }

        // ---- ゲームオーバー・クリアの表示 ----

        static void CreateStageOverlay()
        {
            GameObject root = CreateRootCanvas("StageOverlay", StageOverlayOrder);

            // ゲームオーバー：ゲーム画面の上に、少し暗くして大きく出す
            Image gameOver = EditorUIFactory.CreateImage(root.transform, "GameOver", new Color(0f, 0f, 0f, 0.45f), Vector2.zero, Vector2.zero);
            EditorUIFactory.Stretch(gameOver.rectTransform);
            gameOver.raycastTarget = false;
            TMP_Text gameOverText = EditorUIFactory.CreateText(gameOver.transform, "GameOverText", "GAME OVER", 160, new Vector2(0, 60), new Vector2(1600, 220));
            gameOverText.color = new Color(0.9f, 0.1f, 0.1f);
            gameOverText.fontStyle = FontStyles.Bold;
            TMP_Text hint = EditorUIFactory.CreateText(gameOver.transform, "RetryHint", "Press Shift Key", 56, new Vector2(0, -110), new Vector2(1200, 90));

            // クリア
            RectTransform clear = EditorUIFactory.CreateRect(root.transform, "Clear", Vector2.zero, Vector2.zero);
            EditorUIFactory.Stretch(clear);
            TMP_Text clearText = EditorUIFactory.CreateText(clear, "ClearText", "STAGE CLEAR!", 140, new Vector2(0, 40), new Vector2(1600, 200));
            clearText.color = new Color(1f, 0.85f, 0.2f);
            clearText.fontStyle = FontStyles.Bold;

            var overlay = root.AddComponent<StageOverlay>();
            EditorUIFactory.SetReferences(overlay,
                ("gameOverRoot", gameOver.gameObject),
                ("retryHint", hint.gameObject),
                ("clearRoot", clear.gameObject));

            gameOver.gameObject.SetActive(false);
            clear.gameObject.SetActive(false);
            SavePrefab(root, StageOverlayPath);
        }

        // ---- ポーズ画面 ----

        static void CreatePauseMenu()
        {
            GameObject root = CreateRootCanvas("PauseMenu", PauseMenuOrder);

            Image menu = EditorUIFactory.CreateImage(root.transform, "Menu", new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.zero);
            EditorUIFactory.Stretch(menu.rectTransform);
            EditorUIFactory.CreateText(menu.transform, "Header", "PAUSE", 120, new Vector2(0, 280), new Vector2(1000, 160));

            Transform list = EditorUIFactory.CreateVerticalList(menu.transform, "Buttons", new Vector2(0, 140));
            Button resume = EditorUIFactory.CreateButton(list, "ResumeButton", "Resume");
            Button retry = EditorUIFactory.CreateButton(list, "RetryButton", "Retry");
            Button sound = EditorUIFactory.CreateButton(list, "SoundButton", "Sound");
            Button back = EditorUIFactory.CreateButton(list, "BackButton", "Back");

            // 音量設定画面は、作ってあるプレハブをそのまま中に入れる（入れ子のプレハブ）
            GameObject soundPrefab = SoundSettingsBuilder.GetOrCreatePrefab();
            var soundPanel = (GameObject)PrefabUtility.InstantiatePrefab(soundPrefab, root.transform);
            soundPanel.transform.SetAsLastSibling();
            soundPanel.SetActive(false);

            var pause = root.AddComponent<PauseMenu>();
            EditorUIFactory.SetReferences(pause,
                ("menuRoot", menu.gameObject),
                ("resumeButton", resume),
                ("retryButton", retry),
                ("soundButton", sound),
                ("backButton", back),
                ("soundSettings", soundPanel.GetComponent<Sounds.SoundSettingsPanel>()));

            menu.gameObject.SetActive(false);
            SavePrefab(root, PauseMenuPath);
        }

        // ---- 共通 ----

        // 置くだけで表示できるよう、プレハブごとに自分の Canvas を持たせる
        static GameObject CreateRootCanvas(string name, int sortingOrder)
        {
            GameObject root = EditorUIFactory.CreateCanvas().gameObject;
            root.name = name;
            root.GetComponent<Canvas>().sortingOrder = sortingOrder;
            return root;
        }

        static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
