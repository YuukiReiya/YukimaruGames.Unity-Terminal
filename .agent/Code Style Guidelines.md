# Code Style Guidelines

このドキュメントは、プロジェクトにおけるコーディング規約とドキュメント化のルールを定義します。
## File Organization

コードの治安と保守性を維持するため、ファイル構成に以下のルールを適用します。

### 1. Interface and Implementation Separation
- **原則**: インターフェースとその実装クラス（派生クラス）は、原則として**別々のファイル**として定義してください。
- **理由**: インターフェースは複数の継承が考えられるため、同一ファイルに実装を含めると、継承時に不要なクラス構造まで参照されてしまう（治安が悪くなる）のを防ぐためです。
- **例外 (Interface Grouping)**:  `IXXXXProvider`, `IXXXXMutator`, および複合インターフェースである `IXXXXAccessor` のような、**密接に関連し、単一の責務を多角的に定義する抽象型のセット**については、過度なファイル分割を避けるため、1つの `.cs` ファイルにまとめて記述することを推奨します。
  - **例**: `IScrollAccessor.cs` 内に `IScrollProvider`, `IScrollMutator` を定義する。

### 2. Technical Folder Pluralization (複数形アライメント)
- **原則**: `Attributes` や `Exceptions` といった、技術的なカテゴリを表すフォルダ名および名前空間は、必ず**複数形**に統一してください。
- **理由**: そのフォルダが「属性（Attribute）の集まり」や「例外（Exception）の定義群」であることを明示し、個別のクラス名（単数形）との名前の衝突を防ぐためです。

### 3. Technical Layering (技術別フォルダ構成)
- **原則**: 各レイヤー（Domain, Application, Presentation, Infrastructure）の内部は、機能単位ではなく**技術的な役割（Interfaces, Services, Presenters, Renderers, Models等）**で整理します。
- **命名規則（複数形 vs 単数形）**: 
  - **技術トップディレクトリ**: 必ず**複数形**とします（例: `Presenters`, `Interfaces`, `Models`）。
  - **機能サブディレクトリ**: 必ず**単数形**とします（例: `Log`, `Input`, `Window`）。
  - **名前空間**: 物理ディレクトリと1字1句一致させます（例: `YukimaruGames.Terminal.Presentation.Models.Log`）。
- **フォルダの階層化**: 技術フォルダ内において、関連するファイル群をまとめる場合は**1段までのサブフォルダ化**を行い、プロジェクト全体の構造的対称性を維持します。
- **理由**: UPMパッケージとしての各コンポーネントの再利用性を高めつつ、複雑なUI構成における視認性と名前空間の整合性を両立させるためです。

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

### 5. Naming by Abstraction Level (抽象度による命名)
システム内の役割を明確にするため、抽象度のレベルに応じて一貫した語彙を使用してください。

- **`Provider` (供給者 / 読み取り専用窓口)**
  - **役割**: インターフェースとして定義され、データの取得機能のみを外部に公開する。
  - **性質**: 依存先に対して「読み取り専用」であることを保証するために使用する。
  - **例**: `IFontProvider`, `IColorPaletteProvider`

- **`Mutator` (変異者 / 書き込み専用窓口)**
  - **役割**: インターフェースとして定義され、データの変更機能（SetterやMethod）のみを外部に公開する。
  - **性質**: 演出の実行や設定の更新など、「状態を変化させる」責務を明示するために使用する。
  - **例**: `IAnimationDataMutator`, `IScrollMutator`

- **`Accessor` (アクセス者 / 状態の実体と同期)**
  - **Interface (`IXXXXAccessor`)**: `IXXXXProvider` と `IXXXXMutator` の両方を継承した複合インターフェース。
    - **用途**: `WindowPresenter` のように、同一の対象に対して「取得」と「変更」の両方の権限を単一の依存先として必要とする場合に定義する。
  - **Concrete Class (`XXXXAccessor`)**: この実体クラス。`Provider` と `Mutator`（および定義されていれば `Accessor` インターフェース）を実装する。
  - **性質**: データの「実体」を保持し、インスペクターからの設定反映やシステム内での状態共有の基盤となる。
  - **配置に関する重要ルール (境界保護)**: 
    - **Presentation層**: 描画（`UnityEngine.Color`, `GUIStyle` 等）に密結合するAccessorは、**Presentation層内でInterfaceと実装を完結**させます。これは Application層（Pure C#）のインターフェースにUnity依存を流入させないための境界保護措置です。
    - **Infrastructure層**: ファイルIO、ネットワーク、リフレクション等、外部システムとの接続を担うAccessorは、**Application/Domain層でPure C#なインターフェースを定義**し、その実装をInfrastructureに配置します（DIP）。
  - **例**: `IAnimationDataAccessor`, `AnimationDataAccessor`

- **`Context` (文脈 / 状態管理 / 複合構成)**
  - **役割**: 複数の素材やパラメータを組み合わせ、特定の用途（UIスタイルなど）において整合性が取れた「状態」として提供する。
  - **性質**: 高レイヤーであり、複数のProviderを組み合わせて利用（依存）することが一般的。
  - **例**: `StyleContext`, `ViewContext`

- **`ValueObject` / `Model` (値オブジェクト / モデル)**:
  - **役割**: 等価性を持つ不変なデータ、または特定の文脈（UI等）のみで共有されるデータ構造を表す。
  - **配置**: そのモデルが最も本質的に関わるレイヤー（`Domain`, `Presentation`, `Infrastructure`, `Runtime` 等）の **`.Models`** フォルダに配置する。
  - **原則**: 他レイヤーから不要なアクセスを望まない場合は、対象となるレイヤーに閉じ込めるように配置を決定する。

- **`Domain Service` (ドメインサービス)**:
  - **役割**: 「ターミナルとしての根本的なルール」に基づいた計算や状態変化を行う。
  - **配置**: `Domain/Services/`
  - **特徴**: 自己完結したロジック（パース、履歴管理等）を持つ。

- **`Application Service` (アプリケーションサービス)**:
  - **役割**: 「システムの進行役（Coordinator/Facade）」。UIからの入力を受け取ってドメインを動かす。
  - **配置**: `Application/Services/`
  - **特徴**: ロジック自体は持たず、複数のドメインサービスやリポジトリの呼び出し制御に徹する。

- **`UseCase` (ユースケース)**:
  - **役割**: 「特定の完了した処理シナリオ（ワークフロー）」。
  - **配置**: `Application/UseCases/`
  - **特徴**: 単なる移譲以上の、特定のビジネス目的に沿った一連の操作手順をカプセル化する。

- **重要**: 命名の新たな粒度やパターン（例: イベントを示す `Evt`, 通信用の `Request`/`Response` など）が導入される際は、適宜このガイドラインを更新し、一貫性を維持してください。

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
