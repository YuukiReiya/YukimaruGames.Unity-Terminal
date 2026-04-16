# System Patterns

## アーキテクチャ

DDD（ドメイン駆動設計）に基づく6層アーキテクチャを採用。
各レイヤーはAssembly Definition（`.asmdef`）で物理的に依存方向が強制されている。

```
Assets/YukimaruGames/Terminal/Runtime/
├── SharedKernel/        # 全レイヤー共通（Enum, Interface）— Zero Dependency
├── Domain/
│   ├── Abstractions/    # Domain.API: インターフェース・ドメインモデル（純粋抽象）
│   └── Services/        # Domain.Core: ドメインロジック実装
├── Application/         # オーケストレーション・アプリケーション状態
├── Presentation/        # View・Presenter（UI・入力）
├── Infrastructure/      # 外部システム連携・実装詳細
└── Runtime/             # Composition Root（Bootstrapper・DI配線）
```

依存方向:
```
Presentation → Application → Domain.API ← Domain.Core
                                  ↑
                            Infrastructure

SharedKernel ← 全レイヤーが参照可能
Runtime（Composition Root）→ 全レイヤー
```

## 主要な設計パターン

### DIコンテナ
<!-- TODO(USER): 実際の使用状況に合わせて更新してください -->
- 使用ツール: VContainer（未導入の場合はServiceLocatorパターンで代替）
- 登録場所: `Runtime/Installer/`（TerminalStandardInstaller等）

### イベント通知
<!-- TODO(USER): 実際の使用方法を記載してください -->
- 使用方法:（例: UniRx / R3 / C# event）

### 非同期処理
<!-- TODO(USER): 実際の使用方法を記載してください -->
- 使用方法:（例: UniTask）

### Configuration & Extensibility
- **Zero Config Defaults**: 標準構成はPOCOで動作し、ScriptableObject不要
- **Installer Responsibility**: Installerが構成の定義・取得を担当
- **Bootstrapperは設定状態を保持しない**: 構成取得はInstallerに委譲

## コンポーネントの関係

<!-- TODO(USER): 主要なクラス間の関係を記載してください -->
- `TerminalBootstrapper` — エントリーポイント。Installerを呼び出してDI配線を行う
- `TerminalStandardInstaller` — 標準のDI登録実装
- `IInstaller` — Installer契約の定義（Runtime/Interface/）

## 重要な実装方針

- **Vertical Slice**: レイヤー内部は技術的役割ではなく機能単位で整理
- **No Prefix命名**: 名前空間がコンテキストを定義するため、ライブラリ名プリフィックス不要（`TerminalView` → `MainView`）
- **Interface/Implementation分離**: インターフェースと実装は必ず別ファイル
- **`sealed` デフォルト**: 特殊な理由がない限りクラスには `sealed` を付与
- **`<inheritdoc />` 活用**: 実装クラスではインターフェースのドキュメントを継承
