using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] float hiddenTime = 2f;
    [Tooltip("上から落ちてくる速さ(ユニット/秒)")]
    [SerializeField] float dropSpeed = 12f;
    [Tooltip("下に降りてから止まっている時間(秒)")]
    [SerializeField] float waitAfterDrop = 3f;
    [Tooltip("逆さまになる角度(度)")]
    [SerializeField] float upsideDownAngle = 180f;
    [Tooltip("左右に揺れる角度(度)")]
    [SerializeField] float swayAngle = 30f;
    [Tooltip("左右に揺れる速さ(往復回数/秒)")]
    [SerializeField] float swayFrequency = 1.5f;
    [Tooltip("横へ進む速さ(ユニット/秒)")]
    [SerializeField] float sweepMoveSpeed = 5f;
    [Tooltip("床へ沈む量(ユニット)。大きいほど低い位置を通ります")]
    [SerializeField] float videoSinkDepth = 0.8f;
    [Tooltip("横移動の開始位置(中央からのX座標)。画面の外に置いてください")]
    [SerializeField] float videoStartX = -6f;
    [Tooltip("横移動の終了位置(中央からのX座標)。画面の外に置いてください")]
    [SerializeField] float videoEndX = 6f;
    [Tooltip("横へ抜けてから上へ戻るまでの待ち時間(秒)")]
    [SerializeField] float waitBeforeReturn = 0.5f;
    [Tooltip("上から戻ってくる速さ(ユニット/秒)")]
    [SerializeField] float returnSpeed = 5f;
    [Tooltip("最後に上から戻ってくる時の開始高さ(ユニット)。liftHeightの1.5倍が目安")]
    [SerializeField] float returnHeight = 9f;

    [Header("攻撃後の硬直")]
    [Tooltip("攻撃後の硬直時間(秒)")]
    [SerializeField] float recoverTime = 1.0f;

    [Header("米粒攻撃")]
    [Tooltip("狙う相手(プレイヤー)")]
    [SerializeField] Transform player;
    [Tooltip("米粒のPrefab")]
    [SerializeField] GameObject ricePrefab;
    [Tooltip("頭上に出す3つの位置(ボスからのオフセット)。左・真ん中・右の順")]
    [SerializeField]
    Vector3[] riceSpawnOffsets = {
        new Vector3(-1.5f, 2.5f, 0f),
        new Vector3(0f, 3f, 0f),
        new Vector3(1.5f, 2.5f, 0f)
    };
    [Tooltip("米粒が頭上に出てから、打ち始めるまでの時間(秒)")]
    [SerializeField] float riceAppearTime = 0.5f;
    [Tooltip("打つ前にプレイヤーを確認する時間(秒)")]
    [SerializeField] float aimTime = 0.3f;
    [Tooltip("1発撃ってから次を確認するまでの間隔(秒)")]
    [SerializeField] float shotInterval = 0.5f;
    [Tooltip("米粒の飛ぶ速さ(ユニット/秒)")]
    [SerializeField] float riceSpeed = 10f;
    [Tooltip("米粒が消えるまでの時間(秒)")]
    [SerializeField] float riceLifeTime = 4f;
    [Tooltip("米粒のダメージ量。プレイヤーの TakeDamage(int) に渡されます")]
    [SerializeField] int riceDamage = 1;
    [Tooltip("プレイヤーに当たったと判定する距離(ユニット)")]
    [SerializeField] float riceHitRadius = 0.5f;
    [Tooltip("打つ前に、米粒が敵の方向へ回転する時間(秒)")]
    [SerializeField] float rotateTime = 0.3f;
    [Tooltip("回転し終えてから発射するまでの硬直時間(秒)")]
    [SerializeField] float fireStiffTime = 0.2f;

    [Header("ピボット")]
    [Tooltip("回転の中心。ボスのローカル座標で指定します(スケール前の値)")]
    [SerializeField] Vector3 pivotOffset = new Vector3(0f, -1f, 0f);
    [SerializeField] bool showPivotGizmo = true;

    [Header("ランダム攻撃")]
    [Tooltip("ONにすると、ため攻撃(左・右)・掃き攻撃(左・右)・米粒攻撃の5種類をランダムに繰り返します")]
    [SerializeField] bool autoRandomAttack = true;
    [Tooltip("1つの攻撃が終わってから次の攻撃を出すまでの間隔(秒)")]
    [SerializeField] float attackInterval = 2f;

    [Header("攻撃SE(各攻撃ごとに複数追加できます)")]
    [Tooltip("叩き付け(左)の攻撃で鳴らすSE")]
    public List<AttackSE> seSlamLeft = new List<AttackSE>();
    [Tooltip("叩き付け(右)の攻撃で鳴らすSE")]
    public List<AttackSE> seSlamRight = new List<AttackSE>();
    [Tooltip("掃き攻撃(左から右)で鳴らすSE")]
    public List<AttackSE> seSweepLeftToRight = new List<AttackSE>();
    [Tooltip("掃き攻撃(右から左)で鳴らすSE")]
    public List<AttackSE> seSweepRightToLeft = new List<AttackSE>();
    [Tooltip("米粒攻撃で鳴らすSE")]
    public List<AttackSE> seRice = new List<AttackSE>();

    [System.Serializable]
    public class AttackSE
    {
        [Tooltip("再生する音声")] public AudioClip clip;
        [Tooltip("攻撃動作の開始からの再生開始時刻(秒)")] public float startTime = 0f;
        [Tooltip("停止する時刻(秒)。開始より後に設定しない場合は最後まで鳴らします")] public float endTime = 0f;
        [Tooltip("ループ再生するか。終了時刻が設定されていない場合は攻撃が終わるまで繰り返します")] public bool loop = false;
        [Tooltip("音量")] [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("ピッチ")] [Range(-3f, 3f)] public float pitch = 1f;
        [Tooltip("0 = 2D(距離による減衰なし)")] [Range(0f, 1f)] public float spatialBlend = 0f;
        [Tooltip("左右の定位(-1=左, 0=中央, 1=右)。2Dで有効")] [Range(-1f, 1f)] public float stereoPan = 0f;
        [Tooltip("優先度(0が最優先、256が最低)")] [Range(0, 256)] public int priority = 128;
    }

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

    // ===== 攻撃SE =====
    void PlayAttackSEs(List<AttackSE> list)
    {
        Debug.Log("[BossSE] PlayAttackSEs count=" + (list == null ? -1 : list.Count));
        if (list == null) return;
        foreach (AttackSE se in list)
        {
            if (se != null) StartCoroutine(PlayAttackSE(se));
        }
    }

    IEnumerator PlayAttackSE(AttackSE se)
    {
        Debug.Log("[BossSE] start wait clip=" + (se.clip == null ? "NULL" : se.clip.name) + " start=" + se.startTime + " end=" + se.endTime + " loop=" + se.loop);
        if (se.clip == null) yield break;
        if (se.startTime > 0f) yield return new WaitForSeconds(se.startTime);

        GameObject go = new GameObject("AttackSE_" + se.clip.name);
        go.transform.SetParent(transform, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = se.clip;
        src.volume = se.volume;
        src.pitch = se.pitch;
        src.spatialBlend = se.spatialBlend;
        src.panStereo = se.stereoPan;
        src.priority = se.priority;
        src.loop = se.loop;
        src.playOnAwake = false;
        src.Play();
        Debug.Log("[BossSE] playing " + se.clip.name + " isPlaying=" + src.isPlaying + " vol=" + src.volume);

        // 停止時刻が開始より後ならその時刻で止める。そうでなければ最後まで鳴らす
        if (se.loop && se.endTime <= se.startTime)
        {
            // ループで終了時刻がない場合は、攻撃が終わるまで鳴らし続ける
            yield return new WaitUntil(() => !IsAttacking);
        }
        else
        {
            float duration = se.endTime > se.startTime ? se.endTime - se.startTime : se.clip.length;
            yield return new WaitForSeconds(duration);
        }

        if (go != null) Destroy(go);
    }

    void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    void Start()
    {
        if (autoRandomAttack) StartCoroutine(RandomAttackLoop());
    }

    // 攻撃が終わってから attackInterval 秒待ち、ランダムな攻撃を1つ出すことを繰り返す
    IEnumerator RandomAttackLoop()
    {
        while (true)
        {
            yield return new WaitUntil(() => !IsAttacking);
            yield return new WaitForSeconds(attackInterval);
            PlayRandomAttack();
        }
    }

    // 5種類の攻撃からランダムに1つ実行する
    void PlayRandomAttack()
    {
        switch (Random.Range(0, 5))
        {
            case 0: StartSlam(); break;                  // ため攻撃(左)
            case 1: StartSlamRightFirst(); break;        // ため攻撃(右)
            case 2: StartRiseSweep(); break;             // 掃き攻撃(左から右)
            case 3: StartRiseSweepRightToLeft(); break;  // 掃き攻撃(右から左)
            default: StartRiceAttack(); break;           // 米粒攻撃
        }
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

    // 動画の動きの掃く攻撃(左から右へ・1回)
    public void StartRiseSweep()
    {
        if (!IsAttacking) StartCoroutine(AttackRoutine(0f, true, false));
    }

    // 動画の動きの掃く攻撃(右から左へ・1回)
    public void StartRiseSweepRightToLeft()
    {
        if (!IsAttacking) StartCoroutine(AttackRoutine(0f, true, true));
    }

    // ===== 攻撃全体 =====

    // side: -1 = 左から掃く / +1 = 右から掃く(従来の攻撃のみ使用)
    // video: true = 動画の動きの攻撃 / false = 従来の掃く攻撃
    // rightToLeft: 動画の動きの攻撃の向き(true=右から左へ / false=左から右へ)
    IEnumerator AttackRoutine(float side, bool video, bool rightToLeft = false)
    {
        IsAttacking = true;

        // 1. 中央へ移動
        yield return MoveToCenter();

        // 攻撃の種類に合わせたSEを、攻撃動作の開始時刻から鳴らす
        if (video)
            PlayAttackSEs(rightToLeft ? seSweepRightToLeft : seSweepLeftToRight);
        else
            PlayAttackSEs(side < 0f ? seSlamLeft : seSlamRight);

        if (video)
        {
            // 2. 基準を保存(動画の攻撃は膨張・溜め・停止なし)
            CaptureBase();

            // 3. 動画の動き
            yield return VideoSweep(rightToLeft);
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

            // 6-2. 元の大きさまで縮める(パッと戻さない)
            yield return Shrink();
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
    IEnumerator VideoSweep(bool rightToLeft)
    {
        // 横移動の開始位置と終了位置(インスペクターで指定した値。右から左なら入れ替える)
        float startX = rightToLeft ? videoEndX : videoStartX;
        float exitX = rightToLeft ? videoStartX : videoEndX;

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
                              videoSinkDepth, 0f, dropTime);

        // 3-2. 下に降りたまま、数秒待つ
        yield return new WaitForSeconds(waitAfterDrop);

        // 4. 左右に揺れながら、横へ進んで画面の外へ出る
        float swayTime = Mathf.Abs(exitX - startX) / Mathf.Max(sweepMoveSpeed, 0.01f);
        yield return SwayPass(startX, exitX, upsideDownAngle, swayTime);

        // 5. 画面の外で待つ
        yield return new WaitForSeconds(waitBeforeReturn);

        // 6. 上から戻ってきて、元の姿勢へ(returnHeightの高さから降りてくる)
        currentRise = returnHeight;
        currentSink = 0f;
        SetLean(upsideDownAngle, 0f);
        float returnTime = returnHeight / Mathf.Max(returnSpeed, 0.01f);
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

    // ===== 米粒攻撃 =====

    // 米粒攻撃(1回)
    public void StartRiceAttack()
    {
        if (!IsAttacking) StartCoroutine(RiceAttackRoutine());
    }

    // 頭上に3つ出す → 左・真ん中・右の順に、打つ瞬間にプレイヤーを狙って発射
    IEnumerator RiceAttackRoutine()
    {
        if (ricePrefab == null)
        {
            Debug.LogWarning("ricePrefab が設定されていません");
            yield break;
        }

        IsAttacking = true;

        // 1. 頭上に3つ出す
        PlayAttackSEs(seRice);

        GameObject[] rice = new GameObject[riceSpawnOffsets.Length];
        for (int i = 0; i < rice.Length; i++)
        {
            rice[i] = Instantiate(ricePrefab, transform.position + riceSpawnOffsets[i], Quaternion.identity);
        }
        yield return new WaitForSeconds(riceAppearTime);

        // 2. 1発目 確認→発射 / 2発目 確認→発射 / 3発目 確認→発射
        for (int i = 0; i < rice.Length; i++)
        {
            // 確認
            yield return new WaitForSeconds(aimTime);

            // 回転: rotateTime をかけて、米粒の「下」を敵の方向へ向ける
            Quaternion startRot = rice[i].transform.rotation;
            float rotTimer = 0f;
            while (rotTimer < rotateTime)
            {
                rotTimer += Time.deltaTime;
                float t = Mathf.Clamp01(rotTimer / rotateTime);
                Vector3 aimDir = DirToPlayer(rice[i].transform.position);
                Quaternion targetRot = Quaternion.Euler(0f, 0f, AngleForDir(aimDir));
                rice[i].transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            // 硬直(回転し終えてから発射まで止まる)
            yield return new WaitForSeconds(fireStiffTime);

            // 発射(この瞬間のプレイヤーの方向へまっすぐ飛ばす)
            Vector3 fireDir = DirToPlayer(rice[i].transform.position);
            StartCoroutine(FlyRice(rice[i], fireDir));

            yield return new WaitForSeconds(shotInterval);
        }

        // 3. 硬直
        yield return new WaitForSeconds(recoverTime);
        IsAttacking = false;
    }

    // 米粒の位置からプレイヤーへの方向(2D)。プレイヤーがいなければ下向き
    Vector3 DirToPlayer(Vector3 from)
    {
        if (player == null) return Vector3.down;
        Vector3 dir = player.position - from;
        dir.z = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.down;
    }

    // 米粒の「下」が dir を向くための回転角(度)
    float AngleForDir(Vector3 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;

    // 米粒を指定方向へ飛ばす。プレイヤーに当たったらダメージを与えて消える
    IEnumerator FlyRice(GameObject rice, Vector3 dir)
    {
        // 米粒の「下」(-Y)がプレイヤーの方向を向くように回転させる
        // (-Y を dir へ向けるには、dir の角度に +90 度すればよい)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        rice.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float timer = 0f;
        while (timer < riceLifeTime && rice != null)
        {
            timer += Time.deltaTime;
            rice.transform.position += dir * riceSpeed * Time.deltaTime;

            // 当たり判定(プレイヤーとの距離で判定)
            if (player != null && Vector3.Distance(rice.transform.position, player.position) <= riceHitRadius)
            {
                // プレイヤーのダメージ処理を呼ぶ(無敵中・HP0の時は PlayerController 側で無効)
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(riceDamage);
                Destroy(rice);
                yield break;
            }

            yield return null;
        }

        if (rice != null) Destroy(rice);
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

    // 膨らんだ大きさから元の大きさまで滑らかに縮める(時間は growTime と同じ)
    IEnumerator Shrink()
    {
        Vector3 startScale = transform.localScale;
        float timer = 0f;

        while (timer < growTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / growTime);
            transform.localScale = Vector3.Lerp(startScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
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
}