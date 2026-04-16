# Active Context

<!-- このファイルは作業のたびに更新する。現在の状態を常に反映させる。 -->

## 現在取り組んでいること

- Cline（VSCode AI Agentツール）の環境整備
  - `.clinerules/` のルールファイル・ワークフローを実際のプロジェクト構成に合わせて更新
  - `memory-bank/` の初期セットアップ・下書き記入

## 直近の変更

- `.clinerules/02-architecture-ddd.md` — KI（Architecture Overview）ベースで全面書き換え（6層構成を反映）
- `.clinerules/04-project-structure.md` — サンプルからターミナルUIライブラリの実態に書き換え
- `.clinerules/03-cline-behavior.md`, `05-workflow.md`, `06-custom-commands.md` — パス・レイヤー参照を修正
- `.clinerules/workflows/` — `new-feature.md`, `refactor.md`, `add-test.md` のパス・レイヤー修正
- `memory-bank/` 全ファイル — テンプレートから実態を反映した下書きに更新

## 次のステップ

1. `memory-bank/` 各ファイルの `TODO(USER)` 箇所を記入する
2. `04-project-structure.md` の `TODO(USER)` 箇所を記入する
3. （後回し）MCP連携の検討

## 現在の判断・検討事項

- `memory-bank/` はリポジトリ管理とし、他のユーザーにも適用されるようにする方針
- ユーザー個人依存の情報（絶対パス、ユーザー名等）は `memory-bank/` に含めない

## 重要なパターン・気づき

- `.clinerules/` のファイルはCline再起動（Reload Window）後に反映される
- `workflows/` サブフォルダは特殊扱いでルールとして読み込まれず、呼び出し時のみトークン消費
- 同じ確認プロンプトを繰り返すとキャッシュが返ることがある → ファイル名を直接指定して確認する
