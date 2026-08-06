using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Application.Services.Modes
{
    /// <summary>
    /// <see cref="IModeTransitionRequestSink"/> の実装. 要求を積むだけで、適用はディスパッチャが行う.
    /// </summary>
    internal sealed class ModeTransitionRequestSink : IModeTransitionRequestSink
    {
        internal enum RequestKind
        {
            Push,
            Replace,
            Pop,
        }

        internal readonly struct Request
        {
            public readonly RequestKind Kind;
            public readonly ITerminalMode Mode;
            public readonly int Count;
            public readonly ITerminalMode ExpectedTop;

            public Request(RequestKind kind, ITerminalMode mode, int count, ITerminalMode expectedTop)
            {
                Kind = kind;
                Mode = mode;
                Count = count;
                ExpectedTop = expectedTop;
            }
        }

        private static readonly Request[] Empty = Array.Empty<Request>();

        private readonly ICommandLogger _logger;
        private readonly List<Request> _pending = new();

        // 採番カウンタ(単調増加、0は「未発行」を表す値として予約し使わない)と、
        // 現在アクティブなターンのIDを分離して保持する。1フィールド兼用にすると
        // EndTurn/Abortでの0リセット後に同じ値が再発行され、古いトークンによる
        // EndTurn/Abortの呼び出しが「別ターン」として誤って受理されてしまう.
        private long _nextTurnId;
        private long _activeTurnId;
        private ITerminalMode _turnTop;

        public ModeTransitionRequestSink(ICommandLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// ターンを開始する. 戻り値のトークンは <see cref="EndTurn"/>/<see cref="Abort"/> に渡す.
        /// </summary>
        public long BeginTurn(ITerminalMode currentTop)
        {
            if (_activeTurnId != 0)
            {
                _logger?.Send(MessageType.Warning, "BeginTurn was called while another turn is still active. The previous turn is discarded.");
            }

            _pending.Clear();
            _turnTop = currentTop;
            _activeTurnId = ++_nextTurnId;
            return _activeTurnId;
        }

        /// <summary>
        /// ターンを正常終了し、積まれた要求を取り出す.
        /// </summary>
        public Request[] EndTurn(long turnId)
        {
            if (turnId == 0 || _activeTurnId != turnId)
            {
                return Empty;
            }

            var result = _pending.Count == 0 ? Empty : _pending.ToArray();
            _pending.Clear();
            _activeTurnId = 0;
            _turnTop = null;
            return result;
        }

        /// <summary>
        /// ターンを異常終了し、積まれた要求を全て破棄する(トランザクショナル).
        /// </summary>
        public void Abort(long turnId)
        {
            if (turnId == 0 || _activeTurnId != turnId)
            {
                return;
            }

            _pending.Clear();
            _activeTurnId = 0;
            _turnTop = null;
        }

        void IModeTransitionRequestSink.RequestPush(ITerminalMode mode)
        {
            if (mode is null)
            {
                _logger?.Send(MessageType.Warning, "RequestPush was called with a null mode. Discarded.");
                return;
            }

            Enqueue(RequestKind.Push, mode, 0);
        }

        void IModeTransitionRequestSink.RequestReplace(ITerminalMode mode)
        {
            if (mode is null)
            {
                _logger?.Send(MessageType.Warning, "RequestReplace was called with a null mode. Discarded.");
                return;
            }

            Enqueue(RequestKind.Replace, mode, 0);
        }

        void IModeTransitionRequestSink.RequestPop(int count)
        {
            Enqueue(RequestKind.Pop, null, Math.Max(1, count));
        }

        private void Enqueue(RequestKind kind, ITerminalMode mode, int count)
        {
            if (_activeTurnId == 0)
            {
                _logger?.Send(MessageType.Warning, $"Mode transition request ({kind}) was made outside of a mode turn and has been discarded.");
                return;
            }

            if (mode != null && _pending.Exists(r => ReferenceEquals(r.Mode, mode)))
            {
                _logger?.Send(MessageType.Warning, $"Mode instance '{mode.GetType().Name}' was requested twice in the same turn. The duplicate request is discarded.");
                return;
            }

            _pending.Add(new Request(kind, mode, count, _turnTop));
        }
    }
}
