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
├── Samples~/            # Sample群（Package ManagerのSamplesタブからImport。1Sample=1サブフォルダ）
│   ├── BasicSetup/            # "Basic Setup & Commands"（デモ・チュートリアル）
│   ├── UIToolkit/              # "UI Backend: UIToolkit"（コード。正式な機能拡張）
│   └── UIToolkit.Resources/    # "UI Backend: UIToolkit Default Resources"
│       └── Resources/Terminal/UIToolkit/  # Resources.Loadで解決されるデフォルトアセット
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

- `FindObjectOfType()` は使用禁止（DI経由で依存を解決する）
- パスの起点は `Assets/YukimaruGames/Terminal/Runtime/` であり `Assets/Scripts/` ではない

## UIバックエンド用Sampleの同梱デフォルトアセット規約

UIToolkit/uGUI等、任意導入のUIバックエンドは**別パッケージではなくPackage ManagerのSamples機構**
（`package.json`の`"samples"`配列、`~`サフィックスでAssetDatabaseから除外されるフォルダ）で提供する。

### すべて`Samples~/`配下に置く（重要な制約）

Unity公式マニュアルにより、`package.json`の`samples[].path`は**必ず`Samples~/`から始まる
パスでなければならない**（"the path to the sample folder starting at the `Samples~` folder"）。
`Samples~/`以外のパッケージ直下トップレベルに独自の`~`フォルダ（例: `UIToolkit~/`）を作る方式は、
Unityが`~`終わりのフォルダをAssetDatabaseから除外すること自体は正しいが、Package Managerの
Samplesタブ・Import機能としては**公式にサポートされない**（2026-08-10、実装前に判明・訂正）。

「デモ・チュートリアル」（例: `BasicSetup`）と「正式な機能拡張」（UIToolkit/uGUI等）は性質が
異なるが、両者とも`Samples~/`直下の**サブフォルダとして区別する**（`Samples~/BasicSetup/`・
`Samples~/UIToolkit/`のように、フォルダを分けることで区別し、トップレベル自体は分けない）。

### バックエンドごとに「コード」と「デフォルトアセット」を別Sampleに分ける

機能だけ導入して自前アセットに差し替えたいユーザーが、不要な`Resources`同梱を避けられるようにする。

- `package.json`の`"samples"`エントリは`"UI Backend: <Backend>"`（コード）／
  `"UI Backend: <Backend> Default Resources"`（アセット）の組で用意する
  （Package ManagerのSamplesタブはカテゴリ表示を持たないフラット一覧のため、視覚的にまとまるよう
  displayNameのプレフィックスで揃える）
- コード側配置・`"path"`: `Samples~/<Backend>/`（例: `Samples~/UIToolkit/`）。`Resources`フォルダは
  含まない
- デフォルトアセット側配置: `Samples~/<Backend>.Resources/Resources/Terminal/<Backend>/` 配下
  （例: `Samples~/UIToolkit.Resources/Resources/Terminal/UIToolkit/DefaultTerminal.uxml`。
  `<Backend>`と`Resources`は`.`区切りとし、`UIToolkitResources`のように連結しない）。
  `"path"`は`Samples~/<Backend>.Resources`（`Resources`フォルダそのものではなく、それを内包する
  親フォルダを指定すること。理由は次項）

### なぜ`<Backend>`表記が物理パスに2回（`UIToolkit.Resources/.../Terminal/UIToolkit/`）出るのか

Package ManagerのSample Importは「`"path"`で指定したフォルダの**中身**」だけをコピーし、
`"path"`フォルダ自身の名前は破棄される。もし`"path"`を`Resources`より深い階層
（例: `Samples~/Resources/UIToolkit`）に設定すると、Import後は`Resources`という
祖先フォルダ自体が失われ、`Resources.Load`が機能しなくなる。

そのため、

- `Samples~/<Backend>.Resources/`: バックエンドごとに独立してImportできるようにするための`"path"`用の
  区切り（`Resources`を内包する側）
- `Resources/Terminal/<Backend>/`: `Resources.Load`のキー衝突を防ぐための論理パス（後述）

という**目的の異なる2つの理由**でそれぞれ`<Backend>`名が必要になり、結果として物理パス上に
同じバックエンド名が2回登場する。冗長に見えるが、「バックエンドごとの独立Import」と
「Resources.Loadの動作」を両立させる上で技術的に必要な重複であり、削れない
（全バックエンド共通の1つのSampleにまとめれば重複は消えるが、その場合バックエンド単位での
個別Importができなくなる）。

### 参照方法・フォールバック

- `Resources.Load<T>("Terminal/<Backend>/...")` の文字列パスで解決する
  （`[SerializeField] private VisualTreeAsset _default = ...` のようなGUID直接参照はしない。
  GUIDは利用側プロジェクトの資産更新・Sample再importで壊れやすいため）
- Installer側のデフォルト値解決は `Composition.Shared.Extensions.UnityObjectExtensions.OrResource<T>()`
  （`_override.OrResource("Terminal/<Backend>/...")`）を使う。Resources Sample未導入で解決できない場合は
  例外にせず、ログ警告＋最小限フォールバック、またはユーザーへの明確な導入案内に留める
- `Resources`フォルダは各バックエンドの「Default Resources」Sample側にのみ置く。コード側Sample・
  コアパッケージ本体には置かない（未Importならプロジェクトに一切含まれないことを保証するため）
- 複数バックエンドのResources Sampleを同時導入しても、`Resources`フォルダはプロジェクト全体で
  1つの仮想空間にマージされるため、`Terminal/<Backend>/...`のようにバックエンド名でサブフォルダを
  分け、パス衝突を防ぐ（第三者アセットとの衝突リスク低減も兼ねる）
- Addressablesは依存追加のコストが大きいため不採用（詳細判断の経緯は
  [Issue #129](https://github.com/YuukiReiya/YukimaruGames.Unity-Terminal/issues/129) 参照）
