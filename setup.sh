#!/bin/sh

# 設定ファイルはホームディレクトリ配下のファイルパスを参照する。
# configファイルはリポジトリ別に管理できるようにしておいた方が都合良さそうなので
# configファイルの実体はリポジトリで管理できるように、ホームディレクトリ側はシンボリックリンクを貼るつくりにしておく。
# NOTE:
# Nixを併用することでプロジェクトの開発環境に入った時のみリンクを切り替えるつくりも可能になりそう。

TARGET_DIR=".config/opencode"
TARGET_FILE="opencode.jsonc"
TARGET_PATH="$TARGET_DIR/$TARGET_FILE"

LINK_DIR="$HOME/$TARGET_DIR"
LINK_PATH="$LINK_DIR/$TARGET_FILE"


# 1. リンク先のディレクトリを作成（念のため）
mkdir -p "$LINK_DIR"

# 2. リポジトリ内のファイルを、~/.config/opencode/opencode.jsonc へリンク
ln -sf "$(pwd)/$TARGET_PATH" "$LINK_PATH"

# 3. リンク先出力
echo "Created link: $(pwd)/$TARGET_PATH -> $LINK_PATH"