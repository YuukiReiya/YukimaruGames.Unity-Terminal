#!/bin/sh

# 引数の受け取り
TITLE=$1
BODY=$2
BRANCH=${3:-master}
DRAFT=$4

# 一時ファイルのパス（カレントディレクトリに隠しファイルとして作成）
TMP_FILE=".tmp_pr_body_$(date +%s).md"

# 本文を一時ファイルに書き出す
# printfを使うことで、末尾の改行や特殊文字を安全に扱う
printf "%s" "$BODY" > "$TMP_FILE"

# ghコマンドの組み立て
# 引数が空でないかチェックしながら実行
if [ "$DRAFT" = "true" ]; then
    gh pr create --title "$TITLE" --body-file "$TMP_FILE" --base "$BRANCH" --draft
else
    gh pr create --title "$TITLE" --body-file "$TMP_FILE" --base "$BRANCH"
fi

# 終了ステータスを保持
EXIT_CODE=$?

# 一時ファイルの削除
rm -f "$TMP_FILE"

exit $EXIT_CODE