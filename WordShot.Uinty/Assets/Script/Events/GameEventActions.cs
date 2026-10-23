using System;
using UnityEngine;
using UnityEngine.Events;
using WordShot.Sounds;

namespace WordShot.Events
{
    /// <summary>
    /// 条件に合ったときに行う処理。インスペクターのプルダウンで種類を選ぶ。
    /// </summary>
    /// <remarks>
    /// シーンの中のオブジェクトを指定する処理（UnityEvent・Animator）は、
    /// シーンに置く SceneEvents の中でだけ使える（データファイルからはシーンの物を指定できないため）。
    /// </remarks>
    [Serializable]
    public abstract class GameEventAction
    {
        public abstract void Execute();
    }

    // ---- 音 ----

    [Serializable, MenuName("音/効果音を鳴らす")]
    public class PlaySoundAction : GameEventAction
    {
        [Tooltip("鳴らす音。空なら下の名前で探す")]
        public SoundCue cue;
        [Tooltip("Resources/Sounds にある SoundCue の名前")]
        public string cueName;

        public override void Execute()
        {
            if (cue != null) Sound.Play(cue);
            else if (!string.IsNullOrEmpty(cueName)) Sound.Play(cueName);
        }
    }

    [Serializable, MenuName("音/BGMを切り替える")]
    public class PlayBgmAction : GameEventAction
    {
        public SoundCue cue;
        public string cueName;
        [Min(0f)] public float fadeTime = Sound.DefaultBgmFade;

        public override void Execute()
        {
            if (cue != null) Sound.PlayBgm(cue, fadeTime);
            else if (!string.IsNullOrEmpty(cueName)) Sound.PlayBgm(cueName, fadeTime);
        }
    }

    [Serializable, MenuName("音/BGMを止める")]
    public class StopBgmAction : GameEventAction
    {
        [Min(0f)] public float fadeTime = Sound.DefaultBgmFade;
        public override void Execute() => Sound.StopBgm(fadeTime);
    }

    [Serializable, MenuName("音/効果音をすべて止める")]
    public class StopAllSeAction : GameEventAction
    {
        public override void Execute() => Sound.StopAllSe();
    }

    // ---- フラグ ----

    [Serializable, MenuName("フラグ/フラグを設定する")]
    public class SetStateAction : GameEventAction
    {
        [Tooltip("設定するフラグ（「どれでも」は選べない）")]
        [NoAnyFlag] public FlagRef state;

        public override void Execute()
        {
            if (!state.Apply()) Debug.LogWarning($"[Event] フラグ「{state.Label}」を設定できません");
        }
    }

    // ---- シーンの物を動かす（SceneEvents の中で使う）----

    [Serializable, MenuName("シーン/UnityEventを呼ぶ")]
    public class UnityEventAction : GameEventAction
    {
        [Tooltip("表示・非表示の切り替えや、コンポーネントの関数呼び出しなど")]
        public UnityEvent onExecute = new UnityEvent();
        public override void Execute() => onExecute?.Invoke();
    }

    [Serializable, MenuName("シーン/Animatorのトリガー")]
    public class AnimatorTriggerAction : GameEventAction
    {
        public Animator animator;
        public string trigger;

        public override void Execute()
        {
            if (animator != null && !string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
        }
    }

    [Serializable, MenuName("シーン/表示・非表示")]
    public class SetActiveAction : GameEventAction
    {
        public GameObject target;
        public bool active = true;

        public override void Execute()
        {
            if (target != null) target.SetActive(active);
        }
    }

    // ---- 確認用 ----

    [Serializable, MenuName("確認用/ログを出す")]
    public class DebugLogAction : GameEventAction
    {
        public string message = "イベントが呼ばれました";
        public override void Execute() => Debug.Log($"[Event] {message}");
    }

    /// <summary>
    /// インスペクターのプルダウンに出す名前（"音/効果音を鳴らす" のように / で階層にできる）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MenuNameAttribute : Attribute
    {
        public string Path { get; }
        public MenuNameAttribute(string path) => Path = path;
    }

    /// <summary>FlagRef の欄で「どれでも」を選べないようにする</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NoAnyFlagAttribute : PropertyAttribute { }
}
