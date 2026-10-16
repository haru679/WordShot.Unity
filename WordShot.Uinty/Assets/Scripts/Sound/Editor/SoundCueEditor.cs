using UnityEditor;
using UnityEngine;

namespace WordShot.Sounds.EditorTools
{
    /// <summary>
    /// SoundCue のインスペクタに「試聴」ボタンを付ける。ゲームを再生しなくても、音を聞きながら調整できる。
    /// </summary>
    [CustomEditor(typeof(SoundCue))]
    public class SoundCueEditor : Editor
    {
        AudioSource previewSource;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var cue = (SoundCue)target;
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ 試聴", GUILayout.Height(28))) PlayPreview(cue);
                if (GUILayout.Button("■ 停止", GUILayout.Height(28))) StopPreview(cue);
            }

            if (!cue.HasClips)
            {
                EditorGUILayout.HelpBox("音源が入っていません。鳴らしても警告が出るだけで、何も鳴りません。", MessageType.Warning);
            }
            if (cue.Category == SoundCategory.BGM && !cue.Loop)
            {
                EditorGUILayout.HelpBox("BGM ですが Loop がオフです。ジングルのように1回だけ流す曲なら、このままで大丈夫です。", MessageType.Info);
            }

            string path = AssetDatabase.GetAssetPath(cue);
            if (path.Contains("/Resources/Sounds/"))
            {
                EditorGUILayout.HelpBox($"名前で鳴らすとき： Sound.Play(\"{cue.name}\")", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Resources/Sounds フォルダの外にあるので、名前では鳴らせません（インスペクタでつなげば鳴らせます）。",
                    MessageType.Info);
            }
        }

        void PlayPreview(SoundCue cue)
        {
            // ゲーム中は本番と同じ鳴らし方にする（音量の設定も反映される）
            if (Application.isPlaying)
            {
                Sound.Play(cue);
                return;
            }

            AudioClip clip = cue.PickClip();
            if (clip == null) return;

            if (previewSource == null)
            {
                previewSource = EditorUtility
                    .CreateGameObjectWithHideFlags("SoundCuePreview", HideFlags.HideAndDontSave, typeof(AudioSource))
                    .GetComponent<AudioSource>();
            }

            // 押すたびに揺らぎが変わるので、何度か押して聞き比べられる
            previewSource.clip = clip;
            previewSource.volume = cue.RandomVolume();
            previewSource.pitch = cue.RandomPitch();
            previewSource.loop = cue.Loop;
            previewSource.Play();
        }

        void StopPreview(SoundCue cue)
        {
            if (Application.isPlaying) Sound.Stop(cue);
            if (previewSource != null) previewSource.Stop();
        }

        void OnDisable()
        {
            if (previewSource != null) DestroyImmediate(previewSource.gameObject);
        }
    }
}
