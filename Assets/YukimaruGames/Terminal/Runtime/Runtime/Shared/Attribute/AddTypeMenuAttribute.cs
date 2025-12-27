using System;
using System.IO;

namespace YukimaruGames.Terminal.Runtime.Shared
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class AddTypeMenuAttribute : Attribute
    {
        public string MenuName { get; }
        public int Order { get; }

        /// <summary>
        /// Initializes a new attribute instance that assigns a hierarchical menu path and an optional order to a type.
        /// </summary>
        /// <param name="menuName">The menu path or name for the type; segments may be separated by '/' to form a hierarchy.</param>
        /// <param name="order">Ordering priority for the menu entry; lower values appear before higher values (default is 0).</param>
        public AddTypeMenuAttribute(string menuName, int order = 0)
        {
            MenuName = menuName;
            Order = order;
        }

        /// <summary>
        /// Split the attribute's MenuName into path segments using '/' as the separator.
        /// </summary>
        /// <returns>An array of path segments from MenuName; empty if MenuName is null, empty, or whitespace. Empty segments are removed.</returns>
        public string[] GetSplitPathSegments()
        {
            var separators = new[] { '/', };
            return string.IsNullOrWhiteSpace(MenuName) ? Array.Empty<string>() : MenuName.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Gets the final segment (the menu name) from the attribute's MenuName path.
        /// </summary>
        /// <returns>The last path segment of <c>MenuName</c> if present; otherwise an empty string.</returns>
        public string GetMenuNameWithoutPath()
        {
            var menuNames = GetSplitPathSegments();
            var length = menuNames.Length;
            return 0 < length ? menuNames[length - 1] : string.Empty;
        }
    }
}