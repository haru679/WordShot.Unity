using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace WordShot.Events.Editor
{
    /// <summary>プランナー向けのメニュー（Tools → WordShot）</summary>
    public static class EventMenu
    {
        const string CommonFolder = "Assets/Data/Resources/Events";
        const string CommonAsset = CommonFolder + "/CommonEvents.asset";

        /// <summary>全シーン共通の「いつ・何をするか」を開く。なければ作る</summary>
        [MenuItem("Tools/WordShot/共通イベントを開く", priority = 0)]
        static void OpenCommonEvents()
        {
            var data = AssetDatabase.LoadAssetAtPath<StateEventData>(CommonAsset);
            if (data == null)
            {
                Directory.CreateDirectory(CommonFolder);
                AssetDatabase.Refresh();
                data = ScriptableObject.CreateInstance<StateEventData>();
                AssetDatabase.CreateAsset(data, CommonAsset);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
        }

        /// <summary>今あるフラグの種類と値をコンソールに出す（だれが何を足したか確かめる用）</summary>
        [MenuItem("Tools/WordShot/フラグの一覧を表示", priority = 1)]
        static void ListFlags()
        {
            var text = new StringBuilder("[Event] フラグの一覧\n");
            foreach (var type in FlagRegistry.Groups)
            {
                text.AppendLine($"・{FlagRegistry.Label(type)}（{type.Name}）：{string.Join(" / ", System.Enum.GetNames(type))}");
            }
            Debug.Log(text.ToString());
        }

        /// <summary>このシーンに SceneEvents を置く（もうあれば、それを選ぶ）</summary>
        [MenuItem("Tools/WordShot/このシーンにイベントを置く", priority = 2)]
        static void AddSceneEvents()
        {
            var existing = Object.FindAnyObjectByType<SceneEvents>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                return;
            }
            var go = new GameObject("SceneEvents", typeof(SceneEvents));
            Undo.RegisterCreatedObjectUndo(go, "SceneEvents を置く");
            Selection.activeObject = go;
        }
    }
}
