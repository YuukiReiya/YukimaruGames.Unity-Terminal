using UnityEditor.IMGUI.Controls;

namespace YukimaruGames.Terminal.Editor
{
    internal readonly struct TypeDropdownCache
    {
        internal AdvancedTypeDropdown TypeDropdown { get; }
        internal AdvancedDropdownState State { get; }

        /// <summary>
        /// Initializes a new instance of TypeDropdownCache containing the specified type dropdown and its state.
        /// </summary>
        /// <param name="typeDropdown">The AdvancedTypeDropdown to store.</param>
        /// <param name="state">The AdvancedDropdownState associated with the dropdown.</param>
        internal TypeDropdownCache(AdvancedTypeDropdown typeDropdown, AdvancedDropdownState state)
        {
            TypeDropdown = typeDropdown;
            State = state;
        }
    }
}