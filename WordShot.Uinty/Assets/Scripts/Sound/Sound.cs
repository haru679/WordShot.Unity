namespace WordShot.Sounds
{
    /// <summary>
    /// 音を鳴らすときの窓口。どのスクリプトからでも Sound.Play(cue) のように呼べる。
    /// </summary>
    /// <example>
    /// [SerializeField] SoundCue hitSound;   // インスペクタで SoundCue をつなぐ
    /// Sound.Play(hitSound);
    ///
    /// Sound.Play("boss_shamoji_attack");    // データ（JSON）に書かれた名前で鳴らす
    /// Sound.PlayBgm("bgm_boss_shamoji");
    /// </example>
    public static class Sound
    {
        /// <summary>BGM のクロスフェードにかける時間（秒）</summary>
        public const float DefaultBgmFade = 1f;

        static SoundManager Manager => SoundManager.Instance;

        /// <summary>音量の設定。設定画面から使う</summary>
        public static SoundVolumes Volumes => Manager.Volumes;

        public static bool IsPaused => Manager.IsPaused;

        /// <summary>効果音・打鍵音・UI の音を鳴らす。BGM の SoundCue なら PlayBgm と同じ</summary>
        public static void Play(SoundCue cue) => Manager.Play(cue);

        /// <summary>Resources/Sounds にある SoundCue を、ファイル名で鳴らす</summary>
        public static void Play(string cueName)
        {
            SoundCue cue = Manager.FindCue(cueName);
            if (cue != null) Manager.Play(cue);
        }

        public static void Stop(SoundCue cue) => Manager.Stop(cue);

        public static void StopAllSe() => Manager.StopAllSe();

        /// <summary>BGM を切り替える。同じ曲が流れていれば、そのまま続ける</summary>
        public static void PlayBgm(SoundCue cue, float fadeTime = DefaultBgmFade) => Manager.PlayBgm(cue, fadeTime);

        public static void PlayBgm(string cueName, float fadeTime = DefaultBgmFade)
        {
            SoundCue cue = Manager.FindCue(cueName);
            if (cue != null) Manager.PlayBgm(cue, fadeTime);
        }

        public static void StopBgm(float fadeTime = DefaultBgmFade) => Manager.StopBgm(fadeTime);

        /// <summary>ポーズ画面を開くときに true、閉じるときに false で呼ぶ</summary>
        public static void SetPaused(bool paused) => Manager.SetPaused(paused);
    }
}
