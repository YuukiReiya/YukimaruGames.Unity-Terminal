# Tech Context

## 開発環境

- Unity バージョン: Unity 6（6000.0）以降
- C# バージョン: C# 9.0以上
- IDE: Visual Studio Code
- OS: Windows
- AIツール: Cline（VSCode拡張）

## 使用パッケージ

<!-- TODO(USER): バージョンを実態に合わせて記入してください -->

| パッケージ | バージョン | 用途 |
|-----------|-----------|------|
| UniTask   | <!-- TODO(USER): 要記入 --> | 非同期処理 |
| VContainer| <!-- TODO(USER): 要記入 --> | DI（未導入の場合はServiceLocator） |
| UniRx / R3| <!-- TODO(USER): 要記入 --> | Reactiveプログラミング |

## 技術的制約

- Domain層（Domain.API / Domain.Core）では `using UnityEngine` 禁止
- `FindObjectOfType()` / `GameObject.Find()` 使用禁止
- `public` フィールド禁止（プロパティ経由でアクセスする）
- `#region` 使用禁止
- `MonoBehaviour` は Presentation層のみに配置

## 開発ツール・ワークフロー

- バージョン管理: Git / GitHub
- ブランチ戦略: `feature/`, `fix/`, `refactor/` のprefixを使用
- テストフレームワーク: Unity Test Framework（NUnit）
- パッケージ形式: UPM（Unity Package Manager）— `com.yukimaru-games.terminal`
- アーキテクチャ依存制御: Assembly Definition（`.asmdef`）で物理的に強制

## 既知の技術的課題

<!-- TODO(USER): 現在抱えている技術的負債・課題があれば記載してください -->
-
