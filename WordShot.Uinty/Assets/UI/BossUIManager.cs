using System.Collections;
using TMPro;
using UnityEngine;
using WordShot.Data;

public class BossUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Boss_AI bossAI;
    [SerializeField] private TextMeshProUGUI skillNameTextUI;   // 技名用
    [SerializeField] private TextMeshProUGUI dialogueTextUI;    // セリフ用

    [Header("JSON Data")]
    [SerializeField] private TextAsset bossJsonFile;

    [Header("Rice Attack Settings")]
    [Tooltip("ボスのTransform (位置や回転を取得するため) ")]
    [SerializeField] private Transform bossTransform;

    [Header("しゃも箒のセリフ設定")]
    [SerializeField] private Vector3 broomOffsetRight = new Vector3(1.5f, -1.0f, 0f);
    [SerializeField] private Vector3 broomOffsetLeft = new Vector3(-1.5f, -1.0f, 0f);

    // 所も箒の追尾中かどうかのフラグ
    private bool isFollowingBroom = false;
    private bool broomRightToLeft = false;


    // 米粒弾の発射回数カウンター
    private int riceShotCount = 0;

    private BossData bossData;

    // 削除用タイマーを管理する変数
    private Coroutine hideDialogueCoroutine;

    // 最後の弾が発射されてから消えるまでの時間
    [SerializeField] private float dialogueDisplayDuration = 1.5f;

    void Start()
    {
        // 1. JSONを読み込む
        if (bossJsonFile != null)
        {
            bossData = JsonUtility.FromJson<BossData>(bossJsonFile.text);
        }

        // 2. ボスからの通知（インデックスを受け取る）に登録
        if (bossAI != null)
        {
            bossAI.onAttackIndexStarted += HandleAttackStarted;

            // 米粒弾の発射イベント
            bossAI.onRiceShotFired += HandleRiceShotFired;

            // 所も箒プレビューのイベント登録
            bossAI.onBroomPreviewStarted += HandleBroomPreviewStarted;

            // しゃも箒終了イベントの登録
            bossAI.onBroomEnded += HandleBroomEnded;

        }

        if (skillNameTextUI != null)
        {
            skillNameTextUI.text = "";
        }
        if (dialogueTextUI != null)
        {
            dialogueTextUI.text = "";
        }
    }

    private void Update()
    {
        if (!isFollowingBroom || bossTransform == null || dialogueTextUI == null)
        {
            return;
        }

        // 移動方向による配置場所の切り替え（左 -> 右なら右下、右 -> 左なら左下）
        Vector3 offset = broomRightToLeft ? broomOffsetLeft : broomOffsetRight;
        Vector3 targetWorldPos = bossTransform.position + offset;

        // ワールド座標をUIのローカル座標に変換
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(targetWorldPos);
        Canvas canvas = dialogueTextUI.canvas;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
        {
            dialogueTextUI.rectTransform.localPosition = localPoint;
        }

        // 回転の補正（ボスのZ回転が -180° の時はテキストのZ回転を 0° にする）
        float bossZRot = bossTransform.eulerAngles.z;
        // 0 ~ 360度表記になっている場合もあるため、-180 ~ 180の範囲に正規化または比較
        if (bossZRot > 180f) bossZRot -= 360f;

        float textZRot = 0f;
        if (Mathf.Abs(bossZRot - (-180f)) > 1f) // -180度でなければボスの回転を追従
        {
            // 「Z回転が-180の時はテキストの角度はZ = 0にする」に準拠
            textZRot = 0f;
        }
        else
        {
            textZRot = 0f;
        }

        dialogueTextUI.rectTransform.localRotation = Quaternion.Euler(0f, 0f, textZRot);
    }

    void OnDestroy()
    {
        if (bossAI != null)
        {
            bossAI.onAttackIndexStarted -= HandleAttackStarted;
            bossAI.onRiceShotFired -= HandleRiceShotFired;

            bossAI.onBroomPreviewStarted -= HandleBroomPreviewStarted;
            bossAI.onBroomEnded -= HandleBroomEnded;
        }
    }

    // ボスから渡された番号（0, 1, 2）を元に技名を表示
    private void HandleAttackStarted(int jsonIndex)
    {
        if (bossData == null || bossData.attacks == null) return;

        if (jsonIndex >= 0 && jsonIndex < bossData.attacks.Length)
        {
            var attack = bossData.attacks[jsonIndex];

            // 技名表示
            if (skillNameTextUI != null)
            {
                skillNameTextUI.text = attack.skillName;
            }

            // 攻撃ごとのセリフ
            switch (jsonIndex)
            {
                case 0:     // しゃも切り
                    if (skillNameTextUI != null)
                    {
                        dialogueTextUI.text = "";
                    }
                    break;

                case 1:     // しゃも箒
                    if (skillNameTextUI != null)
                    {
                        dialogueTextUI.text = "";
                        // dialogueTextUI.text = attack.line;
                    }
                    break;

                case 2:     // 米粒弾
                    if (skillNameTextUI != null)
                    {
                        // dialogueTextUI.text = "";
                        // dialogueTextUI.text = attack.line;
                    }
                    break;
            }
        }
    }

    private void HandleRiceShotFired()
    {
        if (dialogueTextUI == null || bossTransform == null) return;

        // 発射回数を 1 -> 2 -> 3 と進め、3を超えたら1に戻す
        riceShotCount = (riceShotCount % 3) + 1;

        // 回数に応じた位置（ボスのまわりの相対座標）を設定
        Vector3 worldOffset = Vector3.zero;
        switch (riceShotCount)
        {
            case 1: // 1回目：左上
                worldOffset = new Vector3(-3.0f, 1.5f, 0f);
                break;
            case 2: // 2回目：上
                worldOffset = new Vector3(-3.0f, 2.0f, 0f);
                break;
            case 3: // 3回目：右上
                worldOffset = new Vector3(3.0f, 2.5f, 0f);
                break;
        }

        // ボスのワールド座標 + オフセット計算
        Vector3 targetWorldPos = bossTransform.position + worldOffset;

        // ワールド座標を画面上のピクセル座標に変換
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(targetWorldPos);

        // スクリーン座標を、UI Canvasのローカル座標に変換してテキストに適用
        Canvas canvas = dialogueTextUI.canvas;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Camera uiCamera =
            (canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint))
        {
            dialogueTextUI.rectTransform.localPosition = localPoint;
        }

        // セリフを表示
        dialogueTextUI.text = "ダダにゃ！！";

        // 古いタイマーはリセット
        if (hideDialogueCoroutine != null)
        {
            StopCoroutine(hideDialogueCoroutine);
            hideDialogueCoroutine = null;
        }

        // 3回目の弾が発射されたときだけ消去タイマースタート
        if (riceShotCount == 3)
        {
            // n秒後にセリフを消すタイマーをスタート
            hideDialogueCoroutine = StartCoroutine(HideDialogueAfterSeconds(dialogueDisplayDuration));
        }
    }

    private void HandleBroomPreviewStarted(bool rightToLeft)
    {
        broomRightToLeft = rightToLeft;
        isFollowingBroom = true;

        // しゃも箒のjsonデータからセリフを取得して表示
        if (bossData != null && bossData.attacks != null && bossData.attacks.Length > 1)
        {
            if (dialogueTextUI != null)
            {
                dialogueTextUI.text = bossData.attacks[1].line;
            }
        }

    }

    public void EndBroomDialogue()
    {
        isFollowingBroom = false;
        if (dialogueTextUI != null)
        {
            dialogueTextUI.text = "";
        }

        if (skillNameTextUI != null)
        {
            skillNameTextUI.text = "";
        }
    }

    private void HandleBroomEnded()
    {
        // 追従を止める
        isFollowingBroom = false;

        // 技名とセリフを非表示（空っぽ）にする
        if (dialogueTextUI != null)
        {
            dialogueTextUI.text = "";
        }
        if (skillNameTextUI != null)
        {
            skillNameTextUI.text = "";
        }
    }


    private IEnumerator HideDialogueAfterSeconds(float delay)
    {
        yield return new WaitForSeconds(delay);

        // セリフを消す
        if (dialogueTextUI != null)
        {
            dialogueTextUI.text = ""; // テキストを消す
        }

        if (skillNameTextUI != null)
        {
            skillNameTextUI.text = "";
        }

        hideDialogueCoroutine = null;
    }
}