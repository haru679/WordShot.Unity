using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordShot.Flow
{
    /// <summary>
    /// リザルト画面。GameFlow.LastResult を表示し、リトライかタイトルへ進む。
    /// </summary>
    /// <remarks>
    /// 日本語フォントが決まるまでは、文字化けしないよう表示は英語にしている。
    /// </remarks>
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] Button retryButton;
        [SerializeField] Button titleButton;

        void Start()
        {
            StageResult result = GameFlow.Instance.LastResult;

            if (result == null)
            {
                // リザルトシーンを直接再生したとき
                resultText.text = "NO RESULT";
                detailText.text = "Play a stage first";
                retryButton.interactable = false;
            }
            else
            {
                resultText.text = result.IsCleared ? "CLEAR!" : "GAME OVER";
                detailText.text = $"{result.StageScene}   Time {result.PlayTime:0.0}s";
            }

            retryButton.onClick.AddListener(GameFlow.Instance.RetryStage);
            titleButton.onClick.AddListener(GameFlow.Instance.GoToTitle);

            Button selected = retryButton.interactable ? retryButton : titleButton;
            EventSystem.current.SetSelectedGameObject(selected.gameObject);
        }
    }
}
