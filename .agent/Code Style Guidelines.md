# Code Style Guidelines

このドキュメントは、プロジェクトにおけるコーディング規約とドキュメント化のルールを定義します。
## File Organization

コードの治安と保守性を維持するため、ファイル構成に以下のルールを適用します。

### 1. Interface and Implementation Separation
- **原則**: インターフェースとその実装クラス（派生クラス）は、必ず**別々のファイル**として定義してください。
- **理由**: インターフェースは複数の継承が考えられるため、同一ファイルに実装を含めると、継承時に不要なクラス構造まで参照されてしまう（治安が悪くなる）のを防ぐためです。

## Documentation (XML Comments)

コードの可読性と保守性を高めるため、以下のXMLコメント規則を徹底してください。

### 1. Interface-first Documentation
- **原則**: インターフェース側に詳細なドキュメントを記載します。
- **項目**: `<summary>`, `<param>`, `<returns>` など、そのメンバの目的と振る舞いを明確に定義してください。

### 2. Implementation with `<inheritdoc />`
- **原則**: 実装クラス（派生クラス）では、基本的に `<inheritdoc />` を使用してインターフェース側のコメントを継承します。
- **メリット**: ドキュメントの二重管理を防ぎ、情報の整合性を保ちます。

### 3. Usage of `<remarks>`
- **派生クラス固有の記述**: 実装クラス独自の注意点、制約、利用想定がある場合は `<remarks>` タグを使用します。
- **ベース側の参照**: インターフェース側の `<remarks>` も参照・表示させたい場合は、派生クラスの `<remarks>` 内で `<see cref="..." />` を使用して明示的に誘導してください。
  - ※ C#の仕様上、派生側で `<remarks>` を書くとベース側の `<remarks>` が上書き（非表示）されるため、重要な情報は `see` で繋ぐ必要があります。

### 4. Code Standards
- **sealed**: 特殊な理由（継承を前提とした設計など）がない限り、クラスには原則として `sealed` キーワードを付与してください。
- **Encapsulation**: メンバのアクセス修飾子は最小限（private/protected）に留め、必要に応じて公開してください。

## Example

```csharp
/// <summary>
/// ターミナルのテーマ設定を提供します。
/// </summary>
/// <remarks>
/// 基本的な配色やフォント情報を保持します。
/// </remarks>
public interface ITerminalTheme
{
    /// <summary>背景色を取得します。</summary>
    Color Background { get; }
}

/// <inheritdoc />
/// <remarks>
/// Unityのランタイム上で使用されるデフォルトの実装です。
/// <see cref="ITerminalTheme"/> の基本ルールに従います。
/// </remarks>
public sealed class TerminalTheme : ITerminalTheme
{
    public Color Background { get; set; }
}
```
