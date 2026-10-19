using TMPro;
using UnityEngine;
using WordShot.Flow;
using WordShot.Sounds;

namespace WordShot.Stages
{
    /// <summary>
    /// ステージセレクトに置く扉（ステージの入口）。プレイヤーが触れている状態で Enter を押すと、そのステージへ移る。
    /// </summary>
    /// <remarks>
    /// 見た目を変えるとき：body の SpriteRenderer の画像を差し替える（テクスチャを貼る）。
    /// ロック中は、その画像の色を暗くする。ロック中だけ別の画像にしたいときは lockedSprite に入れる。
    /// </remarks>
    [RequireComponent(typeof(Collider2D))]
    public class StageDoor : MonoBehaviour
    {
        [SerializeField] StageData stage;

        [Header("見た目")]
        [SerializeField] SpriteRenderer body;
        [Tooltip("ロック中に色へ掛ける明るさ（1 = そのまま、0 = 真っ黒）。白なら 0.55 でグレーになる")]
        [SerializeField, Range(0f, 1f)] float lockedBrightness = 0.55f;
        [Tooltip("ロック中だけ別の画像にしたいときに入れる（空なら同じ画像を暗くする）")]
        [SerializeField] Sprite lockedSprite;
        [Tooltip("ステージ名と LOCKED / CLEAR を出す文字")]
        [SerializeField] TMP_Text label;
        [Tooltip("プレイヤーが触れているときに出す案内（Enter）")]
        [SerializeField] GameObject prompt;

        [Header("音（なくてもよい）")]
        [SerializeField] SoundCue enterSound;
        [SerializeField] SoundCue lockedSound;

        Color baseColor;
        Sprite baseSprite;
        bool isPlayerNear;

        public StageData Stage => stage;
        public bool IsUnlocked => StageProgress.IsUnlocked(stage);

        void Awake()
        {
            // シーンに置いたときの色と画像を「元の見た目」として覚えておく
            baseColor = body.color;
            baseSprite = body.sprite;
        }

        void OnEnable()
        {
            StageProgress.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            StageProgress.Changed -= Refresh;
        }

        /// <summary>プレイヤーが触れた・離れたときに、プレイヤーから呼ばれる</summary>
        public void SetPlayerNear(bool isNear)
        {
            isPlayerNear = isNear;
            UpdatePrompt();
        }

        /// <summary>決定キーで呼ぶ。ステージへ移れたら true</summary>
        public bool TryEnter()
        {
            if (stage == null)
            {
                Debug.LogWarning($"[StageDoor] {name} に StageData が設定されていません");
                return false;
            }
            if (!IsUnlocked)
            {
                if (lockedSound != null) Sound.Play(lockedSound);
                return false;
            }

            if (enterSound != null) Sound.Play(enterSound);
            GameFlow.Instance.StartStage(stage);
            return true;
        }

        void Refresh()
        {
            bool unlocked = IsUnlocked;

            body.sprite = !unlocked && lockedSprite != null ? lockedSprite : baseSprite;
            body.color = unlocked
                ? baseColor
                : new Color(baseColor.r * lockedBrightness, baseColor.g * lockedBrightness, baseColor.b * lockedBrightness, baseColor.a);

            if (label != null) label.text = LabelText(unlocked);
            UpdatePrompt();
        }

        string LabelText(bool unlocked)
        {
            if (stage == null) return "(no stage)";
            if (!unlocked) return $"{stage.DisplayName}\nLOCKED";
            if (StageProgress.IsCleared(stage)) return $"{stage.DisplayName}\nCLEAR";
            return stage.DisplayName;
        }

        // ロック中は「Enter」の案内を出さない（LOCKED の表示で分かるようにする）
        void UpdatePrompt()
        {
            if (prompt != null) prompt.SetActive(isPlayerNear && IsUnlocked);
        }
    }
}
