using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordShot.Data
{
    /// <summary>
    /// 押すたびに、次の言語に切り替えるボタン。「Language : 日本語」のように今の言語を出す。
    /// </summary>
    /// <remarks>
    /// 言語の並びは、翻訳ファイルがある言語（Localization.AvailableLanguages）。言語を足せば、ここにも自動で出る。
    /// 「Language」はどの言語の人にも見つけやすいよう、英語のまま出す。
    /// </remarks>
    [RequireComponent(typeof(Button))]
    public class LanguageButton : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Next);
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

        void Next()
        {
            var languages = Localization.AvailableLanguages;
            int index = languages.ToList().FindIndex(l => l.Code == GameLanguage.Current);
            GameLanguage.Set(languages[(index + 1) % languages.Count].Code);
        }

        void Refresh()
        {
            LanguageInfo current = Localization.AvailableLanguages.FirstOrDefault(l => l.Code == GameLanguage.Current);
            label.text = $"Language : {current?.DisplayName ?? GameLanguage.Current}";
        }
    }
}
