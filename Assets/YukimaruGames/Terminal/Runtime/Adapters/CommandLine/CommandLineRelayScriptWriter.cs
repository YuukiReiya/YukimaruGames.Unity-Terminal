using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// 外部ターミナル側で動かす中継スクリプト(1行入力→ソケット送信、受信→コンソール出力を繰り返す)を
    /// 一時ディレクトリへ書き出すユーティリティ.
    /// </summary>
    /// <remarks>
    /// 追加ランタイム依存(Python等)を要求しないよう、Windowsは標準搭載のPowerShell、
    /// macOSは標準搭載のbash(/dev/tcp疑似デバイス)のみで完結させている.
    /// </remarks>
    internal static class CommandLineRelayScriptWriter
    {
        private const string WindowsRelayFileName = "yukimaru_terminal_relay.ps1";
        private const string MacRelayFileName = "yukimaru_terminal_relay.sh";
        private const string MacLauncherFileName = "yukimaru_terminal_launcher.applescript";
        private const string TokenFileName = "yukimaru_terminal_token.txt";
        private const string PortFileName = "yukimaru_terminal_port.txt";

        /// <summary>セッションディレクトリ名の接頭辞(掃除時の判別に使う).</summary>
        private const string SessionDirectoryPrefix = "yukimaru_terminal_";

        // プロンプトを自前でRedraw管理しながら描画するため、標準搭載のPSReadLineには頼らず
        // (スクリプト内蔵ループには効かない)、[Console]::ReadKey()でキー単位に読み取る自前実装で
        // 上下矢印キーによるセッション内履歴呼び出しに対応する。
        // 受信は「プロンプト行が来るまで出力し続けてから入力を受け付ける」同期ループとし、
        // バックグラウンドスレッドでの受信は行わない(非同期に届いた出力が、ユーザーが行編集中の
        // 表示と競合して画面が乱れるのを避けるため).
        private const string WindowsRelayScript = @"param([int]$Port, [string]$TokenPath)

$ErrorActionPreference = 'Stop'

# 引数は省略できる。省略時はこのスクリプトが置かれたディレクトリから補う。
# 利用者が手で貼り付けて接続する用途では、長いパスを2つ渡すとコピー事故が起きやすいため.
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Port) {
    try { $Port = [int]((Get-Content -Path (Join-Path $ScriptDir 'yukimaru_terminal_port.txt') -Raw).Trim()) }
    catch { $Port = 0 }
}
if ([string]::IsNullOrEmpty($TokenPath)) {
    $TokenPath = Join-Path $ScriptDir 'yukimaru_terminal_token.txt'
}

if (-not $Port) {
    Write-Host ""Usage: $($MyInvocation.MyCommand.Name) [-Port <port>] [-TokenPath <token-file>]""
    exit 1
}

# セッショントークンは起動引数ではなく一時ファイル経由で受け取る(引数はタスクマネージャー等から
# 他プロセスに見えてしまうため)。
# 読み取り後にここで削除はしない。削除すると2つ目以降のターミナルが接続できなくなるため、
# 後始末はUnity側(CommandLineSession)がセッションディレクトリごと行う.
try {
    $Token = (Get-Content -Path $TokenPath -Raw).Trim()
}
catch {
    Write-Host ""Failed to read the session token file.""
    exit 1
}

if ([string]::IsNullOrEmpty($Token)) {
    Write-Host ""The session token file was empty.""
    exit 1
}

# サーバーからの制御行の目印。トークンを前置することで、コマンドの実行結果が偶然
# 'PROMPT'等で始まっても制御行と誤認されない(トークンは予測不可能なため).
$promptSentinel = $Token + 'PROMPT'
$completePrefix = $Token + 'COMPLETE:'
$candidatesPrefix = $Token + 'CANDIDATES:'
$noMatchResponse = $Token + 'NOMATCH'

try {
    $client = New-Object System.Net.Sockets.TcpClient
    $client.Connect('127.0.0.1', $Port)
}
catch {
    Write-Host ""Failed to connect to Unity Terminal on port $Port : $_""
    exit 1
}

$stream = $client.GetStream()
$writer = New-Object System.IO.StreamWriter($stream)
$writer.AutoFlush = $true
$reader = New-Object System.IO.StreamReader($stream)

# 接続後の最初の1行としてトークンを送る。これが一致しない接続はサーバー側で
# 何も送られないまま即座に閉じられる.
$writer.WriteLine($Token)

try { [Console]::Clear() } catch { }

Write-Host ""Connected to Unity Terminal (127.0.0.1:$Port). Type commands below. Close this window or Ctrl+C to disconnect.""

$script:history = New-Object System.Collections.Generic.List[string]
$script:historyIndex = 0

$script:lastRows = 1

# 入力行を、スクリプトが保持している状態から毎回作り直す。
# 消した文字数を数えてバックスペースを出す方式だと、スクリプトが把握していない文字が
# 行に混ざったときに消せず、画面と入力バッファが食い違ったまま復帰できなくなる。
# 行頭から書き直せば、画面は常にプロンプト+バッファと一致する(#158)。
#
# 入力がコンソール幅を超えると表示は複数行へ折り返る。現在行だけを消すと折り返した前の行が
# 残って二重に見えるため、直前に何行使ったかを覚えておき、その範囲をまとめて消す。
# ANSIエスケープはコンソールホストの設定によって解釈されないことがあるため、
# カーソル位置の操作で消す.
function Redraw([string]$Prompt, [string]$Text) {
    $line = $Prompt + $Text
    try {
        $width = [Console]::BufferWidth
        if ($width -lt 1) { $width = 80 }

        $top = [Console]::CursorTop - ($script:lastRows - 1)
        if ($top -lt 0) { $top = 0 }

        for ($i = 0; $i -lt $script:lastRows; $i++) {
            $row = $top + $i
            if ($row -ge [Console]::BufferHeight) { break }
            [Console]::SetCursorPosition(0, $row)
            [Console]::Write("" "" * ($width - 1))
        }

        [Console]::SetCursorPosition(0, $top)
        [Console]::Write($line)
        $script:lastRows = [Math]::Max(1, [Math]::Ceiling($line.Length / $width))
        return
    }
    catch {
        # リダイレクト時などカーソルを操作できない環境では、行を消さずに書き直すだけにする.
        [Console]::Write(""`r"" + $line)
        $script:lastRows = 1
    }
}

function Read-LineWithHistory([string]$Prompt) {
    $script:lastRows = 1
    $buffer = New-Object System.Text.StringBuilder
    Redraw $Prompt ''

    while ($true) {
        $key = [Console]::ReadKey($true)

        if ($key.Key -eq [ConsoleKey]::Enter) {
            [Console]::Out.WriteLine()
            $script:lastRows = 1
            $line = $buffer.ToString()
            if ($line.Length -gt 0) {
                $script:history.Add($line)
            }
            $script:historyIndex = $script:history.Count
            return $line
        }
        elseif ($key.Key -eq [ConsoleKey]::Backspace) {
            if ($buffer.Length -gt 0) {
                $buffer.Length = $buffer.Length - 1
            }
            Redraw $Prompt $buffer.ToString()
        }
        elseif ($key.Key -eq [ConsoleKey]::Tab) {
            # IMGUI版のTabキー自動補完に相当する機能。ソケットへ直接リクエストを送り、
            # 応答を待つ。
            # 応答を待っている間にも、Unity側から無関係な非同期のログ行やプロンプト行が
            # 同じソケットへ流れてくることがある(ゲームコードからのログ出力等)。
            # 1行だけ読んで応答とみなすと、それらを補完応答と取り違えた上に本来の
            # ログ行を握り潰してしまうため、目印(トークン付き)が来るまで読み続ける.
            $writer.WriteLine(""AUTOCOMPLETE:"" + $buffer.ToString())
            while ($true) {
                try {
                    $acResponse = $reader.ReadLine()
                }
                catch {
                    $acResponse = $null
                }
                if ($null -eq $acResponse) { break }

                if ($acResponse.StartsWith($completePrefix)) {
                    $buffer.Clear() | Out-Null
                    $buffer.Append($acResponse.Substring($completePrefix.Length)) | Out-Null
                    Redraw $Prompt $buffer.ToString()
                    break
                }
                elseif ($acResponse.StartsWith($candidatesPrefix)) {
                    [Console]::Out.WriteLine()
                    [Console]::Out.WriteLine($acResponse.Substring($candidatesPrefix.Length))
                    $script:lastRows = 1
                    Redraw $Prompt $buffer.ToString()
                    break
                }
                elseif ($acResponse -eq $noMatchResponse) {
                    break
                }
                elseif ($acResponse.StartsWith($promptSentinel)) {
                    # 補完待ちの最中に届いたプロンプトは破棄する。既に入力行を表示中で
                    # 重複してしまう上、無関係なコマンドの完了通知に過ぎないため
                    # (次にEnterを押して実行した際に、改めてプロンプトが送られてくる).
                }
                else {
                    # 通常の出力行。入力途中の行を壊さないよう、改行してから出力し、
                    # プロンプトと入力中バッファを描き直す(候補一覧の表示と同じ手法).
                    [Console]::Out.WriteLine()
                    [Console]::Out.WriteLine($acResponse)
                    $script:lastRows = 1
                    Redraw $Prompt $buffer.ToString()
                }
            }
        }
        elseif ($key.Key -eq [ConsoleKey]::UpArrow) {
            if ($script:historyIndex -gt 0) {
                $script:historyIndex--
                $buffer.Clear() | Out-Null
                $buffer.Append($script:history[$script:historyIndex]) | Out-Null
                Redraw $Prompt $buffer.ToString()
            }
        }
        elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
            if ($script:historyIndex -lt $script:history.Count - 1) {
                $script:historyIndex++
                $buffer.Clear() | Out-Null
                $buffer.Append($script:history[$script:historyIndex]) | Out-Null
                Redraw $Prompt $buffer.ToString()
            }
            elseif ($script:historyIndex -eq $script:history.Count - 1) {
                $script:historyIndex++
                $buffer.Clear() | Out-Null
                Redraw $Prompt """"
            }
        }
        elseif ($key.KeyChar -and -not [char]::IsControl($key.KeyChar)) {
            $buffer.Append($key.KeyChar) | Out-Null
            # 1文字ずつ足すのではなく行ごと引き直す(bash版と同じ理由。#158).
            Redraw $Prompt $buffer.ToString()
            $script:historyIndex = $script:history.Count
        }
    }
}

while ($true) {
    $promptText = $null
    while ($true) {
        try {
            $line = $reader.ReadLine()
        }
        catch {
            $line = $null
        }
        if ($null -eq $line) { break }
        if ($line.StartsWith($promptSentinel)) {
            $promptText = $line.Substring($promptSentinel.Length)
            break
        }
        [Console]::Out.WriteLine($line)
    }

    if ($null -eq $promptText) {
        # ソケットが切断された(EOF). プロンプトを受け取れないまま抜けてきたということ.
        break
    }

    # $inputはPowerShellの予約自動変数(パイプライン入力用)のため、別名を使う.
    $inputLine = Read-LineWithHistory $promptText
    try {
        $writer.WriteLine($inputLine)
    }
    catch {
        break
    }
}

Write-Host ""Disconnected from Unity Terminal.""
$client.Close()
";

        // bashのみで完結させるため、fd3をソケットへ双方向接続(/dev/tcp)する。
        // 当初はGNU Readline(read -e)で上下矢印キーの履歴呼び出しに対応させていたが、
        // readlineは「自分がプロンプトを描画した」という前提で画面を再描画するため、
        // こちらが別途printfしたプロンプト文字列と競合し表示が崩れる不具合があった。
        // そのためreadlineには頼らず、キー単位で読み取る自前実装(PowerShell版と同じ設計)に
        // 統一している。受信も「プロンプト行が来るまで出力し続けてから入力を受け付ける」
        // 同期ループとし、バックグラウンドでの受信は行わない(非同期に届いた出力が、
        // ユーザーが行編集中の表示と競合して画面が乱れるのを避けるため).
        private const string MacRelayScript = @"#!/bin/bash
# 引数は省略できる。省略時はこのスクリプトが置かれたディレクトリから補う。
# 利用者が手で貼り付けて接続する用途では、長いパスを2つ渡すとコピー事故が起きやすいため.
SCRIPT_DIR=$(cd ""$(dirname ""$0"")"" && pwd)
PORT=""${1:-$(cat ""$SCRIPT_DIR/yukimaru_terminal_port.txt"" 2>/dev/null)}""
TOKEN_PATH=""${2:-$SCRIPT_DIR/yukimaru_terminal_token.txt}""

# 端末モードの確定は、画面へ何かを出すより前・接続より前に済ませる。
# ここより後ろで確定していると、起動処理の最中(接続メッセージの表示中など)に打鍵された
# 文字を端末が自前でエコーしてしまい、スクリプトの入力バッファには入っていないのに
# 画面にだけ残る(バックスペースでも消せない)という食い違いが起きる(#158).
ORIGINAL_STTY=$(stty -g 2>/dev/null)

cleanup() {
    exec 3<&- 3>&- 2>/dev/null
    if [ -n ""$ORIGINAL_STTY"" ]; then
        stty ""$ORIGINAL_STTY"" 2>/dev/null
    fi
}
trap cleanup EXIT

# read -n1 は呼び出しごとに端末モードを一時的に切り替えて1文字読むが、矢印キーの
# 高速な連打(OSのキーリピート等)でこれを繰り返すと、切り替えの往復コストが入力の
# 到着に追いつかずバイトが化ける(エスケープシーケンスの一部が生の文字として
# バッファに紛れ込み、キャレット扱いされない余計な文字が現在行に残る不具合の原因だった)。
# stty rawでセッション全体を通して端末モードを1回だけ切り替えておくことで、
# 以後の read -n1 はモード切り替えのオーバーヘッド無しに安定して1バイトずつ読める.
if [ -n ""$ORIGINAL_STTY"" ]; then
    stty raw -echo
fi

# raw modeでは改行だけでは行頭へ戻らないため、以降のメッセージはCRを伴わせる.
if [ -z ""$PORT"" ]; then
    printf 'Usage: %s [port] [token-file]\r\n' ""$0""
    exit 1
fi

# セッショントークンは起動引数ではなく一時ファイル経由で受け取る(引数は`ps`等で
# 同一マシンの他プロセスから丸見えになるため)。
# 読み取り後にここで削除はしない。削除すると2つ目以降のターミナルが接続できなくなるため、
# 後始末はUnity側(CommandLineSession)がセッションディレクトリごと行う.
TOKEN=$(tr -d '\r\n' < ""$TOKEN_PATH"" 2>/dev/null)

if [ -z ""$TOKEN"" ]; then
    printf 'Failed to read the session token file.\r\n'
    exit 1
fi

# サーバーからの制御行の目印。トークンを前置することで、コマンドの実行結果が偶然
# 'PROMPT'等で始まっても制御行と誤認されない(トークンは予測不可能なため).
PROMPT_SENTINEL=""${TOKEN}PROMPT""
COMPLETE_PREFIX=""${TOKEN}COMPLETE:""
CANDIDATES_PREFIX=""${TOKEN}CANDIDATES:""
NOMATCH_RESPONSE=""${TOKEN}NOMATCH""

exec 3<>""/dev/tcp/127.0.0.1/$PORT"" || {
    printf 'Failed to connect to Unity Terminal on port %s\r\n' ""$PORT""
    exit 1
}

# 接続後の最初の1行としてトークンを送る。これが一致しない接続はサーバー側で
# 何も送られないまま即座に閉じられる.
printf '%s\n' ""$TOKEN"" >&3

# ウィンドウは(Terminal.appの仕様上)前回セッションのものを使い回すことがあり、
# 前回の表示が画面に残ったままだと新しいセッションの出力と混ざって見えるため、
# セッション開始時に必ず画面をクリアしてから始める.
printf '\033[2J\033[H'

printf 'Connected to Unity Terminal (127.0.0.1:%s). Type commands below. Close this window or Ctrl+D to disconnect.\r\n' ""$PORT""

HISTORY=()
HISTORY_INDEX=0

# 入力行を、スクリプトが保持している状態から毎回作り直す。
# 消した文字数を数えてバックスペースを出す方式だと、スクリプトが把握していない文字
# (起動処理中に端末がエコーしたもの等)が行に混ざったときに消せず、画面と入力バッファが
# 食い違ったまま復帰できなくなる。行頭へ戻して消してから引き直せば、
# 画面は常にバッファと一致する(#158)。
#
# 入力が端末幅を超えると、表示は複数の物理行へ折り返る。現在の行だけを消すと折り返した
# 前の行が残って二重に見えるため、直前に何行使ったかを覚えておき、その先頭行まで戻ってから
# カーソル以降(ESC[J)をまとめて消す.
TERM_COLS=$(stty size 2>/dev/null | awk '{print $2}')
[ -z ""$TERM_COLS"" ] && TERM_COLS=80
LAST_ROWS=1

# 端末の幅が変わったら追従する(readで待っている間に来たシグナルはreadが戻ってから処理される).
trap 'TERM_COLS=$(stty size 2>/dev/null | awk ""{print \$2}""); [ -z ""$TERM_COLS"" ] && TERM_COLS=80' WINCH

redraw() {
    local text=""$1$2""
    local cols=$TERM_COLS
    case ""$cols"" in
        ''|*[!0-9]*) cols=80 ;;
    esac
    [ ""$cols"" -lt 1 ] && cols=80

    if [ ""$LAST_ROWS"" -gt 1 ]; then
        printf '\033[%dA' $((LAST_ROWS - 1))
    fi
    printf '\r\033[J%s' ""$text""

    LAST_ROWS=$(( (${#text} + cols - 1) / cols ))
    [ ""$LAST_ROWS"" -lt 1 ] && LAST_ROWS=1
}

read_line_with_history() {
    local prompt=$1
    local buffer=""""
    local char seq1 seq2

    LAST_ROWS=1
    redraw ""$prompt"" ""$buffer""

    while true; do
        IFS= read -rsn1 char
        if [ -z ""$char"" ]; then
            # bashのreadは端末からの読み取り時、-n指定でも改行/復帰文字を空文字として
            # 返す(実測で確認した挙動)。CR/LFのバイト値そのものでは一致しないため、
            # 空文字判定でEnterを検出する.
            printf '\r\n'
            LAST_ROWS=1
            if [ -n ""$buffer"" ]; then
                HISTORY+=(""$buffer"")
            fi
            HISTORY_INDEX=${#HISTORY[@]}
            __READLINE_RESULT=""$buffer""
            return 0
        elif [ ""$char"" = $'\x04' ] && [ -z ""$buffer"" ]; then
            # stty rawの下ではCtrl+Dは端末ドライバのEOF処理を経由せず生バイトとして
            # 届くため、ここで明示的にハンドリングしないと切断操作として機能しない
            # (何も入力していない行でのCtrl+Dのみ切断とみなす。一般的なシェルの挙動に合わせる).
            printf '\r\n'
            __READLINE_RESULT=""$buffer""
            __DISCONNECT_REQUESTED=1
            return 0
        elif [ ""$char"" = $'\x7f' ] || [ ""$char"" = $'\x08' ]; then
            if [ -n ""$buffer"" ]; then
                buffer=""${buffer%?}""
            fi
            redraw ""$prompt"" ""$buffer""
        elif [ ""$char"" = $'\t' ]; then
            # IMGUI版のTabキー自動補完に相当する機能。ソケットへ直接リクエストを送り、
            # 応答を待つ(fd3はターミナルのstty raw設定と独立したソケットの読み書きなので、
            # ここで同期的にread <&3しても行編集中の他の入力処理と競合しない)。
            # 応答を待っている間にも、Unity側から無関係な非同期のログ行やプロンプト行が
            # 同じソケットへ流れてくることがある(ゲームコードからのログ出力等)。
            # 1行だけ読んで応答とみなすと、それらを補完応答と取り違えた上に本来の
            # ログ行を握り潰してしまうため、目印(トークン付き)が来るまで読み続ける.
            printf 'AUTOCOMPLETE:%s\n' ""$buffer"" >&3
            local ac_response
            while IFS= read -r ac_response <&3; do
                case ""$ac_response"" in
                    ""$COMPLETE_PREFIX""*)
                        buffer=""${ac_response#$COMPLETE_PREFIX}""
                        redraw ""$prompt"" ""$buffer""
                        break
                        ;;
                    ""$CANDIDATES_PREFIX""*)
                        printf '\r\n%s\r\n' ""${ac_response#$CANDIDATES_PREFIX}""
                        LAST_ROWS=1
                        redraw ""$prompt"" ""$buffer""
                        break
                        ;;
                    ""$NOMATCH_RESPONSE"")
                        break
                        ;;
                    ""$PROMPT_SENTINEL""*)
                        # 補完待ちの最中に届いたプロンプトは破棄する。既に入力行を表示中で
                        # 重複してしまう上、無関係なコマンドの完了通知に過ぎないため
                        # (次にEnterを押して実行した際に、改めてプロンプトが送られてくる).
                        ;;
                    *)
                        # 通常の出力行。入力途中の行を壊さないよう、改行してから出力し、
                        # プロンプトと入力中バッファを描き直す(候補一覧の表示と同じ手法).
                        printf '\r\n%s\r\n' ""$ac_response""
                        LAST_ROWS=1
                        redraw ""$prompt"" ""$buffer""
                        ;;
                esac
            done
        elif [ ""$char"" = $'\x1b' ]; then
            # エスケープシーケンス(矢印/Delete/Home/End等)を読み切る。
            # 上下矢印(CSI: ESC [ A/B)以外にも、Delete(ESC [ 3 ~)のように長さが
            # まちまちなキーや、SS3形式(ESC O A。アプリケーションカーソルモード時の
            # 矢印キー等で使われる)のように導入バイトが""[""ではないキーも存在する。
            # 固定バイト数だけ読むと後続バイトを読み残し、次のreadで生の文字として
            # バッファに混入する不具合の原因になっていた(プロンプト直前に説明のつかない
            # 文字が現れて消せない、という形で表面化した)。
            # bash 3.2(macOS標準)はread -tに小数秒を指定できないため整数秒を使う
            # (実際のキー入力なら後続バイトは即座に届くため、体感の遅延にはならない).
            local esc_seq="""" esc_next
            read -rsn1 -t 1 esc_next
            if [ ""$esc_next"" = ""["" ]; then
                # CSI: 終端文字(英字または~)が来るか、後続バイトが届かなくなる
                # (タイムアウト)まで読み切る.
                esc_seq=""[""
                while true; do
                    read -rsn1 -t 1 esc_next
                    if [ $? -ne 0 ]; then
                        break
                    fi
                    esc_seq=""${esc_seq}${esc_next}""
                    case ""$esc_next"" in
                        [A-Za-z~]) break ;;
                    esac
                    if [ ${#esc_seq} -ge 8 ]; then
                        break
                    fi
                done
            elif [ ""$esc_next"" = ""O"" ]; then
                # SS3: 導入バイト""O""の直後の1文字で確定する(例: ESC O A).
                local ss3_next
                read -rsn1 -t 1 ss3_next
                esc_seq=""O${ss3_next}""
            else
                # Option+キー等のMeta修飾はESCの直後の1文字だけで確定する.
                esc_seq=""$esc_next""
            fi

            if [ ""$esc_seq"" = ""[A"" ]; then
                if [ ""$HISTORY_INDEX"" -gt 0 ]; then
                    HISTORY_INDEX=$((HISTORY_INDEX - 1))
                    buffer=""${HISTORY[$HISTORY_INDEX]}""
                    redraw ""$prompt"" ""$buffer""
                fi
            elif [ ""$esc_seq"" = ""[B"" ]; then
                if [ ""$HISTORY_INDEX"" -lt $((${#HISTORY[@]} - 1)) ]; then
                    HISTORY_INDEX=$((HISTORY_INDEX + 1))
                    buffer=""${HISTORY[$HISTORY_INDEX]}""
                    redraw ""$prompt"" ""$buffer""
                elif [ ""$HISTORY_INDEX"" -eq $((${#HISTORY[@]} - 1)) ]; then
                    HISTORY_INDEX=$((HISTORY_INDEX + 1))
                    buffer=""""
                    redraw ""$prompt"" """"
                fi
            fi
            # それ以外の未知のシーケンス(Left/Right/Delete/Home/End等)は読み切った上で破棄する
            # (現状は上下矢印による履歴呼び出しのみ対応).
        else
            buffer=""${buffer}${char}""
            # 1文字ずつ足すのではなく行ごと引き直す。起動直後に紛れ込んだ文字が
            # 行頭側に残っていても、最初の打鍵で必ず消える(#158).
            redraw ""$prompt"" ""$buffer""
            HISTORY_INDEX=${#HISTORY[@]}
        fi
    done
}

while true; do
    prompt_text=""""
    got_prompt=0
    while IFS= read -r line <&3; do
        case ""$line"" in
            ""$PROMPT_SENTINEL""*)
                prompt_text=""${line#$PROMPT_SENTINEL}""
                got_prompt=1
                break
                ;;
            *)
                printf '%s\r\n' ""$line""
                ;;
        esac
    done

    if [ ""$got_prompt"" -eq 0 ]; then
        # ソケットが切断された(EOF). プロンプトを受け取れないまま抜けてきたということ.
        break
    fi

    __DISCONNECT_REQUESTED=0
    read_line_with_history ""$prompt_text""
    if [ ""$__DISCONNECT_REQUESTED"" -eq 1 ]; then
        break
    fi
    # 組み込みechoは""-n""/""-e""等をオプションとして解釈してしまい、ユーザーの入力が
    # 偶然それらと一致すると改行が送られずセッションが停止する。printfならそのリスクが無い.
    printf '%s\n' ""$__READLINE_RESULT"" >&3
done

printf 'Disconnected from Unity Terminal.\r\n'
";

        // 'tell application "Terminal"' はTerminal.app未起動時にそれ自体が起動のトリガーとなり、
        // 起動直後は既定の空ウィンドウが1枚自動で開く。ここで無条件に"do script"を呼ぶと
        // (既定ウィンドウとは別に)新規ウィンドウがもう1枚開いてしまい、ウィンドウが2枚になる
        // (かつ中継スクリプトが実行されるのは新規ウィンドウ側だけで、既定ウィンドウは未接続のまま
        // 残るため、そちらへ入力しても無反応に見える)。既存ウィンドウの有無を確認し、
        // あれば流用(in window 1)することで常に1枚に抑える.
        private const string MacLauncherScriptTemplate = @"on run argv
    set relayPath to item 1 of argv
    set thePort to item 2 of argv
    set tokenPath to item 3 of argv
    set theCommand to (quoted form of relayPath) & "" "" & thePort & "" "" & (quoted form of tokenPath)
    tell application ""Terminal""
        activate
        if (count of windows) is 0 then
            do script theCommand
        else
            do script theCommand in window 1
        end if
    end tell
end run
";

        /// <summary>
        /// 一時ディレクトリ直下への固定ファイル名書き出しは、TMPDIR未設定の共有環境
        /// (Linux等の/tmp)で他ユーザーによるシンボリックリンク先変更・スクリプト差し替えの
        /// 余地を生む。ユーザー名・プロセスIDに加え、プロセス起動ごとの乱数要素も
        /// ディレクトリ名へ含めることでディレクトリパスそのものの予測を難しくする
        /// (接続の実認証自体は<see cref="CommandLineBridge.Token"/>が担うため、ここでは
        /// symlink解決までの厳密なチェックはせず、パスの当てずっぽうを防ぐ程度に留める).
        /// プロセス内で複数回呼んでも同じディレクトリを指すよう、1回だけ計算して使い回す
        /// (呼び出しごとに乱数を振ると、スクリプト・トークンファイルが別ディレクトリに
        /// 散らばってしまう).
        /// </summary>
        /// <summary>自プロセスのPID(セッションディレクトリ名の生成と掃除の判別に使う).</summary>
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

        private static readonly string ScriptDirectory = Path.Combine(
            Path.GetTempPath(),
            $"{SessionDirectoryPrefix}{Environment.UserName}_{CurrentProcessId}_{Path.GetRandomFileName().Replace(".", string.Empty)}");

        /// <summary>
        /// セッションディレクトリを用意する.
        /// </summary>
        /// <remarks>
        /// <see cref="Directory.CreateDirectory(string)"/>は、そのパスに既にシンボリックリンクが
        /// 存在してもそれを検証せずに使う。同一マシンの別ユーザーがパスを推測して先にリンクを
        /// 仕込んでいた場合、中継スクリプトやセッショントークンをリンク先へ書き込んでしまう(#119)。
        /// 作成後にリンクでないことを確かめ、リンクだった場合は使わずに失敗させる
        /// (消して作り直すとリンクを張り直される競合になりうるため、リンク自体には触れない).
        /// </remarks>
        /// <exception cref="IOException">用意したパスがシンボリックリンクだった場合.</exception>
        private static string EnsureScriptDirectory()
        {
            Directory.CreateDirectory(ScriptDirectory);

            if (IsSymbolicLink(ScriptDirectory))
            {
                throw new IOException(
                    $"The session directory is a symbolic link and cannot be trusted: {ScriptDirectory}");
            }

            return ScriptDirectory;
        }

        /// <summary>
        /// そのパスがシンボリックリンク(Windowsのリパースポイントを含む)か.
        /// </summary>
        /// <remarks>
        /// <c>FileSystemInfo.LinkTarget</c>はUnityのランタイムでは利用できないため
        /// (実測でプロパティ自体が存在しないことを確認)、属性で判定する。
        /// macOS/Windowsのいずれでもシンボリックリンクには
        /// <see cref="FileAttributes.ReparsePoint"/>が立つ(実測で確認).
        /// </remarks>
        internal static bool IsSymbolicLink(string path)
        {
            try
            {
                return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
            }
            catch (Exception)
            {
                // 属性を取得できない場合(存在しない・権限不足)はリンクと断定しない.
                return false;
            }
        }

        /// <summary>
        /// C#の逐語的文字列リテラルはソースファイル上の改行バイトをそのまま保持するため、
        /// リポジトリのチェックアウト設定次第でCRLFになりうる。bashはCRLFのシバン行
        /// (「#!/bin/bash」+CR)を正しく解釈できず起動に失敗するため、書き出し前にLFへ揃える.
        /// </summary>
        private static string NormalizeToLf(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");

        public static string WriteWindowsRelayScript()
        {
            var path = Path.Combine(EnsureScriptDirectory(), WindowsRelayFileName);
            File.WriteAllText(path, WindowsRelayScript);
            return path;
        }

        public static string WriteMacRelayScript()
        {
            var path = Path.Combine(EnsureScriptDirectory(), MacRelayFileName);
            File.WriteAllText(path, NormalizeToLf(MacRelayScript));
            return path;
        }

        /// <summary>
        /// セッション認証用トークンを一時ファイルへ書き出し、そのパスを返す.
        /// </summary>
        /// <remarks>
        /// トークンを中継スクリプトの起動引数として直接渡すと、`ps`やタスクマネージャー等から
        /// 同一マシンの他プロセスに丸見えになり、「ポートを見つけただけの他プロセスを弾く」という
        /// 認証の目的が果たせない。引数にはこのファイルのパスのみを渡し、中継スクリプトは
        /// 読み取り直後にこのファイルを削除する(露出時間を最小化する).
        /// なお改行を含めると読み取り側でのトリムが必要になるため、トークンのみを書き出す.
        /// </remarks>
        public static string WriteTokenFile(string token)
        {
            var path = Path.Combine(EnsureScriptDirectory(), TokenFileName);
            File.WriteAllText(path, token);
            return path;
        }

        /// <summary>
        /// 接続先ポートをセッションディレクトリへ書き出す.
        /// </summary>
        /// <remarks>
        /// 中継スクリプトを引数無しで起動できるようにするためのもの(#160)。
        /// ポート番号は秘密情報ではない(接続の認証はトークンが担う)ため、権限は制限しない.
        /// </remarks>
        public static string WritePortFile(int port)
        {
            var path = Path.Combine(EnsureScriptDirectory(), PortFileName);
            File.WriteAllText(path, port.ToString(CultureInfo.InvariantCulture));
            return path;
        }

        /// <summary>
        /// 自身のセッションディレクトリを削除する.
        /// </summary>
        /// <remarks>
        /// トークンファイルの後始末は中継スクリプト側では行わない(削除すると2つ目以降の
        /// ターミナルが接続できなくなるため)。セッションの終了時にここでまとめて片付ける.
        /// </remarks>
        public static void DeleteSessionDirectory()
        {
            TryDeleteDirectory(ScriptDirectory);
        }

        /// <summary>
        /// 終了済みプロセスが残したセッションディレクトリを掃除する.
        /// </summary>
        /// <remarks>
        /// Unityがクラッシュした場合は<see cref="DeleteSessionDirectory"/>が走らないため、
        /// 起動時にも掃除する。
        /// <para>
        /// ディレクトリ名に含まれるPIDで判別し、<b>生存している他プロセスのものは触らない</b>
        /// (複数のUnityインスタンスが同時に起動している場合に、他方の作業を壊さないため)。
        /// PIDが再利用され無関係なプロセスへ割り当てられていた場合は生存扱いとなり残るが、
        /// 消し過ぎる方向には倒れない。
        /// </para>
        /// <para>
        /// ただし<b>自プロセスのものは、現在のセッション以外なら削除する</b>。
        /// <see cref="ScriptDirectory"/>はドメインリロードのたびに新しい乱数で作られるため、
        /// PIDの生存判定だけでは同一プロセスが残した過去のセッションを回収できない
        /// (実測でPlay Modeの再起動により11件溜まっていた).
        /// </para>
        /// </remarks>
        public static void CleanUpStaleSessionDirectories()
        {
            string[] directories;

            try
            {
                directories = Directory.GetDirectories(Path.GetTempPath(), SessionDirectoryPrefix + "*");
            }
            catch (Exception)
            {
                // 一時ディレクトリを列挙できない環境では掃除を諦める(機能自体は続行できる).
                return;
            }

            foreach (var directory in directories)
            {
                if (string.Equals(directory, ScriptDirectory, StringComparison.Ordinal)) continue;
                if (!TryParseProcessId(Path.GetFileName(directory), out var processId)) continue;

                // 自プロセスの過去セッション(ドメインリロードで捨てられたもの)は確実に不要.
                var isOwnProcess = processId == CurrentProcessId;
                if (!isOwnProcess && IsProcessAlive(processId)) continue;

                TryDeleteDirectory(directory);
            }
        }

        /// <summary>
        /// ディレクトリ名からPIDを取り出す(<c>yukimaru_terminal_{user}_{pid}_{random}</c>).
        /// </summary>
        /// <remarks>
        /// ユーザー名に'_'が含まれうるため、末尾から数えて2番目の要素をPIDとみなす.
        /// </remarks>
        private static bool TryParseProcessId(string directoryName, out int processId)
        {
            processId = 0;

            var parts = directoryName.Split('_');
            if (parts.Length < 4) return false;

            return int.TryParse(parts[^2], NumberStyles.Integer, CultureInfo.InvariantCulture, out processId);
        }

        private static bool IsProcessAlive(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                // 該当PIDのプロセスが存在しない.
                return false;
            }
            catch (Exception)
            {
                // 判定できない場合は生存扱いにして残す(消し過ぎるより安全).
                return true;
            }
        }

        /// <summary>
        /// ディレクトリを中身ごと削除する(失敗しても無視する).
        /// </summary>
        /// <remarks>
        /// シンボリックリンクは<b>再帰削除の対象にしない</b>。リンクを辿って削除すると、
        /// 自分が作ったわけではないリンク先の中身まで消してしまう(#119).
        /// </remarks>
        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;
                if (IsSymbolicLink(path)) return;

                Directory.Delete(path, recursive: true);
            }
            catch (Exception)
            {
                // 使用中・権限不足等で消せない場合は諦める(次回の掃除で回収される).
            }
        }

        public static string WriteMacLauncherScript()
        {
            var path = Path.Combine(EnsureScriptDirectory(), MacLauncherFileName);
            File.WriteAllText(path, NormalizeToLf(MacLauncherScriptTemplate));
            return path;
        }
    }
}
