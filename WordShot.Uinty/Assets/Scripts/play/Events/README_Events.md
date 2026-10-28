# Events：フラグ管理とイベント呼び出し（使い方メモ）

対象：WordShot（Unity 6000.6 / 2D）　場所：`Assets/Scripts/Events/`　名前空間：`WordShot.Events`

---

## 0. ひとことで

- **フラグ** … 「今どうなっているか／何が起きたか」を enum で表したもの（例：`BossState.Attack`）。
- **設定する側** … 出来事が起きた場所で `GameFlags.Set(値)` を1行書くだけ。
- **受け取る側** … 2通りある。
  - プログラム：`GameFlags.Subscribe(値, 関数)` で、その値になったら関数を呼ぶ。
  - インスペクター：「いつ（フラグ）」と「何をするか（音・表示・関数の呼び出しなど）」を選んでつなぐ。
- 設定する側と受け取る側は、お互いを知らなくてよい（スクリプト同士を直接つながない）。

```
[プレイヤーのスクリプト] --GameFlags.Set(PlayerState.Attack)--> [GameFlags]
                                                                    |
                         +------------------------------------------+-------------------------+
                         v                                                                    v
        [ボスのスクリプト] Subscribe で受け取り、被弾処理         [EventManager] インスペクターの設定を見て
                                                                   効果音・演出・関数の呼び出しを実行
```

---

## 1. ファイル一覧

| ファイル | 役割 |
|---|---|
| `GameFlags.cs` | フラグの保管と通知。`Set` / `Get` / `Is` / `Subscribe` |
| `FlagGroup.cs` | `[FlagGroup]` 属性、登録済みフラグの一覧（`FlagRegistry`）、インスペクター用の `FlagRef` |
| `DefaultFlags.cs` | 最初から用意しているフラグ（ゲーム・シーン・プレイヤー・ボス・タイピング・UI） |
| `EventManager.cs` | フラグが設定されたら、インスペクターの設定に合う処理を呼ぶ。再生時に自動で作られる |
| `StateEventData.cs` | 「いつ・何をするか」の一覧（データファイル）。`EventSetup` の定義 |
| `SceneEvents.cs` | シーンに置く「いつ・何をするか」。シーンの中の物を動かすときに使う |
| `GameEventActions.cs` | 「何をするか」の種類（音・フラグ・表示・Animator・UnityEvent・ログ） |
| `AnimationEventRelay.cs` | アニメーションイベントから音・フラグを呼ぶ中継 |
| `FlashEffect.cs` | 点滅の演出（撃破・被ダメージ）。フラグで自動再生 |
| `Editor/FlagRefDrawer.cs` | 「いつ」の欄を「ボス/Attack」形式のメニューにする |
| `Editor/SerializeReferenceDropdownDrawer.cs` | 「何をするか」の欄に種類のメニューを出す |
| `Editor/EventMenu.cs` | メニュー Tools → WordShot（共通イベントを開く など） |

---

## 2. フラグの登録（プログラマー）

### 2-1. 今あるフラグ（`DefaultFlags.cs`）

| 種類（メニュー名） | enum | 値 | だれが設定するか |
|---|---|---|---|
| ゲーム | `GameState` | Playing, Paused, GameOver, Cleared | **自動**（GameFlow から） |
| シーン | `SceneState` | Title, Loading, StageSelect, Stage, Result, Other | **自動**（シーン読み込み時） |
| プレイヤー | `PlayerState` | Idle, Move, Jump, Land, Attack, Damage, Dead | 田場 |
| ボス | `BossState` | Entry, Idle, Windup（予備動作）, Attack, Damage, Dead | 田場 |
| タイピング | `TypingState` | WordStart, KeyCorrect, KeyMiss, MissLimit, WordComplete | 松井 |
| UI | `UIState` | Move, Decide, Cancel, OpenMenu, CloseMenu | 松井 |

### 2-2. 値を足す

enum に名前を足すだけ。**順番はどこでもよい**（インスペクターでは名前で保存しているので、ずれない）。

```csharp
public enum BossState { Entry, Idle, Windup, Attack, Charge, Damage, Dead }   // Charge を追加
```

### 2-3. 種類を足す

**どのファイルに書いてもよい**（自分の担当フォルダでよい）。`[FlagGroup("メニュー名")]` を付けた enum を書くと、自動で登録される。

```csharp
using WordShot.Events;

[FlagGroup("ギミック")]                 // "ステージ/ギミック" のように / で階層にもできる
public enum GimmickState { Start, Lock, Unlock }

[FlagGroup("進行", Order = 50)]          // Order：メニューの並び順（小さいほど上）
public enum TutorialState { NotDone, Done }
```

### 2-4. 名前を変える・消すとき

インスペクターで選んであった欄が、赤く「⚠ 見つからない」になる。**変える前にプランナーに声をかける**。
今あるフラグは Tools → WordShot → フラグの一覧を表示 でコンソールに出せる。

---

## 3. フラグを設定する（プログラマー）

```csharp
using WordShot.Events;

GameFlags.Set(PlayerState.Attack);      // プレイヤーが攻撃した
GameFlags.Set(TypingState.KeyMiss);     // 打ち間違えた
GameFlags.Set(BossState.Dead);          // ボスを倒した
```

- **同じ値でも毎回通知する**。KeyMiss のように何度も起きる出来事は、起きるたびに `Set` を呼べばよい。
- 「状態」（今ジャンプ中）も「出来事」（今攻撃した）も同じ書き方でよい。
- アニメーションから設定したいときは 7章。

---

## 4. フラグを読む・受け取る（プログラマー）

### 4-1. 今の値を調べる

```csharp
if (GameFlags.Is(PlayerState.Dead)) return;          // 今の値が Dead か（一度も設定されていなければ false）
PlayerState now = GameFlags.Get<PlayerState>();       // 今の値（一度も設定されていなければ enum の先頭の値）
```

### 4-2. 他のスクリプトの状態を受け取って処理する

例：**プレイヤーが攻撃した状態になったら、ボスが被弾処理をする**。

```csharp
using System;
using UnityEngine;
using WordShot.Events;

public class BossHealth : MonoBehaviour
{
    [SerializeField] int hp = 100;
    [SerializeField] int damagePerHit = 10;

    Action unsubscribe;   // 登録を外すための関数

    void OnEnable()
    {
        // PlayerState が Attack になったら OnPlayerAttack を呼ぶ
        unsubscribe = GameFlags.Subscribe(PlayerState.Attack, OnPlayerAttack);
    }

    void OnDisable()
    {
        unsubscribe?.Invoke();   // 必ず外す（外さないと、消えたオブジェクトの関数が呼ばれる）
    }

    void OnPlayerAttack()
    {
        hp -= damagePerHit;
        GameFlags.Set(hp > 0 ? BossState.Damage : BossState.Dead);   // 自分の状態も知らせる → 音や演出につながる
    }
}
```

種類のどの値でも受け取りたいとき（引数に新しい値が入る）：

```csharp
unsubscribe = GameFlags.Subscribe<BossState>(state =>
{
    if (state == BossState.Windup) ShowWarning();    // 予備動作 → 攻撃予告を出す
    else if (state == BossState.Attack) HideWarning();
});
```

複数を受け取るとき：

```csharp
Action unsubscribe;

void OnEnable()
{
    Action a = GameFlags.Subscribe(PlayerState.Attack, OnPlayerAttack);
    Action b = GameFlags.Subscribe(GameState.GameOver, OnGameOver);
    unsubscribe = () => { a(); b(); };
}

void OnDisable() => unsubscribe?.Invoke();
```

### 4-3. 注意

- **登録は OnEnable、解除は OnDisable** で対にする。
- 呼ばれるのは `Set` を呼んだその場（同じフレーム・同じ処理の中）。
- フラグは「何が起きたか」だけを伝える。**ダメージ量などの数値は運ばない**。数値が必要なら、受け取った側が自分のデータ（インスペクターの値・BossData など）を使う。
- 受け取った処理の中で別のフラグを `Set` してよい（上の例のボスの Damage / Dead）。ただし、お互いに `Set` し合って止まらない作りにしない。

---

## 5. インスペクターでつなぐ（プランナー）

### 5-1. 全シーン共通の設定（音・BGM など）

1. メニュー **Tools → WordShot → 共通イベントを開く**（なければ `Assets/Data/Resources/Events/CommonEvents.asset` が自動で作られる）。
2. **Setups** の ＋ で1行増やす。
3. **When（いつ）**：クリックして「ボス/Attack」のように選ぶ。
   - 「ボス/（どれでも）」を選ぶと、ボスのどの値になっても動く。
4. **Only While（さらに、このときだけ）**：必要なときだけ ＋ で足す。例：「シーン/Stage」＝ステージの中だけ。
5. **Actions（何をするか）**：＋ → 種類をメニューから選び、中身を設定する。上から順に実行される。
6. **Memo（一覧の名前）**：空なら「▶ ボス/Attack → 効果音を鳴らす」が自動で入る。自分で書けばそちらが優先。

Resources/Events に置いたデータは、どのシーンでも自動で読み込まれる。

### 5-2. 「何をするか」の種類

| メニュー | 内容 | 使える場所 |
|---|---|---|
| 音/効果音を鳴らす | SoundCue（または Resources/Sounds の名前）を鳴らす | どこでも |
| 音/BGMを切り替える | BGM をクロスフェードで切り替える | どこでも |
| 音/BGMを止める | BGM をフェードアウト | どこでも |
| 音/効果音をすべて止める | 鳴っている効果音を止める | どこでも |
| フラグ/フラグを設定する | 別のフラグを設定する（連鎖させる） | どこでも |
| シーン/表示・非表示 | オブジェクトを出す・消す | **SceneEvents のみ** |
| シーン/Animatorのトリガー | Animator のトリガーを引く | **SceneEvents のみ** |
| シーン/UnityEventを呼ぶ | 任意のコンポーネントの関数を呼ぶ | **SceneEvents のみ** |
| 確認用/ログを出す | コンソールに文字を出す | どこでも |

「SceneEvents のみ」の3つは、シーンの中のオブジェクトを指定するため、データファイル（共通）では指定できない。

### 5-3. 「この状態になったら、この関数を走らせる」

例：**ボスが予備動作（Windup）に入ったら、攻撃予告UIの `Show()` を呼ぶ**。

1. 対象のシーンを開き、**Tools → WordShot → このシーンにイベントを置く**（`SceneEvents` が置かれて選択される）。
2. Setups の ＋ → **When** に「ボス/Windup」。
3. **Actions** の ＋ → 「シーン/UnityEventを呼ぶ」。
4. 出てきた欄の ＋ → 左の枠に攻撃予告UIのオブジェクトをドラッグ → 右のメニューで `AttackWarning > Show()` を選ぶ。
   - 引数が1つ（int・float・string・bool・Object）の関数も選べる。値はその場で入力する。
   - 呼べるのは **public** な関数。

簡単なものは専用の種類のほうが早い：表示だけなら「シーン/表示・非表示」、アニメーションなら「シーン/Animatorのトリガー」。

### 5-4. 設定例

| いつ | さらに、このときだけ | 何をするか | 置き場所 |
|---|---|---|---|
| シーン/StageSelect | | 音/BGMを切り替える（ステージセレクトの曲） | 共通 |
| タイピング/KeyMiss | | 音/効果音を鳴らす（ミス音） | 共通 |
| ボス/Attack | シーン/Stage | 音/効果音を鳴らす（攻撃音） | 共通 |
| ゲーム/Paused | | 音/効果音を鳴らす（ポーズ音） | 共通 |
| ボス/Windup | | シーン/UnityEventを呼ぶ（攻撃予告の Show） | SceneEvents |
| ボス/Dead | | シーン/Animatorのトリガー（撃破アニメ） | SceneEvents |

---

## 6. 点滅の演出（FlashEffect）

ボスやプレイヤーのオブジェクトに付ける。

| 項目 | 内容 |
|---|---|
| Play When | いつ点滅するか（例：ボス/Damage）。空なら `Play()` を呼んだときだけ |
| Mode | Blink（表示・非表示を繰り返す）／Tint（色を掛ける） |
| Tint Color | Tint のときの色 |
| Flash Duration / Flash Count | 1回の時間（秒）と回数 |
| After | 終わったあと：Keep（そのまま）／Deactivate（非表示）／Destroy（削除） |
| Targets | 点滅させる SpriteRenderer。空なら自分と子から自動で探す |

---

## 7. アニメーションから呼ぶ（AnimationEventRelay）

Animator と同じオブジェクトに付け、アニメーションのイベントで関数を選ぶ。

| 関数 | 文字列の欄 | 例 |
|---|---|---|
| `PlaySound` | SoundCue の名前（Resources/Sounds） | `boss_shamoji_attack` |
| `PlaySoundCue` | （Object の欄に SoundCue を入れる） | |
| `SetState` | 「種類.値」（enum 名でもメニュー名でもよい） | `BossState.Attack` ／ `ボス.Attack` |

---

## 8. 確認のしかた

再生中に Hierarchy の **DontDestroyOnLoad → EventManager** を選ぶ。

| 項目 | 使い方 |
|---|---|
| Log Events | チェックすると、呼ばれた設定（Memo）がコンソールに出る |
| Debug Flag ＋ Test Set Flag | フラグを選び、Test Set Flag にチェック → 実際にそのフラグを設定する（本番と同じ流れ。Subscribe しているスクリプトも動く） |
| Global Data | 読み込まれている共通データの一覧 |

各設定の **Test Play** にチェック → その設定の「何をするか」だけを、条件なしですぐ実行する。

---

## 9. 決まりごと

- フラグの**名前を変える・消す前に、プランナーに声をかける**（インスペクターの選択が外れるため）。
- `Subscribe` したら **OnDisable で必ず外す**。
- フラグは出来事・状態だけ。数値は運ばない。
- `GameState` と `SceneState` は自動で入るので、プログラムから `Set` しない。
- このプロジェクトはドメインのリロードが無効。GameFlags は再生のたびに自動で空に戻る。

---

## 10. API 早見表（AI・コード補完向け）

```csharp
namespace WordShot.Events

// 登録
[FlagGroup(string label, Order = int)]   // enum に付ける。label は "A/B" で階層可

// GameFlags（static）
void   Set<T>(T value)                        where T : struct, Enum   // 設定して通知（同値でも通知）
void   Set(Enum value)
T      Get<T>()                               where T : struct, Enum   // 未設定なら default
Enum   Get(Type enumType)                                              // 未設定なら null
bool   Is<T>(T value)                         where T : struct, Enum   // 未設定なら false
Action Subscribe<T>(T value, Action handler)  where T : struct, Enum   // 指定値になったら呼ぶ。戻り値で解除
Action Subscribe<T>(Action<T> handler)        where T : struct, Enum   // どの値でも呼ぶ。戻り値で解除
bool   SetFromString(string "Type.Value")                              // 型名かメニュー名＋値名
event  Action<Type> StateChanged                                       // 低レベルの通知（enum の型）

// FlagRegistry（static）：登録済みの種類
IReadOnlyList<Type> Groups;  Type Find(string fullName);  Type FindByName(string nameOrLabel);  string Label(Type)

// FlagRef（インスペクター用の struct）：group = enum の FullName、value = 値の名前（空 = どれでも）
bool IsTriggeredBy(Type changed);  bool IsCurrent();  bool Apply();  string Label

// EventSetup：memo / when(FlagRef) / onlyWhile(List<FlagRef>) / actions(List<GameEventAction>, SerializeReference) / testPlay
// 発火条件：when の種類が設定された && when.IsCurrent() && onlyWhile がすべて IsCurrent()

// GameEventAction を継承すると「何をするか」に追加できる
[Serializable, MenuName("音/効果音を鳴らす")]
public class PlaySoundAction : GameEventAction { public override void Execute() { ... } }
```

### 「何をするか」を新しく作るとき

```csharp
using System;
using WordShot.Events;

[Serializable, MenuName("演出/画面を揺らす")]     // メニューに出る名前
public class ShakeCameraAction : GameEventAction
{
    public float power = 0.3f;                    // インスペクターに出る設定
    public override void Execute() => CameraShake.Play(power);
}
```

`[Serializable]` と、引数なしで作れること（コンストラクタなし）が条件。シーンの物を参照するフィールドを持つ場合は、SceneEvents の中でだけ使える。
