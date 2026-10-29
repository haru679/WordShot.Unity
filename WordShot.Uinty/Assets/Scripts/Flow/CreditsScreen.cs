using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WordShot.Data;

namespace WordShot.Flow
{
    /// <summary>
    /// クレジット（使った素材のサイトと、作った人の名前）。タイトル画面の「クレジット」で開く。
    /// </summary>
    /// <remarks>
    /// 開くと、後ろの画面にぼかしをかけて少し暗くし、文字を上から下へ流す。
    /// 全部が下へ流れきるか、何かキーを押すと閉じて、タイトル画面に戻る。
    /// 見た目はすべてコードで作る（シーンに置くのはこのスクリプトだけ）。
    /// 載せる内容を変えるときは、下の Lines を書き換える。素材サイトは「名前とリンクを出す」のが使う条件なので消さないこと。
    /// </remarks>
    public class CreditsScreen : MonoBehaviour
    {
        const int SortingOrder = 500;          // タイトル画面より手前、暗転（GameFlow）より奥
        const int BlurDownscale = 12;          // 画面を何分の1に縮めてから引き伸ばすか（大きいほど強くぼける）

        [Tooltip("文字が流れる速さ（1920x1080 の画面で、1秒に何ピクセル）")]
        [SerializeField, Min(10f)] float scrollSpeed = 140f;
        [Tooltip("後ろの画面の明るさ（1 = そのまま、0 = 真っ黒）")]
        [SerializeField, Range(0f, 1f)] float backgroundBrightness = 0.55f;

        enum Style { Heading, Name, Link, Space }

        // 上から順に並べる。文字は上から下へ流れるので、一番下の行から先に見えてくる
        // （素材サイトを先に、作った人を最後に見せるため、作った人を上、素材サイトを下に書く）
        static (Style style, string text)[] Lines() => new[]
        {
            (Style.Heading, Localization.TranslateOrSame("プランナー")),
            (Style.Name, "前田陽輝"),
            (Style.Space, ""),
            (Style.Heading, Localization.TranslateOrSame("プログラマー")),
            (Style.Name, "川田瑞輝"),
            (Style.Name, "田場優佑"),   // 名前（読み）の順：かわた・たば・まつい
            (Style.Name, "松井大翔"),
            (Style.Space, ""),
            (Style.Space, ""),
            (Style.Heading, Localization.TranslateOrSame("BGM素材・使用楽曲")),
            (Style.Name, "創作堂さくら紅葉"),
            (Style.Link, "https://yukizakura.net/"),
            (Style.Space, ""),
            (Style.Name, "効果音ラボ"),
            (Style.Link, "https://soundeffect-lab.info/"),
        };

        GameObject root;
        RectTransform content;
        RenderTexture blurred;
        GameObject returnSelection;
        int openedFrame;

        public bool IsOpen => root != null;

        /// <summary>クレジットを開く。閉じたら returnSelection を選び直す</summary>
        public void Open(GameObject returnSelection)
        {
            if (IsOpen) return;
            this.returnSelection = returnSelection;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            // タイトル画面のボタンが反応しないようにする（閉じた次のフレームで戻す）。
            // 止めると EventSystem.current が空になるので、戻すために覚えておく
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null) eventSystem.enabled = false;

            // 今の画面を写して、ぼかした背景にする
            RenderTexture screen = CaptureScreen();
            if (screen != null)
            {
                blurred = Blur(screen);
                RenderTexture.ReleaseTemporary(screen);
            }

            Build();
            openedFrame = Time.frameCount;

            float screenHeight = ((RectTransform)root.transform).rect.height;
            Canvas.ForceUpdateCanvases();
            float contentHeight = content.rect.height;

            // 文字のかたまりの下の端が、画面の上の端にある所から始めて、上の端が画面の下に消えるまで流す
            float y = screenHeight / 2f + contentHeight / 2f;
            float endY = -screenHeight / 2f - contentHeight / 2f;
            while (y > endY)
            {
                if (KeyPressed()) break;
                y -= scrollSpeed * Time.unscaledDeltaTime;
                content.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }

            Close();
            yield return null;
            if (eventSystem != null)
            {
                eventSystem.enabled = true;
                if (returnSelection != null) eventSystem.SetSelectedGameObject(returnSelection);
            }
        }

        // 開いたときの決定キーでは閉じないよう、次のフレームから見る
        bool KeyPressed()
        {
            if (Time.frameCount <= openedFrame) return false;
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }

        void Close()
        {
            if (root != null) Destroy(root);
            root = null;
            if (blurred != null)
            {
                blurred.Release();
                Destroy(blurred);
                blurred = null;
            }
        }

        void OnDestroy() => Close();

        // 今の画面（カメラの絵＋画面の文字やボタン）を、カメラで描き直して写す。
        // 「描き終わるのを待って画面を写す」方法は、画面を描かない環境（自動テストなど）で止まってしまうため、この方法にしている
        static RenderTexture CaptureScreen()
        {
            Camera camera = Camera.main;
            if (camera == null) return null;

            var target = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 24);
            // 画面に直接描いている UI（Screen Space - Overlay）を、少しの間だけカメラに描かせる
            var overlays = new System.Collections.Generic.List<Canvas>();
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                overlays.Add(canvas);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.5f;
            }
            RenderTexture previous = camera.targetTexture;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            camera.targetTexture = previous;
            foreach (Canvas canvas in overlays) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.ForceUpdateCanvases();
            return target;
        }

        // 小さく縮めてから、なめらかに引き伸ばして表示するとぼける（専用のシェーダーがいらない）
        static RenderTexture Blur(Texture source)
        {
            int w = Mathf.Max(1, source.width / 4), h = Mathf.Max(1, source.height / 4);
            var half = RenderTexture.GetTemporary(w, h, 0);
            half.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, half);

            var small = new RenderTexture(Mathf.Max(1, source.width / BlurDownscale), Mathf.Max(1, source.height / BlurDownscale), 0)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Graphics.Blit(half, small);
            RenderTexture.ReleaseTemporary(half);
            return small;
        }

        // ---- 見た目（コードで作る） ----

        void Build()
        {
            root = new GameObject("Credits", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // ぼかした背景（少し暗く）
            var background = Stretch(new GameObject("Background", typeof(RectTransform), typeof(RawImage)).transform, root.transform);
            var raw = background.GetComponent<RawImage>();
            raw.texture = blurred;
            raw.color = new Color(backgroundBrightness, backgroundBrightness, backgroundBrightness, 1f);

            // 流れる文字のかたまり
            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content = (RectTransform)contentObject.transform;
            content.SetParent(root.transform, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(1600f, 0f);
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var (style, text) in Lines()) AddLine(style, text);
        }

        void AddLine(Style style, string text)
        {
            var line = new GameObject(style.ToString(), typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            line.transform.SetParent(content, false);
            var tmp = line.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            (tmp.fontSize, tmp.color) = style switch
            {
                Style.Heading => (44f, new Color(1f, 0.8f, 0.4f)),
                Style.Name => (60f, Color.white),
                Style.Link => (36f, new Color(0.7f, 0.85f, 1f)),
                _ => (60f, Color.clear),
            };
            line.GetComponent<LayoutElement>().preferredHeight = style == Style.Space ? 40f : tmp.fontSize * 1.3f;
        }

        static RectTransform Stretch(Transform child, Transform parent)
        {
            var rect = (RectTransform)child;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
