using System;
using UnityEngine;

namespace WordShot.Flow
{
    /// <summary>
    /// ボスを倒すと出てくる、拾うとステージクリアになるアイテム（ねこまんま）。BattleOutcome がコードで作る。
    /// </summary>
    /// <remarks>
    /// 出てきた場所から床まで落ちて、ふわふわ上下に揺れる。床に着いた後にプレイヤー（タグが Player）が触れると Collected が呼ばれる。
    /// </remarks>
    public class ClearItem : MonoBehaviour
    {
        const string PlayerTag = "Player";

        float floorY;          // 落ちて止まる高さ（アイテムの中心）
        float fallSpeed;
        float bobHeight;
        bool landed;
        bool collected;

        /// <summary>プレイヤーが拾ったとき</summary>
        public event Action Collected;

        /// <summary>アイテムを作る。position から floorY まで落ちる</summary>
        public static ClearItem Create(Sprite sprite, Vector3 position, float size, float floorY, float fallSpeed, float bobHeight)
        {
            var go = new GameObject("ClearItem");
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;   // ステージの物より手前に出す
            if (sprite != null)
            {
                // 画像の大きさにかかわらず、幅か高さの大きい方が size になるようにする
                float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                go.transform.localScale = Vector3.one * (size / Mathf.Max(spriteSize, 0.01f));
            }

            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = sprite != null ? Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y) : 0.5f;

            // トリガーの判定を確実に取るため、動かない Rigidbody2D を付ける
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;

            var item = go.AddComponent<ClearItem>();
            item.floorY = floorY + size * 0.5f;
            item.fallSpeed = fallSpeed;
            item.bobHeight = bobHeight;
            return item;
        }

        void Update()
        {
            Vector3 pos = transform.position;
            if (!landed)
            {
                pos.y = Mathf.MoveTowards(pos.y, floorY, fallSpeed * Time.deltaTime);
                landed = Mathf.Approximately(pos.y, floorY);
            }
            else
            {
                // 拾えることが分かるよう、ふわふわ揺らす
                pos.y = floorY + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * bobHeight;
            }
            transform.position = pos;
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            if (collected || !landed) return;   // 落ちている途中は拾えない（出てきたことが見えるように）
            GameObject owner = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (!owner.CompareTag(PlayerTag)) return;

            collected = true;
            Collected?.Invoke();
            Destroy(gameObject);
        }
    }
}
