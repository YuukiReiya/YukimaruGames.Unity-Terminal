# カスタムコマンド定義

以下のコマンドをチャットで入力することで、定型作業をすばやく実行できる。

---

## `/new-feature [機能名]`

新機能の雛形ファイルを生成する。

**実行内容:**
1. 機能名からドメイン用語を確認する
2. 必要なレイヤー（SharedKernel / Domain.API / Domain.Core / Application / Presentation / Infrastructure）を提案する
3. 各レイヤーのスケルトンコードを生成する
4. 配置先ファイルパスを提示してユーザーの承認を得る

**使用例:**
```
/new-feature コマンド実行履歴管理
```

---

## `/refactor [ファイルパスまたはクラス名]`

対象コードをDDD方針に沿ってリファクタリング提案する。

**実行内容:**
1. 対象コードの現在のレイヤー配置を分析する
2. 問題点（レイヤー違反・命名規則違反など）を列挙する
3. 修正案を提示し、承認後に実施する

**使用例:**
```
/refactor MainPresenter
```

---

## `/review [ファイルパスまたはクラス名]`

対象コードを以下の観点でレビューする。

**レビュー観点:**
- DDDレイヤーの依存方向が守られているか（.asmdefで強制済みの範囲含む）
- 命名規則（01-coding-style.md）に準拠しているか
- Domain層（Domain.API / Domain.Core）にUnityへの依存がないか
- `SerializeField` / `public` フィールドの使い方
- null チェックの方法

**使用例:**
```
/review Assets/YukimaruGames/Terminal/Runtime/Domain/Abstractions/ITerminalTheme.cs
```

---

## `/layer-check`

プロジェクト全体のレイヤー違反を検出する。

**実行内容:**
1. `Assets/YukimaruGames/Terminal/Runtime/` 以下のファイルをスキャンする
2. 依存方向の違反（例: Domain層の `using UnityEngine`）を列挙する
3. 修正の優先度を提案する

---

## `/add-test [クラス名]`

指定クラスのユニットテストの雛形を生成する。

**実行内容:**
1. 対象クラスのpublicメソッドを列挙する
2. 各メソッドのテストケース（正常系・異常系）を提案する
3. `Assets/YukimaruGames/Terminal/Tests/` 以下にテストファイルを生成する

**使用例:**
```
/add-test LogRenderer
```
