using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WordShot.Data;

namespace WordShot.Flow
{
    /// <summary>
    /// リザルト画面。GameFlow.LastResult を表示し、リトライかタイトルへ進む。
    /// </summary>
    /// <remarks>
    /// 今は使っていない（ゲームオーバーはその場で表示し、クリア後はステージセレクトへ戻る）。
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
                resultText.text = Localization.TranslateOrSame("まだ結果がありません");
                detailText.text = Localization.TranslateOrSame("先にステージを遊んでください");
                retryButton.interactable = false;
            }
            else
            {
                // 翻訳が必要な文字を編集画面が自動で集められるよう、文字は TranslateOrSame の中に直接書く
                resultText.text = result.IsCleared
                    ? Localization.TranslateOrSame("クリア！")
                    : Localization.TranslateOrSame("ゲームオーバー");
                detailText.text = $"{result.StageScene}   Time {result.PlayTime:0.0}s";
            }

            retryButton.onClick.AddListener(GameFlow.Instance.RetryStage);
            titleButton.onClick.AddListener(GameFlow.Instance.GoToTitle);

            Button selected = retryButton.interactable ? retryButton : titleButton;
            EventSystem.current.SetSelectedGameObject(selected.gameObject);
        }
    }
}
