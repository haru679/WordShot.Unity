using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WordShot.Sounds;

namespace WordShot.Flow
{
    /// <summary>
    /// ポーズ画面。シーンに置くだけで、Esc キーでポーズできるようになる。
    /// </summary>
    /// <remarks>
    /// プレハブ：Assets/Prefabs/UI/PauseMenu.prefab
    /// ステージでは「Retry」が出る。ステージ以外（ステージセレクトなど）では出ない。
    /// 「Back」はステージならステージセレクトへ、それ以外ならタイトルへ戻る。
    /// </remarks>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject menuRoot;
        [SerializeField] Button resumeButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button soundButton;
        [Tooltip("言語を切り替えるボタン（なくても動く）")]
        [SerializeField] Button languageButton;
        [SerializeField] Button backButton;
        [SerializeField] SoundSettingsPanel soundSettings;

        void Awake()
        {
            // ボタンを操作するには EventSystem が必要。シーンになければ作る
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            resumeButton.onClick.AddListener(() => GameFlow.Instance.Resume());
            retryButton.onClick.AddListener(() => GameFlow.Instance.RetryStage());
            soundButton.onClick.AddListener(() => soundSettings.Open(soundButton.gameObject));
            backButton.onClick.AddListener(OnBack);
        }

        void Start()
        {
            menuRoot.SetActive(false);
            GameFlow.Instance.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (GameFlow.Instance != null) GameFlow.Instance.StateChanged -= OnStateChanged;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            // 音量設定を開いているときの Esc は、音量設定を閉じるだけにする
            if (soundSettings.IsOpen || soundSettings.LastClosedFrame == Time.frameCount) return;

            GameFlow flow = GameFlow.Instance;
            // 難易度を選ぶ画面を Esc で閉じた同じフレームに、ポーズまで開かないよう IsPlaying も見る
            if (flow.State == PlayState.Playing && flow.IsPlaying) flow.Pause();
            else if (flow.State == PlayState.Paused) flow.Resume();
        }

        void OnStateChanged(PlayState state)
        {
            bool paused = state == PlayState.Paused;
            menuRoot.SetActive(paused);
            if (!paused)
            {
                if (soundSettings.IsOpen) soundSettings.Close();
                return;
            }

            // ステージ以外では、やり直しを出さない
            retryButton.gameObject.SetActive(GameFlow.Instance.IsInStage);
            LinkNavigation();
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        void OnBack()
        {
            if (GameFlow.Instance.IsInStage) GameFlow.Instance.GoToStageSelect();
            else GameFlow.Instance.GoToTitle();
        }

        // 上下キーで、表示しているボタンの間だけを移動するようにする
        void LinkNavigation()
        {
            Button[] buttons = new[] { resumeButton, retryButton, soundButton, languageButton, backButton }
                .Where(button => button != null && button.gameObject.activeSelf)
                .ToArray();

            for (int i = 0; i < buttons.Length; i++)
            {
                var navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? buttons[i - 1] : null,
                    selectOnDown = i < buttons.Length - 1 ? buttons[i + 1] : null,
                };
                buttons[i].navigation = navigation;
            }
        }
    }
}
