# Agent Workflow Guidelines

このドキュメントは、AIエージェントがプロジェクトで作業を行う際の基本的なワークフローとルールを定義します。

## Git Workflow Rules

### Branch Management
- **Rebase-oriented**: 常に `git pull --rebase` を使用し、mergeは特別な指示がない限り禁止
- **Branch Strategy**:
  1. 最新masterを取得: `git fetch origin master && git checkout master && git pull --rebase origin master`
  2. 新しい作業用ブランチを作成: `git checkout -b <branch-name>`
  3. 作業完了後、PRを作成
  4. マージ済みブランチは削除、未マージブランチは確認を促す
  
  ※ 同等の処理であればコマンドはワンラインにまとめて可

### Branch Naming Convention
- `feat/<feature-name>`: 新機能追加
- `fix/<bug-name>`: バグ修正
- `refactor/<target>`: リファクタリング
- `docs/<document-name>`: ドキュメント更新
- `test/<test-name>`: テスト追加・修正

## Task Execution Workflow

### 作業前の準備
1. **現状分析**: 既存コードの構造を理解
2. **問題の洗い出し**: 具体的な課題を特定
3. **解決策の提案**: メリット・デメリット・注意点を網羅
4. **言語化**: ユーザーが読みやすい形で提示
5. **承認待ち**: Goサインを受けてから実装開始

### 実装中のルール
- **段階的な進行**: Phase分けして、各Phase完了後に動作確認
- **コミット粒度**: 意味のある単位でコミット
- **テスト**: 各Phase完了時に動作確認

## .agent Folder Management

### 目的
`.agent` フォルダは、スレッドを跨いでAIエージェントがプロジェクト固有のルールやコンテキストを理解するための「知識ベース」として機能します。

### 更新タイミング
- **新しいルールが確立された時**: 即座にドキュメント化
- **実作業よりも優先**: 累積データの更新を最優先
- **並行・後回し禁止**: 作業を止めて更新
- **レビュー依頼必須**: 更新後は必ず `notify_user` でユーザーにレビューを依頼

### ファイル命名規則
- `<Category> Guidelines.md`: カテゴリごとのガイドライン
- 例: `Pull Request Guidelines.md`, `Code Style Guidelines.md`

### 想定ファイル
- `Pull Request Guidelines.md`: PR作成ルール（既存）
- `Agent Workflow Guidelines.md`: エージェントのワークフロー（本ファイル）
- `Code Style Guidelines.md`: コーディング規約
- `Architecture Overview.md`: アーキテクチャ概要
- `Testing Guidelines.md`: テスト方針
- `Deployment Workflow.md`: デプロイ手順

## Communication Rules

### Feedback Handling
- **Automated Reviews (e.g. CodeRabbit)**:
  - **DO NOT** apply fixes automatically without User approval
  - 影響が大きい場合はPR上で `@YuukiReiya` にメンションして判断を仰ぐ
  - **Priority**: User Judgement > Automated Tools (ReviewBot < User)

### Reporting
- **簡潔さ**: 冗長な説明を避け、要点を明確に
- **構造化**: 箇条書きやセクション分けで読みやすく
- **日本語**: 全ての記述は日本語で行う
