using System;
using UnityEditor.IMGUI.Controls;

namespace YukimaruGames.Terminal.Editor
{
    internal sealed class AdvancedTypeDropdownItem : AdvancedDropdownItem
    {
        internal Type Type { get; }

        internal AdvancedTypeDropdownItem(Type type, string name) : base(name)
        {
            Type = type;
        }
    }
}
