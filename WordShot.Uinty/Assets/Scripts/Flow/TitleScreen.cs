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

        void Start()
        {
            startButton.onClick.AddListener(() => GameFlow.Instance.GoToStageSelect());
            quitButton.onClick.AddListener(GameFlow.Instance.QuitGame);

            if (optionsButton != null && soundSettings != null)
            {
                optionsButton.onClick.AddListener(() => soundSettings.Open(optionsButton.gameObject));
            }

            // マウスがなくても矢印キー＋Enterで選べるよう、Start を選んでおく
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
    }
}
