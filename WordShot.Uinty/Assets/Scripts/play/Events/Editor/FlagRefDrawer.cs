using System;
using UnityEditor;
using UnityEngine;

namespace WordShot.Events.Editor
{
    /// <summary>
    /// FlagRef の欄を、「ボス/Attack」のように1回のクリックで選べるメニューにする。
    /// [FlagGroup] の付いた enum は、だれがどこに書いても自動でメニューに並ぶ。
    /// </summary>
    [CustomPropertyDrawer(typeof(FlagRef))]
    public class FlagRefDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty group = property.FindPropertyRelative(nameof(FlagRef.group));
            SerializedProperty value = property.FindPropertyRelative(nameof(FlagRef.value));
            bool allowAny = fieldInfo == null || !Attribute.IsDefined(fieldInfo, typeof(NoAnyFlagAttribute));

            EditorGUI.BeginProperty(position, label, property);
            Rect buttonRect = EditorGUI.PrefixLabel(position, label);

            var current = new FlagRef { group = group.stringValue, value = value.stringValue };
            bool missing = !current.IsEmpty && (current.GroupType == null
                || (!string.IsNullOrEmpty(current.value) && !Enum.IsDefined(current.GroupType, current.value)));

            Color oldColor = GUI.color;
            if (missing) GUI.color = new Color(1f, 0.55f, 0.55f);   // 名前が変わって見つからないものは赤く出す
            string text = missing ? "⚠ " + current.Label : current.Label;
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(text, missing ? "enum の名前が変わったか、消されています" : null), FocusType.Keyboard))
            {
                ShowMenu(property.serializedObject, property.propertyPath, current, allowAny);
            }
            GUI.color = oldColor;

            EditorGUI.EndProperty();
        }

        static void ShowMenu(SerializedObject serializedObject, string path, FlagRef current, bool allowAny)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("（未選択）"), current.IsEmpty, () => Assign(serializedObject, path, "", ""));
            menu.AddSeparator("");

            foreach (Type type in FlagRegistry.Groups)
            {
                string groupLabel = FlagRegistry.Label(type);
                string fullName = type.FullName;

                if (allowAny)
                {
                    bool on = current.group == fullName && string.IsNullOrEmpty(current.value);
                    menu.AddItem(new GUIContent($"{groupLabel}/（どれでも）"), on, () => Assign(serializedObject, path, fullName, ""));
                    menu.AddSeparator($"{groupLabel}/");
                }
                foreach (string name in Enum.GetNames(type))
                {
                    bool on = current.group == fullName && current.value == name;
                    menu.AddItem(new GUIContent($"{groupLabel}/{name}"), on, () => Assign(serializedObject, path, fullName, name));
                }
            }
            menu.ShowAsContext();
        }

        static void Assign(SerializedObject serializedObject, string path, string group, string value)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property == null) return;
            property.FindPropertyRelative(nameof(FlagRef.group)).stringValue = group;
            property.FindPropertyRelative(nameof(FlagRef.value)).stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
