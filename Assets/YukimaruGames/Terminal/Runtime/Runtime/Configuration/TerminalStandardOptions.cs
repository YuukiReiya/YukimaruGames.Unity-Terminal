#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Runtime.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Runtime.Input.LegacyInput;
#endif

using System;
using UnityEngine;

namespace YukimaruGames.Terminal.Runtime
{
    [Serializable]
    public class TerminalStandardOptions : ITerminalOptions
    {
        [Header("Input Settings")]
        [SerializeField] private InputKeyboardType _inputKeyboardType = InputKeyboardType.InputSystem;
        
#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private LegacyInputKey _legacyInputKey;
#endif
        
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputSystemKey _inputSystemKey;
#endif

        [Header("Command Settings")]
        [SerializeField] private int _bufferSize = 256;
        [SerializeField] private string _prompt = "$";
        [SerializeField] private string _bootupCommand;
        
        [Header("UI Controls")]
        [SerializeField] private bool _buttonVisible;
        [SerializeField] private bool _buttonReverse;
        
        public InputKeyboardType InputKeyboardType => _inputKeyboardType;
        
#if ENABLE_LEGACY_INPUT_MANAGER
        public LegacyInputKey LegacyInputKey => _legacyInputKey;
#endif
        
#if ENABLE_INPUT_SYSTEM
        public InputSystemKey InputSystemKey => _inputSystemKey;
#endif
        
        public int BufferSize => _bufferSize;
        public string Prompt => _prompt;
        public string BootupCommand => _bootupCommand;
        public bool IsButtonVisible => _buttonVisible;
        public bool IsButtonReverse => _buttonReverse;
    }
}
