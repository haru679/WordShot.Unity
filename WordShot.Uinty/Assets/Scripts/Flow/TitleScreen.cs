using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WordShot.Sounds;

namespace WordShot.Flow
{
    /// <summary>
    /// タイトル画面。ビルド設定に入っているステージの数だけ、ボタンを並べる。
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Tooltip("ステージボタンの複製元。非表示のまま置いておく")]
        [SerializeField] Button stageButtonTemplate;

        [Tooltip("ステージボタンを並べる親（縦並び）")]
        [SerializeField] Transform stageButtonParent;

        [SerializeField] Button quitButton;

        [Header("音量設定（なくても動く）")]
        [SerializeField] Button optionsButton;
        [SerializeField] SoundSettingsPanel soundSettings;

        void Start()
        {
            stageButtonTemplate.gameObject.SetActive(false);

            Button firstButton = null;
            foreach (string stage in GameFlow.GetStageScenes())
            {
                Button button = Instantiate(stageButtonTemplate, stageButtonParent);
                button.name = $"StageButton_{stage}";
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = stage;
                button.onClick.AddListener(() => GameFlow.Instance.StartStage(stage));

                if (firstButton == null) firstButton = button;
            }

            if (optionsButton != null && soundSettings != null)
            {
                optionsButton.transform.SetAsLastSibling();
                optionsButton.onClick.AddListener(() => soundSettings.Open(optionsButton.gameObject));
            }

            quitButton.transform.SetAsLastSibling();   // 終了ボタンは一番下
            quitButton.onClick.AddListener(GameFlow.Instance.QuitGame);

            // マウスがなくても矢印キー＋Enterで選べるよう、最初のボタンを選んでおく
            Button selected = firstButton != null ? firstButton : quitButton;
            EventSystem.current.SetSelectedGameObject(selected.gameObject);
        }
    }
}
