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

        const float TextWidth = 150f;
        const float ReadingWidth = 160f;
        const float TranslationWidth = 160f;
        const float RomajiWidth = 170f;

        [SerializeField] string selectedPath;
        [SerializeField] BossData boss;
        [SerializeField] int selectedWord;
        [SerializeField] int selectedDifficulty;   // 0：かんたん　1：ふつう　2：むずかしい
        [SerializeField] bool previewTranslated;
        [SerializeField] string translationLanguage;   // 今、翻訳を入力している言語（例：en）
        [SerializeField] string newBossId = "";
        [SerializeField] string newLanguageCode = "";
        [SerializeField] string newLanguageName = "";
        [SerializeField] int tab;   // 0：ボス　1：画面の文字
        [SerializeField] List<string> uiTexts = new List<string>();
        [SerializeField] string newUiText = "";
        Vector2 uiScroll;

        string[] bossPaths = Array.Empty<string>();
        LanguageInfo[] languages = Array.Empty<LanguageInfo>();
        // 言語ごとの翻訳ファイル。編集した言語のぶんだけ読み、保存のときに全部書き出す
        readonly Dictionary<string, LocalizationFile> translationFiles = new Dictionary<string, LocalizationFile>();
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
            RefreshLanguages();
            RefreshUiTexts();
            translationFiles.Clear();
            if (boss != null) Validate();
        }

        // 画面の文字：シーン・プレハブ・スクリプトから集めたものと、翻訳ファイルにある（ボス以外の）ものを並べる
        void RefreshUiTexts()
        {
            HashSet<string> bossTexts = BossDataFiles.CollectBossTexts();
            var collected = BossDataFiles.CollectUiTexts();
            foreach (LanguageInfo language in languages)
            {
                foreach (LocalizationEntry entry in BossDataFiles.LoadTranslations(language.Code).entries)
                {
                    if (!bossTexts.Contains(entry.ja) && !collected.Contains(entry.ja)) collected.Add(entry.ja);
                }
            }
            foreach (string added in uiTexts.Where(text => !collected.Contains(text) && !bossTexts.Contains(text)).ToList())
                collected.Add(added);   // 手で足したものも残す
            uiTexts = collected;
        }

        void RefreshLanguages()
        {
            languages = BossDataFiles.ListTranslationLanguages();
            if (languages.All(l => l.Code != translationLanguage))
                translationLanguage = languages.Length > 0 ? languages[0].Code : null;
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
            translationFiles.Clear();
            hasUnsavedChanges = false;
            GUI.FocusControl(null);
        }

        void CreateLanguage()
        {
            string code = newLanguageCode.Trim();
            string displayName = string.IsNullOrWhiteSpace(newLanguageName) ? code : newLanguageName.Trim();
            BossDataFiles.CreateLanguage(code, displayName);
            newLanguageCode = "";
            newLanguageName = "";
            RefreshLanguages();
            translationLanguage = code;
            Debug.Log($"[ボスデータ編集] 言語「{displayName}」（{code}.json）を作りました");
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

        // 開いているボスと、編集した言語の翻訳ファイルを保存する
        void Save()
        {
            if (boss != null && !string.IsNullOrEmpty(selectedPath))
            {
                Validate();
                if (report.HasErrors && !EditorUtility.DisplayDialog("エラーがあります",
                        "エラーがあるままだと、このボスはゲームで読み込まれません。\nそれでも保存しますか？", "保存する", "やめる"))
                    return;
                BossDataFiles.Save(selectedPath, boss);
            }

            foreach (LocalizationFile file in translationFiles.Values) BossDataFiles.SaveTranslations(file);
            hasUnsavedChanges = false;
            Debug.Log("[ボスデータ編集] 保存しました");
        }

        void Revert()
        {
            if (boss != null && !string.IsNullOrEmpty(selectedPath)) Load(selectedPath);
            translationFiles.Clear();
            hasUnsavedChanges = false;
            GUI.FocusControl(null);
        }

        public override void SaveChanges()
        {
            Save();
            base.SaveChanges();
        }

        void Validate() => report = BossDataFiles.Validate(selectedPath, boss);

        // ---- 翻訳（日本語の文字をキーにして引く）----

        bool HasTranslationLanguage => !string.IsNullOrEmpty(translationLanguage);

        string TranslationLanguageName =>
            languages.FirstOrDefault(l => l.Code == translationLanguage)?.DisplayName ?? translationLanguage;

        // 今選んでいる言語の翻訳ファイル
        LocalizationFile CurrentTranslations => TranslationsFor(translationLanguage);

        // その言語の翻訳ファイル（初めて使うときに読む）
        LocalizationFile TranslationsFor(string language)
        {
            if (!translationFiles.TryGetValue(language, out LocalizationFile file))
            {
                file = BossDataFiles.LoadTranslations(language);
                translationFiles.Add(language, file);
            }
            return file;
        }

        string GetTranslation(string japanese)
        {
            if (!HasTranslationLanguage || string.IsNullOrEmpty(japanese)) return "";
            return CurrentTranslations.entries.FirstOrDefault(e => e.ja == japanese)?.text ?? "";
        }

        void SetTranslation(string japanese, string translated)
        {
            if (!HasTranslationLanguage || string.IsNullOrEmpty(japanese)) return;

            LocalizationFile file = CurrentTranslations;
            LocalizationEntry entry = file.entries.FirstOrDefault(e => e.ja == japanese);
            if (entry != null) entry.text = translated;
            else file.entries = file.entries.Append(new LocalizationEntry { ja = japanese, text = translated }).ToArray();
        }

        // ---- 画面 ----

        void OnGUI()
        {
            CreateStyles();
            HandleShortcuts();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawBossList();
                using (new EditorGUILayout.VerticalScope())
                {
                    tab = GUILayout.Toolbar(tab, new[] { "ボス", "画面の文字" }, GUILayout.Height(24f));
                    if (tab == 0) DrawEditor();
                    else DrawUiTexts();
                    DrawSaveButtons();
                }
            }
        }

        // 画面の文字（ボタンや見出し）の翻訳を、言語ごとに入力する
        void DrawUiTexts()
        {
            DrawLanguageSelector();
            if (!HasTranslationLanguage) return;

            LocalizationFile file = CurrentTranslations;
            EditorGUI.BeginChangeCheck();

            string fonts = string.Join(", ", file.fallbackFonts ?? Array.Empty<string>());
            string newFonts = EditorGUILayout.TextField(new GUIContent("表示に使うPCのフォント",
                "この言語の文字（ハングル・簡体字など）を出すフォントの名前。カンマで区切ると、前から順に探す。例：Malgun Gothic"), fonts);
            if (newFonts != fonts)
                file.fallbackFonts = newFonts.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0).ToArray();

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("日本語（画面に書いた文字）", EditorStyles.miniBoldLabel, GUILayout.Width(300f));
                GUILayout.Label(TranslationLanguageName, EditorStyles.miniBoldLabel);
            }

            int missing = 0;
            uiScroll = EditorGUILayout.BeginScrollView(uiScroll);
            foreach (string japanese in uiTexts)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(japanese, GUILayout.Width(300f), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    string translated = GetTranslation(japanese);
                    if (string.IsNullOrEmpty(translated)) missing++;
                    string newTranslated = EditorGUILayout.TextField(translated);
                    if (newTranslated != translated) SetTranslation(japanese, newTranslated);
                }
            }
            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck()) hasUnsavedChanges = true;

            if (missing > 0)
                EditorGUILayout.HelpBox($"{TranslationLanguageName}がまだない文字が {missing} 個あります（その文字は日本語のまま出ます）。", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("すべての文字に翻訳があります。", MessageType.Info);

            // 一覧にない文字を手で足す（スクリプトの中で組み立てる文字など）
            using (new EditorGUILayout.HorizontalScope())
            {
                newUiText = EditorGUILayout.TextField("文字を足す（日本語）", newUiText);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newUiText) || uiTexts.Contains(newUiText.Trim())))
                {
                    if (GUILayout.Button("＋ 追加", GUILayout.Width(80f)))
                    {
                        uiTexts.Add(newUiText.Trim());
                        newUiText = "";
                        GUI.FocusControl(null);
                    }
                }
            }
            EditorGUILayout.HelpBox("画面に LocalizedText を付けた文字と、スクリプトの Localization.TranslateOrSame(\"…\") の文字は、「一覧を読み直す」で自動で並びます。", MessageType.None);
        }

        /// <summary>
        /// 保存・読み込み・確認ダイアログ・タブの切り替えは、画面を描いている途中ではなく、描き終わったあとで行う。
        /// </summary>
        /// <remarks>
        /// 描いている途中でファイルを読み書きしたりダイアログを出したりすると、Unity の画面の組み立てが崩れて
        /// 「EndLayoutGroup: BeginLayoutGroup must be called first」のエラーが出る。
        /// また、入力中の欄があると、その値が切り替え後の別の欄に入ってしまうので、先に入力を終わらせる。
        /// </remarks>
        void Defer(Action action)
        {
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;   // 画面が閉じられていたら何もしない
                action();
                Repaint();
            };
        }

        void HandleShortcuts()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.control || e.command) && e.keyCode == KeyCode.S)
            {
                Defer(Save);
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
                    if (GUILayout.Toggle(isSelected, label, "Button") && !isSelected) Defer(() => TrySelect(path));
                }
                EditorGUILayout.EndScrollView();

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("新しいボス（id を半角英数字で）", EditorStyles.miniLabel);
                newBossId = EditorGUILayout.TextField(newBossId);
                using (new EditorGUI.DisabledScope(!BossDataFiles.IsNewValidId(newBossId.Trim())))
                {
                    if (GUILayout.Button("＋ 作成")) Defer(CreateBoss);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("新しい言語（例：ko / 한국어）", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    newLanguageCode = EditorGUILayout.TextField(newLanguageCode, GUILayout.Width(70f));
                    newLanguageName = EditorGUILayout.TextField(newLanguageName);
                }
                using (new EditorGUI.DisabledScope(!BossDataFiles.IsNewValidLanguage(newLanguageCode.Trim())))
                {
                    if (GUILayout.Button("＋ 言語を追加")) Defer(CreateLanguage);
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("一覧を読み直す")) Defer(Refresh);
                if (GUILayout.Button("全部のデータをチェック")) Defer(DataCheckMenu.Run);
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
                DrawAttacks();
                EditorGUILayout.Space(12f);
                DrawDifficulty();

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
            }
        }

        // 翻訳を入力する言語を選ぶ。言語を変えると、名前とルビの欄がその言語に切り替わる
        void DrawLanguageSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("翻訳する言語", GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                if (!HasTranslationLanguage)
                {
                    EditorGUILayout.LabelField("翻訳ファイルがありません（左下の「＋ 言語を追加」で作れます）", EditorStyles.miniLabel);
                    return;
                }

                int index = Mathf.Max(0, Array.FindIndex(languages, l => l.Code == translationLanguage));
                string[] labels = languages.Select(l => $"{l.DisplayName}（{l.Code}）").ToArray();
                // 言語の切り替えは「変更」ではないので、保存していない印を付けない。
                // 入力中の文字が次の言語の欄に入らないよう、描画が終わったあとで切り替える
                bool changed = GUI.changed;
                int newIndex = EditorGUILayout.Popup(index, labels);
                if (newIndex != index)
                {
                    string code = languages[newIndex].Code;
                    Defer(() => translationLanguage = code);
                }
                GUI.changed = changed;
            }
        }

        void DrawBasics()
        {
            EditorGUILayout.LabelField($"{boss.id}.json", EditorStyles.boldLabel);
            DrawLanguageSelector();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(new GUIContent("id", "ファイル名と同じ。変えるときはファイル名を変える"), boss.id);
            }
            boss.name = EditorGUILayout.TextField(new GUIContent("名前", "画面に出すボスの名前（日本語）"), boss.name);

            if (HasTranslationLanguage)
            {
                string translatedName = GetTranslation(boss.name);
                string newTranslatedName = EditorGUILayout.TextField(
                    new GUIContent($"名前（{TranslationLanguageName}）", $"{TranslationLanguageName}版で出す名前"), translatedName);
                if (newTranslatedName != translatedName) SetTranslation(boss.name, newTranslatedName);
            }

            boss.sprite = EditorGUILayout.TextField(new GUIContent("画像の名前", "Resources/Bosses/ の画像の名前（まだなければ空でよい）"), boss.sprite);
            boss.bgm = EditorGUILayout.TextField(new GUIContent("BGM の名前", "Resources/Sounds/ の SoundCue の名前（まだなければ空でよい）"), boss.bgm);
        }

        // ---- 難易度ごとの値 ----

        static readonly string[] DifficultyTabs = { "かんたん", "ふつう", "むずかしい" };
        static readonly string[] InterferenceLabels = { "なし（none）", "あり（normal）", "強い（strong）" };

        Difficulty SelectedDifficulty => DifficultyExtensions.All[Mathf.Clamp(selectedDifficulty, 0, 2)];

        // 選んでいる難易度の値（なければ作る）
        BossDifficultyData CurrentLevel => GetOrCreateLevel(SelectedDifficulty);

        BossDifficultyData GetOrCreateLevel(Difficulty difficulty)
        {
            BossDifficultyData level = boss.Get(difficulty);
            if (level != null) return level;

            level = new BossDifficultyData { level = difficulty.ToKey(), words = new[] { new WordData { text = "", reading = "" } } };
            boss.difficulties = boss.difficulties.Append(level).OrderBy(d => d.Difficulty).ToArray();
            hasUnsavedChanges = true;
            return level;
        }

        void DrawDifficulty()
        {
            EditorGUILayout.LabelField("難易度ごとの値", EditorStyles.boldLabel);

            // 難易度のタブの切り替えは「変更」ではないので、保存していない印を付けない。
            // 数値の欄を選んだまま切り替えると、その入力中の値が次のタブの同じ欄に入ってしまうので、
            // 入力をいったん終わらせてから、描画が終わったあとで切り替える
            bool changed = GUI.changed;
            int newSelected = GUILayout.Toolbar(selectedDifficulty, DifficultyTabs, GUILayout.Height(24f));
            if (newSelected != selectedDifficulty)
            {
                Defer(() =>
                {
                    selectedDifficulty = newSelected;
                    selectedWord = 0;
                });
            }
            GUI.changed = changed;

            BossDifficultyData level = CurrentLevel;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                level.hp = EditorGUILayout.IntField("HP", level.hp);
                level.attackInterval = EditorGUILayout.FloatField(new GUIContent("攻撃の間隔（秒）", "小さいほど攻撃が多い"), level.attackInterval);
                level.attackSpeed = EditorGUILayout.FloatField(new GUIContent("攻撃の速さ（倍率）", "1 = ふつう、1.3 = 3割速い"), level.attackSpeed);
                int interference = EditorGUILayout.Popup(new GUIContent("妨害", "タイピングへの妨害の強さ"), (int)level.Interference, InterferenceLabels);
                level.interference = ((Interference)interference).ToKey();

                if (selectedDifficulty > 0)
                {
                    Difficulty previous = SelectedDifficulty - 1;
                    if (GUILayout.Button($"{DifficultyTabs[selectedDifficulty - 1]}の値とお題をコピーする", GUILayout.Width(240f)))
                    {
                        string from = DifficultyTabs[selectedDifficulty - 1];
                        string to = DifficultyTabs[selectedDifficulty];
                        // 確認ダイアログは、描画が終わったあとで出す（描画の途中で出すと画面が崩れる）
                        Defer(() =>
                        {
                            if (!EditorUtility.DisplayDialog("コピーしますか？", $"{to}の値とお題を、{from}と同じにします。", "コピーする", "やめる")) return;
                            CopyLevel(GetOrCreateLevel(previous), level);
                            hasUnsavedChanges = true;
                            Validate();
                        });
                    }
                }
            }

            EditorGUILayout.Space(6f);
            DrawWords(level);
        }

        static void CopyLevel(BossDifficultyData from, BossDifficultyData to)
        {
            to.hp = from.hp;
            to.attackInterval = from.attackInterval;
            to.attackSpeed = from.attackSpeed;
            to.interference = from.interference;
            to.words = from.words.Select(w => new WordData { text = w.text, reading = w.reading }).ToArray();
        }

        void DrawWords(BossDifficultyData level)
        {
            EditorGUILayout.LabelField($"お題（{level.words.Length}）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(32f);
                GUILayout.Label("表記", EditorStyles.miniBoldLabel, GUILayout.Width(TextWidth));
                GUILayout.Label("読み（ひらがな）", EditorStyles.miniBoldLabel, GUILayout.Width(ReadingWidth));
                if (HasTranslationLanguage)
                    GUILayout.Label($"ルビ（{TranslationLanguageName}）", EditorStyles.miniBoldLabel, GUILayout.Width(TranslationWidth));
                GUILayout.Label("打ち方の例", EditorStyles.miniBoldLabel, GUILayout.Width(RomajiWidth));
            }

            int removeIndex = -1;
            for (int i = 0; i < level.words.Length; i++)
            {
                WordData word = level.words[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 番号を押すと、そのお題をプレビューに出す
                    if (GUILayout.Button(i == selectedWord ? "▶" : (i + 1).ToString(), GUILayout.Width(28f))) selectedWord = i;

                    word.text = EditorGUILayout.TextField(word.text, GUILayout.Width(TextWidth));
                    word.reading = EditorGUILayout.TextField(word.reading, GUILayout.Width(ReadingWidth));

                    if (HasTranslationLanguage)
                    {
                        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(word.text)))
                        {
                            string translated = GetTranslation(word.text);
                            string newTranslated = EditorGUILayout.TextField(translated, GUILayout.Width(TranslationWidth));
                            if (newTranslated != translated) SetTranslation(word.text, newTranslated);
                        }
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
                level.words = level.words.Where((_, index) => index != removeIndex).ToArray();
                GUI.changed = true;
            }
            if (GUILayout.Button("＋ お題を追加", GUILayout.Width(140f)))
            {
                level.words = level.words.Append(new WordData { text = "", reading = "" }).ToArray();
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

        // 画面の3段表示（ルビ／表記／ローマ字）を、日本語版と、選んでいる言語の版で見比べる
        void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("プレビュー（番号を押したお題）", EditorStyles.boldLabel, GUILayout.Width(200f));
                if (HasTranslationLanguage)
                {
                    string[] tabs = { "日本語版", $"{TranslationLanguageName}版" };
                    previewTranslated = GUILayout.Toolbar(previewTranslated ? 1 : 0, tabs, GUILayout.Width(220f)) == 1;
                }
            }
            WordData[] words = CurrentLevel.words;
            if (words.Length == 0) return;

            bool translatedView = previewTranslated && HasTranslationLanguage;
            WordData word = words[Mathf.Clamp(selectedWord, 0, words.Length - 1)];
            string translated = GetTranslation(word.text);
            string ruby = translatedView && !string.IsNullOrEmpty(translated) ? translated : word.reading;
            (string romaji, _) = RomajiExample(word.reading);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(ruby, rubyStyle);
                GUILayout.Label(word.text, textStyle);
                GUILayout.Label(romaji, romajiStyle);
            }
            if (translatedView && string.IsNullOrEmpty(translated))
                EditorGUILayout.HelpBox($"{TranslationLanguageName}のルビがないので、{TranslationLanguageName}版でも読み（ひらがな）が出ます。", MessageType.None);
        }

        void DrawReport()
        {
            EditorGUILayout.LabelField("チェック結果", EditorStyles.boldLabel);
            foreach (string error in report.Errors) EditorGUILayout.HelpBox(StripFileName(error), MessageType.Error);
            foreach (string warning in report.Warnings) EditorGUILayout.HelpBox(StripFileName(warning), MessageType.Warning);

            // 翻訳の書き忘れは、言語ごとに知らせる（選んでいない言語のぶんも）
            string[] needed = new[] { boss.name }.Concat(boss.AllWords.Select(w => w.text))
                .Where(text => !string.IsNullOrEmpty(text)).Distinct().ToArray();
            bool anyMissing = false;
            foreach (LanguageInfo language in languages)
            {
                LocalizationEntry[] entries = TranslationsFor(language.Code).entries;
                string[] missing = needed.Where(text => entries.All(e => e.ja != text || string.IsNullOrEmpty(e.text))).ToArray();
                if (missing.Length == 0) continue;

                anyMissing = true;
                EditorGUILayout.HelpBox($"{language.DisplayName}がまだありません：{string.Join("、", missing)}", MessageType.Warning);
            }

            if (!report.HasErrors && report.Warnings.Count == 0 && !anyMissing)
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
                    if (GUILayout.Button("元に戻す", GUILayout.Width(100f))) Defer(Revert);
                    if (GUILayout.Button("保存（Ctrl+S）", GUILayout.Width(140f))) Defer(Save);
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
