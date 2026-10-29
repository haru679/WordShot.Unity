using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using WordShot.Sounds;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] private float speed = 5f;

    [Header("重力")]
    [Tooltip("重力の強さ（単位/秒²）。大きいほど速く落下する")]
    [SerializeField] private float gravity = 30f;

    [Tooltip("落下速度の上限（単位/秒）")]
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("ジャンプ")]
    [Tooltip("最大ジャンプ高度（長押ししたときの高さ）")]
    [SerializeField] private float jumpHeight = 3f;

    [Tooltip("最低ジャンプ高度（すぐ離しても到達する高さ）。jumpHeight 以下にすること")]
    [SerializeField] private float minJumpHeight = 1f;

    [Tooltip("上昇中の速度（単位/秒）。上昇中は重力がかからない")]
    [SerializeField] private float riseSpeed = 10f;

    [Tooltip("空中ジャンプを含めた最大ジャンプ回数（2 なら 2 段ジャンプ）")]
    [SerializeField] private int maxJumpCount = 2;

    [Header("接地判定")]
    [Tooltip("接地判定の位置。プレイヤーの足元に空のオブジェクトを置いて割り当てる")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("接地判定の円の半径")]
    [SerializeField] private float groundCheckRadius = 0.1f;

    [Tooltip("地面として扱うレイヤー")]
    [SerializeField] private LayerMask groundLayer;

    [Header("ダメージ")]
    [Tooltip("最大HP。HPバーへの表示と、0になったときのゲームオーバーは BattleOutcome が行う")]
    [SerializeField] private int maxHp = 3;

    [Tooltip("敵に当たったときに受けるダメージ量")]
    [SerializeField] private int contactDamage = 1;

    [Tooltip("ダメージを受けた後の無敵時間（秒）。点滅の長さもこの値になる")]
    [SerializeField] private float invincibleDuration = 1.5f;

    [Header("点滅")]
    [Tooltip("被弾時の点滅のON/OFF")]
    [SerializeField] private bool enableBlink = true;

    [Tooltip("表示/非表示を切り替える間隔（秒）。小さいほど速く点滅する")]
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("音（なくてもよい）")]
    [Tooltip("ジャンプしたときの音")]
    [SerializeField] private SoundCue jumpSound;
    [Tooltip("二段ジャンプしたときの音（空ならジャンプの音）")]
    [SerializeField] private SoundCue doubleJumpSound;
    [Tooltip("ダメージを受けたときの音")]
    [SerializeField] private SoundCue damageSound;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;

    private float moveInput;
    private bool jumpRequested;   // ジャンプキーが押された（FixedUpdate で消費）
    private bool jumpHeld;        // ジャンプキーを押し続けている
    private int jumpCount;
    private bool isRising;        // 上昇中か
    private bool leftGround;      // ジャンプ後に一度でも地面を離れたか
    private float jumpStartY;

    private int currentHp;
    private float invincibleEndTime = -1f;   // この時刻までは無敵

    private Renderer[] renderers;
    private Coroutine blinkRoutine;

    // 向きを変える画像（子オブジェクトの SpriteRenderer をすべて対象にする）。画像は右向きを想定
    private SpriteRenderer[] spriteRenderers;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    /// <summary>HPが変わったとき（今のHP, 最大HP）。HPバーの表示に使う</summary>
    public event System.Action<int, int> HpChanged;

    /// <summary>HPが0になったとき。この後は動けなくなる</summary>
    public event System.Action Died;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        // 重力は Rigidbody2D ではなくこのスクリプトで管理する
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        currentHp = maxHp;

        // 点滅対象（子オブジェクトを含むすべての Renderer）
        renderers = GetComponentsInChildren<Renderer>();

        // 向き反転の対象（子オブジェクトを含むすべての SpriteRenderer）
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    // インスペクターで値を変更したときに呼ばれる
    void OnValidate()
    {
        // 値の制約をかける(最低高度が最大高度を超えないようにする)
        if (minJumpHeight > jumpHeight) minJumpHeight = jumpHeight;
        if (minJumpHeight < 0f) minJumpHeight = 0f;
        if (riseSpeed < 0.01f) riseSpeed = 0.01f;

        // ダメージ・点滅関連の制約
        if (maxHp < 1) maxHp = 1;
        if (contactDamage < 0) contactDamage = 0;
        if (invincibleDuration < 0f) invincibleDuration = 0f;
        if (blinkInterval < 0.01f) blinkInterval = 0.01f;
    }

    void OnDisable()
    {
        // 点滅中に非活性になった場合も、表示状態に戻しておく
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        SetVisible(true);
    }

    void Update()
    {
        // 入力の取得（左右移動：← →）
        moveInput = 0f;
        jumpHeld = false;

        // キーボードがないとき・死んだ後は動かない
        if (Keyboard.current == null || IsDead) return;

        if (Keyboard.current.leftArrowKey.isPressed) moveInput -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed) moveInput += 1f;

        // 向きを変える：初期は右向きの画像なので、左に進むときだけ反転する（止まったら最後の向きのまま）
        if (moveInput != 0f) SetFacingLeft(moveInput < 0f);

        // ジャンプ入力の取得
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }
        // ジャンプキーを押し続けているかどうか
        jumpHeld = Keyboard.current.spaceKey.isPressed || Keyboard.current.upArrowKey.isPressed;
    }

    // FixedUpdate は物理演算の更新タイミングで呼ばれる
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool grounded = IsGrounded();

        // 地面を離れたことを記録
        if (!grounded)
        {
            leftGround = true;
        }

        // 地面に着地したらジャンプ回数をリセット（ジャンプ直後の誤リセットを防ぐため leftGround を確認）
        if (grounded && !isRising && leftGround && rb.linearVelocity.y <= 0.01f)
        {
            jumpCount = 0;
            leftGround = false;
        }

        // ジャンプ開始
        if (jumpRequested && jumpCount < maxJumpCount)
        {
            StartJump();
        }
        jumpRequested = false;

        float vy = rb.linearVelocity.y;

        // 上昇中の処理
        if (isRising)
        {
            float height = rb.position.y - jumpStartY;

            bool reachedMax = height >= jumpHeight;
            bool releasedAfterMin = !jumpHeld && height >= minJumpHeight;

            if (reachedMax || releasedAfterMin)
            {
                // 上昇終了。この時点から重力がかかる
                isRising = false;
                vy = 0f;
            }
            else
            {
                // 上昇中は重力なしで一定速度
                vy = riseSpeed;
            }
        }

        // 上昇中でない場合は重力を加える
        if (!isRising)
        {
            // 重力を加える（落下速度に上限あり）
            vy -= gravity * dt;
            vy = Mathf.Max(vy, -maxFallSpeed);
        }

        rb.linearVelocity = new Vector2(moveInput * speed, vy);
    }

    // 画像を左右に反転する（右向きの画像を基準にする）
    void SetFacingLeft(bool left)
    {
        if (spriteRenderers == null) return;

        foreach (var sr in spriteRenderers)
        {
            if (sr != null) sr.flipX = left;
        }
    }

    // ジャンプ開始処理
    void StartJump()
    {
        jumpCount++;
        SoundCue sound = jumpCount >= 2 && doubleJumpSound != null ? doubleJumpSound : jumpSound;
        if (sound != null) Sound.Play(sound);
        jumpStartY = rb.position.y;
        isRising = true;
        leftGround = false;
    }

    // 接地判定
    bool IsGrounded()
    {
        if (groundCheck == null) return false;

        // groundCheck が自分自身のときは、足元（当たり判定の下の端）で調べる。
        // 中心で調べると、プレイヤーを大きく表示したシーン（ステージセレクト）で床に届かず、着地できないため
        Vector2 point = groundCheck.position;
        if (groundCheck == transform && bodyCollider != null)
            point = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);

        return Physics2D.OverlapCircle(point, groundCheckRadius, groundLayer) != null;
    }

    // =========================================================== 敵との当たり判定 ======================================================//

    // 物理コリジョン（Collider 同士の衝突）
    void OnCollisionEnter2D(Collision2D collision) => TryTakeDamage(collision.gameObject);
    void OnCollisionStay2D(Collision2D collision) => TryTakeDamage(collision.gameObject);

    // トリガー（Is Trigger にした Collider との接触）
    void OnTriggerEnter2D(Collider2D other) => TryTakeDamage(other.gameObject);
    void OnTriggerStay2D(Collider2D other) => TryTakeDamage(other.gameObject);

    // Enemy タグのオブジェクトに触れていて、無敵中でなければダメージを受ける
    void TryTakeDamage(GameObject other)
    {
        if (!other.CompareTag("Enemy")) return;
        TakeDamage(contactDamage);
    }

    // 外部(敵の攻撃・米粒など)からダメージを受ける。無敵中・HP0の時は無効
    public void TakeDamage(int amount)
    {
        if (Time.time < invincibleEndTime) return;   // 無敵中
        if (currentHp <= 0) return;                  // 既にHPが0

        currentHp -= amount;
        invincibleEndTime = Time.time + invincibleDuration;

        // 無敵時間と同じ長さで点滅させる
        StartBlink();

        if (currentHp < 0) currentHp = 0;
        if (damageSound != null) Sound.Play(damageSound);
        Debug.Log($"ダメージを受けた！ ダメージ: {amount} / HP: {currentHp}/{maxHp}");
        HpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Debug.Log("HPが0になった");
            Died?.Invoke();
        }
    }

    /// <summary>これ以上ダメージを受けないようにする（ボスを倒した後の演出中など）</summary>
    public void SetInvincibleForever()
    {
        invincibleEndTime = float.MaxValue;
    }

    // ================================================================ 点滅 ==============================================================//

    void StartBlink()
    {
        if (!enableBlink) return;

        // 念のため既存の点滅を止めてから開始する
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
        }
        blinkRoutine = StartCoroutine(BlinkRoutine(invincibleDuration));
    }

    IEnumerator BlinkRoutine(float duration)
    {
        float interval = Mathf.Max(blinkInterval, 0.01f);
        float elapsed = 0f;
        bool visible = false;

        while (elapsed < duration)
        {
            visible = !visible;
            SetVisible(visible);

            // 残り時間を超えないように待機時間を切り詰める
            float wait = Mathf.Min(interval, duration - elapsed);
            yield return new WaitForSeconds(wait);
            elapsed += wait;
        }

        // 点滅終了後は必ず表示に戻す
        SetVisible(true);
        blinkRoutine = null;
    }

    void SetVisible(bool visible)
    {
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    // Scene ビューで接地判定の範囲を確認できるように表示
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}