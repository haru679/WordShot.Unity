using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public enum DropMode
{
    Tap,   // 押した瞬間にすり抜ける
    Hold   // 押している間すり抜ける
}

[RequireComponent(typeof(Collider2D))]
public class Footing : MonoBehaviour
{
    [SerializeField] DropMode dropMode = DropMode.Tap;// すり抜けモード
    // [SerializeField] KeyCode dropKey = KeyCode.S; // すり抜けキー
    [SerializeField] float dropTime = 0.3f;       // すり抜けを有効にしておく時間(秒)

    Collider2D footCol;
    readonly HashSet<Collider2D> onFoot = new HashSet<Collider2D>();          // 足場に乗っているプレイヤー
    readonly Dictionary<Collider2D, float> dropping = new Dictionary<Collider2D, float>(); // すり抜け中 → 残り時間
    readonly List<Collider2D> tmp = new List<Collider2D>();// すり抜け中のプレイヤーを一時的に格納するリスト

    void Awake()
    {
        // 足場のコライダーを取得
        footCol = GetComponent<Collider2D>();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // プレイヤーが足場に乗ったときに登録
        if (col.gameObject.CompareTag("Player")) onFoot.Add(col.collider);
    }

    void OnCollisionExit2D(Collision2D col)
    {
        // プレイヤーが足場から離れたときに登録解除
        if (col.gameObject.CompareTag("Player")) onFoot.Remove(col.collider);
    }

    void Update()
    {
        if (Keyboard.current == null) return;// キーボードが接続されていない場合は処理しない
        var sKey = Keyboard.current.sKey;    // すり抜けキー(S)の状態を取得

        if (dropMode == DropMode.Tap)
        {
            // 押した瞬間だけ判定
            if (!sKey.wasPressedThisFrame) return;

            foreach (var player in onFoot)
            {
                if (dropping.ContainsKey(player)) continue;
                StartDrop(player);
            }
        }
        else // DropMode.Hold
        {
            // 押している間は毎フレーム判定（タイマーを更新し続ける）
            if (!sKey.isPressed) return;

            foreach (var player in onFoot)
            {
                StartDrop(player);
            }
        }
    }

    void StartDrop(Collider2D player)
    {
        // まだ衝突を無効にしていなければ無効化する
        if (!dropping.ContainsKey(player))
        {
            // 足場のコライダーとプレイヤーのコライダーの衝突を無効化
            Physics2D.IgnoreCollision(player, footCol, true);
        }
        dropping[player] = dropTime;
    }


    // すり抜け中のプレイヤーの残り時間を減らし、時間切れになったら衝突を戻す
    void FixedUpdate()
    {
        if (dropping.Count == 0) return;

        tmp.Clear();                // すり抜け中のプレイヤーを一時的に格納するリスト
        tmp.AddRange(dropping.Keys);// すり抜け中のプレイヤーを一時的に格納するリストに追加

        // すり抜け中のプレイヤーの残り時間を減らす
        foreach (var player in tmp)
        {
            if (player == null)
            {
                // プレイヤーが削除されていた場合は、すり抜け中リストから削除
                dropping.Remove(player);
                continue;
            }

            dropping[player] -= Time.fixedDeltaTime;
            if (dropping[player] > 0f) continue;

            // 足場より完全に下に落ちてから衝突を戻す（めり込み防止）
            if (player.bounds.max.y <= footCol.bounds.min.y)
            {
                Physics2D.IgnoreCollision(player, footCol, false);
                dropping.Remove(player);
            }
        }
    }
}