# コーディングスタイル規約

## 言語・バージョン
- C# 9.0以上を使用する
- Unity 2022 LTS以降を前提とする

## 命名規則

| 対象 | 規則 | 例 |
|------|------|-----|
| 名前空間・クラス・構造体・enum | PascalCase | `CommandRegistry`, `MessageType` |
| インターフェース | `I` + PascalCase | `ICommandRegistry`, `ICommandLogger` |
| メソッド | PascalCase | `TryGet()`, `Add()` |
| プロパティ | PascalCase | `EntryPoint`, `IsInstalled` |
| privateフィールド | _camelCase | `_commands`, `_logger` |
| 定数（const） | PascalCase | `BootUpMessage`, `Window` |
| ローカル変数・引数 | camelCase | `command`, `methodInfo` |

命名にはライブラリ名（`Terminal`）のプリフィックスを付けない。名前空間がコンテキストを表すため、`TerminalCommandRegistry` ではなく `CommandRegistry` のように簡潔にする。

## クラス内の記述順序

1. `[SerializeField]` フィールド／Unityイベント関連フィールド
2. privateフィールド（`readonly` を優先する）
3. コンストラクタ
4. プロパティ
5. Unityライフサイクルメソッド（`MonoBehaviour` を継承する場合のみ、実行順に並べる: `Awake` → `OnValidate` → `Update` → `OnGUI` → `OnDestroy` 等）
6. publicメソッド
7. privateメソッド

```csharp
public sealed class CommandRegistry : ICommandRegistry
{
    private readonly Dictionary<string, CommandHandler> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICommandLogger _logger;

    public CommandRegistry(ICommandLogger logger) => _logger = logger;

    public bool Add(string command, CommandHandler handle) { /* ... */ }

    private void Add(string command, MethodInfo methodInfo, TerminalCommandAttribute attribute) { /* ... */ }
}
```

`MonoBehaviour` を使うのは Composition Root（`Runtime/Bootstrapper`）や入力アダプター（`Adapters/Input`）などごく一部に限られる。それ以外のレイヤー（Domain / Application / Presentation の大半 / Infrastructure）はプレーンなC#クラスであり、Unityライフサイクルメソッドの並びは適用対象外。

## SerializeField の使い方
- Inspectorに公開したいフィールドは `public` ではなく `[SerializeField] private` を使う
- `public` フィールドは原則禁止（プロパティまたはpublicメソッド経由でアクセスする）

```csharp
// Good
[SerializeField, SerializeInterface] private IInstaller _installer = new TerminalStandardInstaller();

// Bad
public IInstaller installer;
```

## null チェック
- `UnityEngine.Object` を継承する型（`MonoBehaviour`, `Component`, `GameObject` 等）のnullチェックは `== null` / `!= null` を使う（Unityの疑似nullに対応していないため `?.` は避ける）
- それ以外のプレーンなC#型（インターフェース実装、POCO、ドメインモデル等）では `?.` や `??` を通常どおり使ってよい

```csharp
// Unityオブジェクト（Good）
if (_scope != null)
{
    _installer?.Uninstall(_scope);
}

// プレーンなC#型（Good）
_logger?.Send(MessageType.Error, $"Command '{command}' is already defined.");
```

## コメント方針
- クラスと public メソッド・プロパティには XML ドキュメントコメント（`<summary>`）を書く。文末は句点で終える
- インターフェースの実装メソッドで説明が重複する場合は `<inheritdoc/>` を使う
- 処理の「何をするか」ではなく「なぜそうするか」をコメントする
- TODO コメントは `// TODO: 内容` の形式で書く（担当者名は載せない）

```csharp
/// <summary>
/// コマンドの追加.
/// </summary>
/// <param name="command">コマンド名</param>
/// <param name="handle">コマンドのハンドル</param>
public bool Add(string command, CommandHandler handle)
{
    if (_commands.TryAdd(command, handle)) return true;
    _logger?.Send(MessageType.Error, $"Command '{command}' is already defined.");
    return false;
}

/// <inheritdoc/>
public bool TryGet(string command, out CommandHandler handler) => _commands.TryGetValue(command, out handler);
```

## その他
- マジックナンバー・マジックストリングは `const` または `[SerializeField]` フィールドに抽出する
- 1クラス1ファイル。ファイル名はクラス名と一致させる
  - 例外: DTOや軽量なValueObject等、関連性の高い小さな型は1ファイルに集約してよい（この場合ファイル名は集約単位を表す名前にする）
- レイヤー構成・依存方向のルールは [02-architecture-ddd.md](02-architecture-ddd.md) を参照する
