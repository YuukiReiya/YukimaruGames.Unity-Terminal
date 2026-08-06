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
        private long _turnId;
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
            _pending.Clear();
            _turnTop = currentTop;
            return ++_turnId;
        }

        /// <summary>
        /// ターンを正常終了し、積まれた要求を取り出す.
        /// </summary>
        public Request[] EndTurn(long turnId)
        {
            if (_turnId != turnId || turnId == 0)
            {
                return Empty;
            }

            var result = _pending.Count == 0 ? Empty : _pending.ToArray();
            _pending.Clear();
            _turnId = 0;
            _turnTop = null;
            return result;
        }

        /// <summary>
        /// ターンを異常終了し、積まれた要求を全て破棄する(トランザクショナル).
        /// </summary>
        public void Abort(long turnId)
        {
            if (_turnId != turnId)
            {
                return;
            }

            _pending.Clear();
            _turnId = 0;
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
            if (_turnId == 0)
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
