using System;
using UnityEditor.IMGUI.Controls;

namespace YukimaruGames.Terminal.Editor
{
    internal sealed class AdvancedTypeDropdownItem : AdvancedDropdownItem
    {
        internal Type Type { get; }

        /// <summary>
        /// Creates a dropdown item that associates the specified type with the given display name.
        /// </summary>
        /// <param name="type">The System.Type to associate with this dropdown item.</param>
        /// <param name="name">The display name shown in the dropdown.</param>
        internal AdvancedTypeDropdownItem(Type type, string name) : base(name)
        {
            Type = type;
        }
    }
}