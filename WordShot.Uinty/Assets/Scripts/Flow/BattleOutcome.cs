using System.Collections;
using System.Linq;
using UnityEngine;

namespace WordShot.Flow
{
    /// <summary>
    /// ボス戦の決着（プレイヤーとボスの死と、その後）。ボス戦のシーン（GameScene）に1つ置く。
    /// </summary>
    /// <remarks>
    /// ・プレイヤーのHPが0 → ボスの攻撃を止め、プレイヤーが倒れて消える → ゲームオーバー（Shift でやり直し）
    /// ・ボスのHPが0（お題を打ち切った）→ ボスの攻撃を止め、ボスが震えながら縮んで消える → ねこまんまが落ちてくる
    ///   → プレイヤーが拾うとステージクリア（セーブしてステージセレクトへ）
    /// ・プレイヤーのHPを、プレイヤーのHPバーに出す
    /// 参照が空のときは、シーンの中から自動で探す。演出の長さはインスペクターで変えられる。
    /// </remarks>
    public class BattleOutcome : MonoBehaviour
    {
        [Header("参照（空ならシーンから自動で探す）")]
        [SerializeField] PlayerController player;
        [SerializeField] Boss_AI boss;
        [SerializeField] TypingGameManager typing;
        [SerializeField] BossUIManager bossUI;
        [Tooltip("プレイヤーのHPバー")]
        [SerializeField] HPBar playerHPBar;

        [Header("プレイヤーが死んだとき")]
        [Tooltip("倒れて消えるまでの時間(秒)")]
        [SerializeField, Min(0f)] float playerDeathTime = 1f;
        [Tooltip("倒れる角度(度)")]
        [SerializeField] float playerFallAngle = 90f;

        [Header("ボスを倒したとき")]
        [Tooltip("震えながら縮んで消えるまでの時間(秒)")]
        [SerializeField, Min(0f)] float bossDeathTime = 1.5f;
        [Tooltip("震える幅(ユニット)")]
        [SerializeField, Min(0f)] float bossShakeWidth = 0.15f;

        [Header("ボスを倒した後に出るアイテム（拾うとクリア）")]
        [Tooltip("アイテムの画像（ねこまんま）")]
        [SerializeField] Sprite clearItemSprite;
        [Tooltip("アイテムの大きさ(ユニット)")]
        [SerializeField, Min(0.1f)] float clearItemSize = 1.2f;
        [Tooltip("床まで落ちる速さ(ユニット/秒)")]
        [SerializeField, Min(0.1f)] float clearItemFallSpeed = 6f;
        [Tooltip("床で上下に揺れる高さ(ユニット)")]
        [SerializeField, Min(0f)] float clearItemBobHeight = 0.2f;
        [Tooltip("拾ってから、ステージセレクトへ戻るまでの時間(秒)。この間「ステージクリア！」が出る")]
        [SerializeField, Min(0f)] float clearWaitAfterCollect = 1f;

        [Header("共通")]
        [Tooltip("消えてから、ゲームオーバー／クリアの表示を出すまでの間(秒)")]
        [SerializeField, Min(0f)] float waitBeforeResult = 0.3f;

        bool decided;   // 決着がついたか（両方が同時に死んでも、先に決まった方だけにする）

        void Awake()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (boss == null) boss = FindAnyObjectByType<Boss_AI>();
            if (typing == null) typing = FindAnyObjectByType<TypingGameManager>();
            if (bossUI == null) bossUI = FindAnyObjectByType<BossUIManager>();
            if (playerHPBar == null)
            {
                // HPバーが2つある（プレイヤー・ボス）ので、名前で探す
                playerHPBar = FindObjectsByType<HPBar>(FindObjectsInactive.Include)
                    .FirstOrDefault(bar => bar.name.ToLowerInvariant().Contains("pl"));
            }
        }

        void OnEnable()
        {
            if (player != null)
            {
                player.HpChanged += ShowPlayerHp;
                player.Died += OnPlayerDied;
            }
            if (typing != null) typing.BossDefeated += OnBossDefeated;
        }

        void OnDisable()
        {
            if (player != null)
            {
                player.HpChanged -= ShowPlayerHp;
                player.Died -= OnPlayerDied;
            }
            if (typing != null) typing.BossDefeated -= OnBossDefeated;
        }

        void Start()
        {
            if (player != null) ShowPlayerHp(player.CurrentHp, player.MaxHp);
        }

        // プレイヤーのHP（例：3のうち2）を、HPバーの目盛り（例：50）に合わせて出す
        void ShowPlayerHp(int hp, int maxHp)
        {
            if (playerHPBar == null || maxHp <= 0) return;
            playerHPBar.SetHP(Mathf.RoundToInt((float)hp / maxHp * playerHPBar.GetMaxHP()));
        }

        // ---- プレイヤーの死 ----

        void OnPlayerDied()
        {
            if (decided) return;
            decided = true;
            StopBattle();
            StartCoroutine(PlayerDeathRoutine());
        }

        IEnumerator PlayerDeathRoutine()
        {
            Transform body = player.transform;
            SpriteRenderer[] sprites = player.GetComponentsInChildren<SpriteRenderer>();
            Color[] colors = sprites.Select(s => s.color).ToArray();
            Quaternion startRotation = body.rotation;
            // 向いている方と反対へ倒れる（左向きなら右へ）
            bool facingLeft = sprites.Length > 0 && sprites[0].flipX;
            Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, facingLeft ? -playerFallAngle : playerFallAngle);

            for (float t = 0f; t < playerDeathTime; t += Time.deltaTime)
            {
                float p = t / playerDeathTime;
                body.rotation = Quaternion.Slerp(startRotation, endRotation, Mathf.SmoothStep(0f, 1f, p));
                SetAlpha(sprites, colors, 1f - p);
                yield return null;
            }
            body.rotation = endRotation;
            SetAlpha(sprites, colors, 0f);

            yield return new WaitForSeconds(waitBeforeResult);
            GameFlow.Instance?.FinishStage(false);
        }

        // ---- ボスの死 ----

        void OnBossDefeated()
        {
            if (decided) return;
            decided = true;
            StopBattle();
            if (player != null) player.SetInvincibleForever();   // 演出の間にやられないように
            StartCoroutine(BossDeathRoutine());
        }

        IEnumerator BossDeathRoutine()
        {
            Transform body = boss.transform;
            SpriteRenderer[] sprites = boss.GetComponentsInChildren<SpriteRenderer>();
            Color[] colors = sprites.Select(s => s.color).ToArray();
            Vector3 startPosition = body.position;
            Vector3 startScale = body.localScale;

            for (float t = 0f; t < bossDeathTime; t += Time.deltaTime)
            {
                float p = t / bossDeathTime;
                // 震えながら縮み、後半で薄くなって消える
                Vector2 shake = Random.insideUnitCircle * bossShakeWidth * (1f - p);
                body.position = startPosition + (Vector3)shake;
                body.localScale = startScale * Mathf.Lerp(1f, 0.6f, p);
                SetAlpha(sprites, colors, Mathf.Clamp01((1f - p) * 2f));
                yield return null;
            }
            body.position = startPosition;
            SetAlpha(sprites, colors, 0f);
            foreach (Collider2D col in boss.GetComponentsInChildren<Collider2D>()) col.enabled = false;   // 消えた後は当たらない

            yield return new WaitForSeconds(waitBeforeResult);
            SpawnClearItem(startPosition);
        }

        // ボスがいた場所からアイテムを落とす。プレイヤーが拾ったらステージクリア
        void SpawnClearItem(Vector3 bossPosition)
        {
            // 真下の床（ground レイヤー）の高さを探す。足場（Footing）は通り抜けて、一番下の床まで落とす。
            // 見つからなければプレイヤーの足元
            float floorY = player != null ? player.GetComponent<Collider2D>().bounds.min.y : bossPosition.y;
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(bossPosition, Vector2.down, 50f, LayerMask.GetMask("ground")))
            {
                if (hit.collider.GetComponent<Footing>() != null) continue;
                floorY = hit.point.y;
                break;
            }

            var item = ClearItem.Create(clearItemSprite, new Vector3(bossPosition.x, bossPosition.y, 0f),
                clearItemSize, floorY, clearItemFallSpeed, clearItemBobHeight);
            item.Collected += () => GameFlow.Instance?.FinishStage(true, clearWaitAfterCollect);
        }

        // ---- 共通 ----

        // 決着がついたら、ボスの攻撃・技名の表示・タイピングを止める
        void StopBattle()
        {
            if (boss != null) boss.StopAttacking();
            if (bossUI != null) bossUI.EndBroomDialogue();   // 技名・セリフを消す
            if (typing != null) typing.enabled = false;
        }

        static void SetAlpha(SpriteRenderer[] sprites, Color[] colors, float alpha)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color c = colors[i];
                c.a *= alpha;
                sprites[i].color = c;
            }
        }
    }
}
