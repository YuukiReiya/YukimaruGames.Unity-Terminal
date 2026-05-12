#!/bin/sh

# 設定ファイルはホームディレクトリ配下のファイルパスを参照する。
# configファイルはリポジトリ別に管理できるようにしておいた方が都合良さそうなので
# configファイルの実体はリポジトリで管理できるように、ホームディレクトリ側はシンボリックリンクを貼るつくりにしておく。
# NOTE:
# Nixを併用することでプロジェクトの開発環境に入った時のみリンクを切り替えるつくりも可能になりそう。

# opencode.jsonc
OPCODE_DIR=".config/opencode"
OPCODE_FILE="opencode.jsonc"
OPCODE_SRC="$(pwd)/$OPCODE_DIR/$OPCODE_FILE"
OPCODE_DEST="$HOME/$OPCODE_DIR/$OPCODE_FILE"

# .bash_aliases
ALIASES_FILE=".bash_aliases"
ALIASES_SRC="$(pwd)/$ALIASES_FILE"
ALIASES_DEST="$HOME/$ALIASES_FILE"

# --- リンク作成関数 ---
# 引数: 1=実体パス, 2=リンク先パス
create_link() {
    src=$1
    dest=$2
    dest_dir=$(dirname "$dest")

    # 0. リンク元ファイルの存在確認
    if [ ! -f "$src" ]; then
        echo "ERROR: Source file not found: $src"
        return 1
    fi

    # 1. リンク先ディレクトリの作成
    mkdir -p "$dest_dir"

    # 2. 状態判定
    if [ -L "$dest" ]; then
        # すでにシンボリックリンクなら強制上書き
        ln -sf "$src" "$dest"
        echo "Updated link: $dest -> $src"
    elif [ -f "$dest" ]; then
        # 実体ファイルが存在する場合は安全のためスキップ
        echo "SKIPPED: $dest already exists as a regular file."
    else
        # 何もなければ新規作成
        ln -s "$src" "$dest"
        echo "Created link: $dest -> $src"
    fi
}

# 1. 設定ファイルのリンク形成
create_link "$OPCODE_SRC" "$OPCODE_DEST"
create_link "$ALIASES_SRC" "$ALIASES_DEST"

# 2. mise 環境の構築
echo "\n--- mise environment setup ---"

if command -v mise >/dev/null 2>&1; then
    eval "$(mise activate bash --shims)"
    echo "Trusting mise.toml..."
    mise trust
    echo "Installing tools from mise.toml..."
    mise install --yes
else
    echo "WARNING: mise not found. Please install mise (https://mise.jdx.dev) to manage project tools."
fi


echo "\nSetup completed. Please run 'source ~/.bashrc' to reflect alias changes."