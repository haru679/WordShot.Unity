using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class TypingData
{
    public string ruby;          // ルビ
    public string targetText;    // 表示テキスト
}

public class TypingGameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mistakeCountTextUI;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rubyTextUI;
    [SerializeField] private TextMeshProUGUI targetTextUI;
    [SerializeField] private TextMeshProUGUI inputTextUI;

    [Header("Typing Data")]
    [SerializeField] private List<TypingData> typingList = new List<TypingData>();

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

    void Start()
    {
        if (typingList.Count > 0)
        {
            LoadQuestion(0);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
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

    private void CountMistake()
    {
        // 現在のお題のタイプミス数
        currentMistakeCount++;

        // 全てのお題の合計タイプミス数
        totalMistakeCount++;

        UpdateMistakeCountDisplay();

        Debug.Log($"今回のお題：{currentMistakeCount}回");
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

        if (isValidPrefix)
        {
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

    private void LoadQuestion(int index)
    {
        currentData = typingList[index];

        // 入力状態をリセット
        currentInput = "";

        // 現在のお題のタイプミス数だけリセット
        currentMistakeCount = 0;

        // 表示も0回に戻す
        UpdateMistakeCountDisplay();

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