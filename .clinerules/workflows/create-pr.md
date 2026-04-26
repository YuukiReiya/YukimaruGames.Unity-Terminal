# PR作成ワークフロー

現在のブランチの変更内容からPRタイトル・本文を自動生成してPRを作成する。

---

## Step 1: ブランチ・差分の確認

**重要：固定のブランチ名（main/master）を使用せず、現在のリポジトリの状況を確認すること。**

以下のコマンドで現在の状態とベース（親）ブランチを特定する:

```bash
git branch --show-current
# リモートのデフォルトブランチを特定する
git symbolic-ref refs/remotes/origin/HEAD | sed 's@^refs/remotes/origin/@@'
# 上記で特定したブランチ（例: main）との差分を確認
git log @{u}..HEAD --oneline || git log origin/main..HEAD --oneline
```

- 現在のブランチ名
- mainブランチからのコミット一覧
- 既存のPRがないかを確認する

---

## Step 2: 差分の分析

特定したベースブランチとの差分を把握する:
変更の目的・影響範囲を分析する。

```
git diff origin/$(git symbolic-ref refs/remotes/origin/HEAD | sed 's@^refs/remotes/origin/@@')...HEAD
```


---

## Step 3: PR情報の生成・確認

分析結果をもとに以下を提案し、ユーザーの承認を得る:

**タイトル:**
```
[種別] 変更内容の概要
```

**本文テンプレート:**
```markdown
## 変更内容

- 

## 変更理由

- 

## 影響範囲

- 変更したレイヤー:
- 影響するクラス:

## 確認事項

- [ ] DDDレイヤーの依存方向が守られている
- [ ] 命名規則に準拠している
- [ ] Domain層にUnity依存がない
```

承認が取れるまで次のステップに進まない。

---

## Step 4: PRの作成 (Agentアイデンティティ適用)

承認された内容でPRを作成する。必ず 08-github-instruction.md に従い、環境変数を注入して実行すること。

1. `.agents/git-credentials.txt` から情報を取得。
2. 以下の形式でコマンドを組み立てて実行する（ベースブランチを明示的に指定）：

```bash
GIT_AUTHOR_NAME="[NAME]" GIT_AUTHOR_EMAIL="[EMAIL]" GIT_COMMITTER_NAME="[NAME]" GIT_COMMITTER_EMAIL="[EMAIL]" gh pr create --title "[タイトル]" --body "[本文]" --draft --base [STEP1で特定したブランチ名]
```

作成されたPRのURLを報告する。
