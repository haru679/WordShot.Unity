using System;
using System.Collections.Generic;
using System.Linq;

namespace WordShot.Data
{
    /// <summary>
    /// ボス1体ぶんのデータ。Assets/Data/Resources/Bosses/（ボスの名前）.json に書く。
    /// </summary>
    /// <remarks>
    /// JSON の項目名は、ここの変数名と同じにする（例："hp": 100）。
    /// 項目を足すときは、ここに変数を足すだけでよい（今あるファイルはそのまま読める）。
    /// 項目の名前や意味を変えるときは CurrentVersion を上げ、古いファイルを直す。
    /// ボス共通の項目（名前・画像・BGM・攻撃の言葉）と、難易度ごとの項目（HP・速さ・妨害・お題）に分けている。
    /// </remarks>
    [Serializable]
    public class BossData
    {
        /// <summary>今のデータの形の番号。2 で難易度ごとの値（difficulties）を足した</summary>
        public const int CurrentVersion = 2;

        public int version;

        /// <summary>ボスを見分ける名前（半角英数字）。ファイル名と同じにする。例：shamoji_neko</summary>
        public string id;

        /// <summary>画面に出す名前（日本語）。英語版では翻訳ファイルから引く。例：しゃもじ猫</summary>
        public string name;

        /// <summary>ボスの画像の名前（Resources/Bosses/ の画像。まだなければ空でよい）</summary>
        public string sprite;

        /// <summary>このボスの戦闘で流す BGM の名前（Resources/Sounds/ の SoundCue。なければ空でよい）</summary>
        public string bgm;

        /// <summary>攻撃の種類と、攻撃のときに出す言葉（どの難易度でも同じ）</summary>
        public AttackData[] attacks = Array.Empty<AttackData>();

        /// <summary>難易度ごとの値。easy / normal / hard を1つずつ書く</summary>
        public BossDifficultyData[] difficulties = Array.Empty<BossDifficultyData>();

        /// <summary>今の言語での名前（英語版なら翻訳。翻訳がなければ日本語のまま）</summary>
        public string DisplayName => Localization.TranslateOrSame(name);

        /// <summary>全部の難易度のお題（翻訳のチェックなどに使う）</summary>
        public IEnumerable<WordData> AllWords =>
            difficulties.Where(d => d?.words != null).SelectMany(d => d.words).Where(w => w != null);

        /// <summary>その難易度の値。書かれていなければ null</summary>
        public BossDifficultyData Get(Difficulty difficulty)
        {
            foreach (BossDifficultyData data in difficulties)
            {
                if (data != null && data.level == difficulty.ToKey()) return data;
            }
            return null;
        }
    }

    /// <summary>
    /// ボスの、難易度1つぶんの値（企画書「難易度とプレイ時間」：単語の長さ・攻撃の速さ・妨害が変わる）。
    /// </summary>
    [Serializable]
    public class BossDifficultyData
    {
        /// <summary>どの難易度か（easy / normal / hard）</summary>
        public string level;

        public int hp = 100;

        /// <summary>攻撃の間隔（秒）。小さいほど攻撃が多い</summary>
        public float attackInterval = 2f;

        /// <summary>攻撃（弾など）の速さの倍率。1 = ふつう、1.3 = 3割速い</summary>
        public float attackSpeed = 1f;

        /// <summary>タイピングへの妨害の強さ（none / normal / strong）</summary>
        public string interference = "none";

        /// <summary>この難易度で出すお題。かんたんは短い単語、むずかしいは長い単語</summary>
        public WordData[] words = Array.Empty<WordData>();

        public Difficulty Difficulty => DifficultyExtensions.TryParse(level, out Difficulty value) ? value : Difficulty.Easy;

        public Interference Interference => DifficultyExtensions.TryParse(interference, out Interference value) ? value : Interference.None;
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
