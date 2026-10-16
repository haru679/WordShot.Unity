using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordShot.EditorTools;
using WordShot.Flow;
using WordShot.Flow.EditorTools;

namespace WordShot.Sounds.EditorTools
{
    /// <summary>
    /// 音量設定画面のプレハブを作り、タイトル画面に「Options」ボタンを付けるエディタ用ツール。
    /// メニュー「WordShot > 音量設定画面を作成」から実行する。
    /// </summary>
    /// <remarks>
    /// プレハブがすでにあれば作り直さない。作ったあとの見た目は、プレハブを開いて自由に直してよい。
    /// </remarks>
    public static class SoundSettingsBuilder
    {
        const string PrefabFolder = "Assets/Prefabs/UI";
        const string PrefabPath = PrefabFolder + "/SoundSettingsPanel.prefab";

        static readonly (VolumeChannel channel, string label)[] RowDefinitions =
        {
            (VolumeChannel.Master, "Master"),
            (VolumeChannel.BGM, "BGM"),
            (VolumeChannel.SE, "SE"),
            (VolumeChannel.Typing, "Typing"),
        };

        [MenuItem("WordShot/音量設定画面を作成")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SoundSettingsPanel>(PrefabPath) == null) CreatePrefab();
            else Debug.Log($"[SoundSettingsBuilder] {PrefabPath} はすでにあるので作りません");

            AddToTitleScene();
            Debug.Log("[SoundSettingsBuilder] 完了。TitleScene を再生して Options を押してください");
        }

        /// <summary>音量設定画面のプレハブ。なければ作る（ポーズ画面のプレハブを作るときにも使う）</summary>
        public static GameObject GetOrCreatePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) CreatePrefab();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        static void CreatePrefab()
        {
            // 画面全体を薄暗くする板（後ろのボタンをマウスで押せないようにする役目もある）
            var root = new GameObject("SoundSettingsPanel", typeof(RectTransform), typeof(Image));
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            EditorUIFactory.Stretch((RectTransform)root.transform);

            Image window = EditorUIFactory.CreateImage(root.transform, "Window", new Color(0.13f, 0.13f, 0.18f), Vector2.zero, new Vector2(1100, 720));
            EditorUIFactory.CreateText(window.transform, "Header", "Sound", 64, new Vector2(0, 285), new Vector2(600, 90));

            var panel = root.AddComponent<SoundSettingsPanel>();
            var serialized = new SerializedObject(panel);
            SerializedProperty rows = serialized.FindProperty("rows");
            rows.arraySize = RowDefinitions.Length;

            var navigationOrder = new List<Selectable>();
            for (int i = 0; i < RowDefinitions.Length; i++)
            {
                (VolumeChannel channel, string label) = RowDefinitions[i];
                Transform row = EditorUIFactory.CreateRect(window.transform, $"Row_{channel}", new Vector2(0, 160 - i * 110), new Vector2(1000, 80));

                TMP_Text labelText = EditorUIFactory.CreateText(row, "Label", label, 40, new Vector2(-390, 0), new Vector2(200, 80));
                labelText.alignment = TextAlignmentOptions.Left;
                Slider slider = EditorUIFactory.CreateSlider(row, "Slider", new Vector2(-60, 0), new Vector2(420, 30));
                TMP_Text valueText = EditorUIFactory.CreateText(row, "Value", "80%", 36, new Vector2(220, 0), new Vector2(120, 80));
                Toggle mute = EditorUIFactory.CreateToggle(row, "Mute", new Vector2(330, 0), 40);
                TMP_Text muteLabel = EditorUIFactory.CreateText(row, "MuteLabel", "Mute", 32, new Vector2(420, 0), new Vector2(120, 80));
                muteLabel.alignment = TextAlignmentOptions.Left;

                // ミュートのチェックから左キーで、同じ行のスライダーに戻れるようにする
                Navigation muteNavigation = mute.navigation;
                muteNavigation.selectOnLeft = slider;
                mute.navigation = muteNavigation;

                SerializedProperty element = rows.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("channel").enumValueIndex = (int)channel;
                element.FindPropertyRelative("slider").objectReferenceValue = slider;
                element.FindPropertyRelative("mute").objectReferenceValue = mute;
                element.FindPropertyRelative("valueText").objectReferenceValue = valueText;

                navigationOrder.Add(slider);
                navigationOrder.Add(mute);
            }

            Button close = EditorUIFactory.CreateButton(window.transform, "CloseButton", "Close", new Vector2(0, -280), new Vector2(300, 80));
            navigationOrder.Add(close);
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 上下キーは「スライダー → ミュート → 次のスライダー …… → Close」の順に動く。
            // スライダーの左右キーは音量の変更に使う
            EditorUIFactory.SetVerticalNavigation(navigationOrder);
            foreach (Selectable selectable in navigationOrder)
            {
                if (selectable is not Slider) continue;
                Navigation navigation = selectable.navigation;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                selectable.navigation = navigation;
            }

            EditorUIFactory.EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        static void AddToTitleScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameLoopSceneBuilder.TitlePath);
            // シーンを開くと読み込み済みのアセットが片付けられることがあるので、開いたあとに読み込む
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var title = Object.FindAnyObjectByType<TitleScreen>();
            if (title == null)
            {
                Debug.LogError("[SoundSettingsBuilder] TitleScene に TitleScreen がありません。先に「ゲームループのシーンを作成」を実行してください");
                return;
            }

            var serialized = new SerializedObject(title);
            if (serialized.FindProperty("soundSettings").objectReferenceValue != null)
            {
                Debug.Log("[SoundSettingsBuilder] タイトル画面にはすでに音量設定が付いています");
                return;
            }

            var list = (Transform)serialized.FindProperty("stageButtonParent").objectReferenceValue;
            Button options = EditorUIFactory.CreateButton(list, "OptionsButton", "Options");

            // 一番手前に描くため、Canvas の最後の子にする。最初は閉じておく
            Transform canvas = list.GetComponentInParent<Canvas>().transform;
            var panelObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            var panel = panelObject.GetComponent<SoundSettingsPanel>();
            panel.transform.SetAsLastSibling();
            panel.gameObject.SetActive(false);

            serialized.FindProperty("optionsButton").objectReferenceValue = options;
            serialized.FindProperty("soundSettings").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene);
        }
    }
}
