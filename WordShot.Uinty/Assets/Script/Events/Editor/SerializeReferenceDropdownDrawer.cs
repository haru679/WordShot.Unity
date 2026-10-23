using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WordShot.Events;

namespace WordShot.Events.Editor
{
    /// <summary>
    /// [SerializeReference] の欄に、種類を選ぶプルダウンを出す。
    /// 「何をするか」（GameEventAction）を、インスペクターのメニューから選べるようにする。
    /// </summary>
    [CustomPropertyDrawer(typeof(GameEventAction), true)]
    public class SerializeReferenceDropdownDrawer : PropertyDrawer
    {
        const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // リストの要素をコピーすると、同じ中身を指したままになるので、別の中身に分ける
            ForceUniqueInstance(property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect buttonRect = EditorGUI.PrefixLabel(line, label);

            object value = property.managedReferenceValue;
            string buttonLabel = value == null ? "（未選択）" : DisplayName(value.GetType());
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(buttonLabel), FocusType.Keyboard))
            {
                ShowMenu(property);
            }

            if (value != null)
            {
                EditorGUI.indentLevel++;
                var rect = new Rect(position.x, line.yMax + Spacing, position.width, 0f);

                // 直下の項目だけを描く（孫の項目は PropertyField が描く）
                SerializedProperty child = property.Copy();
                SerializedProperty end = property.GetEndProperty();
                bool enterChildren = true;
                while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
                {
                    enterChildren = false;
                    rect.height = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(rect, child, true);
                    rect.y += rect.height + Spacing;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float height = EditorGUIUtility.singleLineHeight;
            if (property.managedReferenceValue == null) return height;

            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(child, true) + Spacing;
            }
            return height;
        }

        // ---- 種類の選択 ----

        static void ShowMenu(SerializedProperty property)
        {
            Type baseType = GetFieldType(property.managedReferenceFieldTypename);
            if (baseType == null) return;

            // メニューを選んだときには property が古くなっていることがあるので、パスで探し直す
            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;
            Type currentType = property.managedReferenceValue?.GetType();

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("（未選択）"), currentType == null, () => Assign(serializedObject, path, null));
            menu.AddSeparator("");

            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(type => !type.IsAbstract && !type.IsGenericType && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(DisplayName);

            foreach (Type type in types)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(DisplayName(type)), currentType == type,
                    () => Assign(serializedObject, path, Activator.CreateInstance(captured)));
            }
            menu.ShowAsContext();
        }

        static void Assign(SerializedObject serializedObject, string path, object value)
        {
            serializedObject.Update();
            SerializedProperty exact = serializedObject.FindProperty(path);
            if (exact == null) return;
            exact.managedReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        static string DisplayName(Type type)
        {
            var attribute = type.GetCustomAttribute<MenuNameAttribute>();
            return attribute != null ? attribute.Path : ObjectNames.NicifyVariableName(type.Name);
        }

        // "Assembly-CSharp WordShot.Events.GameEventAction" のような文字列から型を探す
        static Type GetFieldType(string typename)
        {
            if (string.IsNullOrEmpty(typename)) return null;
            int space = typename.IndexOf(' ');
            if (space <= 0) return null;
            try
            {
                return Assembly.Load(typename.Substring(0, space)).GetType(typename.Substring(space + 1));
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ---- コピーした要素を別の中身にする ----

        static void ForceUniqueInstance(SerializedProperty property)
        {
            if (property.managedReferenceValue == null) return;
            long id = property.managedReferenceId;

            // 同じ中身を指している欄が、自分より前にあるか探す（最初に見つかった欄はそのまま使う）
            bool sharedWithEarlier = false;
            SerializedProperty iterator = property.serializedObject.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ManagedReference) continue;
                if (iterator.managedReferenceId != id) continue;

                // 自分が最初なら、そのまま使う。前に同じものがあれば複製する
                sharedWithEarlier = iterator.propertyPath != property.propertyPath;
                break;
            }
            if (!sharedWithEarlier) return;

            object original = property.managedReferenceValue;
            object copy = JsonUtility.FromJson(JsonUtility.ToJson(original), original.GetType());
            property.managedReferenceValue = copy;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
