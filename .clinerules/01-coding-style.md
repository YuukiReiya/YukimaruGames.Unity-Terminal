# コーディングスタイル規約

## 言語・バージョン
- C# 9.0以上を使用する
- Unity 2022 LTS以降を前提とする

## 命名規則

| 対象 | 規則 | 例 |
|------|------|-----|
| クラス・構造体 | PascalCase | `PlayerHealth`, `EnemyController` |
| インターフェース | `I` + PascalCase | `IAttackable`, `IDamageable` |
| メソッド | PascalCase | `TakeDamage()`, `ResetPosition()` |
| プロパティ | PascalCase | `CurrentHp`, `MaxSpeed` |
| privateフィールド | _camelCase | `_currentHp`, `_moveSpeed` |
| 定数 | UPPER_SNAKE_CASE | `MAX_HEALTH`, `BASE_DAMAGE` |
| ローカル変数・引数 | camelCase | `damageAmount`, `targetPosition` |
| イベント | PascalCase + `On` prefix | `OnDied`, `OnHealthChanged` |

## MonoBehaviour の記述順序

以下の順序で記述すること:

```csharp
// 1. Unityイベント関連フィールド
[SerializeField] private int _maxHp = 100;

// 2. privateフィールド
private int _currentHp;

// 3. プロパティ
public int CurrentHp => _currentHp;

// 4. Unityライフサイクルメソッド（実行順に並べる）
private void Awake() { }
private void Start() { }
private void Update() { }
private void OnDestroy() { }

// 5. publicメソッド
public void TakeDamage(int amount) { }

// 6. privateメソッド
private void Die() { }
```

## SerializeField の使い方
- Inspectorに公開したいフィールドは `public` ではなく `[SerializeField] private` を使う
- `public` フィールドは原則禁止（プロパティ経由でアクセスする）

```csharp
// Good
[SerializeField] private float _moveSpeed = 5f;
public float MoveSpeed => _moveSpeed;

// Bad
public float moveSpeed = 5f;
```

## null チェック
- Unity オブジェクトの null チェックは `== null` を使う（`?.` 演算子は Unity の疑似 null に対応していないため）

```csharp
// Good
if (_rigidbody == null) return;

// Bad（Unityオブジェクトには使わない）
_rigidbody?.AddForce(direction);
```

## コメント方針
- クラスと public メソッドには必ず XML ドキュメントコメントを書く
- 処理の「何をするか」ではなく「なぜそうするか」をコメントする
- TODO コメントは `// TODO(担当者名): 内容` の形式で書く

```csharp
/// <summary>
/// プレイヤーにダメージを与える。
/// ダメージ計算後にHPが0以下になった場合は死亡処理を呼び出す。
/// </summary>
/// <param name="amount">与えるダメージ量（正の値）</param>
public void TakeDamage(int amount)
{
    // 防御力計算はBalanceManagerに委譲するため、ここでは生の値を渡す
    _currentHp -= amount;
    if (_currentHp <= 0) Die();
}
```

## その他
- マジックナンバーは定数または `[SerializeField]` フィールドに抽出する
- `#region` は使用禁止（クラスが大きくなっているサインであるため、分割を検討する）
- 1クラス1ファイル。ファイル名はクラス名と一致させる
