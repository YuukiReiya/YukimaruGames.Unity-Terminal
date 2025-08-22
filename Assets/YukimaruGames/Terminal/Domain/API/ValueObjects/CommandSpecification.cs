using System;
using System.Reflection;

namespace YukimaruGames.Terminal.Domain.Model
{
    /// <summary>
    /// コマンドの設計情報.
    /// </summary>
    /// <remarks>
    /// specと略す.
    /// </remarks>
    public readonly struct CommandSpecification : IEquatable<CommandSpecification>
    {
        /// <summary>
        /// メソッド情報.
        /// </summary>
        public MethodInfo Method { get; }
        
        /// <summary>
        /// メタ情報.
        /// </summary>
        public CommandMeta Meta { get; }

        public CommandSpecification(MethodInfo methodInfo, CommandMeta metadata)
        {
            Method = methodInfo;
            Meta = metadata;
        }
        
        public bool Equals(CommandSpecification other)
        {
            return
                Method.Equals(other.Method) &&
                Meta.Equals(other.Meta);
        }

        public override bool Equals(object obj)
        {
            return obj is CommandSpecification other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Method, Meta);
        }

        public static bool operator ==(CommandSpecification left, CommandSpecification right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommandSpecification left, CommandSpecification right)
        {
            return !left.Equals(right);
        }
    }
}
