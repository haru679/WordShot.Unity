using UnityEngine;
using UnityEngine.InputSystem;
using WordShot.Flow;

namespace WordShot.Stages
{
    /// <summary>
    /// ステージセレクト用の仮のプレイヤー。本物のプレイヤーができたら差し替える。
    /// ←→：移動　↑ / Space：ジャンプ　Enter：触れている扉に入る
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class StageSelectPlayer : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float jumpSpeed = 12f;

        Rigidbody2D body;
        float moveInput;
        bool jumpRequested;
        bool isGrounded;
        StageDoor nearDoor;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            // 止まっている間も着地の判定（OnCollisionStay2D）が届くよう、眠らせない
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        void Update()
        {
            moveInput = 0f;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !GameFlow.Instance.IsPlaying) return;   // ポーズ中・切り替え中は操作しない

            if (keyboard.leftArrowKey.isPressed) moveInput -= 1f;
            if (keyboard.rightArrowKey.isPressed) moveInput += 1f;

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) jumpRequested = true;

            bool decide = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            if (decide && nearDoor != null) nearDoor.TryEnter();
        }

        void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = moveInput * moveSpeed;
            if (jumpRequested && isGrounded) velocity.y = jumpSpeed;
            body.linearVelocity = velocity;

            jumpRequested = false;
            isGrounded = false;   // 次の物理計算の OnCollisionStay2D で、地面に立っていればまた true になる
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            // 下からの接触（足元）があれば、地面に立っている
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f) isGrounded = true;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out StageDoor door)) return;

            if (nearDoor != null) nearDoor.SetPlayerNear(false);
            nearDoor = door;
            door.SetPlayerNear(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out StageDoor door) || door != nearDoor) return;

            door.SetPlayerNear(false);
            nearDoor = null;
        }
    }
}
