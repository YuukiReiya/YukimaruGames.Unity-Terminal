using System;

namespace YukimaruGames.Terminal.Runtime.Shared
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class AddTypeMenuAttribute : Attribute
    {
        public string MenuName { get; }
        public int Order { get; }

        public AddTypeMenuAttribute(string menuName, int order = 0)
        {
            MenuName = menuName;
            Order = order;
        }

        public string[] GetSplitPathSegments()
        {
            var separators = new[] { '/', };
            return string.IsNullOrWhiteSpace(MenuName) ? Array.Empty<string>() : MenuName.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        public string GetMenuNameWithoutPath()
        {
            var menuNames = GetSplitPathSegments();
            var length = menuNames.Length;
            return 0 < length ? menuNames[length - 1] : string.Empty;
        }
    }
}
