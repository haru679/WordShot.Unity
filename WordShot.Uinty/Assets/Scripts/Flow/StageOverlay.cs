using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordShot.Data;
using WordShot.Sounds;

namespace WordShot.Flow
{
    /// <summary>
    /// ステージの上に重ねる表示（ゲームオーバー・クリア）。ステージのシーンに置くだけで動く。
    /// </summary>
    /// <remarks>
    /// プレハブ：Assets/Prefabs/UI/StageOverlay.prefab
    /// 表示を出すだけで、やり直し（Shift キー）やステージセレクトへ戻る処理（Ctrl キー）は GameFlow が行う。
    /// ゲームオーバーの画面には画像を1枚張れる（Game Over Image）。画像を入れたときは画像だけを出す
    /// （画像に「Shift でやり直し・Ctrl でステージセレクト」まで描いてあるので、文字の案内は出さない）。
    /// </remarks>
    public class StageOverlay : MonoBehaviour
    {
        const float BlinkInterval = 0.5f;

        // ゲームオーバーの画像の大きさ（画面の中央に大きく出す）
        static readonly Vector2 PictureSize = new Vector2(1400f, 700f);
        const float StageSelectHintY = -200f;
        const float StageSelectHintFontSize = 40f;

        [SerializeField] GameObject gameOverRoot;
        [Tooltip("点滅させる案内（Press Shift Key）")]
        [SerializeField] GameObject retryHint;
        [SerializeField] GameObject clearRoot;

        [Header("ゲームオーバーの画面")]
        [Tooltip("ゲームオーバーの画面に出す画像（1枚）。入れると画像だけを出す。空なら文字の案内を出す")]
        [SerializeField] Sprite gameOverImage;

        [Header("音（なくてもよい）")]
        [SerializeField] SoundCue gameOverSound;
        [SerializeField] SoundCue clearSound;

        TMP_Text stageSelectHint;

        void Awake()
        {
            if (gameOverImage != null) ShowPictureOnly();
            else CreateStageSelectHint();
        }

        void Start()
        {
            gameOverRoot.SetActive(false);
            clearRoot.SetActive(false);
            GameFlow.Instance.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (GameFlow.Instance != null) GameFlow.Instance.StateChanged -= OnStateChanged;
            GameLanguage.Changed -= RefreshStageSelectHint;
        }

        void OnStateChanged(PlayState state)
        {
            gameOverRoot.SetActive(state == PlayState.GameOver);
            clearRoot.SetActive(state == PlayState.Cleared);

            if (state == PlayState.GameOver && gameOverSound != null) Sound.Play(gameOverSound);
            if (state == PlayState.Cleared && clearSound != null) Sound.Play(clearSound);
        }

        void Update()
        {
            // 時間が止まっていても点滅するよう、unscaled の時間を使う
            if (gameOverRoot.activeSelf && gameOverImage == null)
            {
                retryHint.SetActive(Mathf.Repeat(Time.unscaledTime, BlinkInterval * 2f) < BlinkInterval);
            }
        }

        // ゲームオーバーの画面を画像だけにする（もとからある文字は隠す）
        void ShowPictureOnly()
        {
            foreach (Transform child in gameOverRoot.transform) child.gameObject.SetActive(false);

            var picture = new GameObject("GameOverPicture", typeof(RectTransform), typeof(Image));
            picture.transform.SetParent(gameOverRoot.transform, false);
            picture.transform.SetAsFirstSibling();

            var image = picture.GetComponent<Image>();
            image.sprite = gameOverImage;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var rect = (RectTransform)picture.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = PictureSize;
        }

        // 「Ctrl キーでステージセレクトへ」の案内（ずっと出しておく）
        void CreateStageSelectHint()
        {
            var hint = new GameObject("StageSelectHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hint.transform.SetParent(gameOverRoot.transform, false);

            stageSelectHint = hint.GetComponent<TextMeshProUGUI>();
            TMP_Text reference = retryHint.GetComponent<TMP_Text>();
            if (reference != null) stageSelectHint.font = reference.font;
            stageSelectHint.fontSize = StageSelectHintFontSize;
            stageSelectHint.alignment = TextAlignmentOptions.Center;
            stageSelectHint.color = new Color(1f, 1f, 1f, 0.85f);
            stageSelectHint.raycastTarget = false;

            var rect = (RectTransform)hint.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, StageSelectHintY);
            rect.sizeDelta = new Vector2(1200f, 90f);

            GameLanguage.Changed += RefreshStageSelectHint;   // 言語を変えたら、案内も出し直す
            RefreshStageSelectHint();
        }

        void RefreshStageSelectHint()
        {
            if (stageSelectHint != null) stageSelectHint.text = Localization.TranslateOrSame("Ctrlキーでステージセレクトへ");
        }
    }
}
