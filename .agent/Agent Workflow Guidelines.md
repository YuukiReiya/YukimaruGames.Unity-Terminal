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
- **内容のレビュー優先**: 指摘内容をガイドライン (.md) に反映した際、**commit/pushする前に必ず編集内容を `notify_user` にてユーザーにレビュー依頼すること。** 意図の取り違いによる誤ったルールのコミットを防止するため。
- **PR作成・公開必須**: 承認後に `git add/commit/push` を行い、**Draftではなく「Ready for Review (Open)」状態のPR**を作成（または更新）すること。作成時は必ず [Pull Request Guidelines.md](./Pull%20Request%20Guidelines.md) を確認し、Assignee, Reviewer, Labels が規定通り設定されていることを徹底する。
- **ブランチ移動の禁止**: 作成したPRがマージされるまで、他の作業ブランチへの移動や実装タスクの開始を禁止する。PRのマージを確認後、`master`を最新化してから次の作業ブランチを作成すること。
- **実作業よりも優先**: 累積データの更新（知識ベースの同期）を最優先とする。

### ファイル命名規則
- `<Category> Guidelines.md`: カテゴリごとのガイドライン
- 例: `Pull Request Guidelines.md`, `Code Style Guidelines.md`

### 管理ファイル一覧
- [Pull Request Guidelines.md](./Pull%20Request%20Guidelines.md): PR作成ルール
- [Agent Workflow Guidelines.md](./Agent%20Workflow%20Guidelines.md): エージェントのワークフロー（本ファイル）
- [Code Style Guidelines.md](./Code%20Style%20Guidelines.md): コーディング規約
- `Architecture Overview.md`: アーキテクチャ概要（未作成）
- `Testing Guidelines.md`: テスト方針（未作成）
- `Deployment Workflow.md`: デプロイ手順（未作成）

## Communication Rules

### Pull Request Communication
- **Comment Response**: ユーザーからのコメント・指摘に対応した際は、必ずPR上で対応内容を返信すること（対応したかどうかが不明瞭にならないようにするため）
- **Implementation Discussion**: 実装に対する質問をされた際や、ユーザーの指摘が自身の意図と異なる（または不適切と感じる）場合は、遠慮なく設計意図や背景を説明し、議論を行うこと
- **Shell Escaping Avoidance**: `gh pr edit` や `gh pr comment` などで本文にバッククォート（`）や特殊文字が含まれる場合、シェル環境によるエスケープ（`\` への変換など）を防ぐため、直接引数に渡さず必ず `--body-file` を使用して一時ファイル経由で投稿すること

### Feedback Handling
- **Automated Reviews (e.g. CodeRabbit)**:
  - **DO NOT** apply fixes automatically without User approval
  - 影響が大きい場合はPR上で `@YuukiReiya` にメンションして判断を仰ぐ
  - **Priority**: User Judgement > Automated Tools (ReviewBot < User)

### Reporting
- **簡潔さ**: 冗長な説明を避け、要点を明確に
- **構造化**: 箇条書きやセクション分けで読みやすく
- **日本語**: 全ての記述は日本語で行う
