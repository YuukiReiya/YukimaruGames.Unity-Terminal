#!/bin/sh
# 引数: 1:prefix, 2:subject, 3:name, 4:email

PREFIX=$1
SUBJECT=$2
NAME=$3
EMAIL=$4

# credentialsがない場合の明示的な失敗
if [ -z "$NAME" ] || [ -z "$EMAIL" ]; then
    echo "ERROR_CREDENTIALS_MISSING: .agents/git-credentials.txt が見つかりません。"
    exit 1
fi

# メッセージの組み立て
FULL_MESSAGE="${PREFIX}: ${SUBJECT}"

# 一時ファイルを使用した安全なコミット（クォーテーションエラー回避）
echo "$FULL_MESSAGE" > .tmp_commit_msg
git commit --author="$NAME <$EMAIL>" -F .tmp_commit_msg --no-edit
EXIT_CODE=$?

# 後片付け
rm -f .tmp_commit_msg
exit $EXIT_CODE