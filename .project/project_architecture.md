# Project Architecture Analysis (YukimaruGames.Unity-Terminal)

## 📌 現在のアーキテクチャ方針
当プロジェクトは、当初のVertical Slice Architecture (VSA)から、依存性逆転と明確なレイヤリングを強制する **Evolved Clean Architecture (Layered)** へリファクタリングを進行中です。
パッケージの再利用性を高めるため、各レイヤー内部は機能単位ではなく **「技術的な役割（Technical Layering）」** でフォルダ分割されます。

---

## 🏗️ 構成レイヤーと現在の解析結果

### 1. `Domain.Abstractions` (抽象・定義)
ドメインの契約、不変のモデル、例外、メタデータを定義する中核部分です。実装コードを持たず、他レイヤーからの依存対象となります。
- **配置**: `Runtime/Domain/Abstractions/`
- **解析内容**:
  - `Interfaces/Services/`: `ICommandParser`, `ICommandRegistry` など、ドメインロジックの契約。
  - `Interfaces/Repositories/`: `ICommandHistory` など、データの永続化に関する契約。
  - `Models/Entities/`: `CommandLog` など一意性を持つ要素。
  - `Models/ValueObjects/`: `CommandArgument`, `CommandMeta` など不変のデータ構造。
  - `Attributes/`, `Exceptions/`: 属性と例外。

### 2. `Domain.Services` (ドメイン実装)
`Domain.Abstractions` で定義されたインターフェースの実態（コアなビジネスロジック）を実装します。
- **配置**: `Runtime/Domain/Services/`
- **解析内容**:
  - `Services/`: `CommandParser`, `CommandRegistry`, `CommandInvoker` など。複雑なパースや管理ロジックを実行します。
  - `Repositories/`: `CommandHistory` （現状ドメイン層に存在しますが、本来の構成ではインフラ層に属する候補です）。

### 3. `Application` (アプリケーション・調整)
外界からの要求を受け取り、ドメイン層のサービスを統括（オーケストレーション）する層です。
- **配置**: `Runtime/Application/`
- **解析内容** (Technical Layering移行済):
  - `Interfaces/`: `ITerminalService`
  - `Services/`: `TerminalService`。複数のドメインサービスを使い、コマンド実行フローを制御。
  - `Models/`: `LogEntry` などのDTO。
  - `Mappers/`: `LogMapper`。ドメインの `CommandLog` をアプリケーション層の `LogEntry` に変換。

### 4. `Infrastructure` (インフラ・技術詳細)
UnityのAPIやC#のリフレクションなど、プラットフォーム依存の強い処理を実行します。（※ Technical Layering への再編待ち）
- **配置**: `Runtime/Infrastructure/`
- **現状の解析内容** (VSA形式から再編予定):
  - `Commands/`: `CommandDiscoverer` (リフレクション探索), `CommandFactory` (インスタンス生成)。
  - `UI/`: `GUIStyleAccessor`, `ColorPaletteAccessor`, `PixelTextureRepository` など。UnityのUIシステムとのアダプター群。
  - **今後の移行予定**: `Discovery`, `Factories`, `Accessors`, `Repositories` への分割。

### 5. `UI` -> `Presentation` (プレゼンテーション)
Renderer(View)とPresenterから成り、UIの描画とイベント処理を担います。MVP (Passive View) パターンを採用しています。（※ Technical Layering への再編待ち）
- **配置**: `Runtime/UI/` (今後 `Presentation/` へ改称予定)
- **現状の解析内容** (VSA形式):
  - `Main/`, `Log/`, `Input/`, `Launcher/`, `Window/` など機能単位でフォルダが分かれています。
  - クラス: `TerminalCoordinator`, `TerminalWindow(MainView)`, 各RendererおよびPresenter。
  - **今後の再編（決定事項）**:
    - `TerminalWindow` / `TerminalWindowPresenter` の分離。
    - `TerminalCoordinator` による複数Presenterの統合管理。
    - フォルダは `Presenters`, `Renderers`, `Interfaces`, `Accessors` の技術トップレベルへ再編。

### 6. `Runtime` ( Composition Root / Bootstrapper)
DI（依存性注入）やライフサイクル管理を行い、すべてのレイヤーを結合します。
- **配置**: `Runtime/Runtime/`
- **解析内容**:
  - `Bootstrapper/TerminalBootstrapper`: MonoBehaviourエントリーポイント。
  - `Installer/TerminalStandardInstaller`: 具象クラスを登録するDI設定。
  - `Lifecycle/TerminalEntryPoint`: 実質的な初期化ロジック。

### 7. `SharedKernel` (共有カーネル)
全レイヤーから参照可能な基本機能、拡張メソッド、全域的なEnumを提供します。
- **配置**: `Runtime/SharedKernel/`
- **解析内容**:
  - `MessageType` などのEnum、`IUpdatable` インターフェースなど。
