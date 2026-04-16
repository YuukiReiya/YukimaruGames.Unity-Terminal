# Progress

<!-- このファイルは作業のたびに更新する。 -->

## 動作しているもの

- ターミナルUIの基本機能（Bootstrapper / Installer による起動）
- `.clinerules/` による Clineへのルール適用
- `.clinerules/workflows/` によるワークフロー定義

## 未着手・作業中

- [x] `.clinerules/` 6ファイル + `07-memory-bank-instruction.md` の配置
- [x] `.clinerules/workflows/` 5ファイルの配置
- [x] `.clinerules/` 内容を実際のプロジェクト構成に反映
- [x] `memory-bank/` 6ファイルの配置・下書き
- [ ] `memory-bank/` 各ファイルの `TODO(USER)` 箇所を記入
- [ ] `04-project-structure.md` の `TODO(USER)` 箇所を記入
- [ ] `07-memory-bank-instruction.md` にリポジトリ管理方針の注意事項を追記
- [ ] MCP連携の検討（GitHub MCP・Filesystem MCP）

## 既知の問題

- `.clinerules/` のファイル変更後はCline再起動（Reload Window）が必要
- Clineの確認プロンプトでキャッシュが返ることがある

## プロジェクトの意思決定の変遷

- `memory-bank/` は当初 `.gitignore` 登録を検討していたが、リポジトリで管理して他のユーザーにも適用する方針に変更
- ただしユーザー個人依存の情報（絶対パス、ユーザー名等）は含めない制約を追加
