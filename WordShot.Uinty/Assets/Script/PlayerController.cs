using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] private float speed = 5.0f;// 移動速度（ワールド単位/秒）

    [Header("ジャンプ")]
    [Tooltip("ジャンプの高さ（ワールド単位）。押すタイミングに関係なくこの高さまで跳ぶ")]
    [SerializeField] private float jumpHeight = 3.0f;// ジャンプの高さ（ワールド単位）

    [Tooltip("接地判定の位置。プレイヤーの足元に空のオブジェクトを置いて割り当てる")]
    [SerializeField] private Transform groundCheck;// 接地判定の位置

    [Tooltip("接地判定の円の半径")]
    [SerializeField] private float groundCheckRadius = 0.1f;// 接地判定の円の半径

    [Tooltip("地面として扱うレイヤー")]
    [SerializeField] private LayerMask groundLayer;// 地面として扱うレイヤー

    [Tooltip("何回連続でジャンプできるか")]
    [SerializeField] private int maxJumpCount = 2;// 最大ジャンプ回数
    private int jumpCount = 0;// 現在のジャンプ回数

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 左右移動（AD キー）
        if (Keyboard.current.aKey.isPressed)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }

        // ジャンプ（Space または W キー、接地している時だけ）
        bool jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame
                        || Keyboard.current.wKey.wasPressedThisFrame;

        // 地面からジャンプしている場合回数をリセット
        if ( IsGrounded())
        {
            jumpCount = 0;
        }
        // 空中ジャンプ
        if (jumpPressed && jumpCount < maxJumpCount)
        {
            Jump();
            jumpCount++;
        }
        //今のジャンプの回数をデバッグ表示
        Debug.Log($"Jump Count: {jumpCount}");
    }

    void Jump()
    {
        // 高さ h に到達する初速度: v = √(2gh)
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

        // AddForce(Impulse) は現在の速度に上乗せされるため、落下中に押すと高さがずれる。
        // 速度を直接セットすることで、押すタイミングに関係なく高さが一定になる。
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    // Scene ビューで接地判定の範囲を確認できるように表示
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}