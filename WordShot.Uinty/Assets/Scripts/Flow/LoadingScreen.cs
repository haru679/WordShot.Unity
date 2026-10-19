using TMPro;
using UnityEngine;

namespace WordShot.Flow
{
    /// <summary>
    /// ロード画面の見た目（ゲージと％）。LoadingScene に置く。
    /// </summary>
    /// <remarks>
    /// 読み込みの処理は GameFlow が行い、ここには進み具合だけが渡される（SetProgress）。
    /// 見た目は LoadingScene を開いて自由に直してよい。
    /// </remarks>
    public class LoadingScreen : MonoBehaviour
    {
        const float DotInterval = 0.3f;

        [Tooltip("ゲージの中身。右端（anchorMax.x）を進み具合に合わせて伸ばす")]
        [SerializeField] RectTransform gaugeFill;
        [SerializeField] TMP_Text percentText;
        [Tooltip("「NOW LOADING」の文字。後ろの「...」を動かす")]
        [SerializeField] TMP_Text loadingText;

        string loadingBaseText;

        void Awake()
        {
            loadingBaseText = loadingText != null ? loadingText.text.TrimEnd('.') : "";
            SetProgress(0f);
        }

        /// <summary>進み具合（0〜1）を表示する。GameFlow から呼ばれる</summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            Vector2 anchorMax = gaugeFill.anchorMax;
            anchorMax.x = progress;
            gaugeFill.anchorMax = anchorMax;

            if (percentText != null) percentText.text = $"{Mathf.FloorToInt(progress * 100f)}%";
        }

        void Update()
        {
            // 「NOW LOADING.」「..」「...」と点を増やして、止まっていないことを見せる
            if (loadingText == null) return;
            int dots = 1 + (int)(Time.unscaledTime / DotInterval) % 3;
            loadingText.text = loadingBaseText + new string('.', dots);
        }
    }
}
