# 新機能追加ワークフロー

DDDのレイヤー構成に沿って、新機能のスケルトンコードを一括生成する。

---

## Step 1: 仕様確認

以下の情報をユーザーに確認する:

- 実装する機能名（ドメイン用語で）
- 機能の概要（何をするか1〜2文で）
- 必要なレイヤー（SharedKernel / Domain.API / Domain.Core / Application / Presentation / Infrastructure）
- 既存クラスとの依存関係（あれば）

確認が取れるまで次のステップに進まない。

---

## Step 2: ファイルパスの提案

以下の形式でファイルパスを提示し、ユーザーの承認を得る:

```
【Domain.API（Abstractions）】
Assets/YukimaruGames/Terminal/Runtime/Domain/Abstractions/〇〇/I〇〇.cs  # インターフェース定義

【Domain.Core（Services）】
Assets/YukimaruGames/Terminal/Runtime/Domain/Services/〇〇/〇〇Service.cs  # ドメインロジック実装

【Application層】
Assets/YukimaruGames/Terminal/Runtime/Application/Services/〇〇Service.cs

【Presentation層】
Assets/YukimaruGames/Terminal/Runtime/Presentation/〇〇/〇〇Presenter.cs
```

Infrastructure層が必要な場合は追加で提示する。
承認が取れるまで次のステップに進まない。

---

## Step 3: Domain層の生成

Domain.API（Abstractions）にインターフェース・ドメインモデルを、
Domain.Core（Services）にドメインロジック実装を生成する。

生成時の確認事項:
- `using UnityEngine` が含まれていないこと
- `MonoBehaviour` を継承していないこと
- ドメインルールがコードに表現されていること
- XMLドキュメントコメントが付いていること
- インターフェースと実装が別ファイルに分離されていること

---

## Step 4: Application層の生成

Application/Services/ にアプリケーションサービスを生成する。

生成時の確認事項:
- `MonoBehaviour` を継承していないこと
- `Domain.API` のインターフェースのみに依存していること（`Domain.Core` の具象を直接参照しない）
- UnityEngine の型（Vector3等）を使っていないこと

---

## Step 5: Presentation層の生成

Presentation/ にPresenter・Renderer等を生成する。

生成時の確認事項:
- Application層のサービスを経由してロジックを呼び出していること
- ゲームロジックを直接記述していないこと
- `[SerializeField]` を適切に使用していること

---

## Step 6: 生成結果のレビュー

生成したすべてのファイルについて以下を確認し、結果を報告する:

- [ ] レイヤーの依存方向が守られているか
- [ ] Domain層（API / Core）に `using UnityEngine` が混入していないか
- [ ] 命名規則（01-coding-style.md）に準拠しているか
- [ ] インターフェースと実装が分離されているか（Code Style Guidelines準拠）
- [ ] 各クラスにXMLドキュメントコメントがあるか

問題があれば修正案を提示してユーザーの承認を得る。
