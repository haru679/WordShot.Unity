using TMPro;
using UnityEngine;

namespace WordShot.Data
{
    /// <summary>
    /// 画面の文字（TextMeshPro）に付けると、今の言語の文字を出す。言語が変わると、その場で切り替わる。
    /// </summary>
    /// <remarks>
    /// 「Japanese」に日本語の文字を書いておくと、それをキーにして翻訳ファイルから引く（翻訳がなければ日本語のまま）。
    /// 翻訳は「WordShot > ボスデータ編集」の「画面の文字」タブで入力できる。
    /// プログラムで中身を変える文字（％や残り時間など）には付けない。
    /// </remarks>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("日本語の文字。これをキーにして、今の言語の文字を出す")]
        [SerializeField, TextArea] string japanese;

        TMP_Text label;

        public string Japanese
        {
            get => japanese;
            set
            {
                japanese = value;
                Refresh();
            }
        }

        void OnEnable()
        {
            GameLanguage.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameLanguage.Changed -= Refresh;
        }

        void Refresh()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            label.text = Localization.TranslateOrSame(japanese);
        }
    }
}
