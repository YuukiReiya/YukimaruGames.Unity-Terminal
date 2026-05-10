#!/bin/sh

# -----------------------------------------------------------------------------
# Git Bash Launcher
# -----------------------------------------------------------------------------
# WSL上のカレントディレクトリを保持したまま、Windows側のGit Bashを起動する
function gitbash_command() {
    # WSLパスをWindowsパスに変換
    local win_path=$(wslpath -w "$(pwd)")

    # Git-Bashを起動
    #"/mnt/c/Program Files/Git/git-bash.exe" --cd="$win_path" &

    # MINGW64
    "/mnt/c/Program Files/Git/bin/bash.exe"  --cd="$win_path"
}

alias gb='gitbash_command'
alias gitbash='gitbash_command'

# -----------------------------------------------------------------------------
# OpenCode Aliases
# -----------------------------------------------------------------------------
alias oc='opencode'

# opencode --model="<PROVIDERNAME>/<MODELNAME>"
alias oc-qwen='opencode --model="ollama/qwen3.6:27b"'
alias oc-gemma='opencode --model="ollama/gemma4:e4b"'