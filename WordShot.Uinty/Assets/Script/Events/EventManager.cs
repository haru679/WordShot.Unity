using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WordShot.Flow;

namespace WordShot.Events
{
    /// <summary>
    /// フラグ（GameFlags）が変わったら、条件に合う処理（演出・音など）を呼び出す。
    /// シーンをまたいで1つだけ存在し、再生すると自動で作られる。
    /// </summary>
    /// <remarks>
    /// ・Resources/Events にある StateEventData は、どのシーンでも自動で読み込む。
    /// ・シーンに置いた SceneEvents は、そのシーンにいる間だけ有効になる。
    /// ・GameFlow の状態（ポーズ・ゲームオーバー・クリア）とシーンの切り替えは、自動で GameFlags に反映する。
    /// 他のスクリプトからは GameFlags.Set(BossState.Attack) のように状態を変えるだけでよい。
    /// </remarks>
    public class EventManager : MonoBehaviour
    {
        const string DataFolder = "Events";
        const int MaxDepth = 8;   // 処理の中でフラグを変えたときの連鎖の上限（無限ループ防止）

        public static EventManager Instance { get; private set; }

        [Header("全シーン共通のデータ（Resources/Events から自動で読み込む）")]
        [SerializeField] List<StateEventData> globalData = new List<StateEventData>();

        [Header("=== 確認用 ===")]
        [Tooltip("呼び出した処理をコンソールに出す")]
        [SerializeField] bool logEvents;

#pragma warning disable CS0169, CS0414   // 確認用の欄はエディターでだけ使う
        [Tooltip("確かめたいフラグを選ぶ（例：ボス/Attack）")]
        [SerializeField] FlagRef debugFlag;

        [Tooltip("再生中にチェックを入れると、上のフラグを実際に設定する（条件も含めて本番と同じ流れで確かめられる）")]
        [SerializeField] bool testSetFlag;
#pragma warning restore CS0169, CS0414

        readonly List<SceneEvents> sceneEvents = new List<SceneEvents>();
        GameFlow hookedFlow;
        int depth;

        // このプロジェクトはドメインのリロードが無効なので、static の値は再生ごとに必ず上書きする
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateInstance()
        {
            var go = new GameObject(nameof(EventManager));
            Instance = go.AddComponent<EventManager>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            foreach (StateEventData data in Resources.LoadAll<StateEventData>(DataFolder))
            {
                if (!globalData.Contains(data)) globalData.Add(data);
            }

            GameFlags.StateChanged += OnStateChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            GameFlags.StateChanged -= OnStateChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (hookedFlow != null) hookedFlow.StateChanged -= OnPlayStateChanged;
        }

        // ---- シーンごとの登録 ----

        public static void Register(SceneEvents events)
        {
            if (Instance != null && events != null && !Instance.sceneEvents.Contains(events)) Instance.sceneEvents.Add(events);
        }

        public static void Unregister(SceneEvents events)
        {
            if (Instance != null) Instance.sceneEvents.Remove(events);
        }

        // ---- GameFlow・シーンとの連動 ----

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HookGameFlow();
            GameFlags.Set(ToSceneState(scene.name));

            // GameFlow はシーンを読み込むと Playing に戻すので、こちらもそろえる
            if (hookedFlow != null)
            {
                GameState state = ToGameState(hookedFlow.State);
                if (GameFlags.Get<GameState>() != state) GameFlags.Set(state);
            }
        }

        // GameFlow も再生時に自動で作られるが、どちらが先に作られるかは決まっていないので、ここでつなぐ
        void HookGameFlow()
        {
            if (hookedFlow != null || GameFlow.Instance == null) return;
            hookedFlow = GameFlow.Instance;
            hookedFlow.StateChanged += OnPlayStateChanged;
        }

        void OnPlayStateChanged(PlayState state) => GameFlags.Set(ToGameState(state));

        static GameState ToGameState(PlayState state)
        {
            switch (state)
            {
                case PlayState.Paused: return GameState.Paused;
                case PlayState.GameOver: return GameState.GameOver;
                case PlayState.Cleared: return GameState.Cleared;
                default: return GameState.Playing;
            }
        }

        static SceneState ToSceneState(string sceneName)
        {
            if (sceneName == SceneNames.Title) return SceneState.Title;
            if (sceneName == SceneNames.Loading) return SceneState.Loading;
            if (sceneName == SceneNames.StageSelect) return SceneState.StageSelect;
            if (sceneName == SceneNames.Result) return SceneState.Result;
            return GameFlow.IsStage(sceneName) ? SceneState.Stage : SceneState.Other;
        }

        // ---- 呼び出し ----

        void OnStateChanged(Type changedType)
        {
            if (depth >= MaxDepth)
            {
                Debug.LogWarning($"[Event] フラグの連鎖が {MaxDepth} 回を超えたので止めました（{changedType?.Name}）");
                return;
            }

            depth++;
            try
            {
                // 処理の中でシーンの物が増減しても困らないよう、先に一覧を写してから回す
                foreach (EventSetup setup in CollectSetups())
                {
                    if (!setup.IsTriggeredBy(changedType)) continue;

                    if (logEvents) Debug.Log($"[Event] {(string.IsNullOrEmpty(setup.memo) ? setup.when.Label : setup.memo)}");
                    setup.Execute();
                }
            }
            finally
            {
                depth--;
            }
        }

        List<EventSetup> CollectSetups()
        {
            var list = new List<EventSetup>();
            foreach (StateEventData data in globalData)
            {
                if (data == null) continue;
                foreach (EventSetup setup in data.setups) if (setup != null) list.Add(setup);
            }
            foreach (SceneEvents events in sceneEvents)
            {
                if (events == null) continue;
                foreach (EventSetup setup in events.AllSetups()) if (setup != null) list.Add(setup);
            }
            return list;
        }

        // ---- 確認用 ----

#if UNITY_EDITOR
        // インスペクターのチェックは OnValidate の中で実行すると不具合が出やすいので、次のフレームで拾う
        void Update()
        {
            if (testSetFlag)
            {
                testSetFlag = false;
                if (!debugFlag.Apply()) Debug.LogWarning($"[Event] フラグ「{debugFlag.Label}」を設定できません（値まで選んでください）");
            }

            foreach (EventSetup setup in CollectSetups())
            {
                if (!setup.testPlay) continue;
                setup.testPlay = false;
                setup.Execute();
            }
        }
#endif
    }
}
