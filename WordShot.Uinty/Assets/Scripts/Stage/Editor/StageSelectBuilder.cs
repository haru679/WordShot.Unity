using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordShot.EditorTools;
using WordShot.Flow;
using WordShot.Flow.EditorTools;

namespace WordShot.Stages.EditorTools
{
    /// <summary>
    /// ステージセレクトのシーンと、仮のステージデータ（A・B）を作るエディタ用ツール。
    /// メニュー「WordShot > ステージセレクトを作成」から実行する。
    /// </summary>
    /// <remarks>
    /// あわせて、タイトル画面のボタンを「Start」にし、ビルド設定にステージセレクトを登録する。
    /// すでにあるシーン・データは上書きしない。作ったあとの配置や見た目は、Unity上で自由に直してよい。
    /// </remarks>
    public static class StageSelectBuilder
    {
        const string StageDataFolder = "Assets/Data/Stages";
        const string SpriteFolder = "Assets/Sprites/Placeholder";
        const string SquareSpritePath = SpriteFolder + "/Square.png";
        const string PauseMenuPath = "Assets/Prefabs/UI/PauseMenu.prefab";
        const string StageAFile = "Stage_A";
        const string StageBFile = "Stage_B";

        const float GroundTop = -3.5f;
        const int TextSortingOrder = 10;

        [MenuItem("WordShot/ステージセレクトを作成")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Sprite square = GetOrCreateSquareSprite();

            // 仮のステージ：A は最初から、B は A をクリアすると解放
            StageData stageA = GetOrCreateStageData(StageAFile, "stage_a", "Stage A", "GameScene");
            GetOrCreateStageData(StageBFile, "stage_b", "Stage B", "TypingScene", stageA);

            if (File.Exists(GameLoopSceneBuilder.StageSelectPath))
                Debug.Log($"[StageSelectBuilder] {GameLoopSceneBuilder.StageSelectPath} はすでにあるので作りません");
            else
                CreateStageSelectScene(square);

            GameLoopSceneBuilder.RegisterBuildScenes();
            ChangeTitleToStart();
            EditorSceneManager.OpenScene(GameLoopSceneBuilder.StageSelectPath);
            Debug.Log("[StageSelectBuilder] 完了。TitleScene から再生して Start を押してください");
        }

        // ---- シーン ----

        static void CreateStageSelectScene(Sprite square)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 新しいシーンを開くと、作ったばかりのデータが片付けられることがあるので、開いたあとにファイルから読む
            var stageA = AssetDatabase.LoadAssetAtPath<StageData>(StageDataPath(StageAFile));
            var stageB = AssetDatabase.LoadAssetAtPath<StageData>(StageDataPath(StageBFile));

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.2f);
            cameraObject.AddComponent<AudioListener>();

            CreateWorldText(null, "Title", "STAGE SELECT", 9f, new Vector3(0f, 3.6f, 0f));
            CreateWorldText(null, "Help", "Arrow keys: Move / Jump    Enter: Select    Esc: Pause", 3f, new Vector3(0f, 2.6f, 0f))
                .color = new Color(1f, 1f, 1f, 0.6f);

            // 地面と、画面の外へ出ないための見えない壁
            CreateBlock("Ground", square, new Vector2(0f, GroundTop - 0.5f), new Vector2(20f, 1f), new Color(0.35f, 0.3f, 0.26f), 0);
            CreateWall("LeftWall", new Vector2(-9f, 0f));
            CreateWall("RightWall", new Vector2(9f, 0f));

            // 真ん中にプレイヤー、左右にステージの扉
            CreatePlayer(square);
            CreateDoor("Door_StageA", stageA, square, new Vector2(-5f, GroundTop));
            CreateDoor("Door_StageB", stageB, square, new Vector2(5f, GroundTop));

            // ステージセレクトでもポーズできるようにする
            var pauseMenu = AssetDatabase.LoadAssetAtPath<GameObject>(PauseMenuPath);
            if (pauseMenu == null)
            {
                StageUIBuilder.Build();
                pauseMenu = AssetDatabase.LoadAssetAtPath<GameObject>(PauseMenuPath);
            }
            PrefabUtility.InstantiatePrefab(pauseMenu, scene);

            new GameObject(nameof(StageSelectDebugKeys)).AddComponent<StageSelectDebugKeys>();

            EditorSceneManager.SaveScene(scene, GameLoopSceneBuilder.StageSelectPath);
        }

        static void CreatePlayer(Sprite square)
        {
            GameObject player = CreateBlock("Player", square, new Vector2(0f, GroundTop + 0.4f), new Vector2(0.8f, 0.8f), new Color(0.3f, 0.8f, 1f), 5);

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            player.AddComponent<StageSelectPlayer>();
        }

        // 扉：本体の見た目（Body）は子にして、拡大しても文字や当たり判定がつぶれないようにする
        static void CreateDoor(string name, StageData stage, Sprite square, Vector2 bottomCenter)
        {
            var size = new Vector2(1.5f, 2f);
            var door = new GameObject(name);
            door.transform.position = bottomCenter;

            GameObject body = CreateBlock("Body", square, Vector2.zero, size, Color.white, 1, door.transform, addCollider: false);
            body.transform.localPosition = new Vector3(0f, size.y / 2f, 0f);

            // プレイヤーが触れたかを調べる範囲（通り抜けられる）。少しだけ大きくして、触れやすくする
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, size.y / 2f);
            trigger.size = size + new Vector2(0.2f, 0f);

            TMP_Text label = CreateWorldText(door.transform, "Label", stage.DisplayName, 4f, new Vector3(0f, size.y + 0.7f, 0f));
            TMP_Text prompt = CreateWorldText(door.transform, "Prompt", "[Enter]", 3.5f, new Vector3(0f, size.y + 1.6f, 0f));
            prompt.color = new Color(1f, 0.85f, 0.2f);
            prompt.gameObject.SetActive(false);

            var stageDoor = door.AddComponent<StageDoor>();
            EditorUIFactory.SetReferences(stageDoor,
                ("stage", stage),
                ("body", body.GetComponent<SpriteRenderer>()),
                ("label", label),
                ("prompt", prompt.gameObject));
        }

        static GameObject CreateBlock(string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int sortingOrder,
            Transform parent = null, bool addCollider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (addCollider) go.AddComponent<BoxCollider2D>();
            return go;
        }

        static void CreateWall(string name, Vector2 position)
        {
            var wall = new GameObject(name);
            wall.transform.position = position;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
        }

        // ゲーム画面の中に置く文字（UI ではなく、ワールドに置く TextMeshPro）
        static TMP_Text CreateWorldText(Transform parent, string name, string text, float fontSize, Vector3 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(10f, 2f);
            label.sortingOrder = TextSortingOrder;
            return label;
        }

        // ---- 仮の画像とデータ ----

        // 白い四角の画像（1マス = 1ユニット）。あとで本物の画像に差し替える
        static Sprite GetOrCreateSquareSprite()
        {
            if (!File.Exists(SquareSpritePath))
            {
                EditorUIFactory.EnsureFolder(SpriteFolder);

                var texture = new Texture2D(32, 32);
                var pixels = new Color32[32 * 32];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                File.WriteAllBytes(SquareSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                AssetDatabase.ImportAsset(SquareSpritePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        }

        static string StageDataPath(string fileName) => $"{StageDataFolder}/{fileName}.asset";

        static StageData GetOrCreateStageData(string fileName, string stageId, string displayName, string sceneName,
            params StageData[] requiredClears)
        {
            string path = StageDataPath(fileName);
            var existing = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (existing != null) return existing;

            EditorUIFactory.EnsureFolder(StageDataFolder);
            var data = ScriptableObject.CreateInstance<StageData>();
            AssetDatabase.CreateAsset(data, path);

            var serialized = new SerializedObject(data);
            serialized.FindProperty("stageId").stringValue = stageId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("sceneName").stringValue = sceneName;
            SerializedProperty required = serialized.FindProperty("requiredClears");
            required.arraySize = requiredClears.Length;
            for (int i = 0; i < requiredClears.Length; i++)
            {
                required.GetArrayElementAtIndex(i).objectReferenceValue = requiredClears[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return data;
        }

        // ---- タイトル画面 ----

        // 前の「ステージ一覧」のボタンを、ステージセレクトへ進む「Start」ボタンに作り替える
        static void ChangeTitleToStart()
        {
            Scene scene = EditorSceneManager.OpenScene(GameLoopSceneBuilder.TitlePath);
            var title = Object.FindAnyObjectByType<TitleScreen>();
            var serialized = new SerializedObject(title);
            if (serialized.FindProperty("startButton").objectReferenceValue != null) return;

            var quit = (Button)serialized.FindProperty("quitButton").objectReferenceValue;
            Transform list = quit.transform.parent;
            list.name = "Buttons";

            Transform template = list.Find("StageButtonTemplate");
            Button start = template != null
                ? template.GetComponent<Button>()
                : EditorUIFactory.CreateButton(list, "StartButton", "Start");
            start.name = "StartButton";
            start.gameObject.SetActive(true);
            start.GetComponentInChildren<TMP_Text>().text = "Start";
            start.transform.SetAsFirstSibling();
            quit.transform.SetAsLastSibling();   // Start → Options → Quit の順

            serialized.FindProperty("startButton").objectReferenceValue = start;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
