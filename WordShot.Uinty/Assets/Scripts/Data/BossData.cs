using System;

namespace WordShot.Data
{
    /// <summary>
    /// ボス1体ぶんのデータ。Assets/Data/Resources/Bosses/（ボスの名前）.json に書く。
    /// </summary>
    /// <remarks>
    /// JSON の項目名は、ここの変数名と同じにする（例："hp": 100）。
    /// 項目を足すときは、ここに変数を足すだけでよい（今あるファイルはそのまま読める）。
    /// 項目の名前や意味を変えるときは CurrentVersion を上げ、古いファイルを直す。
    /// </remarks>
    [Serializable]
    public class BossData
    {
        /// <summary>今のデータの形の番号。JSON の "version" と比べて、古いファイルを見つける</summary>
        public const int CurrentVersion = 1;

        public int version;

        /// <summary>ボスを見分ける名前（半角英数字）。ファイル名と同じにする。例：shamoji_neko</summary>
        public string id;

        /// <summary>画面に出す名前（日本語）。英語版では翻訳ファイルから引く。例：しゃもじ猫</summary>
        public string name;

        public int hp = 100;

        /// <summary>攻撃の間隔（秒）</summary>
        public float attackInterval = 2f;

        /// <summary>ボスの画像の名前（Resources/Bosses/ の画像。まだなければ空でよい）</summary>
        public string sprite;

        /// <summary>このボスの戦闘で流す BGM の名前（Resources/Sounds/ の SoundCue。なければ空でよい）</summary>
        public string bgm;

        /// <summary>タイピングのお題</summary>
        public WordData[] words = Array.Empty<WordData>();

        /// <summary>攻撃の種類と、攻撃のときに出す言葉</summary>
        public AttackData[] attacks = Array.Empty<AttackData>();

        /// <summary>今の言語での名前（英語版なら翻訳。翻訳がなければ日本語のまま）</summary>
        public string DisplayName => Localization.TranslateOrSame(name);
    }

    /// <summary>
    /// タイピングのお題1つ。
    /// </summary>
    /// <remarks>
    /// 画面の3段表示：上の段＝ルビ（DisplayRuby）／真ん中＝表記（text）／下の段＝ローマ字（reading から作る）。
    /// 英語版でも、表記とローマ字は日本語のまま。ルビだけが翻訳に変わる（企画書 仕様② ルビ翻訳）。
    /// </remarks>
    [Serializable]
    public class WordData
    {
        /// <summary>表記（真ん中に大きく出す）。例：猫じゃらし</summary>
        public string text;

        /// <summary>読み（ひらがなだけ）。ローマ字の打ち方はここから作る。例：ねこじゃらし</summary>
        public string reading;

        /// <summary>上の段に出すルビ。日本語版なら読み、英語版なら翻訳（翻訳がなければ読み）</summary>
        public string DisplayRuby => GameLanguage.IsJapanese
            ? reading
            : Localization.Translate(text) ?? reading;

        /// <summary>
        /// タイピング（TypingGameManager）に渡す形にする。
        /// ruby には読みを入れる（ローマ字はここから作られる）。
        /// </summary>
        public TypingData ToTypingData() => new TypingData { ruby = reading, targetText = text };
    }

    /// <summary>
    /// ボスの攻撃1種類と、攻撃の前に出す言葉（企画書 仕様③ 攻撃予告）。
    /// </summary>
    [Serializable]
    public class AttackData
    {
        /// <summary>攻撃の種類。例：bullet（弾）、rush（突進）、drop（落下）</summary>
        public string type;

        /// <summary>技名。例：しゃも箒!!</summary>
        public string skillName;

        /// <summary>攻撃の直前に言うセリフ。例：ちりはきえるしゃもねぇ</summary>
        public string line;

        /// <summary>弾を撃つ瞬間に出すオノマトペ。例：ダダにゃ!!</summary>
        public string onomatopoeia;
    }
}
