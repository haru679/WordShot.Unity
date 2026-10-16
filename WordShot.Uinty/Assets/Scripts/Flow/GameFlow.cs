using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordShot.Sounds;

namespace WordShot.Flow
{
    /// <summary>
    /// 遊んでいる状態。プレイヤーの操作やタイピングは、Playing のときだけ受け付ける。
    /// </summary>
    public enum PlayState
    {
        /// <summary>ふつうに遊んでいる（メニュー画面もこの状態）</summary>
        Playing,
        /// <summary>ポーズ中。時間が止まっている</summary>
        Paused,
        /// <summary>やられた。時間が止まり、Shift キーでやり直す</summary>
        GameOver,
        /// <summary>ボスを倒した。少し待ってからステージセレクトへ戻る</summary>
        Cleared,
    }

    /// <summary>
    /// ゲーム全体の流れ（シーンの切り替え・ポーズ・ゲームオーバー・クリア）を管理する。
    /// シーンをまたいで1つだけ存在し、どのシーンから再生しても自動で作られる。
    /// </summary>
    /// <remarks>
    /// 他のスクリプトからは GameFlow.Instance.FinishStage(true) のように呼ぶ。シーンに置く必要はない。
    /// プレイヤーの操作やタイピングでは、最初に if (!GameFlow.Instance.IsPlaying) return; と書く。
    /// </remarks>
    public class GameFlow : MonoBehaviour
    {
        const float FadeDuration = 0.3f;
        const float RetryFadeDuration = 0.15f;   // やり直しは素早く
        const float ClearWaitTime = 2.5f;        // クリア表示を見せてから戻るまでの時間

        public static GameFlow Instance { get; private set; }

        /// <summary>今遊んでいる（最後に遊んだ）ステージのシーン名</summary>
        public string CurrentStage { get; private set; }

        /// <summary>直前のステージの結果。まだ遊んでいなければ null</summary>
        public StageResult LastResult { get; private set; }

        /// <summary>シーンを切り替えている途中か</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>今のシーンがステージか</summary>
        public bool IsInStage { get; private set; }

        public PlayState State { get; private set; }

        /// <summary>操作を受け付けてよいか（ポーズ・ゲームオーバー・クリア・切り替え中は false）</summary>
        public bool IsPlaying => State == PlayState.Playing && !IsTransitioning;

        /// <summary>状態が変わったとき。ゲームオーバー表示やポーズ画面が使う</summary>
        public event Action<PlayState> StateChanged;

        /// <summary>
        /// ステージをクリアしたとき（引数はステージのシーン名）。
        /// 次のステージの解放は、ステージセレクトを作るときにここにつなぐ。
        /// </summary>
        public event Action<string> StageCleared;

        CanvasGroup fade;
        float stageStartTime;
        Coroutine clearWait;

        // 最初のシーンが読み込まれる前に、自動で1つ作る。
        // このプロジェクトはドメインのリロードが無効なので、static の値は再生ごとに必ず上書きする。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateInstance()
        {
            var go = new GameObject(nameof(GameFlow));
            Instance = go.AddComponent<GameFlow>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            CreateFadeOverlay();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ---- シーンの切り替え ----

        public void GoToTitle() => LoadScene(SceneNames.Title, FadeDuration);

        /// <summary>ステージセレクトへ。まだステージセレクトのシーンがなければタイトルへ</summary>
        public void GoToStageSelect()
        {
            bool hasStageSelect = Application.CanStreamedLevelBeLoaded(SceneNames.StageSelect);
            LoadScene(hasStageSelect ? SceneNames.StageSelect : SceneNames.Title, FadeDuration);
        }

        public void StartStage(string stageScene) => LoadScene(stageScene, FadeDuration);

        /// <summary>今のステージを最初からやり直す</summary>
        public void RetryStage()
        {
            if (string.IsNullOrEmpty(CurrentStage))
            {
                Debug.LogWarning("[GameFlow] まだステージを遊んでいないので、やり直せません");
                return;
            }
            LoadScene(CurrentStage, RetryFadeDuration);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- ステージの終わり ----

        /// <summary>
        /// ステージを終える。ボスを倒したら true、プレイヤーがやられたら false で呼ぶ。
        /// </summary>
        /// <remarks>
        /// true：クリア表示を出し、少し待ってからステージセレクトへ戻る。
        /// false：時間を止めてゲームオーバー表示を出す。Shift キーでやり直す。
        /// </remarks>
        public void FinishStage(bool isCleared)
        {
            // 撃破とやられたが同じタイミングで来ても、最初の1回だけを採用する
            if (!IsInStage || State != PlayState.Playing || IsTransitioning) return;

            LastResult = new StageResult(CurrentStage, isCleared, Time.time - stageStartTime);

            if (isCleared)
            {
                SetState(PlayState.Cleared);
                StageCleared?.Invoke(CurrentStage);
                clearWait = StartCoroutine(GoToStageSelectAfter(ClearWaitTime));
            }
            else
            {
                Time.timeScale = 0f;
                Sound.SetPaused(true);   // 効果音は止め、BGM は流したままにする
                SetState(PlayState.GameOver);
            }
        }

        IEnumerator GoToStageSelectAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            clearWait = null;
            GoToStageSelect();
        }

        // ---- ポーズ ----

        public void Pause()
        {
            if (State != PlayState.Playing || IsTransitioning) return;

            Time.timeScale = 0f;
            Sound.SetPaused(true);
            SetState(PlayState.Paused);
        }

        public void Resume()
        {
            if (State != PlayState.Paused) return;

            Time.timeScale = 1f;
            Sound.SetPaused(false);
            SetState(PlayState.Playing);
        }

        void SetState(PlayState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || IsTransitioning) return;

            // ゲームオーバー中は Shift キーでやり直す
            if (State == PlayState.GameOver && keyboard.shiftKey.wasPressedThisFrame)
            {
                RetryStage();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 開発用のキー。ボスやプレイヤーができるまで、ステージの終わり方をこれで確かめる。
            // F キーは文字入力とぶつからないので、タイピングの邪魔にならない。製品版には入らない。
            if (!IsInStage || State != PlayState.Playing) return;

            if (keyboard.f1Key.wasPressedThisFrame) FinishStage(true);        // クリア
            else if (keyboard.f2Key.wasPressedThisFrame) FinishStage(false);  // ゲームオーバー
            else if (keyboard.f3Key.wasPressedThisFrame) GoToTitle();         // タイトルへ戻る
#endif
        }

        // ---- ステージの一覧 ----

        /// <summary>
        /// ビルド設定に登録されているシーンのうち、ステージとして遊べるものの一覧。
        /// 新しいステージは、ビルド設定のシーン一覧に追加するだけでここに出てくる。
        /// </summary>
        public static List<string> GetStageScenes()
        {
            var stages = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                if (IsStage(sceneName)) stages.Add(sceneName);
            }
            return stages;
        }

        public static bool IsStage(string sceneName) => !SceneNames.NonStageScenes.Contains(sceneName);

        // ---- 切り替えの中身 ----

        void LoadScene(string sceneName, float fadeDuration)
        {
            if (IsTransitioning) return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GameFlow] シーン「{sceneName}」がビルド設定に登録されていません");
                return;
            }
            StartCoroutine(Transition(sceneName, fadeDuration));
        }

        IEnumerator Transition(string sceneName, float fadeDuration)
        {
            IsTransitioning = true;
            fade.blocksRaycasts = true;   // 暗転中はボタンを押せないようにする

            yield return Fade(0f, 1f, fadeDuration);
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return Fade(1f, 0f, fadeDuration);

            fade.blocksRaycasts = false;
            IsTransitioning = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            // ポーズ・ゲームオーバー（timeScale = 0）中でも暗転できるよう、unscaled の時間を使う
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            fade.alpha = to;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 前のシーンで止めた時間を戻す（効果音の一時停止は SoundManager が戻す）
            Time.timeScale = 1f;
            SetState(PlayState.Playing);

            // クリア後の「待ってから戻る」が、別の方法でシーンを移ったあとに動かないよう止める
            if (clearWait != null)
            {
                StopCoroutine(clearWait);
                clearWait = null;
            }

            // ステージのシーンを直接再生したときも、ここで「今のステージ」として記録される
            IsInStage = IsStage(scene.name);
            if (IsInStage)
            {
                CurrentStage = scene.name;
                stageStartTime = Time.time;
            }
        }

        // 画面全体を覆う黒い板。シーンをまたいでも消えないよう、自分の子にする
        void CreateFadeOverlay()
        {
            var canvasObject = new GameObject("FadeCanvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;   // 一番手前に描く

            fade = canvasObject.GetComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;

            var imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            imageObject.GetComponent<Image>().color = Color.black;

            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }
    }
}
