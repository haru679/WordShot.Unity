using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordShot.Data;

namespace WordShot.Saves
{
    /// <summary>
    /// オートセーブ中に、画面の右下に「セーブ中...」を出す。ゲームの起動時に自動で作られる（シーンに置かなくてよい）。
    /// </summary>
    /// <remarks>
    /// セーブがすぐ終わっても、セーブしたことが伝わるように、最低 MinShowTime 秒は出しておく。
    /// セーブに失敗したときは「セーブに失敗しました」を出す。
    /// 開発用のキー（F11）でセーブデータを消したときは「セーブデータを消しました」を出す。
    /// 時間が止まっている（ポーズ・ゲームオーバー）ときも動くよう、unscaledTime で動かす。
    /// </remarks>
    public class SaveIndicator : MonoBehaviour
    {
        const float MinShowTime = 1f;       // 出し切ってから、最低これだけは出しておく（秒）
        const float FailedShowTime = 3f;    // 失敗したときに出しておく時間（秒）
        const float DeletedShowTime = 2f;   // 開発用：セーブデータを消したときに出しておく時間（秒）
        const float FadeTime = 0.15f;
        const float DotInterval = 0.3f;
        const float SpinSpeed = 270f;       // 回るマークの速さ（度/秒）
        const int SortingOrder = 32000;     // 暗転（GameFlow の FadeCanvas）よりも手前に出す
        const float MinPanelWidth = 260f;
        const float SpinnerSpace = 72f;     // 文字の左に空ける幅（回るマークの場所）
        const float Padding = 24f;

        static readonly Color NormalColor = Color.white;
        static readonly Color FailedColor = new Color(1f, 0.45f, 0.45f);

        CanvasGroup group;
        TMP_Text label;
        RectTransform panel;
        RectTransform spinner;

        bool showing;
        bool showingMessage;    // 「セーブ中...」ではなく、失敗などの知らせを出しているか
        float messageTime;      // 知らせを出しておく時間（秒）
        float shownAt;          // 出し始めた時刻
        float fullyShownAt;     // 出し切った（濃さが最大になった）時刻。ここから MinShowTime 秒は出しておく
        string baseText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateOnStartup()
        {
            var go = new GameObject("SaveIndicator");
            DontDestroyOnLoad(go);
            go.AddComponent<SaveIndicator>();
        }

        void Awake()
        {
            BuildUI();
            group.alpha = 0f;
        }

        void OnEnable()
        {
            SaveSystem.SaveStarted += OnSaveStarted;
            SaveSystem.SaveFinished += OnSaveFinished;
            SaveSystem.Deleted += OnDeleted;
        }

        void OnDisable()
        {
            SaveSystem.SaveStarted -= OnSaveStarted;
            SaveSystem.SaveFinished -= OnSaveFinished;
            SaveSystem.Deleted -= OnDeleted;
        }

        void OnSaveStarted()
        {
            showingMessage = false;
            baseText = Localization.TranslateOrSame("セーブ中");
            label.color = NormalColor;
            spinner.gameObject.SetActive(true);
            FitPanel(baseText + "...", SpinnerSpace);

            // もう出ているときは、出し始めた時刻をそのままにする（何度もセーブしても、ちらつかない）
            if (!showing)
            {
                showing = true;
                shownAt = Time.unscaledTime;
            }
        }

        void OnSaveFinished(bool succeeded)
        {
            if (!succeeded) ShowMessage(Localization.TranslateOrSame("セーブに失敗しました"), FailedColor, FailedShowTime);
        }

        // 開発用のキーでセーブデータを消したとき（開発中だけなので翻訳しない）
        void OnDeleted() => ShowMessage("セーブデータを消しました", NormalColor, DeletedShowTime);

        // 回るマークなしで、文字だけをしばらく出す
        void ShowMessage(string text, Color color, float seconds)
        {
            showing = true;
            showingMessage = true;
            shownAt = Time.unscaledTime;
            messageTime = seconds;
            label.text = text;
            label.color = color;
            spinner.gameObject.SetActive(false);
            FitPanel(text, Padding);
        }

        // 帯の幅を文字に合わせる（言語によって文字の長さが違うので）
        void FitPanel(string widestText, float left)
        {
            label.rectTransform.offsetMin = new Vector2(left, 0f);
            float textWidth = label.GetPreferredValues(widestText).x;
            panel.sizeDelta = new Vector2(Mathf.Max(MinPanelWidth, left + textWidth + Padding), panel.sizeDelta.y);
        }

        void Update()
        {
            if (showing && group.alpha >= 1f)
            {
                bool done = showingMessage ? Time.unscaledTime - shownAt >= messageTime
                                           : !SaveSystem.IsSaving && Time.unscaledTime - fullyShownAt >= MinShowTime;
                if (done) showing = false;
            }

            float before = group.alpha;
            group.alpha = Mathf.MoveTowards(group.alpha, showing ? 1f : 0f, Time.unscaledDeltaTime / FadeTime);
            if (before < 1f && group.alpha >= 1f) fullyShownAt = Time.unscaledTime;
            if (group.alpha <= 0f) return;

            if (!showingMessage)
            {
                // 「セーブ中.」「..」「...」と点を増やし、マークを回して、止まっていないことを見せる
                int dots = 1 + (int)(Time.unscaledTime / DotInterval) % 3;
                label.text = baseText + new string('.', dots);
                spinner.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * SpinSpeed);
            }
        }

        // ---- 見た目（コードで作る） ----

        void BuildUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // 右下の、半透明の黒い帯
            panel = CreateChild("Panel", transform, typeof(Image));
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);
            panelImage.raycastTarget = false;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-32f, 32f);
            panel.sizeDelta = new Vector2(MinPanelWidth, 72f);

            // 回るマーク（ひし形）
            spinner = CreateChild("Spinner", panel, typeof(Image));
            var spinnerImage = spinner.GetComponent<Image>();
            spinnerImage.color = NormalColor;
            spinnerImage.raycastTarget = false;
            spinner.anchorMin = spinner.anchorMax = new Vector2(0f, 0.5f);
            spinner.anchoredPosition = new Vector2(40f, 0f);
            spinner.sizeDelta = new Vector2(22f, 22f);

            // 文字
            var text = CreateChild("Label", panel, typeof(TextMeshProUGUI));
            label = text.GetComponent<TextMeshProUGUI>();
            label.fontSize = 34;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = new Vector2(SpinnerSpace, 0f);
            text.offsetMax = new Vector2(-Padding, 0f);
        }

        static RectTransform CreateChild(string name, Transform parent, System.Type component)
        {
            var go = new GameObject(name, typeof(RectTransform), component);
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
