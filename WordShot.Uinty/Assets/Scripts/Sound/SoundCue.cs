using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WordShot.Sounds
{
    /// <summary>
    /// 音1つぶんの設定。1つの音につき1ファイル作る。
    /// </summary>
    /// <remarks>
    /// 作り方：Project ウィンドウで右クリック → Create → WordShot → Sound Cue。
    /// Resources/Sounds フォルダの中に置くと、Sound.Play("ファイル名") のように名前でも鳴らせる。
    /// </remarks>
    [CreateAssetMenu(menuName = "WordShot/Sound Cue", fileName = "NewSoundCue")]
    public class SoundCue : ScriptableObject
    {
        public enum PlayOrder
        {
            /// <summary>毎回ランダム</summary>
            Random,
            /// <summary>ランダムだが、同じ音を2回続けない</summary>
            RandomNoRepeat,
            /// <summary>上から順番</summary>
            Sequential,
        }

        [Header("音源")]
        [Tooltip("複数入れると、鳴らすたびに切り替わる")]
        [SerializeField] AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField] PlayOrder order = PlayOrder.RandomNoRepeat;
        [SerializeField] SoundCategory category = SoundCategory.SE;

        [Header("音量・ピッチ")]
        [SerializeField, Range(0f, 1f)] float volume = 1f;
        [Tooltip("鳴らすたびに、音量をこの幅で上下に揺らす")]
        [SerializeField, Range(0f, 0.5f)] float volumeVariation;
        [SerializeField, Range(0.1f, 3f)] float pitch = 1f;
        [Tooltip("鳴らすたびに、ピッチをこの幅で上下に揺らす。打鍵音に 0.05 くらい入れると自然に聞こえる")]
        [SerializeField, Range(0f, 0.5f)] float pitchVariation;

        [Header("鳴らし方")]
        [Tooltip("繰り返し鳴らす。BGM はふつうオンにする")]
        [SerializeField] bool loop;
        [Tooltip("同時に鳴らせる数。超えたら一番古いものを止める。0 なら制限なし")]
        [SerializeField, Min(0)] int maxInstances;
        [Tooltip("この秒数より短い間隔で鳴らそうとしたら、無視する")]
        [SerializeField, Min(0f)] float minInterval;
        [Tooltip("この音が鳴っている間、BGM をどれだけ下げるか（0 = 下げない、1 = 無音）")]
        [SerializeField, Range(0f, 1f)] float bgmDuck;

        [NonSerialized] int lastIndex = -1;

        public SoundCategory Category => category;
        public bool Loop => loop;
        public int MaxInstances => maxInstances;
        public float MinInterval => minInterval;
        public float BgmDuck => bgmDuck;
        public bool HasClips => clips != null && clips.Any(clip => clip != null);

        /// <summary>
        /// 次に鳴らす音源を選ぶ。音源が入っていなければ null
        /// </summary>
        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;

            int index;
            if (order == PlayOrder.Sequential)
            {
                index = (lastIndex + 1) % clips.Length;
            }
            else if (order == PlayOrder.RandomNoRepeat && clips.Length > 1 && lastIndex >= 0)
            {
                // 前回の番号から 1〜(数-1) 進める → 前回以外のどれかになる
                index = (lastIndex + Random.Range(1, clips.Length)) % clips.Length;
            }
            else
            {
                index = Random.Range(0, clips.Length);
            }

            lastIndex = index;
            return clips[index];
        }

        /// <summary>揺らぎを入れた音量</summary>
        public float RandomVolume() => Mathf.Clamp01(volume + Random.Range(-volumeVariation, volumeVariation));

        /// <summary>揺らぎを入れたピッチ</summary>
        public float RandomPitch() => Mathf.Max(0.1f, pitch + Random.Range(-pitchVariation, pitchVariation));
    }
}
