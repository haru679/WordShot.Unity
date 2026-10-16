using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordShot.EditorTools;

namespace WordShot.Flow.EditorTools
{
    /// <summary>
    /// タイトル・リザルトのシーンを作り、ビルド設定に登録するエディタ用ツール。
    /// メニュー「WordShot > ゲームループのシーンを作成」から実行する。
    /// </summary>
    /// <remarks>
    /// すでにあるシーンは上書きしない。作り直したいときは、シーンを消してから実行する。
    /// 作ったあとの見た目は、Unity上で自由に直してよい。
    /// </remarks>
    public static class GameLoopSceneBuilder
    {
        const string SceneFolder = "Assets/Scenes";
        public static readonly string TitlePath = $"{SceneFolder}/{SceneNames.Title}.unity";
        static readonly string ResultPath = $"{SceneFolder}/{SceneNames.Result}.unity";

        // 今あるステージ。新しいステージはビルド設定のシーン一覧に追加すればよい
        static readonly string[] StagePaths =
        {
            $"{SceneFolder}/GameScene.unity",
            $"{SceneFolder}/TypingScene.unity",
        };

        static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.12f);

        [MenuItem("WordShot/ゲームループのシーンを作成")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                // TextMeshPro の初期データ（フォントなど）がないと文字が出ない。
                // 読み込みはこの関数が終わったあとに行われるので、いったん止めてもう一度実行してもらう
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.LogWarning("[GameLoopSceneBuilder] TextMeshPro の初期データを読み込みます。終わったら、もう一度メニューから実行してください");
                return;
            }

            if (File.Exists(TitlePath)) Debug.Log($"[GameLoopSceneBuilder] {TitlePath} はすでにあるので作りません");
            else CreateTitleScene();

            if (File.Exists(ResultPath)) Debug.Log($"[GameLoopSceneBuilder] {ResultPath} はすでにあるので作りません");
            else CreateResultScene();

            RegisterBuildScenes();
            EditorSceneManager.OpenScene(TitlePath);
            Debug.Log("[GameLoopSceneBuilder] 完了。TitleScene を開いて再生してください");
        }

        // ---- シーン ----

        static void CreateTitleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            Transform canvas = EditorUIFactory.CreateCanvas();

            EditorUIFactory.CreateText(canvas, "TitleText", "WordShot", 120, new Vector2(0, 280), new Vector2(1200, 160));
            Transform list = EditorUIFactory.CreateVerticalList(canvas, "StageButtons", new Vector2(0, 120));
            Button template = EditorUIFactory.CreateButton(list, "StageButtonTemplate", "Stage");
            Button quit = EditorUIFactory.CreateButton(list, "QuitButton", "Quit");

            var screen = new GameObject(nameof(TitleScreen)).AddComponent<TitleScreen>();
            EditorUIFactory.SetReferences(screen,
                ("stageButtonTemplate", template),
                ("stageButtonParent", list),
                ("quitButton", quit));

            EditorSceneManager.SaveScene(scene, TitlePath);
        }

        static void CreateResultScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            Transform canvas = EditorUIFactory.CreateCanvas();

            TMP_Text result = EditorUIFactory.CreateText(canvas, "ResultText", "CLEAR!", 120, new Vector2(0, 250), new Vector2(1200, 160));
            TMP_Text detail = EditorUIFactory.CreateText(canvas, "DetailText", "Stage   Time 0.0s", 48, new Vector2(0, 120), new Vector2(1200, 80));
            Transform list = EditorUIFactory.CreateVerticalList(canvas, "Buttons", new Vector2(0, 0));
            Button retry = EditorUIFactory.CreateButton(list, "RetryButton", "Retry");
            Button title = EditorUIFactory.CreateButton(list, "TitleButton", "Title");

            var screen = new GameObject(nameof(ResultScreen)).AddComponent<ResultScreen>();
            EditorUIFactory.SetReferences(screen,
                ("resultText", result),
                ("detailText", detail),
                ("retryButton", retry),
                ("titleButton", title));

            EditorSceneManager.SaveScene(scene, ResultPath);
        }

        // タイトルを先頭にする（ゲーム起動時は先頭のシーンから始まる）。今ある登録は消さない
        static void RegisterBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();

            void Add(string path)
            {
                if (File.Exists(path) && scenes.All(s => s.path != path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            Add(TitlePath);
            Add(ResultPath);
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (scenes.All(s => s.path != existing.path)) scenes.Add(existing);
            }
            foreach (string path in StagePaths) Add(path);

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---- 部品 ----

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;

            go.AddComponent<AudioListener>();   // 音を聞く耳。シーンに1つ必要
        }

        // このプロジェクトは新しい Input System だけを使う設定なので、UI の入力も Input System 用のものを使う
        static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
