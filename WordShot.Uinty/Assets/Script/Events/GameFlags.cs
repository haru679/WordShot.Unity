using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordShot.Events
{
    /// <summary>
    /// フラグを1か所で管理する。フラグの種類は [FlagGroup] を付けた enum で、だれでも追加できる。
    /// 設定されると通知し、EventManager がインスペクターで組んだ処理（音・演出）を呼び出す。
    /// </summary>
    /// <example>
    /// GameFlags.Set(BossState.Attack);       // ボスが攻撃した
    /// GameFlags.Set(TypingState.KeyMiss);    // 打ち間違えた
    /// if (GameFlags.Is(PlayerState.Dead)) ...
    /// </example>
    public static class GameFlags
    {
        // 種類ごとの「今の値」
        static readonly Dictionary<Type, Enum> states = new Dictionary<Type, Enum>();

        /// <summary>フラグが設定されたとき（引数は enum の型）。KeyMiss のように何度も起きるので、同じ値でも毎回通知する</summary>
        public static event Action<Type> StateChanged;

        // このプロジェクトはドメインのリロードが無効なので、再生のたびに static の値を空にする
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            states.Clear();
            StateChanged = null;
        }

        public static void Set<T>(T value) where T : struct, Enum => Set((Enum)value);

        public static void Set(Enum value)
        {
            if (value == null) return;
            Type type = value.GetType();
            states[type] = value;
            StateChanged?.Invoke(type);
        }

        /// <summary>今の値。まだ一度も設定されていなければ enum の先頭の値</summary>
        public static T Get<T>() where T : struct, Enum
            => states.TryGetValue(typeof(T), out Enum value) ? (T)value : default;

        /// <summary>今の値。まだ一度も設定されていなければ null</summary>
        public static Enum Get(Type type) => type != null && states.TryGetValue(type, out Enum value) ? value : null;

        public static bool Is<T>(T value) where T : struct, Enum
            => states.TryGetValue(typeof(T), out Enum current) && current.Equals(value);

        // ---- プログラムから受け取る ----

        /// <summary>
        /// 指定の値になったときに handler を呼ぶ。戻り値を呼ぶと登録を外す（OnDisable などで必ず呼ぶ）
        /// </summary>
        /// <example>
        /// Action unsubscribe;
        /// void OnEnable()  => unsubscribe = GameFlags.Subscribe(PlayerState.Attack, OnPlayerAttack);
        /// void OnDisable() => unsubscribe?.Invoke();
        /// </example>
        public static Action Subscribe<T>(T value, Action handler) where T : struct, Enum
        {
            if (handler == null) return () => { };
            return Subscribe<T>(current =>
            {
                if (current.Equals(value)) handler();
            });
        }

        /// <summary>
        /// この種類のどの値になっても handler を呼ぶ（引数は新しい値）。戻り値を呼ぶと登録を外す
        /// </summary>
        public static Action Subscribe<T>(Action<T> handler) where T : struct, Enum
        {
            if (handler == null) return () => { };
            Action<Type> listener = type =>
            {
                if (type == typeof(T)) handler(Get<T>());
            };
            StateChanged += listener;
            return () => StateChanged -= listener;
        }

        /// <summary>
        /// "BossState.Attack" や "ボス.Attack" のような文字列で設定する（アニメーションイベント用）
        /// </summary>
        public static bool SetFromString(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            int dot = text.LastIndexOf('.');
            if (dot <= 0 || dot == text.Length - 1) return false;

            Type type = FlagRegistry.FindByName(text.Substring(0, dot));
            if (type == null) return false;

            string valueName = text.Substring(dot + 1);
            foreach (string name in Enum.GetNames(type))
            {
                if (!string.Equals(name, valueName, StringComparison.OrdinalIgnoreCase)) continue;
                Set((Enum)Enum.Parse(type, name));
                return true;
            }
            return false;
        }
    }
}
