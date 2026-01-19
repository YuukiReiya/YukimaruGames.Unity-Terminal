#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using UnityEngine;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime.Input.LegacyInput
{
    [Serializable,AddTypeMenu("Legacy")]
    public sealed class LegacyInputKeyboardHandler : IKeyboardInputHandler
    {
        //private readonly LegacyInputKey _legacyInputKey;
        [SerializeField] private KeyCode _openKeyCode = KeyCode.LeftBracket;
        [SerializeField] private KeyCode _closeKeyCode = KeyCode.Escape;
        [SerializeField] private KeyCode _executeKeyCode = KeyCode.Return;
        [SerializeField] private KeyCode _prevHistoryKeyCode = KeyCode.UpArrow;
        [SerializeField] private KeyCode _nextHistoryKeyCode = KeyCode.DownArrow;
        [SerializeField] private KeyCode _autocompleteKeyCode = KeyCode.Tab;
        [SerializeField] private KeyCode _focusKeyCode = KeyCode.LeftControl;
        
        
        //public LegacyInputKeyboardHandler(LegacyInputKey legacyInputKey)
        public LegacyInputKeyboardHandler()
        {
            //_legacyInputKey = legacyInputKey;
        }

        public bool WasPressedThisFrame(TerminalAction action)
        {
            //var keyCode = _legacyInputKey.GetKey(action);
            var keyCode = GetKey(action);
            //return keyCode is not UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(keyCode);
            var r=keyCode is not UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(keyCode);

            if (action is TerminalAction.Execute)
            {
                var e = Event.current;

                if (e != null && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
                {
                    // ここで処理
                    Debug.Log("Enter Pressed in OnGUI");
                    // 他のUI要素に反応させたくない場合は消費させる
                    // e.Use(); 
                }

                Debug.Log($"key : {action} -- {r} / {e!=null} : {e?.type}");
            }

            return r;
        }

        public bool WasReleasedThisFrame(TerminalAction action)
        {
            //var keyCode = _legacyInputKey.GetKey(action);
            var keyCode = GetKey(action);
            return keyCode is not UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyUp(keyCode);
        }
        
        public KeyCode GetKey(TerminalAction action) => action switch
        {
            TerminalAction.None => KeyCode.None,
            TerminalAction.Open => _openKeyCode,
            TerminalAction.Close => _closeKeyCode,
            TerminalAction.Execute => _executeKeyCode,
            TerminalAction.PreviousHistory => _prevHistoryKeyCode,
            TerminalAction.NextHistory => _nextHistoryKeyCode,
            TerminalAction.Autocomplete => _autocompleteKeyCode,
            TerminalAction.Focus => _focusKeyCode,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
#endif
