using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace WordShot.Data.EditorTools
{
    /// <summary>
    /// ボスのデータを、JSON を直接書かずに編集する画面。
    /// メニュー「WordShot > ボスデータ編集」か、Bosses フォルダの JSON をダブルクリックで開く。
    /// </summary>
    /// <remarks>
    /// 保存すると、ボスの JSON と翻訳ファイル（Localization/en.json）に書き出す。
    /// ゲームはその JSON を読むので、プログラムを変えずにボスを増やせる。
    /// </remarks>
    public class BossDataEditorWindow : EditorWindow
    {
        static readonly string[] KnownAttackTypes = { "bullet", "rush", "drop" };
        const string TranslationLanguage = GameLanguage.English;

        const float TextWidth = 150f;
        const float ReadingWidth = 160f;
        const float EnglishWidth = 160f;
        const float RomajiWidth = 170f;

        [SerializeField] string selectedPath;
        [SerializeField] BossData boss;
        [SerializeField] int selectedWord;
        [SerializeField] bool previewEnglish;
        [SerializeField] string newBossId = "";

        string[] bossPaths = Array.Empty<string>();
        List<LocalizationEntry> translations = new List<LocalizationEntry>();
        DataReport report = new DataReport();
        string loadError;
        Vector2 listScroll;
        Vector2 mainScroll;

        GUIStyle rubyStyle;
        GUIStyle textStyle;
        GUIStyle romajiStyle;

        [MenuItem("WordShot/ボスデータ編集")]
        public static BossDataEditorWindow Open()
        {
            var window = GetWindow<BossDataEditorWindow>("ボスデータ編集");
            window.minSize = new Vector2(980f, 520f);
            return window;
        }

        // Bosses フォルダの JSON をダブルクリックしたら、この画面で開く
        [OnOpenAsset]
        static bool OnOpenAsset(EntityId entityId, int line)
        {
            string path = AssetDatabase.GetAssetPath(entityId);
            if (!path.StartsWith(BossDataFiles.BossFolder + "/") || !path.EndsWith(".json")) return false;

            Open().TrySelect(path);
            return true;
        }

        void OnEnable()
        {
            saveChangesMessage = "ボスデータに保存していない変更があります。保存しますか？";
            Refresh();
        }

        void Refresh()
        {
            bossPaths = BossDataFiles.ListBossPaths();
            translations = BossDataFiles.LoadTranslations(TranslationLanguage);
            if (boss != null) Validate();
        }

        // ---- 選ぶ・作る・保存 ----

        void TrySelect(string path)
        {
            if (path == selectedPath && boss != null) return;
            if (hasUnsavedChanges)
            {
                int choice = EditorUtility.DisplayDialogComplex("保存していない変更があります",
                    $"{Path.GetFileName(selectedPath)} の変更を保存しますか？", "保存する", "キャンセル", "保存しない");
                if (choice == 1) return;
                if (choice == 0) Save();
            }
            Load(path);
        }

        void Load(string path)
        {
            selectedPath = path;
            selectedWord = 0;
            report = new DataReport();
            boss = BossDataFiles.Load(path, report);
            loadError = boss == null ? string.Join("\n", report.Errors) : null;
            translations = BossDataFiles.LoadTranslations(TranslationLanguage);
            hasUnsavedChanges = false;
            GUI.FocusControl(null);
        }

        void CreateBoss()
        {
            string id = newBossId.Trim();
            string path = BossDataFiles.PathFor(id);
            BossDataFiles.Save(path, BossDataFiles.CreateTemplate(id));
            newBossId = "";
            bossPaths = BossDataFiles.ListBossPaths();
            TrySelect(path);
        }

        void Save()
        {
            if (boss == null) return;
            Validate();
            if (report.HasErrors && !EditorUtility.DisplayDialog("エラーがあります",
                    "エラーがあるままだと、このボスはゲームで読み込まれません。\nそれでも保存しますか？", "保存する", "やめる"))
                return;

            BossDataFiles.Save(selectedPath, boss);
            BossDataFiles.SaveTranslations(TranslationLanguage, translations);
            hasUnsavedChanges = false;
            Debug.Log($"[ボスデータ編集] {Path.GetFileName(selectedPath)} を保存しました");
        }

        public override void SaveChanges()
        {
            Save();
            base.SaveChanges();
        }

        void Validate() => report = BossDataFiles.Validate(selectedPath, boss);

        // ---- 翻訳（日本語の文字をキーにして引く）----

        string GetTranslation(string japanese) =>
            string.IsNullOrEmpty(japanese) ? "" : translations.FirstOrDefault(e => e.ja == japanese)?.text ?? "";

        void SetTranslation(string japanese, string translated)
        {
            if (string.IsNullOrEmpty(japanese)) return;
            LocalizationEntry entry = translations.FirstOrDefault(e => e.ja == japanese);
            if (entry != null) entry.text = translated;
            else translations.Add(new LocalizationEntry { ja = japanese, text = translated });
        }

        // ---- 画面 ----

        void OnGUI()
        {
            CreateStyles();
            HandleShortcuts();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawBossList();
                DrawEditor();
            }
        }

        void HandleShortcuts()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.control || e.command) && e.keyCode == KeyCode.S)
            {
                Save();
                e.Use();
            }
        }

        void DrawBossList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(220f)))
            {
                EditorGUILayout.LabelField("ボス一覧", EditorStyles.boldLabel);
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                foreach (string path in bossPaths)
                {
                    bool isSelected = path == selectedPath;
                    string label = Path.GetFileNameWithoutExtension(path) + (isSelected && hasUnsavedChanges ? " *" : "");
                    if (GUILayout.Toggle(isSelected, label, "Button") && !isSelected) TrySelect(path);
                }
                EditorGUILayout.EndScrollView();

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("新しいボス（id を半角英数字で）", EditorStyles.miniLabel);
                newBossId = EditorGUILayout.TextField(newBossId);
                using (new EditorGUI.DisabledScope(!BossDataFiles.IsNewValidId(newBossId.Trim())))
                {
                    if (GUILayout.Button("＋ 作成")) CreateBoss();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("一覧を読み直す")) Refresh();
                if (GUILayout.Button("全部のデータをチェック")) DataCheckMenu.Run();
            }
        }

        void DrawEditor()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (loadError != null)
                {
                    EditorGUILayout.HelpBox($"このファイルは JSON が壊れていて開けません。\n{loadError}", MessageType.Error);
                    if (GUILayout.Button("ファイルを直接開いて直す")) EditorUtility.OpenWithDefaultApp(selectedPath);
                    return;
                }
                if (boss == null)
                {
                    EditorGUILayout.HelpBox("左の一覧からボスを選ぶか、新しいボスを作ってください。", MessageType.Info);
                    return;
                }

                mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
                EditorGUI.BeginChangeCheck();

                DrawBasics();
                EditorGUILayout.Space(8f);
                DrawWords();
                EditorGUILayout.Space(8f);
                DrawAttacks();

                if (EditorGUI.EndChangeCheck())
                {
                    hasUnsavedChanges = true;
                    Validate();
                }

                EditorGUILayout.Space(8f);
                DrawPreview();
                EditorGUILayout.Space(8f);
                DrawReport();
                EditorGUILayout.EndScrollView();

                DrawSaveButtons();
            }
        }

        void DrawBasics()
        {
            EditorGUILayout.LabelField($"{boss.id}.json", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(new GUIContent("id", "ファイル名と同じ。変えるときはファイル名を変える"), boss.id);
            }
            boss.name = EditorGUILayout.TextField(new GUIContent("名前", "画面に出すボスの名前（日本語）"), boss.name);

            string englishName = GetTranslation(boss.name);
            string newEnglishName = EditorGUILayout.TextField(new GUIContent("名前（英語）", "英語版で出す名前"), englishName);
            if (newEnglishName != englishName) SetTranslation(boss.name, newEnglishName);

            boss.hp = EditorGUILayout.IntField("HP", boss.hp);
            boss.attackInterval = EditorGUILayout.FloatField(new GUIContent("攻撃の間隔（秒）"), boss.attackInterval);
            boss.sprite = EditorGUILayout.TextField(new GUIContent("画像の名前", "Resources/Bosses/ の画像の名前（まだなければ空でよい）"), boss.sprite);
            boss.bgm = EditorGUILayout.TextField(new GUIContent("BGM の名前", "Resources/Sounds/ の SoundCue の名前（まだなければ空でよい）"), boss.bgm);
        }

        void DrawWords()
        {
            EditorGUILayout.LabelField($"お題（{boss.words.Length}）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(32f);
                GUILayout.Label("表記", EditorStyles.miniBoldLabel, GUILayout.Width(TextWidth));
                GUILayout.Label("読み（ひらがな）", EditorStyles.miniBoldLabel, GUILayout.Width(ReadingWidth));
                GUILayout.Label("英語のルビ", EditorStyles.miniBoldLabel, GUILayout.Width(EnglishWidth));
                GUILayout.Label("打ち方の例", EditorStyles.miniBoldLabel, GUILayout.Width(RomajiWidth));
            }

            int removeIndex = -1;
            for (int i = 0; i < boss.words.Length; i++)
            {
                WordData word = boss.words[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 番号を押すと、そのお題をプレビューに出す
                    if (GUILayout.Button(i == selectedWord ? "▶" : (i + 1).ToString(), GUILayout.Width(28f))) selectedWord = i;

                    word.text = EditorGUILayout.TextField(word.text, GUILayout.Width(TextWidth));
                    word.reading = EditorGUILayout.TextField(word.reading, GUILayout.Width(ReadingWidth));

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(word.text)))
                    {
                        string english = GetTranslation(word.text);
                        string newEnglish = EditorGUILayout.TextField(english, GUILayout.Width(EnglishWidth));
                        if (newEnglish != english) SetTranslation(word.text, newEnglish);
                    }

                    (string example, bool ok) = RomajiExample(word.reading);
                    Color previous = GUI.contentColor;
                    if (!ok) GUI.contentColor = new Color(1f, 0.45f, 0.4f);
                    GUILayout.Label(example, GUILayout.Width(RomajiWidth));
                    GUI.contentColor = previous;

                    if (GUILayout.Button("×", GUILayout.Width(24f))) removeIndex = i;
                }
            }

            if (removeIndex >= 0)
            {
                boss.words = boss.words.Where((_, index) => index != removeIndex).ToArray();
                GUI.changed = true;
            }
            if (GUILayout.Button("＋ お題を追加", GUILayout.Width(140f)))
            {
                boss.words = boss.words.Append(new WordData { text = "", reading = "" }).ToArray();
                GUI.changed = true;
            }
        }

        void DrawAttacks()
        {
            EditorGUILayout.LabelField($"攻撃（{boss.attacks.Length}）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("種類", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
                GUILayout.Label("技名", EditorStyles.miniBoldLabel, GUILayout.Width(140f));
                GUILayout.Label("セリフ", EditorStyles.miniBoldLabel, GUILayout.Width(220f));
                GUILayout.Label("オノマトペ", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            }

            int removeIndex = -1;
            for (int i = 0; i < boss.attacks.Length; i++)
            {
                AttackData attack = boss.attacks[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 知っている種類は選ぶだけ。知らない種類が書いてあっても消さない
                    string[] types = KnownAttackTypes.Contains(attack.type) || string.IsNullOrEmpty(attack.type)
                        ? KnownAttackTypes
                        : KnownAttackTypes.Append(attack.type).ToArray();
                    int typeIndex = Mathf.Max(0, Array.IndexOf(types, attack.type));
                    attack.type = types[EditorGUILayout.Popup(typeIndex, types, GUILayout.Width(90f))];

                    attack.skillName = EditorGUILayout.TextField(attack.skillName, GUILayout.Width(140f));
                    attack.line = EditorGUILayout.TextField(attack.line, GUILayout.Width(220f));
                    attack.onomatopoeia = EditorGUILayout.TextField(attack.onomatopoeia, GUILayout.Width(120f));
                    if (GUILayout.Button("×", GUILayout.Width(24f))) removeIndex = i;
                }
            }

            if (removeIndex >= 0)
            {
                boss.attacks = boss.attacks.Where((_, index) => index != removeIndex).ToArray();
                GUI.changed = true;
            }
            if (GUILayout.Button("＋ 攻撃を追加", GUILayout.Width(140f)))
            {
                boss.attacks = boss.attacks.Append(new AttackData { type = KnownAttackTypes[0] }).ToArray();
                GUI.changed = true;
            }
        }

        // 画面の3段表示（ルビ／表記／ローマ字）を、日本語版と英語版で見比べる
        void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("プレビュー（番号を押したお題）", EditorStyles.boldLabel, GUILayout.Width(200f));
                previewEnglish = GUILayout.Toolbar(previewEnglish ? 1 : 0, new[] { "日本語版", "英語版" }, GUILayout.Width(180f)) == 1;
            }
            if (boss.words.Length == 0) return;

            WordData word = boss.words[Mathf.Clamp(selectedWord, 0, boss.words.Length - 1)];
            string english = GetTranslation(word.text);
            string ruby = previewEnglish && !string.IsNullOrEmpty(english) ? english : word.reading;
            (string romaji, _) = RomajiExample(word.reading);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(ruby, rubyStyle);
                GUILayout.Label(word.text, textStyle);
                GUILayout.Label(romaji, romajiStyle);
            }
            if (previewEnglish && string.IsNullOrEmpty(english))
                EditorGUILayout.HelpBox("英語のルビがないので、英語版でも読み（ひらがな）が出ます。", MessageType.None);
        }

        void DrawReport()
        {
            EditorGUILayout.LabelField("チェック結果", EditorStyles.boldLabel);
            foreach (string error in report.Errors) EditorGUILayout.HelpBox(StripFileName(error), MessageType.Error);
            foreach (string warning in report.Warnings) EditorGUILayout.HelpBox(StripFileName(warning), MessageType.Warning);

            string[] missing = new[] { boss.name }.Concat(boss.words.Select(w => w.text))
                .Where(text => !string.IsNullOrEmpty(text) && string.IsNullOrEmpty(GetTranslation(text)))
                .Distinct().ToArray();
            if (missing.Length > 0)
                EditorGUILayout.HelpBox($"英語がまだありません：{string.Join("、", missing)}", MessageType.Warning);

            if (!report.HasErrors && report.Warnings.Count == 0 && missing.Length == 0)
                EditorGUILayout.HelpBox("問題はありません。", MessageType.Info);
        }

        void DrawSaveButtons()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(hasUnsavedChanges ? "保存していない変更があります" : "保存済み", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!hasUnsavedChanges))
                {
                    if (GUILayout.Button("元に戻す", GUILayout.Width(100f))) Load(selectedPath);
                    if (GUILayout.Button("保存（Ctrl+S）", GUILayout.Width(140f))) Save();
                }
            }
        }

        // ---- 小物 ----

        // 読みから作れるローマ字の打ち方の1つ目。作れなければ理由を返す
        static (string text, bool ok) RomajiExample(string reading)
        {
            if (string.IsNullOrEmpty(reading)) return ("", true);
            if (reading.Any(c => !((c >= 'ぁ' && c <= 'ゖ') || c == 'ー'))) return ("ひらがなで書いてください", false);

            List<string> patterns = RomajiEngine.GenerateAllPatterns(RomajiEngine.Parse(reading));
            if (patterns.Count == 0 || patterns[0].Any(c => !(c >= 'a' && c <= 'z') && c != '-')) return ("打ち方が作れません", false);
            return (patterns[0], true);
        }

        static string StripFileName(string message)
        {
            int colon = message.IndexOf('：');
            return colon >= 0 ? message.Substring(colon + 1) : message;
        }

        void CreateStyles()
        {
            if (rubyStyle != null) return;
            rubyStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            textStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fixedHeight = 36f };
            romajiStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
        }
    }
}
