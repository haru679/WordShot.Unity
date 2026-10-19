using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordShot.EditorTools;

namespace WordShot.Flow.EditorTools
{
    /// <summary>
    /// ロード画面のシーン（LoadingScene）を作り、ビルド設定に登録するエディタ用ツール。
    /// メニュー「WordShot > ロード画面を作成」から実行する。
    /// </summary>
    /// <remarks>
    /// すでにあるシーンは上書きしない。作ったあとの見た目は、LoadingScene を開いて自由に直してよい。
    /// </remarks>
    public static class LoadingSceneBuilder
    {
        const float GaugeWidth = 900f;
        const float GaugeHeight = 36f;

        [MenuItem("WordShot/ロード画面を作成")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(GameLoopSceneBuilder.LoadingPath))
                Debug.Log($"[LoadingSceneBuilder] {GameLoopSceneBuilder.LoadingPath} はすでにあるので作りません");
            else
                CreateLoadingScene();

            GameLoopSceneBuilder.RegisterBuildScenes();
            Debug.Log("[LoadingSceneBuilder] 完了。シーンを切り替えるとロード画面が出ます");
        }

        static void CreateLoadingScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.AddComponent<AudioListener>();   // ロード中も BGM が聞こえるように

            Transform canvas = EditorUIFactory.CreateCanvas();

            // 「NOW LOADING...」はゲージの左端にそろえる（点が増えても文字が左右に動かないように）
            TMP_Text loadingText = EditorUIFactory.CreateText(canvas, "LoadingText", "NOW LOADING", 56,
                new Vector2(0f, 60f), new Vector2(GaugeWidth, 80f));
            loadingText.alignment = TextAlignmentOptions.Left;

            // ゲージ：枠（背景）の中で、中身の右端を進み具合に合わせて伸ばす
            Image frame = EditorUIFactory.CreateImage(canvas, "Gauge", new Color(0.2f, 0.2f, 0.25f), new Vector2(0f, -10f),
                new Vector2(GaugeWidth, GaugeHeight));
            Image fill = EditorUIFactory.CreateImage(frame.transform, "Fill", new Color(0.95f, 0.6f, 0.15f), Vector2.zero, Vector2.zero);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;

            // ％はゲージの右端にそろえる
            TMP_Text percentText = EditorUIFactory.CreateText(canvas, "PercentText", "0%", 40,
                new Vector2(0f, -75f), new Vector2(GaugeWidth, 60f));
            percentText.alignment = TextAlignmentOptions.Right;

            var screen = new GameObject(nameof(LoadingScreen)).AddComponent<LoadingScreen>();
            EditorUIFactory.SetReferences(screen,
                ("gaugeFill", fillRect),
                ("percentText", percentText),
                ("loadingText", loadingText));

            EditorSceneManager.SaveScene(scene, GameLoopSceneBuilder.LoadingPath);
        }
    }
}
