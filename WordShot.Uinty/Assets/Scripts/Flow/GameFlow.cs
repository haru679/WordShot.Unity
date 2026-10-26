using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordShot.Data;
using WordShot.Sounds;
using WordShot.Stages;

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
        /// <summary>難易度を選ぶ画面などを開いている。時間が止まり、プレイヤーは動かない</summary>
        Menu,
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
        const float RetryFadeDuration = 0.15f;   // やり直しは素早く（ロード画面も出さない）
        const float ClearWaitTime = 2.5f;        // クリア表示を見せてから戻るまでの時間

        const float MinLoadingTime = 1f;          // 読み込みが速くても、ロード画面はこの秒数は見せる
        const float LoadingFadeDuration = 0.2f;   // ロード画面の出入りの暗転
        const float FullGaugeHoldTime = 0.15f;    // ゲージが満タンになったところを少し見せる

        public static GameFlow Instance { get; private set; }

        /// <summary>今遊んでいる（最後に遊んだ）ステージのシーン名</summary>
        public string CurrentStage { get; private set; }

        /// <summary>
        /// 今遊んでいるステージのデータ。ステージセレクトから入ったときだけ入る
        /// （ステージのシーンを直接再生したときは null で、クリアしても解放は記録されない）
        /// </summary>
        public StageData CurrentStageData { get; private set; }

        /// <summary>今遊んでいるステージの難易度</summary>
        public Difficulty CurrentDifficulty { get; private set; }

        /// <summary>今戦っているボスのデータ（扉から入ったときだけ。ないときは null）</summary>
        public BossData CurrentBoss => CurrentStageData?.Boss;

        /// <summary>
        /// 今の難易度でのボスの値（HP・攻撃の間隔と速さ・妨害・お題）。ボスやタイピングはこれを使う。ないときは null
        /// </summary>
        public BossDifficultyData CurrentBossLevel => CurrentBoss?.Get(CurrentDifficulty);

        /// <summary>直前のステージの結果。まだ遊んでいなければ null</summary>
        public StageResult LastResult { get; private set; }

        /// <summary>シーンを切り替えている途中か</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>今のシーンがステージか</summary>
        public bool IsInStage { get; private set; }

        public PlayState State { get; private set; }

        /// <summary>
        /// 操作を受け付けてよいか（ポーズ・ゲームオーバー・クリア・切り替え中は false）。
        /// 状態が変わったフレームも false にする（ポーズ画面の Resume を押した Enter で、そのまま扉に入ったりしないように）
        /// </summary>
        public bool IsPlaying => State == PlayState.Playing && !IsTransitioning && Time.frameCount != stateChangedFrame;

        /// <summary>状態が変わったとき。ゲームオーバー表示やポーズ画面が使う</summary>
        public event Action<PlayState> StateChanged;

        /// <summary>
        /// ステージをクリアしたとき（引数はステージのシーン名）。
        /// 解放の記録（StageProgress）は GameFlow が行うので、ここは演出などを足したいとき用。
        /// </summary>
        public event Action<string> StageCleared;

        CanvasGroup fade;
        float stageStartTime;
        int stateChangedFrame = -1;
        Coroutine clearWait;
        readonly List<ILoadingTask> pendingTasks = new List<ILoadingTask>();

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

        public void GoToTitle() => LoadScene(SceneNames.Title, FadeDuration, useLoadingScreen: true);

        /// <summary>ステージセレクトへ。まだステージセレクトのシーンがなければタイトルへ</summary>
        public void GoToStageSelect()
        {
            bool hasStageSelect = Application.CanStreamedLevelBeLoaded(SceneNames.StageSelect);
            LoadScene(hasStageSelect ? SceneNames.StageSelect : SceneNames.Title, FadeDuration, useLoadingScreen: true);
        }

        /// <summary>
        /// ステージセレクトから呼ぶ。クリアしたときに、このステージのこの難易度のクリアが記録される（次が解放される）
        /// </summary>
        public void StartStage(StageData stage, Difficulty difficulty = Difficulty.Easy)
        {
            if (stage == null) return;
            if (!LoadScene(stage.SceneName, FadeDuration, useLoadingScreen: true)) return;

            CurrentStageData = stage;
            CurrentDifficulty = difficulty;
        }

        /// <summary>シーン名を指定してステージへ（開発用。クリアしても解放は記録されない）</summary>
        public void StartStage(string stageScene)
        {
            if (LoadScene(stageScene, FadeDuration, useLoadingScreen: true)) CurrentStageData = null;
        }

        /// <summary>今のステージを最初からやり直す。テンポを崩さないよう、ロード画面は出さない</summary>
        public void RetryStage()
        {
            if (string.IsNullOrEmpty(CurrentStage))
            {
                Debug.LogWarning("[GameFlow] まだステージを遊んでいないので、やり直せません");
                return;
            }
            LoadScene(CurrentStage, RetryFadeDuration, useLoadingScreen: false);
        }

        /// <summary>
        /// 次のシーン切り替えのロード画面で、一緒に読み込むものを登録する（切り替えを始める前に呼ぶ）。
        /// ロード画面を出さない切り替え（やり直し）のときは、ロード画面なしで読み込みを待つ。
        /// </summary>
        public void AddLoadingTask(ILoadingTask task)
        {
            if (task != null) pendingTasks.Add(task);
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
                StageProgress.MarkCleared(CurrentStageData, CurrentDifficulty);   // これで次の難易度・次のステージが解放される
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

        // ---- メニュー（難易度を選ぶ画面など）----

        /// <summary>
        /// メニューを開く。時間を止めてプレイヤーが動かないようにする（音は止めない）。ポーズ画面は出ない
        /// </summary>
        public void OpenMenu()
        {
            if (State != PlayState.Playing || IsTransitioning) return;

            Time.timeScale = 0f;
            SetState(PlayState.Menu);
        }

        public void CloseMenu()
        {
            if (State != PlayState.Menu) return;

            Time.timeScale = 1f;
            SetState(PlayState.Playing);
        }

        void SetState(PlayState state)
        {
            if (State == state) return;
            State = state;
            stateChangedFrame = Time.frameCount;
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

        /// <summary>ステージとして扱うシーンか（タイトル・リザルト・ステージセレクト以外）</summary>
        public static bool IsStage(string sceneName) => !SceneNames.NonStageScenes.Contains(sceneName);

        // ---- 切り替えの中身 ----

        // 切り替えを始められたら true
        bool LoadScene(string sceneName, float fadeDuration, bool useLoadingScreen)
        {
            if (IsTransitioning) return false;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GameFlow] シーン「{sceneName}」がビルド設定に登録されていません");
                return false;
            }
            // ロード画面のシーンがまだなければ、ロード画面なしで切り替える
            bool hasLoadingScene = Application.CanStreamedLevelBeLoaded(SceneNames.Loading);
            StartCoroutine(useLoadingScreen && hasLoadingScene
                ? TransitionWithLoadingScreen(sceneName, fadeDuration)
                : Transition(sceneName, fadeDuration));
            return true;
        }

        // ロード画面を出さない切り替え（やり直し）
        IEnumerator Transition(string sceneName, float fadeDuration)
        {
            IsTransitioning = true;
            fade.blocksRaycasts = true;   // 暗転中はボタンを押せないようにする

            yield return Fade(0f, 1f, fadeDuration);
            List<ILoadingTask> tasks = TakePendingTasks();
            foreach (ILoadingTask task in tasks) task.Begin();
            yield return new WaitUntil(() => tasks.All(task => task.IsDone));
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return Fade(1f, 0f, fadeDuration);

            fade.blocksRaycasts = false;
            IsTransitioning = false;
        }

        // 暗転 → ロード画面（ゲージ）→ 暗転 → 次のシーン
        IEnumerator TransitionWithLoadingScreen(string sceneName, float fadeDuration)
        {
            IsTransitioning = true;
            fade.blocksRaycasts = true;

            yield return Fade(0f, 1f, fadeDuration);
            yield return SceneManager.LoadSceneAsync(SceneNames.Loading);   // 軽いシーンなので一瞬で終わる
            var screen = FindAnyObjectByType<LoadingScreen>();
            yield return Fade(1f, 0f, LoadingFadeDuration);

            float startTime = Time.unscaledTime;
            float shown = 0f;   // ゲージに表示している値（減らない）
            List<ILoadingTask> tasks = TakePendingTasks();
            int stepCount = tasks.Count + 1;   // 登録された読み込み＋シーンの読み込み

            void UpdateGauge(float realProgress)
            {
                // 実際の進み具合と「経過時間 ÷ 最低時間」の小さいほうを出す。速く読めても1秒かけて溜まる
                float timeProgress = (Time.unscaledTime - startTime) / MinLoadingTime;
                shown = Mathf.Max(shown, Mathf.Min(realProgress, timeProgress));
                if (screen != null) screen.SetProgress(shown);
            }

            // 先に、登録された読み込みを終わらせる
            // （シーンの読み込みを止めて待っている間は、ほかの読み込みが進まないことがあるため）
            for (int i = 0; i < tasks.Count; i++)
            {
                ILoadingTask task = tasks[i];
                task.Begin();
                while (!task.IsDone)
                {
                    UpdateGauge((i + task.Progress) / stepCount);
                    yield return null;
                }
            }

            // シーンを裏で読み込む。読み終わっても、こちらが許可するまで切り替わらない
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (true)
            {
                // allowSceneActivation が false の間、progress は 0.9 で止まる（＝読み込み完了）
                float sceneProgress = Mathf.Clamp01(operation.progress / 0.9f);
                UpdateGauge((tasks.Count + sceneProgress) / stepCount);

                bool loaded = operation.progress >= 0.9f;
                bool shownLongEnough = Time.unscaledTime - startTime >= MinLoadingTime;
                if (loaded && shownLongEnough && shown >= 1f) break;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(FullGaugeHoldTime);
            yield return Fade(0f, 1f, LoadingFadeDuration);
            operation.allowSceneActivation = true;
            yield return operation;
            yield return Fade(1f, 0f, fadeDuration);

            fade.blocksRaycasts = false;
            IsTransitioning = false;
        }

        // 登録された読み込みを取り出す（同じものを2回読まないよう、登録は空にする）
        List<ILoadingTask> TakePendingTasks()
        {
            var tasks = new List<ILoadingTask>(pendingTasks);
            pendingTasks.Clear();
            return tasks;
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

                // 扉から入ったステージと違うシーンなら（直接再生・開発用キーなど）、ステージのデータは持たない
                if (CurrentStageData != null && CurrentStageData.SceneName != scene.name) CurrentStageData = null;
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
