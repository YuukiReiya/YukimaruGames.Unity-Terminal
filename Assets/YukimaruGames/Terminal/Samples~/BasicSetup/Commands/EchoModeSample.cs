// EchoModeSample.cs
//
// interactive-mode機能の最小サンプル。
// 1. `echo-mode` と打つとモードへ入場する
// 2. 以後の入力はそのままエコーバックされる(継続入力の例として、末尾が `\` の行は
//    継続行として次の入力とまとめて評価される)
// 3. `exit` でモードから抜ける
//
// 使い方: このファイルをプロジェクトの Assembly-CSharp (またはお好みのasmdef)配下に
// コピーしてください(Samples~ は Unity のインポート対象外のため、そのままでは動作しません)。

using System;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Contracts.Attributes;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;

namespace YukimaruGames.Terminal.Samples
{
    /// <summary>
    /// 最小構成のカスタムモード実装例.
    /// </summary>
    public sealed class EchoReplMode : TerminalModeBase
    {
        // OnEnterAsyncで受け取ったcontextをフィールドに保持しておく。
        // [TerminalModeCommand] はインスタンスメソッドとして書けるが、モードの型ごとに
        // 1回だけコンパイルされる都合上、IModeContext自体をパラメータとして注入すること
        // はできない(Push毎に異なるcontextを式木へ焼き込めないため)。
        // モード自身の状態として保持し、`this`経由でアクセスするのが正しい使い方.
        private const string ModeId = "echo";
        private const string PromptText = "echo> ";
        private const string ContinuationPromptText = "...   ";
        private const string EnterMessage = "EchoReplMode に入りました。'exit' で抜けられます。";
        private const string ExitCommandName = "exit";
        private const string ExitHelpText = "Exit EchoReplMode.";

        private IModeContext _context;

        public override string Id => ModeId;
        public override string Prompt => PromptText;
        public override string ContinuationPrompt => ContinuationPromptText;

        public override ValueTask OnEnterAsync(IModeContext context, CancellationToken cancellationToken)
        {
            _context = context;
            _context.Output.Message(EnterMessage);
            return default;
        }

        public override ValueTask<ModeResult> HandleAsync(ModeInput input, IModeContext context, CancellationToken cancellationToken)
        {
            var text = input.Text.Span;

            // 末尾が '\' なら継続入力として扱う(サンプル: 複数行の擬似的な表現).
            if (text.Length > 0 && text[^1] == '\\')
            {
                return new ValueTask<ModeResult>(ModeResult.NeedMoreInput);
            }

            // [TerminalModeCommand] で登録したモード専用コマンド(ここでは"exit")は
            // 自動では呼ばれない。HandleAsyncの中でcontext.Commandsから解決し、
            // 一致したものだけ実行して、それ以外の入力のみエコーする.
            var trimmed = input.Text.ToString().Trim();
            if (context.Commands.TryGet(trimmed, out var handler))
            {
                if (handler.IsAsync)
                {
                    return ExecuteModeCommandAsync(handler, cancellationToken);
                }

                handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);
                return new ValueTask<ModeResult>(ModeResult.Continue);
            }

            context.Output.Message(input.Text.ToString());
            return new ValueTask<ModeResult>(ModeResult.Continue);
        }

        private static async ValueTask<ModeResult> ExecuteModeCommandAsync(CommandHandler handler, CancellationToken cancellationToken)
        {
            await handler.AsyncProc(ReadOnlyMemory<CommandArgument>.Empty, cancellationToken);
            return ModeResult.Continue;
        }

        public override ValueTask OnExitAsync(ModeExitReason reason)
        {
            // ネイティブリソースを持つ場合はここで解放する。
            // reason == EnterFailed の場合、OnEnterAsync が完了していない可能性がある
            // (_context が null のままかもしれない)ため、各フィールドのnullチェック等、
            // 部分初期化状態への防御を忘れないこと.
            return default;
        }

        // [TerminalModeCommand] はモード専用コマンド。インスタンスメソッドとして書け、
        // 自分自身(このモード)のフィールド(_context)へ`this`経由でアクセスできる。
        // typeof(EchoReplMode)の代わりに文字列ID("echo")を使うことも出来る
        // (asmdefを跨いでモード型への参照を持てない場合向け):
        //   [TerminalModeCommand("echo", "exit")]
        [TerminalModeCommand(typeof(EchoReplMode), ExitCommandName, help: ExitHelpText)]
        private void Exit()
        {
            _context.Transitions.RequestPop();
        }
    }

    /// <summary>
    /// EchoReplMode へ入場するためのグローバルコマンド.
    /// </summary>
    public static class EchoModeSampleCommands
    {
        private const string EchoModeCommandName = "echo-mode";
        private const string EchoModeHelpText = "Enter EchoReplMode.";

        // [TerminalCommand]はstaticのみ発見される。IModeTransitionRequestSinkは
        // 起動時に確定済みのシングルトンとしてExpression.Constant経由で注入される
        // (CommandFactory.Create(MethodInfo, in ModeServiceBundle)。詳細は
        // ImmediateModeInstaller.RegisterCommands を参照)。
        [TerminalCommand(EchoModeCommandName, help: EchoModeHelpText)]
        private static void EnterEchoMode(IModeTransitionRequestSink transitions)
        {
            transitions.RequestPush(new EchoReplMode());
        }
    }
}
