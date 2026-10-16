using System;
using UnityEngine;

namespace WordShot.Sounds
{
    /// <summary>
    /// 音量の設定（スライダー4本とミュート）。PlayerPrefs に保存し、次に起動したときも覚えておく。
    /// </summary>
    public class SoundVolumes
    {
        const string KeyPrefix = "Sound.";
        const float DefaultVolume = 0.8f;

        static readonly VolumeChannel[] Channels = (VolumeChannel[])Enum.GetValues(typeof(VolumeChannel));

        readonly float[] volumes = new float[Channels.Length];
        readonly bool[] mutes = new bool[Channels.Length];

        /// <summary>音量かミュートが変わったとき</summary>
        public event Action Changed;

        public float GetVolume(VolumeChannel channel) => volumes[(int)channel];

        public void SetVolume(VolumeChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(volumes[(int)channel], value)) return;

            volumes[(int)channel] = value;
            Changed?.Invoke();
        }

        public bool IsMuted(VolumeChannel channel) => mutes[(int)channel];

        public void SetMuted(VolumeChannel channel, bool muted)
        {
            if (mutes[(int)channel] == muted) return;

            mutes[(int)channel] = muted;
            Changed?.Invoke();
        }

        /// <summary>
        /// 実際に AudioSource にかける音量（全体 × 種類ごと）
        /// </summary>
        public float GetEffective(VolumeChannel channel)
        {
            float master = ToAmplitude(VolumeChannel.Master);
            return channel == VolumeChannel.Master ? master : master * ToAmplitude(channel);
        }

        // スライダーの値をそのまま使うと、半分にしても小さくなった気がしない。
        // 人の耳の感じ方に近づけるため2乗する（スライダー 50% → 実際の音量 25%）
        float ToAmplitude(VolumeChannel channel)
        {
            if (IsMuted(channel)) return 0f;
            float value = volumes[(int)channel];
            return value * value;
        }

        public void Load()
        {
            foreach (VolumeChannel channel in Channels)
            {
                volumes[(int)channel] = PlayerPrefs.GetFloat(Key("Volume", channel), DefaultVolume);
                mutes[(int)channel] = PlayerPrefs.GetInt(Key("Mute", channel), 0) == 1;
            }
            Changed?.Invoke();
        }

        public void Save()
        {
            foreach (VolumeChannel channel in Channels)
            {
                PlayerPrefs.SetFloat(Key("Volume", channel), volumes[(int)channel]);
                PlayerPrefs.SetInt(Key("Mute", channel), mutes[(int)channel] ? 1 : 0);
            }
            PlayerPrefs.Save();
        }

        static string Key(string kind, VolumeChannel channel) => $"{KeyPrefix}{kind}.{channel}";
    }
}
