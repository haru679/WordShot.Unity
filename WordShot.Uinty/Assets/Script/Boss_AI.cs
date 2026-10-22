using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class Boss_AI : MonoBehaviour
{
    [Header("中央へ移動")]
    [SerializeField] Transform centerPoint;     // ステージ中央に置く空のオブジェクト
    [SerializeField] float moveSpeed = 5f;      // 移動速度(ユニット/秒)

    [Header("膨らむ")]
    [SerializeField] float growScale = 1.5f;    // 膨らんだ時の倍率
    [SerializeField] float growTime = 0.6f;     // 膨らむ時間(秒)

    [Header("溜め")]
    [SerializeField] float chargeTime = 2.5f;   // 溜め時間(秒)
    [SerializeField] float chargeAngle = -25f;  // 溜めで後ろに傾ける角度(左攻撃の基準。右攻撃は反転)

    [Header("攻撃直前の停止")]
    [Tooltip("傾いた状態で止まる時間(秒)。ここで攻撃を予告します")]
    [SerializeField] float strikePauseTime = 0.3f;

    [Header("傾く攻撃")]
    [Tooltip("中央から端までの距離(ステージの半分の幅に合わせる)")]
    [SerializeField] float sweepDistance = 4f;
    [Tooltip("頭を地面へ向ける角度(左攻撃の基準。右攻撃は反転)")]
    [SerializeField] float leanDownAngle = 90f;
    [Tooltip("攻撃中に体を下げる量(ユニット)。大きくすると低い位置を掃きます")]
    [SerializeField] float sinkDepth = 0.8f;
    [Tooltip("端まで掃く時間(秒) ← ふるはやさ")]
    [SerializeField] float sweepPassTime = 0.5f;
    [Tooltip("端から中央へ戻り、元の角度に戻る時間(秒)")]
    [SerializeField] float sweepReturnTime = 0.4f;
    [Tooltip("攻撃後の硬直(秒) ← 硬直")]
    [SerializeField] float recoverTime = 1.0f;

    // 掃く攻撃: 次回作成(パラメータ・処理ともに空欄)

    [Header("ピボット")]
    [Tooltip("回転の中心。ボスのローカル座標で指定します(スケール前の値)")]
    [SerializeField] Vector3 pivotOffset = new Vector3(0f, -1f, 0f);
    [SerializeField] bool showPivotGizmo = true;

    Vector3 originalScale;
    Quaternion originalRotation;

    // 膨らんだ後の体制(角度0)の基準値
    Vector3 leanBasePos;
    Quaternion leanBaseRot;
    Vector3 leanBaseScale;

    float currentLean; // 現在の傾き角度
    float currentSink; // 現在の沈み量

    public bool IsAttacking { get; private set; }

    void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    // ===== 外部(AI側)から呼び出す =====

    // 左から攻撃(1回分)
    public void StartSlam()
    {
        if (!IsAttacking) StartCoroutine(AttackRoutine(-1f));
    }

    // 右から攻撃(左攻撃を反転させたもの、1回分)
    public void StartSlamRightFirst()
    {
        if (!IsAttacking) StartCoroutine(AttackRoutine(1f));
    }

    // ===== 傾く攻撃 =====

    // side: -1 = 左から攻撃 / +1 = 右から攻撃
    IEnumerator AttackRoutine(float side)
    {
        IsAttacking = true;

        // 1. 中央へ移動
        yield return MoveToCenter();

        // 2. 膨らむ
        yield return Grow();

        // 3. 傾けの基準を保存
        CaptureBase();

        // 4. 溜め(攻撃する側へ反対に傾ける)
        float charge = -side * chargeAngle;
        yield return Charge(charge);

        // 5. 攻撃直前に少し止まる
        yield return new WaitForSeconds(strikePauseTime);

        // 6. 傾きながら掃く(片方向のみ)
        yield return Sweep(side, charge);

        // 7. 硬直
        yield return new WaitForSeconds(recoverTime);

        ResetPose();
        IsAttacking = false;
    }

    // 片方向へ掃き、最後は元の角度(0度)に戻る
    IEnumerator Sweep(float side, float fromAngle)
    {
        float d = sweepDistance;
        float a = -side * leanDownAngle;   // 攻撃側へ頭を向ける角度(左=+、右=-)

        // 中央 → 攻撃側の端(頭を地面へ向けて沈む)
        yield return SweepPass(0f, side * d, fromAngle, a, sinkDepth, sweepPassTime);

        // 攻撃側の端 → 中央(元の角度へ戻る)
        yield return SweepPass(side * d, 0f, a, 0f, 0f, sweepReturnTime);
    }

    // 角度・横位置・沈み量を同時に補間して移動する
    IEnumerator SweepPass(float fromX, float toX, float fromAngle, float toAngle,
                          float toSink, float duration)
    {
        float startSink = currentSink;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            currentSink = Mathf.Lerp(startSink, toSink, t);
            SetLean(Mathf.Lerp(fromAngle, toAngle, t), Mathf.Lerp(fromX, toX, t));
            yield return null;
        }
    }

    // ===== 共通の動き =====

    IEnumerator MoveToCenter()
    {
        while (true)
        {
            Vector3 pos = transform.position;
            float targetX = centerPoint.position.x;

            if (Mathf.Abs(pos.x - targetX) < 0.01f) break;

            pos.x = Mathf.MoveTowards(pos.x, targetX, moveSpeed * Time.deltaTime);
            transform.position = pos;
            yield return null;
        }
    }

    IEnumerator Grow()
    {
        float timer = 0f;
        Vector3 targetScale = originalScale * growScale;

        while (timer < growTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / growTime);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
    }

    // 0度 → targetAngle へ溜める(攻撃判定なし)
    IEnumerator Charge(float targetAngle)
    {
        currentSink = 0f;

        float timer = 0f;
        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / chargeTime);
            SetLean(Mathf.Lerp(0f, targetAngle, t));
            yield return null;
        }
    }

    // 膨らんだ後の体制を基準として保存
    void CaptureBase()
    {
        leanBasePos = transform.localPosition;
        leanBaseRot = transform.localRotation;
        leanBaseScale = transform.localScale;
    }

    // 元の大きさ・位置・向きに戻す
    void ResetPose()
    {
        transform.localScale = originalScale;
        transform.localPosition = leanBasePos;
        transform.localRotation = originalRotation;
        currentLean = 0f;
        currentSink = 0f;
    }

    // pivotOffset を中心に体を回転させ、横にずらし、沈ませる
    void SetLean(float angle, float offsetX = 0f)
    {
        currentLean = angle;

        Quaternion delta = Quaternion.Euler(0f, 0f, angle); // 回転軸はZ(2Dの想定)
        Vector3 pivotLocal = Vector3.Scale(pivotOffset, leanBaseScale);

        // 回転中心の位置(親空間)
        Vector3 pivotParent = leanBasePos + leanBaseRot * pivotLocal;

        // 回転中心の周りに位置を回す
        Vector3 pos = pivotParent + delta * (leanBasePos - pivotParent);
        pos.x += offsetX;
        pos.y -= currentSink;

        transform.localPosition = pos;
        transform.localRotation = delta * leanBaseRot;
    }

    void OnDrawGizmos()
    {
        if (!showPivotGizmo) return;

        // 再生中は基準値、停止中は現在の状態から計算した位置を表示
        Vector3 basePos = Application.isPlaying ? leanBasePos : transform.localPosition;
        Quaternion baseRot = Application.isPlaying ? leanBaseRot : transform.localRotation;
        Vector3 baseScale = Application.isPlaying ? leanBaseScale : transform.localScale;

        Vector3 pivotWorld = transform.parent != null
            ? transform.parent.TransformPoint(basePos + baseRot * Vector3.Scale(pivotOffset, baseScale))
            : basePos + baseRot * Vector3.Scale(pivotOffset, baseScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivotWorld, 0.15f);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Keyboard.current == null) return;

        // テスト用: Qキーで元の攻撃(左から)、Eキーで反対方向の攻撃(右から)
        if (Keyboard.current.qKey.wasPressedThisFrame) StartSlam();
        if (Keyboard.current.eKey.wasPressedThisFrame) StartSlamRightFirst();
    }
#endif
}