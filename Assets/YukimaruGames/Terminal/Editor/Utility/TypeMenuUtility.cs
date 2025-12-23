using System;
using System.Collections.Generic;
using System.Linq;
using YukimaruGames.Terminal.Runtime.Shared;
namespace YukimaruGames.Terminal.Editor
{
    internal static class TypeMenuUtility
    {
        internal const string kMenuDropdownHeader = "Select Type";
        internal const string kMenuDropdownNullDisplayName = "null";
        
        internal static AddTypeMenuAttribute GetAttribute(Type type)
        {
            return Attribute.GetCustomAttribute(type, typeof(AddTypeMenuAttribute)) as AddTypeMenuAttribute;
        }

        internal static string[] GetSplitPathSegments(Type type)
        {
            if (type == null)
            {
                return Array.Empty<string>();
            }

            var typeMenu = GetAttribute(type);
            if (typeMenu != null)
            {
                return typeMenu.GetSplitPathSegments();
            }

            return type.FullName!.Split('.');
        }

        internal static IEnumerable<Type> OrderByType(this IEnumerable<Type> self)
        {
            return self.Select(CreateTypeWithAttribute)
                .OrderBy(GetTypeOrder)
                .ThenBy(GetTypeName)
                .Select(item => item.Type);
        }

        private static (Type Type, AddTypeMenuAttribute Attribute) CreateTypeWithAttribute(Type type) =>
            (type, GetAttribute(type));

        private static int GetTypeOrder((Type Type, AddTypeMenuAttribute Attribute) item)
        {
            var (type, attribute) = item;
            
            if (type == null)
            {
                return -999;
            }

            return attribute?.Order ?? 1;
        }

        private static string GetTypeName((Type Type, AddTypeMenuAttribute Attribute) item)
        {
            var (type, attribute) = item;
            
            if (type == null)
            {
                return null;
            }

            return attribute?.MenuName ?? type.Name;
        }
    }
}
