# アーキテクチャ・設計方針（DDD × Unity）

## レイヤー構成

```
Assets/Scripts/
├── Presentation/   # MonoBehaviour。View・入力受付のみ
├── Application/    # ユースケース。ゲームの「何をするか」を記述
├── Domain/         # ゲームの核心ロジック。Unityに依存しない
└── Infrastructure/ # 外部システム連携（Save/Load・Audio・外部APIなど）
```

## 各レイヤーの責務

### Presentation層
- `MonoBehaviour` を継承するクラスはここにのみ配置する
- UIの表示・更新、プレイヤー入力の受付のみ行う
- ゲームロジックを**直接書かない**
- Application層のサービスやユースケースを呼び出す

```csharp
// Good: Applicationに委譲する
public class PlayerHpView : MonoBehaviour
{
    [SerializeField] private HpBarUI _hpBar;
    private PlayerHpUseCase _useCase;

    private void Start()
    {
        _useCase = ...; // DI or Locator経由で取得
        _useCase.OnHpChanged += _hpBar.UpdateDisplay;
    }
}

// Bad: Presentationにゲームロジックを書かない
public class PlayerHpView : MonoBehaviour
{
    private int _hp = 100;
    public void TakeDamage(int amount) { _hp -= amount; } // NG
}
```

### Application層
- ユースケースクラス（`〜UseCase`）を配置する
- Domain層のモデル・サービスを組み合わせてユースケースを実現する
- Unityの型（`Vector3`、`GameObject` 等）に依存しない
- `MonoBehaviour` を継承しない

### Domain層
- **Unityへの依存を一切持たない**（`using UnityEngine` 禁止）
- エンティティ・値オブジェクト・ドメインサービス・リポジトリインターフェースを置く
- ゲームの業務ルール（HP上限・ダメージ計算式など）はすべてここに集約する
- 外部ライブラリへの依存も原則禁止

```csharp
// Domain/Model/PlayerHp.cs
public sealed class PlayerHp
{
    public int Value { get; private set; }
    public int Max { get; }

    public PlayerHp(int max)
    {
        Max = max;
        Value = max;
    }

    public PlayerHp Damage(int amount)
    {
        // ドメインルール: HPは0未満にならない
        Value = Math.Max(0, Value - amount);
        return this;
    }

    public bool IsDead => Value <= 0;
}
```

### Infrastructure層
- リポジトリの実装（セーブデータ読み書き等）を配置する
- AudioManager、NetworkClientなど外部システムのアダプターを置く
- Domain層のインターフェースを実装する

## 依存方向のルール

```
Presentation → Application → Domain ← Infrastructure
```

- 矢印は「依存する方向」を示す
- **Domain層は何にも依存しない**（依存関係の中心）
- 外側のレイヤーは内側のレイヤーにのみ依存する
- 逆方向の依存（Domain → Presentation など）は禁止

## フォルダ命名規則

```
Domain/
├── Model/       # エンティティ・値オブジェクト
├── Service/     # ドメインサービス
└── Repository/  # リポジトリインターフェース（IPlayerRepository等）

Application/
└── UseCase/     # ユースケースクラス（PlayerAttackUseCase等）

Infrastructure/
├── Repository/  # リポジトリ実装
└── Adapter/     # 外部システムのアダプター
```

## MonoBehaviour の配置ルール
- `MonoBehaviour` はPresentation層のみに配置する
- Application・Domain・Infrastructure層では `MonoBehaviour` を継承しない
- DIはVContainerを使用する（未導入の場合はServiceLocatorパターンで代替）
