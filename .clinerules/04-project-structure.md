# プロジェクト構造

## Assetsフォルダ構成

```
Assets/
├── Scripts/                    # C#スクリプト（レイヤー構成）
│   ├── Presentation/           # MonoBehaviour・UI・入力
│   ├── Application/            # ユースケース
│   ├── Domain/                 # ゲームロジック（Unity非依存）
│   └── Infrastructure/         # 外部システム連携
├── Scenes/                     # シーンファイル
├── Prefabs/                    # プレハブ
│   ├── Characters/
│   ├── UI/
│   └── Effects/
├── Art/                        # アート素材
│   ├── Sprites/
│   ├── Animations/
│   └── Materials/
├── Audio/                      # 音声素材
│   ├── BGM/
│   └── SE/
└── Resources/                  # Resourcesフォルダ（最小限の使用に留める）
```

## 主要なシステム・モジュール

### ゲームループ管理
- **GameManager**: ゲーム全体の状態管理（`Presentation/Manager/`）

### プレイヤー関連
- **PlayerHp**: プレイヤーのHP管理（`Domain/Model/`）
- **PlayerHpUseCase**: HP変更のユースケース（`Application/UseCase/`）
- **PlayerController**: 入力受付・移動制御（`Presentation/Player/`）

### 永続化
- **IPlayerRepository**: セーブデータのインターフェース（`Domain/Repository/`）
- **PlayerJsonRepository**: JSON形式での保存実装（`Infrastructure/Repository/`）

## 使用パッケージ・ライブラリ

| パッケージ | 用途 | バージョン |
|-----------|------|-----------|
| UniTask | 非同期処理 | 2.x |
| VContainer | DI（依存性注入） | 1.x |
| UniRx または R3 | Reactiveプログラミング | - |

## 命名の対応表（ドメイン用語）

プロジェクト固有のドメイン用語は以下の通り統一すること:

| 日本語 | コード上の名称 |
|--------|--------------|
| プレイヤー | Player |
| 敵 | Enemy |
| HP / ヒットポイント | Hp |
| ダメージ | Damage |
| スキル | Skill |
| ステージ | Stage |

## 注意事項

- `Resources.Load()` は極力使用しない（Addressablesを検討する）
- シングルトンパターンは GameManager など最小限に留める
- `FindObjectOfType()` は使用禁止（DI経由で依存を解決する）
