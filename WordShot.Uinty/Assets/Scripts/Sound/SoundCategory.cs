namespace WordShot.Sounds
{
    /// <summary>
    /// 音の種類。どの音量スライダーに従うか、一時停止中に止まるかが決まる。
    /// </summary>
    public enum SoundCategory
    {
        /// <summary>BGM。一時停止中も流れ続ける</summary>
        BGM,
        /// <summary>効果音（攻撃・ダメージなど）。一時停止中は止まる</summary>
        SE,
        /// <summary>打鍵音。鳴る回数が多いので、音量を別に調整できるようにしている</summary>
        Typing,
        /// <summary>メニューの音。音量は SE のスライダーに従い、一時停止中も鳴る</summary>
        UI,
    }

    /// <summary>
    /// 音量設定のスライダー1本ぶん。
    /// </summary>
    public enum VolumeChannel
    {
        Master,
        BGM,
        SE,
        Typing,
    }

    public static class SoundCategoryExtensions
    {
        /// <summary>この種類の音が、どのスライダーの音量に従うか</summary>
        public static VolumeChannel ToChannel(this SoundCategory category) => category switch
        {
            SoundCategory.BGM => VolumeChannel.BGM,
            SoundCategory.Typing => VolumeChannel.Typing,
            _ => VolumeChannel.SE,
        };
    }
}
