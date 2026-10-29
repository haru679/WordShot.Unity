using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WordShot.Sounds;

namespace WordShot.Flow
{
    /// <summary>
    /// タイトル画面。Start でステージセレクトへ進む。
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;

        [Header("音量設定（なくても動く）")]
        [SerializeField] Button optionsButton;
        [SerializeField] SoundSettingsPanel soundSettings;

        [Header("効果音（なくても動く）")]
        [Tooltip("ボタンを押したときに鳴らすSoundCue(Categoryは UI)")]
        [SerializeField] SoundCue clickSound;

        void Start()
        {
            startButton.onClick.AddListener(() => { PlayClick(); GameFlow.Instance.GoToStageSelect(); });
            quitButton.onClick.AddListener(() => { PlayClick(); GameFlow.Instance.QuitGame(); });

            if (optionsButton != null && soundSettings != null)
            {
                optionsButton.onClick.AddListener(() => { PlayClick(); soundSettings.Open(optionsButton.gameObject); });
            }

            // マウスがなくても矢印キー＋Enterで選べるよう、Start を選んでおく
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        // ボタンを押したときの音。設定されていなければ何もしない
        void PlayClick()
        {
            if (clickSound != null) Sound.Play(clickSound);
        }
    }
}
