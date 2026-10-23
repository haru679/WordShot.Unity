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
using WordShot.Stages;

namespace WordShot.Data.EditorTools
{
    /// <summary>
    /// 今ある画面の文字を、言語の切り替えに対応させるエディタ用ツール。
    /// メニュー「WordShot > 画面を多言語対応にする」から実行する（何度実行しても同じ結果になる）。
    /// </summary>
    /// <remarks>
    /// ・画面の文字に LocalizedText を付け、日本語の文字をキーにする
    /// ・タイトルとポーズ画面に、言語を切り替えるボタン（LanguageButton）を置く
    /// ・日本語のフォント（TanukiMagic SDF）を、TextMeshPro 全体の予備のフォントにする
    /// </remarks>
    public static class LocalizationSetupBuilder
    {
        const string JapaneseFontPath = "Assets/Fonts/TanukiMagic SDF.asset";

        // プレハブ：中のオブジェクトの場所 → 日本語の文字
        static readonly Dictionary<string, (string path, string japanese)[]> PrefabTexts = new Dictionary<string, (string, string)[]>
        {
            ["Assets/Prefabs/UI/PauseMenu.prefab"] = new[]
            {
                ("Menu/Header", "ポーズ"),
                ("Menu/Buttons/ResumeButton/Label", "つづける"),
                ("Menu/Buttons/RetryButton/Label", "やり直す"),
                ("Menu/Buttons/SoundButton/Label", "サウンド"),
                ("Menu/Buttons/BackButton/Label", "もどる"),
            },
            ["Assets/Prefabs/UI/StageOverlay.prefab"] = new[]
            {
                ("GameOver/GameOverText", "ゲームオーバー"),
                ("GameOver/RetryHint", "Shiftキーでやり直し"),
                ("Clear/ClearText", "ステージクリア！"),
            },
            ["Assets/Prefabs/UI/SoundSettingsPanel.prefab"] = new[]
            {
                ("Window/Header", "サウンド"),
                ("Window/Row_Master/Label", "全体"),
                ("Window/Row_BGM/Label", "BGM"),
                ("Window/Row_SE/Label", "効果音"),
                ("Window/Row_Typing/Label", "打鍵音"),
                ("Window/Row_Master/MuteLabel", "ミュート"),
                ("Window/Row_BGM/MuteLabel", "ミュート"),
                ("Window/Row_SE/MuteLabel", "ミュート"),
                ("Window/Row_Typing/MuteLabel", "ミュート"),
                ("Window/CloseButton/Label", "とじる"),
            },
        };

        // シーン：一番上のオブジェクトからの場所 → 日本語の文字
        static readonly Dictionary<string, (string path, string japanese)[]> SceneTexts = new Dictionary<string, (string, string)[]>
        {
            ["Assets/Scenes/TitleScene.unity"] = new[]
            {
                ("Canvas/Buttons/StartButton/Label", "スタート"),
                ("Canvas/Buttons/OptionsButton/Label", "オプション"),
                ("Canvas/Buttons/QuitButton/Label", "おわる"),
            },
            ["Assets/Scenes/StageSelectScene.unity"] = new[]
            {
                ("Title", "ステージセレクト"),
                ("Help", "A / D：移動　W / Space：ジャンプ　Enter：決定　Esc：ポーズ"),
            },
            ["Assets/Scenes/ResultScene.unity"] = new[]
            {
                ("Canvas/Buttons/RetryButton/Label", "リトライ"),
                ("Canvas/Buttons/TitleButton/Label", "タイトルへ"),
            },
        };

        [MenuItem("WordShot/画面を多言語対応にする")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AddJapaneseFallbackFont();

            foreach (var prefab in PrefabTexts) EditPrefab(prefab.Key, prefab.Value);
            foreach (var scene in SceneTexts) EditScene(scene.Key, scene.Value);

            // ロード画面の文字は LoadingScreen が翻訳して「...」を付けるので、キーの日本語だけ入れる
            Scene loading = EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
            Find(loading, "Canvas/LoadingText").GetComponent<TMP_Text>().text = "ロード中";
            EditorSceneManager.SaveScene(loading);

            SetStageName("Assets/Data/Stages/Stage_A.asset", "ステージA");
            SetStageName("Assets/Data/Stages/Stage_B.asset", "ステージB");

            // 画面を作り直すときに足された文字の画像を空にして、フォントのファイルを小さく保つ
            var japaneseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JapaneseFontPath);
            japaneseFont.ClearFontAssetData(true);
            EditorUtility.SetDirty(japaneseFont);
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(GameLoopSceneBuilder.TitlePath);
            Debug.Log("[画面を多言語対応にする] 完了。タイトルの「Language」ボタンで言語を切り替えられます");
        }

        // 日本語のフォントを、TextMeshPro 全体の予備のフォントにする（英語用のフォントにない文字は、これで出す）
        static void AddJapaneseFallbackFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JapaneseFontPath);
            var settings = new SerializedObject(Resources.Load<TMP_Settings>("TMP Settings"));
            SerializedProperty list = settings.FindProperty("m_fallbackFontAssets");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == font) return;
            }
            list.InsertArrayElementAtIndex(0);
            list.GetArrayElementAtIndex(0).objectReferenceValue = font;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static void EditPrefab(string prefabPath, (string path, string japanese)[] texts)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            foreach ((string path, string japanese) in texts) Localize(root.transform.Find(path), japanese, $"{prefabPath} の {path}");

            // ポーズ画面には、サウンドの下に言語のボタンを置く
            if (root.TryGetComponent(out PauseMenu pause))
            {
                Transform list = root.transform.Find("Menu/Buttons");
                Button language = AddLanguageButton(list, list.Find("SoundButton").GetSiblingIndex() + 1);
                EditorUIFactory.SetReferences(pause, ("languageButton", language));
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void EditScene(string scenePath, (string path, string japanese)[] texts)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath);
            foreach ((string path, string japanese) in texts) Localize(Find(scene, path), japanese, $"{scenePath} の {path}");

            // タイトルには、おわるの上に言語のボタンを置く
            if (scenePath == GameLoopSceneBuilder.TitlePath)
            {
                Transform list = Find(scene, "Canvas/Buttons");
                AddLanguageButton(list, list.Find("QuitButton").GetSiblingIndex());
            }
            EditorSceneManager.SaveScene(scene);
        }

        static void Localize(Transform target, string japanese, string where)
        {
            if (target == null)
            {
                Debug.LogWarning($"[画面を多言語対応にする] {where} が見つかりません");
                return;
            }
            if (!target.TryGetComponent(out LocalizedText localized)) localized = target.gameObject.AddComponent<LocalizedText>();
            var serialized = new SerializedObject(localized);
            serialized.FindProperty("japanese").stringValue = japanese;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            target.GetComponent<TMP_Text>().text = japanese;   // エディタで見たときも日本語で出す
        }

        static Button AddLanguageButton(Transform list, int siblingIndex)
        {
            Transform existing = list.Find("LanguageButton");
            if (existing != null) return existing.GetComponent<Button>();

            Button button = EditorUIFactory.CreateButton(list, "LanguageButton", "Language : 日本語");
            button.transform.SetSiblingIndex(siblingIndex);
            var languageButton = button.gameObject.AddComponent<LanguageButton>();
            EditorUIFactory.SetReferences(languageButton, ("label", button.GetComponentInChildren<TMP_Text>()));
            return button;
        }

        static void SetStageName(string path, string japanese)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            var serialized = new SerializedObject(stage);
            serialized.FindProperty("displayName").stringValue = japanese;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
        }

        // シーンの一番上のオブジェクトから、「Canvas/Buttons/StartButton」のような場所でたどる
        static Transform Find(Scene scene, string path)
        {
            string[] parts = path.Split(new[] { '/' }, 2);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                return parts.Length == 1 ? root.transform : root.transform.Find(parts[1]);
            }
            return null;
        }
    }
}
