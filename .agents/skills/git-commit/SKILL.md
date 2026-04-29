---
name: git-commit
description: プロジェクト規定の形式でコミットを実行します。Agentの報告は日本語で行い、設定不足時は安全に中断します。
---

# git-commit

このスキルは、`.agents/git-credentials.txt` の情報を用いてコミットを実行します。
コミットメッセージの内容（英語/日本語）に関わらず、**Agentからの実行結果の報告や提案は必ず日本語**で行ってください。

## Usage

変更内容の分析が完了した際に使用します。
実行前に `.agents/git-credentials.txt` の存在を確認してください。もし存在しない場合は、本スキルを実行してエラーを検知した後、直ちに日本語で状況を説明し、セットアップ用ワークフローへ誘導してください。

## Steps

1. **事前確認**: `.agents/git-credentials.txt` の有無を確認します。
2. **プレフィックスの選択**: 変更内容に基づき、`feat`, `add`, `fix`, `refactor`, `docs`, `chore` から適切なものを選択します。
3. **メッセージの構成**: 変更内容に基づき、適切なメッセージ（英語または日本語）を構成します。
4. **コマンドの実行**: 以下のシェルスクリプトを実行します。
```bash
sh .agents/scripts/git-commit.sh "{{prefix}}" "{{subject}}" "{{name}}" "{{email}}"
```