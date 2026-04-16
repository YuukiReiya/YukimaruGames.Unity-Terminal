# アーキテクチャ・設計方針（DDD × Unity）

## レイヤー構成

本プロジェクトはDDDに基づく6層アーキテクチャを採用している。
各レイヤーはAssembly Definition（`.asmdef`）で物理的に依存方向が強制されている。

```
Assets/YukimaruGames/Terminal/Runtime/
├── SharedKernel/        # 全レイヤー共通の型定義（Enum, Interface）
├── Domain/              # ドメイン層（2分割）
│   ├── Abstractions/    # Domain.API: インターフェース・ドメインモデル（純粋抽象）
│   └── Services/        # Domain.Core: ドメインロジック実装
├── Application/         # アプリケーション層: ユースケース・オーケストレーション
├── Presentation/        # プレゼンテーション層: View・Presenter（UI・入力）
├── Infrastructure/      # インフラストラクチャ層: 外部システム連携・実装詳細
└── Runtime/             # Composition Root: Bootstrapper・DI配線
```

## 各レイヤーの責務

### 1. SharedKernel（`YukimaruGames.Terminal.SharedKernel`）

- 全レイヤーで共通利用されるEnum・Interface・拡張メソッド等を配置する
- **依存: なし（Zero Dependency）**
- **制約**: 他のどのレイヤーにも依存してはならない

### 2. Domain層（2分割構成）

依存逆転の原則（DIP）を徹底するため、Domain層は2つのアセンブリに分離している。

#### Domain.API（`YukimaruGames.Terminal.Domain.API`）— Abstractions/

- **役割**: インターフェース（`IUseCase`, `IRepository`, `IService`）とドメインモデル（純粋POCO）を定義する
- **依存**: `SharedKernel` のみ
- **制約**: Domain.Core・Application・Presentation・Infrastructureに**依存してはならない**。純粋な抽象定義のみ

#### Domain.Core（`YukimaruGames.Terminal.Domain.Core`）— Services/

- **役割**: ドメインロジックの実装（UseCase実装・ドメインサービス）
- **依存**: `Domain.API`, `SharedKernel`
- **制約**: Infrastructure・Presentationに**依存してはならない**

### 3. Application層（`YukimaruGames.Terminal.Application`）

- **役割**: ドメインロジックを組み合わせたオーケストレーション。アプリケーション状態の保持
- **依存**: `Domain.API`, `SharedKernel`
- **制約**: `Domain.API`のインターフェースを使用し、`Domain.Core`の具象クラスを直接参照**してはならない**

```
Application/
├── Interfaces/    # アプリケーション層固有のインターフェース
├── Mappers/       # データ変換・マッピング
├── Models/        # アプリケーション層のモデル（DTO等）
└── Services/      # アプリケーションサービス
```

### 4. Presentation層（`YukimaruGames.Terminal.Presentation`）

- **役割**: View・Presenterロジック。ユーザーインタラクションの処理
- **依存**: `Application`（Interactor/Presenter経由）, `SharedKernel`
- **制約**: Infrastructure・Domain.Coreを**直接参照してはならない**

```
Presentation/
├── Accessors/       # UIコンポーネントへのアクセス
├── Animators/       # アニメーション制御
├── Constants/       # Presentation層固有の定数
├── Coordinators/    # 画面遷移・フロー制御
├── Events/          # Presentationイベント
├── Interfaces/      # Presentation層固有のインターフェース
├── Models/          # View用モデル
├── Presenters/      # Presenterクラス
└── Renderers/       # 描画処理
```

### 5. Infrastructure層（`YukimaruGames.Terminal.Infrastructure`）

- **役割**: `Domain.API`で定義されたインターフェースの具象実装（リポジトリ・外部サービスアダプター）
- **依存**: `Domain.API`, `Domain.Core`（ロジック生成が必要な場合）, `SharedKernel`
- **制約**: 実装詳細レイヤー。外部から直接参照されない

```
Infrastructure/
├── Commands/    # コマンド実装
└── UI/          # Unity UI依存の実装
```

### 6. Composition Root（`YukimaruGames.Terminal.Runtime`）

- **役割**: エントリーポイント（Bootstrapper）。DIコンテナの配線
- **依存**: 全レイヤー
- **制約**: **全レイヤーの具象実装を参照できる唯一の場所**。Bootstrapperは設定状態を保持しない

```
Runtime/
├── Bootstrapper/    # エントリーポイント
├── Configuration/   # 設定定義
├── Context/         # コンテキスト管理
├── Input/           # 入力システム統合
├── Installer/       # DI登録ロジック
├── Interface/       # Runtime層固有のインターフェース
├── Lifecycle/       # ライフサイクル管理
├── Model/           # Runtime層のモデル
└── Shared/          # Runtime層内共有
```

## 依存方向のルール

```
Presentation → Application → Domain.API ← Domain.Core
                                  ↑
                            Infrastructure

SharedKernel ← 全レイヤーが参照可能

Runtime（Composition Root）→ 全レイヤー（唯一の例外）
```

- 矢印は「依存する方向」を示す
- **Domain.APIは何にも依存しない**（SharedKernelを除く）
- 外側のレイヤーは内側のレイヤーにのみ依存する
- 逆方向の依存（Domain → Presentation など）は禁止

## .asmdef による物理的な依存方向の強制

依存方向の違反はAssembly Definition（`.asmdef`）のreferencesで物理的にコンパイルエラーとなる。

- `Domain.API` は `Domain.Core` を参照していない
- `Application` は `Domain.Core` を参照していない（抽象に依存）
- `Presentation` は `Infrastructure` を参照していない

## よくあるレイヤー違反

| 違反 | 説明 | 対策 |
|------|------|------|
| **循環依存** | Domain.API が Domain.Core を参照 | `.asmdef` で物理的に防止済み |
| **ロジック漏出** | Presentationが Domain.Core を直接参照 | Application（Presenter）経由で呼び出す |
| **隠れた結合** | SharedKernel がUI型に依存 | SharedKernelは純粋C#型のみ |
| **構成漏出** | Bootstrapperが構成状態を保持 | 構成取得はInstallerに委譲する |

## 設計原則

### Internal Module Organization（Vertical Slice）
各レイヤーの内部構造は技術的な役割（Presenter/View等）ではなく、機能単位（Vertical Slice）で整理する。

### 命名規則（No Prefix）
- 各モジュールの内部クラスにはライブラリ名（`Terminal`）のプリフィックスを付けない
- 名前空間がコンテキストを定義するため、`TerminalView` → `MainView` のように簡潔にする

### Configuration & Extensibility
- **Zero Config Defaults**: 標準構成はPOCOで動作し、ScriptableObject不要
- **Installer Responsibility**: Installerが構成の定義・取得を担当。Bootstrapperに構成依存を注入しない
- **Meaningful Fields**: 特定のInstaller実装でのみ使う `[SerializeField]` をBootstrapperに置かない（Logic Leakage）
