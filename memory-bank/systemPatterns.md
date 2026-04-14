# System Patterns

## アーキテクチャ

DDD（ドメイン駆動設計）に基づくレイヤーアーキテクチャを採用。

```
Assets/Scripts/
├── Presentation/   # MonoBehaviour。View・入力受付のみ
├── Application/    # ユースケース。ゲームの「何をするか」を記述
├── Domain/         # ゲームの核心ロジック。Unityに依存しない
└── Infrastructure/ # 外部システム連携（Save/Load・Audio・外部APIなど）
```

依存方向: `Presentation → Application → Domain ← Infrastructure`

## 主要な設計パターン

<!-- 採用している設計パターンを記載する -->

### DIコンテナ
- 使用ツール:（例: VContainer / ServiceLocator）
- 登録場所:

### イベント通知
- 使用方法:（例: UniRx / C# event / UnityEvent）

### 非同期処理
- 使用方法:（例: UniTask / Coroutine）

## コンポーネントの関係

<!-- 主要なクラス間の関係を記載する -->

## 重要な実装方針

<!-- コードを読んでもわからない「なぜそうしたか」を記録する -->
-
