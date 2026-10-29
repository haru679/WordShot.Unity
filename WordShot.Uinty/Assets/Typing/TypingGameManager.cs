using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using WordShot.Data;
using WordShot.Flow;
using WordShot.Sounds;

[System.Serializable]
public class TypingData
{
    public string ruby;          // ルビ
    public string targetText;    // 表示テキスト
}

public class TypingGameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mistakeCountTextUI;
    [SerializeField] private SpriteCountDisplay typingMissDisplay;
    [SerializeField] private HPBar bossHPBar;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rubyTextUI;
    [SerializeField] private TextMeshProUGUI targetTextUI;
    [SerializeField] private TextMeshProUGUI inputTextUI;

    [Header("Typing Data")]
    [Tooltip("お題。ゲームが始まると、ボスのデータ（選んだ難易度）の words に置き換わる。ボスのデータがないときだけ、ここのお題を使う")]
    [SerializeField] private List<TypingData> typingList = new List<TypingData>();
    [Tooltip("このシーンを直接再生したとき（ステージセレクトを通らないとき）に使うボスの id と難易度")]
    [SerializeField] private string fallbackBossId = "shamoji_neko";
    [SerializeField] private Difficulty fallbackDifficulty = Difficulty.Easy;

    [Header("音（なくてもよい）")]
    [Tooltip("1文字正しく打ったときの音（キーボード）")]
    [SerializeField] private SoundCue typeSound;
    [Tooltip("打ち間違えたときの音")]
    [SerializeField] private SoundCue missSound;
    [Tooltip("お題を打ち切ったときの攻撃の音")]
    [SerializeField] private SoundCue wordCompleteSound;

    private int currentQuestionIndex = 0;
    private TypingData currentData;

    private List<string> activePatterns = new List<string>(); // このお題で可能な全ローマ字パターン
    private string currentInput = ""; // プレイヤーが今までに打った文字列

    // 現在のお題のタイプミス数
    private int currentMistakeCount = 0;

    // 全てのお題の合計タイプミス数
    private int totalMistakeCount = 0;

    // 解く全のフレームで文字入力居バントが発生したか
    private bool receivedTextInput = false;

    /// <summary>ボスのHPが0になったとき（ステージクリアは BattleOutcome が行う）</summary>
    public event System.Action BossDefeated;

    void Start()
    {
        LoadWordsFromBossData();

        if (typingList.Count > 0)
        {
            LoadRandomQuestion();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // ポーズ中・ゲームオーバー・クリアの後は、入力を受け付けない
        if (GameFlow.Instance != null && !GameFlow.Instance.IsPlaying)
        {
            return;
        }

        // 英字キーと伸ばし棒を押した瞬間だけ確認
        bool keyPressed = Keyboard.current.aKey.wasPressedThisFrame ||
                          Keyboard.current.bKey.wasPressedThisFrame ||
                          Keyboard.current.cKey.wasPressedThisFrame ||
                          Keyboard.current.dKey.wasPressedThisFrame ||
                          Keyboard.current.eKey.wasPressedThisFrame ||
                          Keyboard.current.fKey.wasPressedThisFrame ||
                          Keyboard.current.gKey.wasPressedThisFrame ||
                          Keyboard.current.hKey.wasPressedThisFrame ||
                          Keyboard.current.iKey.wasPressedThisFrame ||
                          Keyboard.current.jKey.wasPressedThisFrame ||
                          Keyboard.current.kKey.wasPressedThisFrame ||
                          Keyboard.current.lKey.wasPressedThisFrame ||
                          Keyboard.current.mKey.wasPressedThisFrame ||
                          Keyboard.current.nKey.wasPressedThisFrame ||
                          Keyboard.current.oKey.wasPressedThisFrame ||
                          Keyboard.current.pKey.wasPressedThisFrame ||
                          Keyboard.current.qKey.wasPressedThisFrame ||
                          Keyboard.current.rKey.wasPressedThisFrame ||
                          Keyboard.current.sKey.wasPressedThisFrame ||
                          Keyboard.current.tKey.wasPressedThisFrame ||
                          Keyboard.current.uKey.wasPressedThisFrame ||
                          Keyboard.current.vKey.wasPressedThisFrame ||
                          Keyboard.current.wKey.wasPressedThisFrame ||
                          Keyboard.current.xKey.wasPressedThisFrame ||
                          Keyboard.current.yKey.wasPressedThisFrame ||
                          Keyboard.current.zKey.wasPressedThisFrame ||
                          Keyboard.current.minusKey.wasPressedThisFrame;

        if (keyPressed)
        {
            // 押されたキーに対応する文字を検出して ProcessInput を呼ぶ
            // ここでは優先順位として最初に見つかったキーのみ処理する
            const string allowed = "abcdefghijklmnopqrstuvwxyz-";
            foreach (char c in allowed)
            {
                if (IsKeyPressedThisFrame(c))
                {
                    ProcessInput(c);
                    break;
                }
            }
        }
    }

    // お題をボスのデータから取る（ステージセレクトで選んだボスと難易度。直接再生したときは fallbackBossId）
    private void LoadWordsFromBossData()
    {
        BossDifficultyData level = GameFlow.Instance != null ? GameFlow.Instance.CurrentBossLevel : null;
        if (level == null) level = BossDatabase.Find(fallbackBossId)?.Get(fallbackDifficulty);

        if (level == null || level.words == null || level.words.Length == 0)
        {
            Debug.LogWarning("[Typing] ボスのデータにお題がないので、インスペクターのお題を使います");
            return;
        }
        typingList = level.words.Select(word => word.ToTypingData()).ToList();
    }

    private void CountMistake()
    {
        if (missSound != null) Sound.Play(missSound);

        // 現在のお題のタイプミス数
        currentMistakeCount++;

        // 全てのお題の合計タイプミス数
        totalMistakeCount++;

        UpdateMistakeCountDisplay();

        // �~�X�p�A�C�R�����E�[����1���炷
        if (typingMissDisplay != null)
        {
            typingMissDisplay.DecreaseCount();

            // �A�C�R�������ׂĂȂ��Ȃ����玟�̂�����
            if (typingMissDisplay.GetCount() <= 0)
            {
                // ���݂̂����̎��̂����֐i��
                int nextIndex = (currentQuestionIndex + 1) % typingList.Count;
                LoadRandomQuestion();
            }
        }

        Debug.Log($"�����̂����F{currentMistakeCount}��");
    }

    private void UpdateMistakeCountDisplay()
    {
        if (mistakeCountTextUI != null)
        {
            mistakeCountTextUI.text = $"タイプミス : {currentMistakeCount}回";
        }
    }

    void OnDestroy()
    {

    }

    private void ProcessInput(char c)
    {

        receivedTextInput = true;

        if (typingList.Count == 0 || currentQuestionIndex >= typingList.Count)
        {
            return;
        }

        string nextInput = currentInput + c;

        bool isValidPrefix = false;
        bool isCompleteMatch = false;

        foreach (string pattern in activePatterns)
        {
            if (pattern == nextInput)
            {
                isValidPrefix = true;
                isCompleteMatch = true;
                break;
            }
            else if (pattern.StartsWith(nextInput))
            {
                isValidPrefix = true;
            }
        }

        if (isCompleteMatch)
        {
            if (typeSound != null) Sound.Play(typeSound);
            if (wordCompleteSound != null) Sound.Play(wordCompleteSound);

            // ミス用のアイコンを1つ減らす
            if (bossHPBar != null)
            {
                bossHPBar.TakeDamage(1);

                // アイコンがずべ手無くなったら次のお題へ進む
                if (bossHPBar.GetCurrentHP() <= 0)
                {
                    rubyTextUI.text = "";
                    targetTextUI.text = $"合計タイピングミス数{totalMistakeCount}回";
                    inputTextUI.text = "";
                    enabled = false;   // もう入力を受け付けない
                    BossDefeated?.Invoke();
                    return;
                }
            }

            // �{�XHP���c���Ă����΁A���̂����������_���ɕ\��
            LoadRandomQuestion();
        }
        else if (isValidPrefix)
        {
            if (typeSound != null) Sound.Play(typeSound);
            currentInput = nextInput;
            UpdateInputDisplay();

            if (isCompleteMatch)
            {
                currentQuestionIndex++;
                if (currentQuestionIndex < typingList.Count)
                {
                    LoadQuestion(currentQuestionIndex);
                }
                else
                {
                    rubyTextUI.text = "";
                    // ここは文字列補間が必要だったため $ を追加
                    targetTextUI.text = $"クリア！\n合計タイプミス：{totalMistakeCount}回";
                    inputTextUI.text = "";
                }
            }
        }
        else
        {
            CountMistake();
        }
    }

    private bool IsKeyPressedThisFrame(char c)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        // 大文字小文字を統一
        c = char.ToLowerInvariant(c);

        switch (c)
        {
            case 'a': return Keyboard.current.aKey.wasPressedThisFrame;
            case 'b': return Keyboard.current.bKey.wasPressedThisFrame;
            case 'c': return Keyboard.current.cKey.wasPressedThisFrame;
            case 'd': return Keyboard.current.dKey.wasPressedThisFrame;
            case 'e': return Keyboard.current.eKey.wasPressedThisFrame;
            case 'f': return Keyboard.current.fKey.wasPressedThisFrame;
            case 'g': return Keyboard.current.gKey.wasPressedThisFrame;
            case 'h': return Keyboard.current.hKey.wasPressedThisFrame;
            case 'i': return Keyboard.current.iKey.wasPressedThisFrame;
            case 'j': return Keyboard.current.jKey.wasPressedThisFrame;
            case 'k': return Keyboard.current.kKey.wasPressedThisFrame;
            case 'l': return Keyboard.current.lKey.wasPressedThisFrame;
            case 'm': return Keyboard.current.mKey.wasPressedThisFrame;
            case 'n': return Keyboard.current.nKey.wasPressedThisFrame;
            case 'o': return Keyboard.current.oKey.wasPressedThisFrame;
            case 'p': return Keyboard.current.pKey.wasPressedThisFrame;
            case 'q': return Keyboard.current.qKey.wasPressedThisFrame;
            case 'r': return Keyboard.current.rKey.wasPressedThisFrame;
            case 's': return Keyboard.current.sKey.wasPressedThisFrame;
            case 't': return Keyboard.current.tKey.wasPressedThisFrame;
            case 'u': return Keyboard.current.uKey.wasPressedThisFrame;
            case 'v': return Keyboard.current.vKey.wasPressedThisFrame;
            case 'w': return Keyboard.current.wKey.wasPressedThisFrame;
            case 'x': return Keyboard.current.xKey.wasPressedThisFrame;
            case 'y': return Keyboard.current.yKey.wasPressedThisFrame;
            case 'z': return Keyboard.current.zKey.wasPressedThisFrame;
            case '-': return Keyboard.current.minusKey.wasPressedThisFrame;

            default: return false; // 他の文字は無視
        }
    }

    private void LoadRandomQuestion()
    {
        if (typingList.Count == 0)
        {
            return;
        }

        // ���肪�������Ȃ��ꍇ�́A���̂������\������
        if (typingList.Count == 1)
        {
            LoadQuestion(0);
            return;
        }

        int randomIndex = Random.Range(0, typingList.Count);

        // �O���Ɠ������肪�I�΂ꂽ�ꍇ�́A�������x���I
        do
        {
            randomIndex = Random.Range(0, typingList.Count);
        }
        while(randomIndex == currentQuestionIndex);

        LoadQuestion(randomIndex);
    }

    private void LoadQuestion(int index)
    {
        currentQuestionIndex = index;
        currentData = typingList[index];

        // 入力状態をリセット
        currentInput = "";

        // 現在のお題のタイプミス数だけリセット
        currentMistakeCount = 0;

        // 表示も0回に戻す
        UpdateMistakeCountDisplay();

        if(typingMissDisplay != null)
        {
            typingMissDisplay.ResetCount();
        }

        rubyTextUI.text = currentData.ruby;
        targetTextUI.text = currentData.targetText;

        // 1. ひらがなを音節に分解
        var syllables = RomajiEngine.Parse(currentData.ruby);

        // 2. 音節の組み合わせから、可能な全ローマ字パターンを自動生成
        activePatterns = RomajiEngine.GenerateAllPatterns(syllables);

        UpdateInputDisplay();
    }

    // 3段目の動的色分け表示（打った方に合わせてガイドが変化するスタイル）
    private void UpdateInputDisplay()
    {
        // 現在の入力に一番フィットしている（前方一致する）パターンをベストマッチとして採用
        string bestMatchedPattern = activePatterns.Count > 0 ? activePatterns[0] : "";
        foreach (string pattern in activePatterns)
        {
            if (pattern.StartsWith(currentInput))
            {
                bestMatchedPattern = pattern;
                break;
            }
        }

        int typedLen = Mathf.Min(currentInput.Length, bestMatchedPattern.Length);
        string typedPart = typedLen > 0 ? bestMatchedPattern.Substring(0, typedLen) : "";
        string untypedPart = (typedLen < bestMatchedPattern.Length) ? bestMatchedPattern.Substring(typedLen) : "";

        // 白（打った部分）とグレー（これからの部分）でリアルタイム表示
        inputTextUI.text = $"<color=#FFFFFF>{typedPart}</color><color=#808080>{untypedPart}</color>";
    }
}