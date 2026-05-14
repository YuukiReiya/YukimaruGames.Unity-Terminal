#!/bin/bash

# --- 設定エリア ---
WIN_PATH="$(pwd)"
MEM_DIR="$WIN_PATH/memories"
MEM_FILE="project-memory.jsonl"
TARGET_PATH="$MEM_DIR/$MEM_FILE"

# ディレクトリが存在しない場合に備えて作成
mkdir -p "$(dirname "$TARGET_PATH")"

# 環境変数をエクスポート
export MEMORY_FILE_PATH="$TARGET_PATH"

# --- 実行エリア ---
# exec を使うことで、シェル自身のプロセスを MCP サーバーに置き換えます。
# これにより、OpenCode がこのシェルを終了させたとき、
# 中の node プロセスも確実に終了（破棄）されます。
exec mise x -- npx -y @modelcontextprotocol/server-memory