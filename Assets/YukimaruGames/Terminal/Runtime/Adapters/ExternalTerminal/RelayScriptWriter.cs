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

        // PowerShellの単一スレッドでは「受信して表示」と「入力して送信」を同時に回せないため、
        // 受信ループはバックグラウンドのParameterizedThreadStartスレッドへ逃がしている.
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

$receiveThread = [System.Threading.Thread]::new([System.Threading.ParameterizedThreadStart]{
    param($r)
    while ($true) {
        try {
            $line = $r.ReadLine()
        }
        catch {
            break
        }
        if ($null -eq $line) { break }
        [Console]::Out.WriteLine($line)
    }
    # 受信ループがソケット切断(EOF)で抜けても、フォアグラウンドの[Console]::In.ReadLine()は
    # ブロックされたままになり続ける([Console]::In.ReadLine()には安全な割り込み方法が無いため)。
    # ウィンドウをゾンビ状態のまま放置しないよう、プロセスごと終了してcmd.exeのプロンプトへ戻す.
    [Console]::Out.WriteLine(""Disconnected from Unity Terminal."")
    [Environment]::Exit(0)
})
$receiveThread.IsBackground = $true
$receiveThread.Start($reader)

Write-Host ""Connected to Unity Terminal (127.0.0.1:$Port). Type commands below. Close this window or Ctrl+C to disconnect.""

while ($true) {
    $line = [Console]::In.ReadLine()
    if ($null -eq $line) { break }
    try {
        $writer.WriteLine($line)
    }
    catch {
        break
    }
}

$client.Close()
";

        // bashのみで完結させるため、fd3をソケットへ双方向接続(/dev/tcp)し、受信専用のサブシェルを
        // バックグラウンドで走らせつつ、フォアグラウンドで標準入力を読んで送信する.
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

PARENT_PID=$$

( while IFS= read -r line <&3; do
      echo ""$line""
  done
  # 受信ループがソケット切断(EOF)で抜けても、フォアグラウンドの`read`(標準入力待ち)は
  # ブロックされたままになり続ける。ウィンドウをゾンビ状態のまま放置しないよう、
  # 親プロセスへTERMを送りメインループの`read`を割り込ませてスクリプトを終了させる
  # (ウィンドウ自体は閉じない。シェルのプロンプトへ戻り、次回の起動で再利用できる状態になる).
  kill -TERM ""$PARENT_PID"" 2>/dev/null
) &
READER_PID=$!

cleanup() {
    kill ""$READER_PID"" 2>/dev/null
    exec 3<&- 3>&- 2>/dev/null
}
trap cleanup EXIT
trap 'echo ""Disconnected from Unity Terminal.""; exit 0' TERM

echo ""Connected to Unity Terminal (127.0.0.1:$PORT). Type commands below. Close this window or Ctrl+D to disconnect.""

while IFS= read -r line; do
    echo ""$line"" >&3
done
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
