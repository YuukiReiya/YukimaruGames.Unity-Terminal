using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

namespace YukimaruGames.Terminal.Application.Services.Modes
{
    /// <summary>
    /// モードスタックの実体. 常に非空・最下段は固定(Pop不可)。
    /// </summary>
    /// <remarks>
    /// このクラス自体は非公開(internal)。Push/Pop等の変更権限は
    /// <see cref="YukimaruGames.Terminal.Application.Services.ExecuteCommandUseCase"/> だけが持つ.
    /// </remarks>
    internal sealed class TerminalModeStack
    {
        private readonly struct Frame
        {
            public readonly ITerminalMode Mode;
            public readonly IModeContext Context;

            public Frame(ITerminalMode mode, IModeContext context)
            {
                Mode = mode;
                Context = context;
            }
        }

        private readonly List<Frame> _frames = new();

        public TerminalModeStack(ITerminalMode root, IModeContext rootContext)
        {
            _frames.Add(new Frame(root, rootContext));
        }

        /// <summary>
        /// 現在(最上段)のモード.
        /// </summary>
        public ITerminalMode Current => _frames[_frames.Count - 1].Mode;

        /// <summary>
        /// 現在(最上段)のモードに紐づくコンテキスト.
        /// </summary>
        public IModeContext CurrentContext => _frames[_frames.Count - 1].Context;

        /// <summary>
        /// 現在の深さ(最下段の root を含む).
        /// </summary>
        public int Depth => _frames.Count;

        /// <summary>
        /// 最上段に新しいモードを積む. 深さが1増える.
        /// </summary>
        public void Push(ITerminalMode mode, IModeContext context) => _frames.Add(new Frame(mode, context));

        /// <summary>
        /// 最上段を指定したモードへ置き換える. 深さは変わらない.
        /// </summary>
        public void Replace(ITerminalMode mode, IModeContext context) => _frames[_frames.Count - 1] = new Frame(mode, context);

        /// <summary>
        /// 最上段を取り除く. 最下段(root)は取り除けない.
        /// </summary>
        /// <returns>取り除かれたモード. 最下段しか無い場合は null.</returns>
        public ITerminalMode Pop()
        {
            if (_frames.Count <= 1)
            {
                return null;
            }

            var top = _frames[_frames.Count - 1];
            _frames.RemoveAt(_frames.Count - 1);
            return top.Mode;
        }

        /// <summary>
        /// 現在のスタック内容の読み取り専用スナップショットを返す(診断用).
        /// </summary>
        public IReadOnlyList<ModeStackFrameInfo> Snapshot()
        {
            var result = new ModeStackFrameInfo[_frames.Count];
            for (var i = 0; i < _frames.Count; i++)
            {
                var frame = _frames[i];
                result[i] = new ModeStackFrameInfo(frame.Mode.Id, frame.Mode.GetType().Name, i);
            }

            return result;
        }
    }
}
