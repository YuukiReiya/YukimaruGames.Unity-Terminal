using System.IO;

namespace YukimaruGames.Terminal.Adapters.ExternalTerminal
{
    /// <summary>
    /// 外部ターミナル側で動かす中継スクリプト(1行入力→ソケット送信、受信→コンソール出力を繰り返す)を
    /// 一時ディレクトリへ書き出すユーティリティ.
    /// </summary>
    /// <remarks>
    /// 追加ランタイム依存(Python等)を要求しないよう、Windowsは標準搭載のPowerShell、
    /// macOSは標準搭載のbash(/dev/tcp疑似デバイス)のみで完結させている.
    /// </remarks>
    internal static class RelayScriptWriter
    {
        private const string WindowsRelayFileName = "yukimaru_terminal_relay.ps1";
        private const string MacRelayFileName = "yukimaru_terminal_relay.sh";
        private const string MacLauncherFileName = "yukimaru_terminal_launcher.applescript";

        // プロンプトを自前でRedraw管理しながら描画するため、標準搭載のPSReadLineには頼らず
        // (スクリプト内蔵ループには効かない)、[Console]::ReadKey()でキー単位に読み取る自前実装で
        // 上下矢印キーによるセッション内履歴呼び出しに対応する。
        // 受信は「プロンプト行が来るまで出力し続けてから入力を受け付ける」同期ループとし、
        // バックグラウンドスレッドでの受信は行わない(非同期に届いた出力が、ユーザーが行編集中の
        // 表示と競合して画面が乱れるのを避けるため).
        private const string WindowsRelayScript = @"param([int]$Port)

$ErrorActionPreference = 'Stop'

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

try { [Console]::Clear() } catch { }

Write-Host ""Connected to Unity Terminal (127.0.0.1:$Port). Type commands below. Close this window or Ctrl+C to disconnect.""

$script:history = New-Object System.Collections.Generic.List[string]
$script:historyIndex = 0

function Redraw($old, $new) {
    if ($old.Length -gt 0) {
        [Console]::Write((""`b"" * $old.Length) + ("" "" * $old.Length) + (""`b"" * $old.Length))
    }
    [Console]::Write($new)
}

function Read-LineWithHistory([string]$Prompt) {
    [Console]::Write($Prompt)
    $buffer = New-Object System.Text.StringBuilder

    while ($true) {
        $key = [Console]::ReadKey($true)

        if ($key.Key -eq [ConsoleKey]::Enter) {
            [Console]::Out.WriteLine()
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
                [Console]::Write(""`b `b"")
            }
        }
        elseif ($key.Key -eq [ConsoleKey]::UpArrow) {
            if ($script:historyIndex -gt 0) {
                $script:historyIndex--
                $old = $buffer.ToString()
                $buffer.Clear() | Out-Null
                $buffer.Append($script:history[$script:historyIndex]) | Out-Null
                Redraw $old $buffer.ToString()
            }
        }
        elseif ($key.Key -eq [ConsoleKey]::DownArrow) {
            $old = $buffer.ToString()
            if ($script:historyIndex -lt $script:history.Count - 1) {
                $script:historyIndex++
                $buffer.Clear() | Out-Null
                $buffer.Append($script:history[$script:historyIndex]) | Out-Null
                Redraw $old $buffer.ToString()
            }
            elseif ($script:historyIndex -eq $script:history.Count - 1) {
                $script:historyIndex++
                $buffer.Clear() | Out-Null
                Redraw $old """"
            }
        }
        elseif ($key.KeyChar -and -not [char]::IsControl($key.KeyChar)) {
            $buffer.Append($key.KeyChar) | Out-Null
            [Console]::Write($key.KeyChar)
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
        if ($line.StartsWith('PROMPT')) {
            $promptText = $line.Substring(6)
            break
        }
        [Console]::Out.WriteLine($line)
    }

    if ($null -eq $promptText) {
        # ソケットが切断された(EOF). プロンプトを受け取れないまま抜けてきたということ.
        break
    }

    $input = Read-LineWithHistory $promptText
    try {
        $writer.WriteLine($input)
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
PORT=""$1""

if [ -z ""$PORT"" ]; then
    echo ""Usage: $0 <port>""
    exit 1
fi

exec 3<>""/dev/tcp/127.0.0.1/$PORT"" || {
    echo ""Failed to connect to Unity Terminal on port $PORT""
    exit 1
}

ORIGINAL_STTY=$(stty -g 2>/dev/null)

cleanup() {
    exec 3<&- 3>&- 2>/dev/null
    if [ -n ""$ORIGINAL_STTY"" ]; then
        stty ""$ORIGINAL_STTY"" 2>/dev/null
    fi
}
trap cleanup EXIT

# ウィンドウは(Terminal.appの仕様上)前回セッションのものを使い回すことがあり、
# 前回の表示が画面に残ったままだと新しいセッションの出力と混ざって見えるため、
# セッション開始時に必ず画面をクリアしてから始める.
printf '\033[2J\033[H'

# read -n1 は呼び出しごとに端末モードを一時的に切り替えて1文字読むが、矢印キーの
# 高速な連打(OSのキーリピート等)でこれを繰り返すと、切り替えの往復コストが入力の
# 到着に追いつかずバイトが化ける(エスケープシーケンスの一部が生の文字として
# バッファに紛れ込み、キャレット扱いされない余計な文字が現在行に残る不具合の原因だった)。
# stty rawでセッション全体を通して端末モードを1回だけ切り替えておくことで、
# 以後の read -n1 はモード切り替えのオーバーヘッド無しに安定して1バイトずつ読める.
if [ -n ""$ORIGINAL_STTY"" ]; then
    stty raw -echo
fi

printf 'Connected to Unity Terminal (127.0.0.1:%s). Type commands below. Close this window or Ctrl+D to disconnect.\r\n' ""$PORT""

HISTORY=()
HISTORY_INDEX=0

redraw() {
    local old_len=$1
    local new_text=$2
    local i
    for ((i = 0; i < old_len; i++)); do
        printf '\b \b'
    done
    printf '%s' ""$new_text""
}

read_line_with_history() {
    local prompt=$1
    local buffer=""""
    local char seq1 seq2

    printf '%s' ""$prompt""

    while true; do
        IFS= read -rsn1 char
        if [ -z ""$char"" ]; then
            # bashのreadは端末からの読み取り時、-n指定でも改行/復帰文字を空文字として
            # 返す(実測で確認した挙動)。CR/LFのバイト値そのものでは一致しないため、
            # 空文字判定でEnterを検出する.
            printf '\r\n'
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
                printf '\b \b'
            fi
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
                    local old_len=${#buffer}
                    buffer=""${HISTORY[$HISTORY_INDEX]}""
                    redraw ""$old_len"" ""$buffer""
                fi
            elif [ ""$esc_seq"" = ""[B"" ]; then
                local old_len=${#buffer}
                if [ ""$HISTORY_INDEX"" -lt $((${#HISTORY[@]} - 1)) ]; then
                    HISTORY_INDEX=$((HISTORY_INDEX + 1))
                    buffer=""${HISTORY[$HISTORY_INDEX]}""
                    redraw ""$old_len"" ""$buffer""
                elif [ ""$HISTORY_INDEX"" -eq $((${#HISTORY[@]} - 1)) ]; then
                    HISTORY_INDEX=$((HISTORY_INDEX + 1))
                    buffer=""""
                    redraw ""$old_len"" """"
                fi
            fi
            # それ以外の未知のシーケンス(Left/Right/Delete/Home/End等)は読み切った上で破棄する
            # (現状は上下矢印による履歴呼び出しのみ対応).
        else
            buffer=""${buffer}${char}""
            printf '%s' ""$char""
            HISTORY_INDEX=${#HISTORY[@]}
        fi
    done
}

while true; do
    prompt_text=""""
    got_prompt=0
    while IFS= read -r line <&3; do
        case ""$line"" in
            PROMPT*)
                prompt_text=""${line#PROMPT}""
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
    echo ""$__READLINE_RESULT"" >&3
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
    set theCommand to (quoted form of relayPath) & "" "" & thePort
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

        public static string WriteWindowsRelayScript()
        {
            var path = Path.Combine(Path.GetTempPath(), WindowsRelayFileName);
            File.WriteAllText(path, WindowsRelayScript);
            return path;
        }

        public static string WriteMacRelayScript()
        {
            var path = Path.Combine(Path.GetTempPath(), MacRelayFileName);
            File.WriteAllText(path, MacRelayScript);
            return path;
        }

        public static string WriteMacLauncherScript()
        {
            var path = Path.Combine(Path.GetTempPath(), MacLauncherFileName);
            File.WriteAllText(path, MacLauncherScriptTemplate);
            return path;
        }
    }
}
