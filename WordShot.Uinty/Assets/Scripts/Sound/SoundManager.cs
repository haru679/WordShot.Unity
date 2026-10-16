using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WordShot.Sounds
{
    /// <summary>
    /// 音を鳴らす本体。シーンをまたいで1つだけ存在し、再生すると自動で作られる。
    /// </summary>
    /// <remarks>
    /// 他のスクリプトからは、窓口の Sound クラス（Sound.Play など）を通して使う。
    /// </remarks>
    public class SoundManager : MonoBehaviour
    {
        const int VoiceCount = 24;            // 同時に鳴らせる効果音の数
        const string CueFolder = "Sounds";    // Resources/Sounds にある SoundCue は名前で鳴らせる
        const float DuckFadeTime = 0.15f;     // BGM を下げる・戻すのにかける時間

        public static SoundManager Instance { get; private set; }

        public SoundVolumes Volumes { get; } = new SoundVolumes();

        public bool IsPaused { get; private set; }

        // 効果音を鳴らす部品。毎回作らず、決まった数を使い回す
        class Voice
        {
            public AudioSource Source;
            public SoundCue Cue;
            public float BaseVolume;   // 音ごとの音量（揺らぎ込み）。これに設定の音量をかける
            public float StartTime;
        }

        // BGM を鳴らす部品。クロスフェードのため2つを交互に使う
        class BgmTrack
        {
            public AudioSource Source;
            public SoundCue Cue;
            public float BaseVolume;
            public float Fade;         // 0〜1。フェードの進み具合
            public float FadeTarget;
            public float FadeSpeed;    // 1秒あたりに Fade が変わる量
        }

        readonly List<Voice> voices = new List<Voice>();
        readonly BgmTrack[] bgmTracks = new BgmTrack[2];
        int currentBgm;

        readonly Dictionary<string, SoundCue> cuesByName = new Dictionary<string, SoundCue>();
        readonly Dictionary<SoundCue, float> lastPlayTimes = new Dictionary<SoundCue, float>();
        readonly HashSet<string> warnedKeys = new HashSet<string>();

        float duckAmount;
        float duckUntil;
        float duckFactor = 1f;

        // このプロジェクトはドメインのリロードが無効なので、static の値は再生ごとに必ず上書きする
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateInstance()
        {
            var go = new GameObject(nameof(SoundManager));
            Instance = go.AddComponent<SoundManager>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            for (int i = 0; i < VoiceCount; i++)
            {
                voices.Add(new Voice { Source = CreateSource() });
            }
            for (int i = 0; i < bgmTracks.Length; i++)
            {
                bgmTracks[i] = new BgmTrack { Source = CreateSource() };
                bgmTracks[i].Source.ignoreListenerPause = true;   // BGM は一時停止中も流す
            }

            LoadCues();
            Volumes.Load();
            Volumes.Changed += ApplyVolumes;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            Volumes.Changed -= ApplyVolumes;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnApplicationQuit() => Volumes.Save();

        AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // 2D ゲームなので、位置による音の変化はつけない
            return source;
        }

        void LoadCues()
        {
            foreach (SoundCue cue in Resources.LoadAll<SoundCue>(CueFolder))
            {
                if (cuesByName.ContainsKey(cue.name))
                {
                    Debug.LogWarning($"[Sound] 「{cue.name}」という名前の SoundCue が2つあります。片方は名前で鳴らせません");
                    continue;
                }
                cuesByName.Add(cue.name, cue);
            }
        }

        // ---- 効果音 ----

        public SoundCue FindCue(string cueName)
        {
            if (cuesByName.TryGetValue(cueName, out SoundCue cue)) return cue;

            WarnOnce($"name:{cueName}", $"[Sound] 「{cueName}」という SoundCue が Resources/{CueFolder} にありません");
            return null;
        }

        public void Play(SoundCue cue)
        {
            if (cue == null)
            {
                WarnOnce("null", "[Sound] 鳴らす SoundCue が設定されていません");
                return;
            }
            if (cue.Category == SoundCategory.BGM)
            {
                PlayBgm(cue, Sound.DefaultBgmFade);
                return;
            }

            float now = Time.unscaledTime;
            if (lastPlayTimes.TryGetValue(cue, out float lastTime) && now - lastTime < cue.MinInterval) return;

            AudioClip clip = cue.PickClip();
            if (clip == null)
            {
                WarnOnce($"clip:{cue.name}", $"[Sound] 「{cue.name}」に音源が入っていません");
                return;
            }
            lastPlayTimes[cue] = now;

            if (cue.MaxInstances > 0) LimitInstances(cue, cue.MaxInstances - 1);

            Voice voice = GetFreeVoice();
            voice.Cue = cue;
            voice.BaseVolume = cue.RandomVolume();
            voice.StartTime = now;

            AudioSource source = voice.Source;
            source.clip = clip;
            source.pitch = cue.RandomPitch();
            source.loop = cue.Loop;
            source.ignoreListenerPause = cue.Category == SoundCategory.UI;   // UI の音だけは一時停止中も鳴らす
            source.volume = voice.BaseVolume * Volumes.GetEffective(cue.Category.ToChannel());
            source.Play();

            if (cue.BgmDuck > 0f && !cue.Loop)
            {
                duckAmount = Mathf.Max(duckAmount, cue.BgmDuck);
                duckUntil = Mathf.Max(duckUntil, now + clip.length / source.pitch);
            }
        }

        /// <summary>この音で鳴っているものを全部止める</summary>
        public void Stop(SoundCue cue)
        {
            if (cue == null) return;

            foreach (Voice voice in voices)
            {
                if (voice.Cue == cue) voice.Source.Stop();
            }
            if (bgmTracks[currentBgm].Cue == cue) StopBgm(Sound.DefaultBgmFade);
        }

        public void StopAllSe()
        {
            foreach (Voice voice in voices) voice.Source.Stop();
        }

        // 同じ音が allowed 個より多く鳴っていたら、古いものから止める
        void LimitInstances(SoundCue cue, int allowed)
        {
            var playing = new List<Voice>();
            foreach (Voice voice in voices)
            {
                if (voice.Cue == cue && voice.Source.isPlaying) playing.Add(voice);
            }
            playing.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            for (int i = 0; i < playing.Count - allowed; i++)
            {
                playing[i].Source.Stop();
            }
        }

        // 空いている部品を探す。全部使っていたら、一番古い音を止めて使う
        Voice GetFreeVoice()
        {
            Voice oldest = voices[0];
            foreach (Voice voice in voices)
            {
                if (!voice.Source.isPlaying) return voice;
                if (voice.StartTime < oldest.StartTime) oldest = voice;
            }
            oldest.Source.Stop();
            return oldest;
        }

        // ---- BGM ----

        public void PlayBgm(SoundCue cue, float fadeTime)
        {
            if (cue == null)
            {
                WarnOnce("null", "[Sound] 鳴らす SoundCue が設定されていません");
                return;
            }

            BgmTrack current = bgmTracks[currentBgm];
            // 同じ曲が流れていれば、最初からにせずそのまま続ける
            if (current.Cue == cue && current.FadeTarget > 0f && current.Source.isPlaying) return;

            AudioClip clip = cue.PickClip();
            if (clip == null)
            {
                WarnOnce($"clip:{cue.name}", $"[Sound] 「{cue.name}」に音源が入っていません");
                return;
            }

            // 今の曲を下げながら、もう片方で次の曲を上げる（クロスフェード）
            FadeTo(current, 0f, fadeTime);

            currentBgm = 1 - currentBgm;
            BgmTrack next = bgmTracks[currentBgm];
            next.Source.Stop();
            next.Source.clip = clip;
            next.Source.pitch = cue.RandomPitch();
            next.Source.loop = cue.Loop;
            next.Cue = cue;
            next.BaseVolume = cue.RandomVolume();
            next.Fade = 0f;
            FadeTo(next, 1f, fadeTime);
            ApplyBgmVolume(next);
            next.Source.Play();
        }

        public void StopBgm(float fadeTime) => FadeTo(bgmTracks[currentBgm], 0f, fadeTime);

        static void FadeTo(BgmTrack track, float target, float fadeTime)
        {
            track.FadeTarget = target;
            if (fadeTime > 0f)
            {
                track.FadeSpeed = 1f / fadeTime;
            }
            else
            {
                track.Fade = target;   // すぐに切り替える
                track.FadeSpeed = 0f;
            }
        }

        void ApplyBgmVolume(BgmTrack track)
        {
            track.Source.volume = track.BaseVolume * track.Fade * duckFactor * Volumes.GetEffective(VolumeChannel.BGM);
        }

        // ---- 毎フレームの処理（フェードと BGM を下げる処理）----

        void Update()
        {
            // 一時停止（timeScale = 0）中も動くよう、unscaled の時間を使う
            float deltaTime = Time.unscaledDeltaTime;

            if (Time.unscaledTime >= duckUntil) duckAmount = 0f;
            duckFactor = Mathf.MoveTowards(duckFactor, 1f - duckAmount, deltaTime / DuckFadeTime);

            foreach (BgmTrack track in bgmTracks)
            {
                if (track.Cue == null) continue;

                track.Fade = Mathf.MoveTowards(track.Fade, track.FadeTarget, track.FadeSpeed * deltaTime);
                if (track.Fade <= 0f && track.FadeTarget <= 0f)
                {
                    track.Source.Stop();
                    track.Cue = null;
                    continue;
                }
                ApplyBgmVolume(track);
            }
        }

        // 設定画面で音量が変わったら、鳴っている効果音にもすぐ反映する（BGM は Update で毎フレーム反映）
        void ApplyVolumes()
        {
            foreach (Voice voice in voices)
            {
                if (voice.Cue == null) continue;
                voice.Source.volume = voice.BaseVolume * Volumes.GetEffective(voice.Cue.Category.ToChannel());
            }
        }

        // ---- 一時停止 ----

        /// <summary>
        /// 一時停止中は、効果音・打鍵音と、シーンに置かれた AudioSource が止まる。BGM と UI の音は鳴り続ける。
        /// </summary>
        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            AudioListener.pause = paused;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // GameFlow がシーン切り替えで timeScale を戻すのに合わせて、一時停止も解除する
            if (IsPaused) SetPaused(false);

            // 前のシーンのループ音（溜め攻撃の音など）が鳴りっぱなしにならないよう止める
            foreach (Voice voice in voices)
            {
                if (voice.Source.loop) voice.Source.Stop();
            }
        }

        // 音源がまだないときなどに、同じ警告でログが埋まらないよう1回だけ出す
        void WarnOnce(string key, string message)
        {
            if (warnedKeys.Add(key)) Debug.LogWarning(message);
        }
    }
}
