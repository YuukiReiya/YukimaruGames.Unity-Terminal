# プロジェクト構造

## 概要

本プロジェクト（`com.yukimaru-games.terminal`）は Unity 向けのターミナルUIライブラリ（UPMパッケージ）である。
ランタイムでターミナル風のインタラクションをUnityプロジェクトに追加するためのツール。

## パッケージ構成

```
Assets/YukimaruGames/Terminal/
├── Editor/              # Editorスクリプト
├── Runtime/             # ランタイムコード（メイン開発対象）
│   ├── SharedKernel/    # 共有カーネル（Enum, Interface）
│   ├── Domain/          # ドメイン層
│   │   ├── Abstractions/  # Domain.API（インターフェース・モデル定義）
│   │   └── Services/      # Domain.Core（ドメインロジック実装）
│   ├── Application/     # アプリケーション層
│   │   ├── Interfaces/    # アプリケーション固有のインターフェース
│   │   ├── Mappers/       # データ変換
│   │   ├── Models/        # DTO等
│   │   └── Services/      # アプリケーションサービス
│   ├── Presentation/    # プレゼンテーション層
│   │   ├── Accessors/     # UIコンポーネントアクセス
│   │   ├── Animators/     # アニメーション制御
│   │   ├── Constants/     # 定数
│   │   ├── Coordinators/  # フロー制御
│   │   ├── Events/        # イベント
│   │   ├── Interfaces/    # Presentation固有インターフェース
│   │   ├── Models/        # Viewモデル
│   │   ├── Presenters/    # Presenter
│   │   └── Renderers/     # 描画処理
│   ├── Infrastructure/  # インフラストラクチャ層
│   │   ├── Commands/      # コマンド実装
│   │   └── UI/            # Unity UI依存の実装
│   └── Runtime/         # Composition Root
│       ├── Bootstrapper/    # エントリーポイント
│       ├── Configuration/   # 設定定義
│       ├── Context/         # コンテキスト管理
│       ├── Input/           # 入力システム統合
│       ├── Installer/       # DI登録
│       ├── Interface/       # Runtime固有インターフェース
│       ├── Lifecycle/       # ライフサイクル管理
│       ├── Model/           # Runtimeモデル
│       └── Shared/          # 内部共有
├── Samples~/            # サンプルシーン
├── Tests/               # テストコード
├── package.json         # UPMパッケージ定義
└── CHANGELOG.md         # 変更履歴
```

## アセンブリ一覧

| Assembly Definition | レイヤー | パス |
|---------------------|---------|------|
| `YukimaruGames.Terminal.SharedKernel` | SharedKernel | `Runtime/SharedKernel/` |
| `YukimaruGames.Terminal.Domain.API` | Domain.API | `Runtime/Domain/Abstractions/` |
| `YukimaruGames.Terminal.Domain.Core` | Domain.Core | `Runtime/Domain/Services/` |
| `YukimaruGames.Terminal.Application` | Application | `Runtime/Application/` |
| `YukimaruGames.Terminal.Presentation` | Presentation | `Runtime/Presentation/` |
| `YukimaruGames.Terminal.Infrastructure` | Infrastructure | `Runtime/Infrastructure/` |
| `YukimaruGames.Terminal.Runtime` | Composition Root | `Runtime/Runtime/` |

## 使用パッケージ・ライブラリ

外部の非同期・DI・Reactiveライブラリ（UniTask, VContainer, UniRx/R3等）には**依存していない**。
`manifest.json` にはUnity公式パッケージ以外の依存はない。

- DI: `IInstaller` / `TerminalRuntimeScope` による自前実装（Composition Rootが配線を担う。VContainer等のDIコンテナは未使用）
- 非同期処理: 標準の `System.Threading.Tasks` を使用

## 命名の対応表（ドメイン用語）

<!-- TODO(USER): プロジェクト固有のドメイン用語があれば追記してください -->

| 日本語 | コード上の名称 |
|--------|--------------|
| ターミナル | Terminal |
| コマンド | Command |
| テーマ | Theme |
| ブートストラッパー | Bootstrapper |
| インストーラー | Installer |

## 注意事項

- `Resources.Load()` は極力使用しない（Addressablesを検討する）
- `FindObjectOfType()` は使用禁止（DI経由で依存を解決する）
- パスの起点は `Assets/YukimaruGames/Terminal/Runtime/` であり `Assets/Scripts/` ではない
