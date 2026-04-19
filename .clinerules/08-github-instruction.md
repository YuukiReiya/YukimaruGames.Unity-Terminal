# GitHub & gh-cli 操作ガイドライン (Agent専用運用)

## 1. コミットユーザーの厳格管理
Cline（Agent）は、コミット実行前に必ず以下の「アイデンティティ確認フロー」を完遂しなければならない。

### 1.1 アイデンティティ確認フロー
1.  **ファイルの存在確認:** `.agents/git-credentials.txt` が存在するか確認する。
2.  **存在しない場合:**
    - ユーザーに対し「Agent用設定ファイルが存在しません。コミットに使用する **[ユーザー名]** と **[メールアドレス]** を教えてください」と質問する。
    - 取得した情報を元に `.agents/git-credentials.txt` を新規作成する。
    - 形式: `NAME=名前` / `EMAIL=メールアドレス`
3.  **存在する場合:** ファイル内の情報を読み取り、そのセッションの変数として保持する。

### 1.2 コミットコマンドの強制
全ての `git commit` 操作において、グローバル設定を無視し、以下のフラグを明示的に付与すること。
- `git commit --author="[NAME] <[EMAIL]>" --no-edit`
- ※ `[NAME]` と `[EMAIL]` は設定ファイルから取得した最新の値を適用する。

### 1.3 .gitignore の徹底
- `.agents/` ディレクトリが `.gitignore` に含まれているか、必ず事前に確認または追記を行う。Agentの個人設定をリポジトリに公開してはならない。

## 2. GitHub CLI (`gh`) ワークフロー
`gh` コマンドはシステムの `git config` を参照するため、実行直前に一時的な環境変数を注入するか、ローカル設定を書き換えて実行する。
- **PR作成:** `gh pr create --title "<title>" --body "<body>" --draft`
- **ログ確認:** コミット後、`git log -n 1 --pretty=fuller` で Author が設定ファイル通りかセルフチェックし、結果をユーザーに報告する。

**推奨される実行方法:**
```bash
# 環境変数を経由して一時的にアイデンティティを上書きし PR を作成する
GIT_AUTHOR_NAME="[NAME]" GIT_AUTHOR_EMAIL="[EMAIL]" GIT_COMMITTER_NAME="[NAME]" GIT_COMMITTER_EMAIL="[EMAIL]" gh pr create --title "..." --body "..." --draft

## 3. プロジェクト固有ルール
- **Unity:** 物理ファイルの変更時は必ず `.meta` ファイルをセットでステージングに含める。
- **Message:** `feat:`, `fix:`, `refactor:`, `docs:` などのプレフィックスを必須とする。

## 4. 禁止事項
- **許可なきPushの禁止:** `git push` は、コミット完了後にユーザーから「pushして」と明確な指示がない限り、**いかなる理由があっても自動で実行してはならない。**
- `--author` フラグを省略した `git commit`。
- `.agents/` 内の設定ファイルをリポジトリへ含めること（`.gitignore` 厳守）。

## 5. 応答言語
- **重要:** ユーザーへの返答は、`03-cline-behavior.md` の規定に従い、必ず**日本語**で行うこと。