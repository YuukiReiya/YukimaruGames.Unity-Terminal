# 新機能追加ワークフロー

DDDのレイヤー構成に沿って、新機能のスケルトンコードを一括生成する。

---

## Step 1: 仕様確認

以下の情報をユーザーに確認する:

- 実装する機能名（ドメイン用語で）
- 機能の概要（何をするか1〜2文で）
- 必要なレイヤー（Domain / Application / Presentation / Infrastructure）
- 既存クラスとの依存関係（あれば）

確認が取れるまで次のステップに進まない。

---

## Step 2: ファイルパスの提案

以下の形式でファイルパスを提示し、ユーザーの承認を得る:

```
【Domain層】
Assets/Scripts/Domain/Model/〇〇.cs         # エンティティ or 値オブジェクト
Assets/Scripts/Domain/Repository/I〇〇Repository.cs  # リポジトリインターフェース（必要な場合）

【Application層】
Assets/Scripts/Application/UseCase/〇〇UseCase.cs

【Presentation層】
Assets/Scripts/Presentation/〇〇/〇〇Presenter.cs
```

Infrastructure層が必要な場合は追加で提示する。
承認が取れるまで次のステップに進まない。

---

## Step 3: Domain層の生成

Domain/Model/ にエンティティまたは値オブジェクトを生成する。

生成時の確認事項:
- `using UnityEngine` が含まれていないこと
- `MonoBehaviour` を継承していないこと
- ゲームのドメインルール（上限値・計算式など）がコードに表現されていること
- XMLドキュメントコメントが付いていること

---

## Step 4: Application層の生成

Application/UseCase/ にユースケースクラスを生成する。

生成時の確認事項:
- クラス名が `〇〇UseCase` の形式であること
- `MonoBehaviour` を継承していないこと
- Domain層のクラスのみに依存していること
- UnityEngine の型（Vector3等）を使っていないこと

---

## Step 5: Presentation層の生成

Presentation/ に MonoBehaviour クラスを生成する。

生成時の確認事項:
- Application層のUseCaseを経由してロジックを呼び出していること
- ゲームロジックを直接記述していないこと
- `[SerializeField]` を適切に使用していること

---

## Step 6: 生成結果のレビュー

生成したすべてのファイルについて以下を確認し、結果を報告する:

- [ ] レイヤーの依存方向が守られているか（Presentation → Application → Domain）
- [ ] Domain層に `using UnityEngine` が混入していないか
- [ ] 命名規則（01-coding-style.md）に準拠しているか
- [ ] 各クラスにXMLドキュメントコメントがあるか

問題があれば修正案を提示してユーザーの承認を得る。
