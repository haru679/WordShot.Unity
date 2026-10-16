using UnityEngine;

namespace WordShot.Sounds
{
    /// <summary>
    /// シーンに置くと、そのシーンが始まったときに BGM を流す。
    /// 前のシーンの曲からは、クロスフェードでつながる。
    /// </summary>
    /// <remarks>
    /// 曲を空にしておくと、BGM を止める（リザルト画面を無音にしたいときなど）。
    /// ボスごとに曲を変えるときは、これを使わずデータから Sound.PlayBgm("名前") で流す。
    /// </remarks>
    public class SceneBgm : MonoBehaviour
    {
        [SerializeField] SoundCue bgm;
        [SerializeField, Min(0f)] float fadeTime = Sound.DefaultBgmFade;

        void Start()
        {
            if (bgm != null) Sound.PlayBgm(bgm, fadeTime);
            else Sound.StopBgm(fadeTime);
        }
    }
}
