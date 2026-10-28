using System;
using System.Collections.Generic;
using System.Reflection;

namespace WordShot.Events
{
    /// <summary>
    /// enum に付けると、フラグの種類として登録される。どのファイルに書いてもよい。
    /// インスペクターの「いつ」のメニューに「名前/値」で自動で並ぶ。
    /// </summary>
    /// <example>
    /// [FlagGroup("ギミック")]
    /// public enum GimmickState { Start, Lock, Unlock }
    ///
    /// GameFlags.Set(GimmickState.Lock);   // 使う側はこれだけ
    /// </example>
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public sealed class FlagGroupAttribute : Attribute
    {
        /// <summary>メニューに出す名前（"ボス" や "ステージ/ギミック" のように / で階層にできる）</summary>
        public string Label { get; }

        /// <summary>メニューに並べる順（小さいほど上）</summary>
        public int Order { get; set; }

        public FlagGroupAttribute(string label) => Label = label;
    }

    /// <summary>
    /// [FlagGroup] が付いた enum の一覧。最初に使ったときに1回だけ探す。
    /// </summary>
    public static class FlagRegistry
    {
        static Dictionary<string, Type> byFullName;
        static List<Type> groups;

        /// <summary>登録されている種類（並び順どおり）</summary>
        public static IReadOnlyList<Type> Groups
        {
            get { Ensure(); return groups; }
        }

        public static Type Find(string fullName)
        {
            Ensure();
            return !string.IsNullOrEmpty(fullName) && byFullName.TryGetValue(fullName, out Type type) ? type : null;
        }

        /// <summary>"BossState" のような型名か、"ボス" のようなメニュー名から探す</summary>
        public static Type FindByName(string name)
        {
            Ensure();
            foreach (Type type in groups)
            {
                if (type.Name == name || Label(type) == name) return type;
            }
            return null;
        }

        public static string Label(Type type)
        {
            var attribute = type?.GetCustomAttribute<FlagGroupAttribute>();
            return attribute != null && !string.IsNullOrEmpty(attribute.Label) ? attribute.Label : type?.Name;
        }

        static int Order(Type type) => type.GetCustomAttribute<FlagGroupAttribute>()?.Order ?? 0;

        static void Ensure()
        {
            if (byFullName != null) return;

            byFullName = new Dictionary<string, Type>();
            groups = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Unity や .NET 本体の中は探さない（時間がかかるだけなので）
                string name = assembly.GetName().Name;
                if (name.StartsWith("Unity") || name.StartsWith("System") || name.StartsWith("Mono") || name == "mscorlib" || name == "netstandard") continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (Type type in types)
                {
                    if (type == null || !type.IsEnum || !type.IsDefined(typeof(FlagGroupAttribute), false)) continue;
                    byFullName[type.FullName] = type;
                    groups.Add(type);
                }
            }
            groups.Sort((a, b) => Order(a) != Order(b) ? Order(a).CompareTo(Order(b)) : string.CompareOrdinal(Label(a), Label(b)));
        }
    }

    /// <summary>
    /// インスペクターで選ぶ「フラグの種類と値」。名前で保存するので、enum の値の順番を変えてもずれない。
    /// 値が空なら「その種類のどれでも」。
    /// </summary>
    [Serializable]
    public struct FlagRef
    {
        public string group;   // enum の完全な型名
        public string value;   // enum の値の名前（空 = どれでも）

        public bool IsEmpty => string.IsNullOrEmpty(group);

        public Type GroupType => FlagRegistry.Find(group);

        /// <summary>この種類のフラグが変わったときか</summary>
        public bool IsTriggeredBy(Type changedType) => !IsEmpty && changedType != null && changedType.FullName == group;

        /// <summary>今のフラグがこの値か（値が空なら、その種類が1回でも設定されていれば true）</summary>
        public bool IsCurrent()
        {
            Type type = GroupType;
            if (type == null) return false;

            Enum current = GameFlags.Get(type);
            if (current == null) return false;
            return string.IsNullOrEmpty(value) || current.ToString() == value;
        }

        /// <summary>この値にフラグを設定する（値が空なら何もしない）</summary>
        public bool Apply()
        {
            Type type = GroupType;
            if (type == null || string.IsNullOrEmpty(value) || !Enum.IsDefined(type, value)) return false;
            GameFlags.Set((Enum)Enum.Parse(type, value));
            return true;
        }

        public string Label
        {
            get
            {
                if (IsEmpty) return "（未選択）";
                Type type = GroupType;
                string groupLabel = type != null ? FlagRegistry.Label(type) : $"見つからない:{group}";
                return $"{groupLabel}/{(string.IsNullOrEmpty(value) ? "どれでも" : value)}";
            }
        }

        public override string ToString() => Label;
    }
}
