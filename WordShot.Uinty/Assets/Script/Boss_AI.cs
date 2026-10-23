using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class Boss_AI : MonoBehaviour
{
    [Header("ステージ中央へ移動")]
    [SerializeField] Transform centerPoint;     // ステージ中央に置く空のオブジェクト
    [SerializeField] float moveSpeed = 5f;      // 移動速度(ユニット/秒)

    [Header("膨張(Qキー / Eキー)")]
    [SerializeField] float growScale = 1.5f;    // 膨らんだ時の倍率
    [SerializeField] float growTime = 0.6f;     // 膨らむ時間(秒)

    [Header("溜め(Qキー / Eキー)")]
    [SerializeField] float chargeTime = 2.5f;   // 溜め時間(秒)
    [SerializeField] float chargeAngle = -25f;  // 溜めで後ろに傾ける角度(左攻撃の基準。右攻撃は反転)

    [Header("攻撃前の停止(Qキー / Eキー)")]
    [Tooltip("傾いた状態で止まる時間(秒)。ここで攻撃予告を出します")]
    [SerializeField] float strikePauseTime = 0.3f;

    [Header("掃く攻撃 - 共通(Qキー / Eキー)")]
    [Tooltip("掃く距離(ステージの半分の幅に合わせる)")]
    [SerializeField] float sweepDistance = 4f;
    [Tooltip("床へ倒れる角度(度)。左攻撃は右に、右攻撃は左に倒れます")]
    [SerializeField] float leanDownAngle = 90f;
    [Tooltip("床に倒れた時の沈み込み量(ユニット)。大きいと低い位置を掃きます")]
    [SerializeField] float sinkDepth = 0.8f;

    [Header("掃く攻撃 - 従来(Qキー / Eキー)")]
    [Tooltip("倒れて掃く時間(秒)")]
    [SerializeField] float sweepPassTime = 0.5f;
    [Tooltip("戻りの時間(秒)")]
    [SerializeField] float sweepReturnTime = 0.4f;

    [Header("掃く攻撃 - 動画の動き(＠キー)")]
    [Tooltip("画面の上へ消えるまで上がる高さ(ユニット)。ステージの大きさに合わせて調整してください")]
    [SerializeField] float liftHeight = 6f;
    [Tooltip("上へ上がる速さ(ユニット/秒)")]
    [SerializeField] float liftSpeed = 6f;
    [Tooltip("画面の外で待つ時間(秒)")]
    [SerializeField] float hiddenTime = 1f;
    [Tooltip("上から落ちてくる速さ(ユニット/秒)")]
    [SerializeField] float dropSpeed = 12f;
    [Tooltip("逆さまになる角度(度)")]
    [SerializeField] float upsideDownAngle = 180f;
    [Tooltip("左右に揺れる角度(度)")]
    [SerializeField] float swayAngle = 30f;
    [Tooltip("左右に揺れる速さ(往復回数/秒)")]
    [SerializeField] float swayFrequency = 1.5f;
    [Tooltip("横へ進む向き。1=右へ、-1=左へ(動画では左から右へ進んでいたので1)")]
    [SerializeField] float swayDirection = 1f;
    [Tooltip("横へ進む速さ(ユニット/秒)")]
    [SerializeField] float sweepMoveSpeed = 5f;
    [Tooltip("画面の外へ抜けるまでの余白(ユニット)")]
    [SerializeField] float exitMargin = 2f;
    [Tooltip("横へ抜けてから上へ戻るまでの待ち時間(秒)")]
    [SerializeField] float waitBeforeReturn = 0.5f;
    [Tooltip("上から戻ってくる速さ(ユニット/秒)")]
    [SerializeField] float returnSpeed = 5f;

    [Header("攻撃後の硬直")]
    [Tooltip("攻撃後の硬直時間(秒)")]
    [SerializeField] float recoverTime = 1.0f;

    [Header("ピボット")]
    [Tooltip("回転の中心。ボスのローカル座標で指定します(スケール前の値)")]
    [SerializeField] Vector3 pivotOffset = new Vector3(0f, -1f, 0f);
    [SerializeField] bool showPivotGizmo = true;

    Vector3 originalScale;
    Quaternion originalRotation;

    // 基準となる体の状態(角度0)
    Vector3 leanBasePos;
    Quaternion leanBaseRot;
    Vector3 leanBaseScale;

    float currentLean;  // 現在の傾け角度
    float currentSink;  // 現在の沈み量
    float currentRise;  // 現在の上昇量

    public bool IsAttacking { get; private set; }

    void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    // ===== 外部(AI側)から呼び出す =====

    // 左から掃く攻撃(従来・1回)
    public void StartSlam()
    {
        if (!IsAttacking) StartCoroutine(AttackRoutine(-1f, false));
    }

    // 右から掃く攻撃(従来・1回)
    public void StartSlamRightFirst()
    {
        if (!IsAttacking) StartCoroutine(AttackRoutine(1f, false));
    }

    // 動画の動きの掃く攻撃(1回)
    public void StartRiseSweep()
    {
        if (!IsAttacking) StartCoroutine(AttackRoutine(0f, true));
    }

    // ===== 攻撃全体 =====

    // side: -1 = 左から掃く / +1 = 右から掃く(従来の攻撃のみ使用)
    // video: true = 動画の動きの攻撃 / false = 従来の掃く攻撃
    IEnumerator AttackRoutine(float side, bool video)
    {
        IsAttacking = true;

        // 1. 中央へ移動
        yield return MoveToCenter();

        if (video)
        {
            // 2. 基準を保存(動画の攻撃は膨張・溜め・停止なし)
            CaptureBase();

            // 3. 動画の動き
            yield return VideoSweep();
        }
        else
        {
            // 2. 膨らむ
            yield return Grow();

            // 3. 傾きの基準を保存
            CaptureBase();

            // 4. 溜め(攻撃する側へ反対に傾ける)
            float charge = -side * chargeAngle;
            yield return Charge(charge);

            // 5. 攻撃前に少し止まる
            yield return new WaitForSeconds(strikePauseTime);

            // 6. 掃く
            yield return Sweep(side, charge);
        }

        // 7. 硬直
        yield return new WaitForSeconds(recoverTime);

        ResetPose();
        IsAttacking = false;
    }

    // ===== 従来の掃く攻撃(Qキー / Eキー) =====
    IEnumerator Sweep(float side, float fromAngle)
    {
        float d = sweepDistance;
        float a = -side * leanDownAngle;   // 攻撃方向へ倒れる角度(左=+、右=-)

        // 倒れて掃く(下へ沈みながら)
        yield return SweepPass(0f, side * d, fromAngle, a, sinkDepth, sweepPassTime);

        // 端から中央へ戻る(元の角度へ)
        yield return SweepPass(side * d, 0f, a, 0f, 0f, sweepReturnTime);
    }

    // 角度・位置・沈み量を同時に補間して動かす(従来)
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

    // ===== 動画の動きの攻撃(＠キー) =====
    // 上へ上がって画面外 → 逆さまで床へ落ちる → 左右に揺れながら横へ進んで画面外
    // → 上から戻ってきて元の姿勢へ
    IEnumerator VideoSweep()
    {
        float dir = Mathf.Sign(swayDirection) >= 0f ? 1f : -1f;
        float offX = sweepDistance + exitMargin;          // 画面の外までの距離
        float startX = -dir * offX;                       // 画面の外(進行方向の逆側)からスタート
        float exitX = dir * offX;                         // 画面の外へ抜ける位置

        // 1. 上へ上がり、画面の上へ消える(姿勢は変えない)
        float liftTime = liftHeight / Mathf.Max(liftSpeed, 0.01f);
        yield return RisePass(0f, 0f, 0f, 0f, 0f, liftHeight, liftTime);

        // 2. 画面の外で待つ
        yield return new WaitForSeconds(hiddenTime);

        // 3. 画面の外(左)で逆さまになり、床の高さまで落ちる
        currentRise = liftHeight;
        currentSink = 0f;
        SetLean(upsideDownAngle, startX);
        float dropTime = liftHeight / Mathf.Max(dropSpeed, 0.01f);
        yield return RisePass(startX, startX, upsideDownAngle, upsideDownAngle,
                              sinkDepth, 0f, dropTime);

        // 4. 左右に揺れながら、横へ進んで画面の外へ出る
        float swayTime = Mathf.Abs(exitX - startX) / Mathf.Max(sweepMoveSpeed, 0.01f);
        yield return SwayPass(startX, exitX, upsideDownAngle, swayTime);

        // 5. 画面の外で待つ
        yield return new WaitForSeconds(waitBeforeReturn);

        // 6. 上から戻ってきて、元の姿勢へ
        currentRise = liftHeight;
        currentSink = 0f;
        SetLean(upsideDownAngle, 0f);
        float returnTime = liftHeight / Mathf.Max(returnSpeed, 0.01f);
        yield return RisePass(0f, 0f, upsideDownAngle, 0f, 0f, 0f, returnTime);
    }

    // 角度・位置・沈み量・上昇量を同時に補間して動かす(動画の動き用)
    IEnumerator RisePass(float fromX, float toX, float fromAngle, float toAngle,
                         float toSink, float toRise, float duration)
    {
        duration = Mathf.Max(duration, 0.0001f);

        float startSink = currentSink;
        float startRise = currentRise;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float e = Mathf.SmoothStep(0f, 1f, t);   // 始めと終わりをゆっくりにする

            currentSink = Mathf.Lerp(startSink, toSink, e);
            currentRise = Mathf.Lerp(startRise, toRise, e);
            SetLean(Mathf.Lerp(fromAngle, toAngle, e), Mathf.Lerp(fromX, toX, e));
            yield return null;
        }

        // 最後の位置を確実に合わせる
        currentSink = toSink;
        currentRise = toRise;
        SetLean(toAngle, toX);
    }

    // 逆さまの姿勢を保ったまま、左右に揺れながら横へ進む
    IEnumerator SwayPass(float fromX, float toX, float baseAngle, float duration)
    {
        duration = Mathf.Max(duration, 0.0001f);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // 左右の揺れ(sin波)を、逆さまの角度に足す
            float sway = swayAngle * Mathf.Sin(timer * swayFrequency * Mathf.PI * 2f);
            SetLean(baseAngle + sway, Mathf.Lerp(fromX, toX, t));
            yield return null;
        }

        SetLean(baseAngle, toX);
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

    // 0度 → targetAngle へ傾ける(攻撃する時)
    IEnumerator Charge(float targetAngle)
    {
        currentSink = 0f;
        currentRise = 0f;

        float timer = 0f;
        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / chargeTime);
            SetLean(Mathf.Lerp(0f, targetAngle, t));
            yield return null;
        }
    }

    // 基準となる体の状態を保存
    void CaptureBase()
    {
        leanBasePos = transform.localPosition;
        leanBaseRot = transform.localRotation;
        leanBaseScale = transform.localScale;
    }

    // 体の大きさ・位置・向きを元に戻す
    void ResetPose()
    {
        transform.localScale = originalScale;
        transform.localPosition = leanBasePos;
        transform.localRotation = originalRotation;
        currentLean = 0f;
        currentSink = 0f;
        currentRise = 0f;
    }

    // pivotOffset を中心に体を回転させ、横にずらし、沈み・上昇を反映する
    void SetLean(float angle, float offsetX = 0f)
    {
        currentLean = angle;

        Quaternion delta = Quaternion.Euler(0f, 0f, angle); // 回転軸はZ(2Dの想定)
        Vector3 pivotLocal = Vector3.Scale(pivotOffset, leanBaseScale);

        // 回転中心の位置(親空間)
        Vector3 pivotParent = leanBasePos + leanBaseRot * pivotLocal;

        // 回転中心の周りに位置を決める
        Vector3 pos = pivotParent + delta * (leanBasePos - pivotParent);
        pos.x += offsetX;
        pos.y += currentRise - currentSink;

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

        // テスト用
        // Qキー: 左から掃く(従来)
        // Eキー: 右から掃く(従来)
        // ＠キー(日本語キーボードでは「P」の右隣の「@」): 動画の動きの攻撃
        if (Keyboard.current.qKey.wasPressedThisFrame) StartSlam();
        if (Keyboard.current.eKey.wasPressedThisFrame) StartSlamRightFirst();
        if (Keyboard.current.leftBracketKey.wasPressedThisFrame) StartRiseSweep();
    }
#endif
}