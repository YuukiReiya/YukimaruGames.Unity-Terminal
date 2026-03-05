ターミナルUTF-8強制: run_commandツール使用時、以下を必ず適用すること。
これを怠ると日本語の文字化けが原因で、無限ループやクラッシュが起きる。

- PowerShell: コマンド冒頭に [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; を付与
- CMD: コマンド冒頭に chcp 65001 > nul && を付与
- Python実行時: $env:PYTHONUTF8=1; を前置
- ファイル書き込み(Set-Content等): -Encoding UTF8 を必ず明示
