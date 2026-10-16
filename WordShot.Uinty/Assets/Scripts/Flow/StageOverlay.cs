using UnityEngine;
using WordShot.Sounds;

namespace WordShot.Flow
{
    /// <summary>
    /// ステージの上に重ねる表示（ゲームオーバー・クリア）。ステージのシーンに置くだけで動く。
    /// </summary>
    /// <remarks>
    /// プレハブ：Assets/Prefabs/UI/StageOverlay.prefab
    /// 表示を出すだけで、やり直し（Shift キー）やステージセレクトへ戻る処理は GameFlow が行う。
    /// </remarks>
    public class StageOverlay : MonoBehaviour
    {
        const float BlinkInterval = 0.5f;

        [SerializeField] GameObject gameOverRoot;
        [Tooltip("点滅させる案内（Press Shift Key）")]
        [SerializeField] GameObject retryHint;
        [SerializeField] GameObject clearRoot;

        [Header("音（なくてもよい）")]
        [SerializeField] SoundCue gameOverSound;
        [SerializeField] SoundCue clearSound;

        void Start()
        {
            gameOverRoot.SetActive(false);
            clearRoot.SetActive(false);
            GameFlow.Instance.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (GameFlow.Instance != null) GameFlow.Instance.StateChanged -= OnStateChanged;
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
            if (gameOverRoot.activeSelf)
            {
                retryHint.SetActive(Mathf.Repeat(Time.unscaledTime, BlinkInterval * 2f) < BlinkInterval);
            }
        }
    }
}
