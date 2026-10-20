using System.Collections.Generic;
using UnityEngine;

public static class RomajiEngine
{
    public class Syllable
    {
        public string hira;
        public List<string> romajis;
    }

    // ひらがなから音節リストを生成する
    public static List<Syllable> Parse(string hiraText)
    {
        List<Syllable> result = new List<Syllable>();
        int i = 0;

        while (i < hiraText.Length)
        {
            // 小さい「っ」の処理
            if (hiraText[i] == 'っ')
            {
                // 「っ」が最後にある場合
                if (i + 1 >= hiraText.Length)
                {
                    result.Add(new Syllable
                    {
                        hira = "っ",
                        romajis = new List<string> { "xtsu", "ltsu" }
                    });

                    i++;
                    continue;
                }

                // 「っ」の次の音節を取得する
                int nextIndex = i + 1;
                string nextKey = hiraText.Substring(nextIndex, 1);

                // 次の文字が「ゃ・ゅ・ょ」なら拗音として扱う
                if (nextIndex + 1 < hiraText.Length)
                {
                    string twoChars = hiraText.Substring(nextIndex, 2);

                    if (IsSupportedTwoChars(twoChars))
                    {
                        nextKey = twoChars;
                    }
                }

                // 次の音節のローマ字候補を取得する
                List<string> nextVariants = GenerateVariants(nextKey);
                List<string> sokuonVariants = new List<string>();

                foreach (string romaji in nextVariants)
                {
                    if (string.IsNullOrEmpty(romaji))
                    {
                        continue;
                    }

                    char first = romaji[0];

                    // 母音と「n」以外の先頭子音を重ねる
                    if (IsSokuonConsonant(first))
                    {
                        string candidate = first + romaji;

                        if (!sokuonVariants.Contains(candidate))
                        {
                            sokuonVariants.Add(candidate);
                        }
                    }
                }

                // 子音を重ねられる場合は「っ」と次の音節をまとめる
                if (sokuonVariants.Count > 0)
                {
                    result.Add(new Syllable
                    {
                        hira = "っ" + nextKey,
                        romajis = sokuonVariants
                    });

                    i = nextIndex + nextKey.Length;
                }
                else
                {
                    // 次が母音などの場合は「っ」を独立させる
                    result.Add(new Syllable
                    {
                        hira = "っ",
                        romajis = new List<string> { "xtsu", "ltsu" }
                    });

                    i++;
                }

                continue;
            }

            // 通常の音節を取得する
            string key = hiraText.Substring(i, 1);

            if (i + 1 < hiraText.Length)
            {
                string twoChars = hiraText.Substring(i, 2);

                if (IsSupportedTwoChars(twoChars))
                {
                    key = twoChars;
                }
            }

            result.Add(new Syllable
            {
                hira = key,
                romajis = GenerateVariants(key)
            });

            i += key.Length;
        }

        // 「ん」の処理は次の手順で修正する
        return result;
    }

    // ひらがな1つ（または2つ）から、一般的なローマ字のゆらぎを自動網羅するロジック
    private static List<string> GenerateVariants(string hira)
    {
        List<string> list = new List<string>();

        switch (hira)
        {
            // あ行
            case "あ": list.AddRange(new[] { "a" }); break;
            case "い": list.AddRange(new[] { "i", "yi" }); break;
            case "う": list.AddRange(new[] { "u", "wu", "whu" }); break;
            case "え": list.AddRange(new[] { "e" }); break;
            case "お": list.AddRange(new[] { "o" }); break;

            // か行
            case "か": list.AddRange(new[] { "ka", "ca" }); break;
            case "き": list.AddRange(new[] { "ki" }); break;
            case "く": list.AddRange(new[] { "ku", "cu", "qu" }); break;
            case "け": list.AddRange(new[] { "ke" }); break;
            case "こ": list.AddRange(new[] { "ko", "co" }); break;

            // さ行
            case "さ": list.AddRange(new[] { "sa" }); break;
            case "し": list.AddRange(new[] { "shi", "si", "ci" }); break;
            case "す": list.AddRange(new[] { "su" }); break;
            case "せ": list.AddRange(new[] { "se", "ce" }); break;
            case "そ": list.AddRange(new[] { "so" }); break;

            // た行
            case "た": list.AddRange(new[] { "ta" }); break;
            case "ち": list.AddRange(new[] { "chi", "ti" }); break;
            case "つ": list.AddRange(new[] { "tsu", "tu" }); break;
            case "て": list.AddRange(new[] { "te" }); break;
            case "と": list.AddRange(new[] { "to" }); break;

            // な行
            case "な": list.AddRange(new[] { "na" }); break;
            case "に": list.AddRange(new[] { "ni" }); break;
            case "ぬ": list.AddRange(new[] { "nu" }); break;
            case "ね": list.AddRange(new[] { "ne" }); break;
            case "の": list.AddRange(new[] { "no" }); break;

            // は行
            case "は": list.AddRange(new[] { "ha" }); break;
            case "ひ": list.AddRange(new[] { "hi" }); break;
            case "ふ": list.AddRange(new[] { "hu", "fu" }); break;
            case "へ": list.AddRange(new[] { "he" }); break;
            case "ほ": list.AddRange(new[] { "ho" }); break;

            // ま行
            case "ま": list.AddRange(new[] { "ma" }); break;
            case "み": list.AddRange(new[] { "mi" }); break;
            case "む": list.AddRange(new[] { "mu" }); break;
            case "め": list.AddRange(new[] { "me" }); break;
            case "も": list.AddRange(new[] { "mo" }); break;

            // や行
            case "や": list.AddRange(new[] { "ya" }); break;
            case "ゆ": list.AddRange(new[] { "yu" }); break;
            case "よ": list.AddRange(new[] { "yo" }); break;

            // ら行
            case "ら": list.AddRange(new[] { "ra" }); break;
            case "り": list.AddRange(new[] { "ri" }); break;
            case "る": list.AddRange(new[] { "ru" }); break;
            case "れ": list.AddRange(new[] { "re" }); break;
            case "ろ": list.AddRange(new[] { "ro" }); break;

            // わ行・他
            case "わ": list.AddRange(new[] { "wa" }); break;
            case "を": list.AddRange(new[] { "wo" }); break;
            case "ん": list.AddRange(new[] { "n", "nn"}); break;

            // が行
            case "が": list.AddRange(new[] { "ga" }); break;
            case "ぎ": list.AddRange(new[] { "gi" }); break;
            case "ぐ": list.AddRange(new[] { "gu" }); break;
            case "げ": list.AddRange(new[] { "ge" }); break;
            case "ご": list.AddRange(new[] { "go" }); break;

            // ざ行
            case "ざ": list.AddRange(new[] { "za" }); break;
            case "じ": list.AddRange(new[] { "zi" }); break;
            case "ず": list.AddRange(new[] { "zu" }); break;
            case "ぜ": list.AddRange(new[] { "ze" }); break;
            case "ぞ": list.AddRange(new[] { "zo" }); break;

            // だ行
            case "だ": list.AddRange(new[] { "da" }); break;
            case "ぢ": list.AddRange(new[] { "di" }); break;
            case "づ": list.AddRange(new[] { "du" }); break;
            case "で": list.AddRange(new[] { "de" }); break;
            case "ど": list.AddRange(new[] { "do" }); break;

            // ば行
            case "ば": list.AddRange(new[] { "ba" }); break;
            case "び": list.AddRange(new[] { "bi" }); break;
            case "ぶ": list.AddRange(new[] { "bu" }); break;
            case "べ": list.AddRange(new[] { "be" }); break;
            case "ぼ": list.AddRange(new[] { "bo" }); break;

            // ぱ行
            case "ぱ": list.AddRange(new[] { "pa" }); break;
            case "ぴ": list.AddRange(new[] { "pi" }); break;
            case "ぷ": list.AddRange(new[] { "pu" }); break;
            case "ぺ": list.AddRange(new[] { "pe" }); break;
            case "ぽ": list.AddRange(new[] { "po" }); break;

            // 拗音
            case "きゃ": list.AddRange(new[] { "kya", "kilya", "kixya" }); break;
            case "きゅ": list.AddRange(new[] { "kyu", "kilyu", "kixyu" }); break;
            case "きょ": list.AddRange(new[] { "kyo", "kilyo", "kixyo" }); break;
            case "ぎゃ": list.AddRange(new[] { "gya", "gilya", "gixya" }); break;
            case "ぎゅ": list.AddRange(new[] { "gyu", "gilyu", "gixyu" }); break;
            case "ぎょ": list.AddRange(new[] { "gyo", "gilyo", "gixyo" }); break;

            case "しゃ": list.AddRange(new[] { "sha", "sya", "silya", "sixya" }); break;
            case "しゅ": list.AddRange(new[] { "shu", "syu", "silyu", "sixyu" }); break;
            case "しょ": list.AddRange(new[] { "sho", "syo", "silyo", "sixyo" }); break;
            case "じゃ": list.AddRange(new[] { "ja", "jya", "jilya", "jixya" }); break;
            case "じゅ": list.AddRange(new[] { "ju", "jyu", "jilyu", "jixyu" }); break;
            case "じょ": list.AddRange(new[] { "jo", "jyo", "jilyo", "jixyo" }); break;

            case "ちゃ": list.AddRange(new[] { "tya", "cya", "cha", "tilya", "tixya" }); break;
            case "ちゅ": list.AddRange(new[] { "tyu", "cyu", "chu", "tilyu", "tixyu" }); break;
            case "ちょ": list.AddRange(new[] { "tyo", "cyo", "cho", "tilyo", "tixyo" }); break;
            case "ぢゃ": list.AddRange(new[] { "dya", "dilya", "dixya" }); break;
            case "ぢゅ": list.AddRange(new[] { "dyu", "dilyu", "dixyu" }); break;
            case "ぢょ": list.AddRange(new[] { "dyo", "dilyo", "dixyo" }); break;

            case "にゃ": list.AddRange(new[] { "nya", "nilya", "nixya" }); break;
            case "にゅ": list.AddRange(new[] { "nyu", "nilyu", "nixyu" }); break;
            case "にょ": list.AddRange(new[] { "nyo", "nilyo", "nixyo" }); break;

            case "ひゃ": list.AddRange(new[] { "hya", "hilya", "hixya" }); break;
            case "ひゅ": list.AddRange(new[] { "hyu", "hilyu", "hixyu" }); break;
            case "ひょ": list.AddRange(new[] { "hyo", "hilyo", "hixyo" }); break;
            case "びゃ": list.AddRange(new[] { "bya", "bilya", "bixya" }); break;
            case "びゅ": list.AddRange(new[] { "byu", "bilyu", "bixyu" }); break;
            case "びょ": list.AddRange(new[] { "byo", "bilyo", "bixyo" }); break;
            case "ぴゃ": list.AddRange(new[] { "pya", "pilya", "pixya" }); break;
            case "ぴゅ": list.AddRange(new[] { "pyu", "pilyu", "pixyu" }); break;
            case "ぴょ": list.AddRange(new[] { "pyo", "pilyo", "pixyo" }); break;

            case "みゃ": list.AddRange(new[] { "mya", "milya", "mixya" }); break;
            case "みゅ": list.AddRange(new[] { "myu", "milyu", "mixyu" }); break;
            case "みょ": list.AddRange(new[] { "myo", "milyo", "mixyo" }); break;

            case "りゃ": list.AddRange(new[] { "rya", "rilya", "rixya" }); break;
            case "りゅ": list.AddRange(new[] { "ryu", "rilyu", "rixyu" }); break;
            case "りょ": list.AddRange(new[] { "ryo", "rilyo", "rixyo" }); break;



            default:
                list.Add(hira);
                break;
        }
        return list;
    }

    private static bool IsSupportedTwoChars(string text)
    {
        if (text.Length != 2)
        {
            return false;
        }

        char second = text[1];

        return second == 'ゃ'
            || second == 'ゅ'
            || second == 'ょ';
    }

    private static bool IsSokuonConsonant(char c)
    {
        c = char.ToLowerInvariant(c);

        return c != 'a'
            && c != 'i'
            && c != 'u'
            && c != 'e'
            && c != 'o'
            && c != 'n';
    }

    // 「ん」の後に母音（a, i, u, e, o, y）以外の文字（子音）が来る場合、
    // 「n」1回だと次の文字と混ざってしまうため、「nn」や「n'」を確実に通すための後処理
    private static void PostProcessForNn(List<Syllable> syllables)
    {
        for (int i = 0; i < syllables.Count; i++)
        {
            if (syllables[i].hira == "ん")
            {
                // もし次の文字が存在するなら
                if (i + 1 < syllables.Count)
                {
                    string nextHiraFirstRomaji = syllables[i + 1].romajis[0];
                    char firstCharOfNext = nextHiraFirstRomaji[0];

                    // 次の文字が「あ行(a, i, u, e, o, y, n等)」以外の子音の場合
                    if (firstCharOfNext != 'a' && firstCharOfNext != 'i' && firstCharOfNext != 'u' &&
                        firstCharOfNext != 'e' && firstCharOfNext != 'o' && firstCharOfNext != 'y')
                    {
                        // 「n」単体だと次と混ざるので、「nn」や「n'」を優先・確実にする
                        // （すでに romajis に入っているので、ここでは並び順の調整や追加補正を行う）
                    }
                }
            }
        }
    }

    public static List<string> GenerateAllPatterns(List<Syllable> syllables)
    {
        List<string> currentPatterns = new List<string> { "" };

        foreach (var syl in syllables)
        {
            List<string> nextPatterns = new List<string>();
            foreach (var prefix in currentPatterns)
            {
                foreach (var romaji in syl.romajis)
                {
                    nextPatterns.Add(prefix + romaji);
                }
            }
            currentPatterns = nextPatterns;
        }

        return currentPatterns;
    }
}