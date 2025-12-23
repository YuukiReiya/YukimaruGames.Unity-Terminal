using UnityEditor.IMGUI.Controls;

namespace YukimaruGames.Terminal.Editor
{
    internal readonly struct TypeDropdownCache
    {
        internal AdvancedTypeDropdown TypeDropdown { get; }
        internal AdvancedDropdownState State { get; }

        internal TypeDropdownCache(AdvancedTypeDropdown typeDropdown, AdvancedDropdownState state)
        {
            TypeDropdown = typeDropdown;
            State = state;
        }
    }
}
