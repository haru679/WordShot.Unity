using UnityEngine;
using WordShot.Sounds;

namespace WordShot.Events
{
    /// <summary>
    /// アニメーションイベントから、音やフラグを呼ぶための中継。Animator と同じオブジェクトに付ける。
    /// </summary>
    /// <remarks>
    /// アニメーションのイベントで関数を選び、文字列の欄に次のように書く。
    /// ・PlaySound … SoundCue の名前（Resources/Sounds にあるもの）
    /// ・SetState  … "BossState.Attack" や "ボス.Attack" のように「種類.値」
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public class AnimationEventRelay : MonoBehaviour
    {
        public void PlaySound(string cueName)
        {
            if (!string.IsNullOrEmpty(cueName)) Sound.Play(cueName);
        }

        /// <summary>イベントの Object 欄に SoundCue を入れて鳴らす</summary>
        public void PlaySoundCue(Object cue)
        {
            if (cue is SoundCue soundCue) Sound.Play(soundCue);
        }

        public void SetState(string text)
        {
            if (!GameFlags.SetFromString(text)) Debug.LogWarning($"[Event] 「{text}」は状態として読めません（例：BossState.Attack）", this);
        }
    }
}
