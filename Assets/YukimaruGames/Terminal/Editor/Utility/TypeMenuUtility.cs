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
        
        /// <summary>
        /// Retrieves the AddTypeMenuAttribute applied to the specified type.
        /// </summary>
        /// <returns>The AddTypeMenuAttribute instance if the attribute is present on the type, `null` otherwise.</returns>
        internal static AddTypeMenuAttribute GetAttribute(Type type)
        {
            return Attribute.GetCustomAttribute(type, typeof(AddTypeMenuAttribute)) as AddTypeMenuAttribute;
        }

        /// <summary>
        /// Get path segments used to display a type in selection menus.
        /// </summary>
        /// <param name="type">The type to derive display path segments from.</param>
        /// <returns>An array of path segments for the given type; empty array if <paramref name="type"/> is null. If the type has an AddTypeMenuAttribute, its segments are returned; otherwise the type's FullName is split on '.'.</returns>
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

        /// <summary>
        /// Order a sequence of types by their menu-order and display name for type-selection UIs.
        /// </summary>
        /// <param name="self">The sequence of types to order; elements may be null.</param>
        /// <returns>The sequence ordered first by the associated AddTypeMenuAttribute.Order (attribute missing defaults to 1; null types are ordered first), then by the attribute's MenuName or the type's Name.</returns>
        internal static IEnumerable<Type> OrderByType(this IEnumerable<Type> self)
        {
            return self.Select(CreateTypeWithAttribute)
                .OrderBy(GetTypeOrder)
                .ThenBy(GetTypeName)
                .Select(item => item.Type);
        }

        /// <summary>
            /// Creates a tuple containing the provided Type and its associated AddTypeMenuAttribute (if any).
            /// </summary>
            /// <param name="type">The Type to pair; may be null to represent a null entry.</param>
            /// <returns>A tuple whose `Type` element is the provided type and whose `Attribute` element is the associated AddTypeMenuAttribute, or null if none exists.</returns>
            private static (Type Type, AddTypeMenuAttribute Attribute) CreateTypeWithAttribute(Type type) =>
            (type, GetAttribute(type));

        /// <summary>
        /// Compute the ordering priority for a (Type, AddTypeMenuAttribute) pair.
        /// </summary>
        /// <param name="item">A tuple containing the Type and its associated AddTypeMenuAttribute (the attribute may be null).</param>
        /// <returns>
        /// An integer used for ordering: -999 if the Type is null, the attribute's Order if present, otherwise 1.
        /// </returns>
        private static int GetTypeOrder((Type Type, AddTypeMenuAttribute Attribute) item)
        {
            var (type, attribute) = item;
            
            if (type == null)
            {
                return -999;
            }

            return attribute?.Order ?? 1;
        }

        /// <summary>
        /// Gets the display name for a type using the attribute's menu name when available, otherwise the type's simple name.
        /// </summary>
        /// <param name="item">A tuple where Item1 is the <see cref="Type"/> and Item2 is its <see cref="AddTypeMenuAttribute"/> (either may be null).</param>
        /// <returns>The attribute's <c>MenuName</c> if present, the type's <c>Name</c> if the attribute is absent, or <c>null</c> if the type is <c>null</c>.</returns>
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