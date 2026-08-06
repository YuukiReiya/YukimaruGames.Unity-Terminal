# Changelog

このプロジェクトの注目すべき変更はこのファイルに記録します。
形式は [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に、バージョニングは
[Semantic Versioning](https://semver.org/lang/ja/) に準拠することを目指します。

## [Unreleased]

### Added

- **interactive-mode(モードスタック)機能**: `python`のようなコマンドで入り、以後の入力が
  別解釈に切り替わり、`exit`等で戻る、CMD/PowerShell/Rubyのinteractive-modeに相当する機能を追加。
  「通常状態も1つのモード」として統一するモードスタック方式(Issue #113)。
  - `ITerminalMode` / `TerminalModeBase`(Domain.Contracts): モードの契約と既定実装を提供する抽象基底クラス
  - `IModeTransitionRequestSink`: モードはスタックを直接操作せず、Push/Replace/Popの要求を積むだけの一方向の受け皿
  - `IModeOutput`: モード実行中の逐次出力用の狭いインターフェイス(`ITerminalService`丸ごとではない)
  - `IModeContext`: モードが`OnEnterAsync`等で受け取る実行時コンテキスト(Commands/Output/Transitions/Stack)
  - `[TerminalModeCommand]`属性: モード専用コマンド(インスタンスメソッドとして書ける)を、モード型の
    継承チェーンを直接辿る方式で発見。基底クラスに宣言した共通コマンドも派生モードへ継承される
  - `terminal.stack`診断コマンド: 現在のモードスタック(型名・識別子・深さ)を表示
  - `ITerminalService.Prompt` / `AllowsConcurrentSpinner`: 現在のモードに応じて変化するプロンプト表示
  - `ITerminalOptions.AdditionalCommandAssemblies`: コマンド/モードを独自asmdefに置く場合の追加走査先指定
  - `TerminalBootstrapper.ShutdownAsync()`: モードの`OnExitAsync`連鎖の完走を待てる、明示的な非同期シャットダウン経路
  - サンプル: `Samples~/Commands/EchoModeSample.cs`(最小構成のカスタムモード実装例)

### Changed

- `CommandFactory`: パラメータ分類ロジックを`ParameterPlan`へ一般化。「末尾のCancellationToken 1個だけ
  特別扱い」という従来の前提を廃し、`IModeOutput`等の任意個数・任意位置のサービス注入に対応
- `ExecuteCommandUseCase`: モードスタックの唯一の所有者かつ現在モードの読み取り専用ビュー
  (Prompt/History/Autocomplete/継続入力状態)の提供者として再設計。1行の解釈自体は現在モードへ
  委譲するだけの薄いディスパッチャに縮退
- `TerminalService`: `ExecuteCommandUseCase`への委譲Facadeとして再整理。`NextHistory`/`PrevHistory`/
  `Autocomplete`の委譲先を固定のリポジトリから現在モード経由に変更
- `PromptRenderer`: プロンプト表示をInstallerからのpush型から、`ITerminalService.Prompt`を毎フレーム
  読みに行くpull型に変更
- `TerminalCommandAttribute`: `sealed`を撤廃(ラッパー属性を利用者が定義できるようにするための対称的な拡張)
- `TerminalRuntimeScope` / `IInstaller`: `IAsyncDisposable`に対応。モードの`OnExitAsync`連鎖の完走を
  待てる非同期破棄経路を追加(同期版は引き続きフォールバックとして提供)

### Fixed

- Ctrl+C(`ITerminalService.Interrupt()`、旧`Cancel()`)が、コマンド実行中でない(モード入力待ちの)
  タイミングでは一切発火しなかった不具合を修正

### Breaking Changes

未リリースのため、以下の破壊的変更は許容しています。

- `ITerminalService.Cancel()` を `Interrupt()` に改名
- `IPromptRenderer.Prompt` の setter を削除(pull型化に伴う)
- `IExecuteCommandUseCase.CancelCommandIfNeeded()` を `Interrupt()` に統合
- `ITerminalMode.HandleAsync` は `ModeInput` を値渡しにする(`in`不可。C#の言語仕様上、
  `async`メソッドは`ref`/`in`/`out`パラメータを取れないため)
- `TerminalService`/`ExecuteCommandUseCase`のコンストラクタ引数を変更
