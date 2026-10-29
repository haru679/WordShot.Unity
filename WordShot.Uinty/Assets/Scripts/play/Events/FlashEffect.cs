using System;
using System.Collections;
using UnityEngine;

namespace WordShot.Events
{
    /// <summary>
    /// 点滅の演出（撃破・被ダメージなど）。条件を設定しておくと、フラグが変わったときに自動で点滅する。
    /// UnityEvent から Play() を呼んでもよい。
    /// </summary>
    public class FlashEffect : MonoBehaviour
    {
        public enum FlashMode
        {
            /// <summary>表示・非表示を繰り返す</summary>
            Blink,
            /// <summary>色を変える（SpriteRenderer の color に掛け合わせる）</summary>
            Tint,
        }

        public enum AfterFlash { Keep, Deactivate, Destroy }

        [Header("いつ点滅するか（空なら Play() を呼んだときだけ）")]
        [SerializeField] FlagRef playWhen;

        [Header("点滅の設定")]
        [SerializeField] FlashMode mode = FlashMode.Blink;
        [SerializeField] Color tintColor = new Color(1f, 0.4f, 0.4f);
        [Tooltip("1回の点滅にかかる時間（秒）")]
        [SerializeField, Min(0.01f)] float flashDuration = 0.1f;
        [SerializeField, Min(1)] int flashCount = 5;
        [Tooltip("点滅し終わったあと")]
        [SerializeField] AfterFlash after = AfterFlash.Keep;

        [Tooltip("点滅させる絵。空なら自分と子から探す")]
        [SerializeField] SpriteRenderer[] targets;

        Color[] originalColors;
        Coroutine running;

        void Awake()
        {
            if (targets == null || targets.Length == 0) targets = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++) originalColors[i] = targets[i] != null ? targets[i].color : Color.white;
        }

        void OnEnable() => GameFlags.StateChanged += OnStateChanged;

        void OnDisable()
        {
            GameFlags.StateChanged -= OnStateChanged;
            if (running != null) Restore();
            running = null;
        }

        void OnStateChanged(Type changedType)
        {
            if (playWhen.IsTriggeredBy(changedType) && playWhen.IsCurrent()) Play();
        }

        public void Play()
        {
            if (running != null || !isActiveAndEnabled) return;
            running = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            for (int i = 0; i < flashCount; i++)
            {
                Apply(on: true);
                yield return new WaitForSeconds(flashDuration);
                Apply(on: false);
                yield return new WaitForSeconds(flashDuration);
            }
            running = null;

            if (after == AfterFlash.Destroy) Destroy(gameObject);
            else if (after == AfterFlash.Deactivate) gameObject.SetActive(false);
        }

        void Apply(bool on)
        {
            if (!on)
            {
                Restore();
                return;
            }
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                if (mode == FlashMode.Blink) targets[i].enabled = false;
                else targets[i].color = originalColors[i] * tintColor;
            }
        }

        void Restore()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                targets[i].enabled = true;
                targets[i].color = originalColors[i];
            }
        }
    }
}
