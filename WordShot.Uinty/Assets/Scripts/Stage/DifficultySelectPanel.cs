using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WordShot.Data;
using WordShot.Flow;

namespace WordShot.Stages
{
    /// <summary>
    /// ステージセレクトで、扉に入るときに難易度を選ぶ画面。ステージセレクトのシーンに1つ置く。
    /// </summary>
    /// <remarks>
    /// 解放されていない難易度は「ロック中」で選べない。クリアした難易度には「クリア」と出す。
    /// 上下キーと Enter で選び、Esc でもどる。開いている間は時間を止め、プレイヤーは動かない（GameFlow.OpenMenu）。
    /// 置いていないときは、扉はそのまま「かんたん」で始まる。
    /// </remarks>
    public class DifficultySelectPanel : MonoBehaviour
    {
        [Tooltip("開いたときに出す部分（閉じているときは非表示）")]
        [SerializeField] GameObject panel;
        [Tooltip("ボスの名前を出す文字")]
        [SerializeField] TMP_Text titleText;
        [Tooltip("かんたん・ふつう・むずかしいの順")]
        [SerializeField] Button[] buttons = new Button[3];

        StageData stage;
        int openedFrame = -1;

        public bool IsOpen => panel.activeSelf;

        void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Difficulty difficulty = DifficultyExtensions.All[i];
                buttons[i].onClick.AddListener(() => Choose(difficulty));
            }
            panel.SetActive(false);
        }

        /// <summary>扉から呼ぶ。そのステージの難易度を選ぶ画面を開く</summary>
        public void Open(StageData target)
        {
            if (IsOpen || target == null) return;

            // ボタンを操作するには EventSystem が必要。シーンになければ作る
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            stage = target;
            openedFrame = Time.frameCount;
            GameFlow.Instance.OpenMenu();
            panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            GameFlow.Instance.CloseMenu();
        }

        void Update()
        {
            if (!IsOpen) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        void Choose(Difficulty difficulty)
        {
            // 扉で押した Enter が、開いたばかりのボタンにも届いて勝手に決まらないようにする
            if (Time.frameCount == openedFrame || !StageProgress.IsUnlocked(stage, difficulty)) return;

            panel.SetActive(false);
            GameFlow.Instance.CloseMenu();
            GameFlow.Instance.StartStage(stage, difficulty);
        }

        void Refresh()
        {
            titleText.text = stage.Boss?.DisplayName ?? Localization.TranslateOrSame(stage.DisplayName);

            for (int i = 0; i < buttons.Length; i++)
            {
                Difficulty difficulty = DifficultyExtensions.All[i];
                bool unlocked = StageProgress.IsUnlocked(stage, difficulty);
                string state = !unlocked ? Localization.TranslateOrSame("ロック中")
                    : StageProgress.IsCleared(stage, difficulty) ? Localization.TranslateOrSame("クリア")
                    : "";

                buttons[i].interactable = unlocked;
                buttons[i].GetComponentInChildren<TMP_Text>().text =
                    string.IsNullOrEmpty(state) ? difficulty.DisplayName() : $"{difficulty.DisplayName()}　<size=70%>{state}</size>";
            }

            LinkNavigation();

            // 最初は「まだクリアしていない中で一番やさしい難易度」を選んでおく（全部クリア済みなら、一番むずかしい難易度）
            Button first = buttons
                .Where((button, i) => button.interactable && !StageProgress.IsCleared(stage, DifficultyExtensions.All[i]))
                .FirstOrDefault() ?? buttons.LastOrDefault(button => button.interactable);
            EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
        }

        // 上下キーで、選べる難易度の間だけを移動する
        void LinkNavigation()
        {
            Button[] selectable = buttons.Where(button => button.interactable).ToArray();
            for (int i = 0; i < selectable.Length; i++)
            {
                selectable[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? selectable[i - 1] : null,
                    selectOnDown = i < selectable.Length - 1 ? selectable[i + 1] : null,
                };
            }
        }
    }
}
