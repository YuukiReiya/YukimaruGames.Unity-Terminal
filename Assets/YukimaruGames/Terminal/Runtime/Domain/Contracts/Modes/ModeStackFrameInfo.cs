using System;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// モードスタック上の1段分の、読み取り専用のメタ情報.
    /// </summary>
    /// <remarks>
    /// モードの内部状態には一切触れない、診断表示用の最小限の情報のみを保持する.
    /// </remarks>
    public readonly struct ModeStackFrameInfo : IEquatable<ModeStackFrameInfo>
    {
        /// <summary>
        /// モードの識別子(<see cref="ITerminalMode.Id"/>).
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// モードの実行時型名.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// スタックの深さ(最下段の<c>ExecutionMode</c>が0).
        /// </summary>
        public int Depth { get; }

        public ModeStackFrameInfo(string id, string typeName, int depth)
        {
            Id = id;
            TypeName = typeName;
            Depth = depth;
        }

        public bool Equals(ModeStackFrameInfo other)
        {
            return Id == other.Id && TypeName == other.TypeName && Depth == other.Depth;
        }

        public override bool Equals(object obj)
        {
            return obj is ModeStackFrameInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, TypeName, Depth);
        }
    }
}
