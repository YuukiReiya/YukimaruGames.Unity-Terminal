#if UNITY_EDITOR
#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif
//#undef ENABLE_INPUT_SYSTEM
//#undef ENABLE_LEGACY_INPUT_MANAGER

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Runtime;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Editor
{
    [CustomEditor(typeof(TerminalBootstrapper))]
    public sealed class TerminalBootstrapperEditor : UnityEditor.Editor
    {
        private enum Tab
        {
            View,
            Animation,
            Input,
            System,
        }

        // SerializedObject
        private SerializedObject _serializedObject;
        
        // Property
        private SerializedProperty _fontProp;
        private SerializedProperty _fontSizeProp;
        private SerializedProperty _backgroundColorProp;
        private SerializedProperty _messageColorProp;
        private SerializedProperty _entryColorProp;
        private SerializedProperty _warningColorProp;
        private SerializedProperty _errorColorProp;
        private SerializedProperty _assertColorProp;
        private SerializedProperty _exceptionColorProp;
        private SerializedProperty _systemColorProp;
        private SerializedProperty _inputColorProp;
        private SerializedProperty _caretColorProp;
        private SerializedProperty _selectionColorProp;
        private SerializedProperty _promptColorProp;
        private SerializedProperty _cursorFlashSpeedProp;
        private SerializedProperty _inputKeyboardTypeProp;
        private SerializedProperty _durationProp;
        private SerializedProperty _compactScaleProp;
        private SerializedProperty _anchorProp;
        private SerializedProperty _windowStyleProp;
        private SerializedProperty _bufferSizeProp;
        private SerializedProperty _promptProp;
        private SerializedProperty _bootupCommandProp;
        private SerializedProperty _bootupWindowStateProp;
        private SerializedProperty _inputSystemKeyProp;
        private SerializedProperty _legacyInputKeyProp;
        
        
        private Tab _tab = Tab.View;
        private Lazy<GUIStyle> _toolbarStyleLazy;
        private Lazy<GUIStyle> _typeStyleLazy;
        private readonly Lazy<GUIStyle> _popupStyleLazy = new(() => new GUIStyle(EditorStyles.popup)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        });
        
        private const float MinWidth = 30f;
        private const float MinHeight = 30f;
        
        private const string OpenKeyName = "OpenKey";
        private const string CloseKeyName = "CloseKey";
        private const string ExecuteKeyName = "ExecuteKey";
        private const string PrevHistoryKeyName = "PrevHistoryKey";
        private const string NextHistoryKeyName = "NextHistoryKey";
        private const string AutocompleteKeyName = "AutocompleteKey";
        private const string FocusKeyName = "FocusKey";
        
        private void OnEnable()
        {
            _serializedObject = serializedObject;
            
            // view-prop
            {
                _fontProp = _serializedObject.FindProperty("_font");
                _fontSizeProp = _serializedObject.FindProperty("_fontSize");
                _backgroundColorProp = _serializedObject.FindProperty("_backgroundColor");
                _messageColorProp = _serializedObject.FindProperty("_messageColor");
                _entryColorProp  = _serializedObject.FindProperty("_entryColor");
                _warningColorProp = _serializedObject.FindProperty("_warningColor");
                _errorColorProp = _serializedObject.FindProperty("_errorColor");
                _assertColorProp = _serializedObject.FindProperty("_assertColor");
                _exceptionColorProp = _serializedObject.FindProperty("_exceptionColor");
                _systemColorProp = _serializedObject.FindProperty("_systemColor");
                _inputColorProp = _serializedObject.FindProperty("_inputColor");
                _caretColorProp = _serializedObject.FindProperty("_caretColor");
                _selectionColorProp = _serializedObject.FindProperty("_selectionColor");
                _cursorFlashSpeedProp =  _serializedObject.FindProperty("_cursorFlashSpeed");
                _promptColorProp = _serializedObject.FindProperty("_promptColor");
            }

            // input-prop
            {
                _inputKeyboardTypeProp = _serializedObject.FindProperty("_inputKeyboardType");
#if ENABLE_INPUT_SYSTEM
                _inputSystemKeyProp = _serializedObject.FindProperty("_inputSystemKey");
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                _legacyInputKeyProp = _serializedObject.FindProperty("_legacyInputKey");
#endif
            }

            // animation-prop
            {
                _anchorProp = _serializedObject.FindProperty("_anchor");
                _windowStyleProp = _serializedObject.FindProperty("_windowStyle");
                _durationProp = _serializedObject.FindProperty("_duration");
                _compactScaleProp = _serializedObject.FindProperty("_compactScale");
            }
            
            // system-prop
            {
                _bufferSizeProp = _serializedObject.FindProperty("_bufferSize");
                _promptProp = _serializedObject.FindProperty("_prompt");
                _bootupCommandProp = _serializedObject.FindProperty("_bootupCommand");
                _bootupWindowStateProp = _serializedObject.FindProperty("_bootupWindowState");
            }

            _toolbarStyleLazy = new Lazy<GUIStyle>(() => new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(0, 0, 0, 13),
                stretchWidth = true,
                richText = true,
            });

            _typeStyleLazy = new Lazy<GUIStyle>(() => new GUIStyle(EditorStyles.radioButton)
            {
                padding = new RectOffset(-6,-6,0,0),
                alignment = TextAnchor.MiddleCenter,
            });
        }

        private void OnDisable()
        {
            _toolbarStyleLazy = null;
            _typeStyleLazy = null;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(5f);
            using (new EditorGUILayout.HorizontalScope())
            {
                _tab = (Tab)GUILayout.Toolbar(
                    (int)_tab,
                    Enum.GetNames(typeof(Tab)),
                    _toolbarStyleLazy.Value,
                    GUILayout.MinWidth(MinWidth),
                    GUILayout.MinHeight(MinHeight));
            }
            EditorGUILayout.Space(6f);
            
            using (new EditorGUILayout.VerticalScope())
            {
                RenderCategory();
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void RenderCategory()
        {
            switch (_tab)
            {
                case Tab.View:
                    RenderViewCategory();
                    return;
                case Tab.Animation:
                    RenderAnimationCategory();
                    break;
                case Tab.Input:
                    RenderInputCategory();
                    break;
                case Tab.System:
                    RenderSystemCategory();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RenderViewCategory()
        {
            EditorGUILayout.LabelField("Font", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_fontProp, true);
                EditorGUILayout.PropertyField(_fontSizeProp, new GUIContent("Size"));
            }

            EditorGUILayout.Space(6f);

            EditorGUILayout.LabelField("Background", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_backgroundColorProp);
            
            EditorGUILayout.Space(6f);
            
            EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_promptColorProp);
            
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_inputColorProp);
            EditorGUILayout.PropertyField(_caretColorProp);
            EditorGUILayout.PropertyField(_selectionColorProp);
            
            EditorGUILayout.Space(2f);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(3f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Slider(_cursorFlashSpeedProp, 0f, 3f);
                    if (GUILayout.Button("RESET", EditorStyles.miniButton, GUILayout.Width(60f)))
                    {
                        _cursorFlashSpeedProp.floatValue = 1.886792f; // GUI.skin.settings.cursorFlashSpeed : 実値.
                    }
                }
                EditorGUILayout.Space(3f);
            }

            EditorGUILayout.Space(6f);
            
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_messageColorProp);
            EditorGUILayout.PropertyField(_entryColorProp);
            EditorGUILayout.PropertyField(_warningColorProp);
            EditorGUILayout.PropertyField(_errorColorProp);
            EditorGUILayout.PropertyField(_assertColorProp);
            EditorGUILayout.PropertyField(_exceptionColorProp);
            EditorGUILayout.PropertyField(_systemColorProp);

            EditorGUILayout.Space(6f);
        }

        private void RenderAnimationCategory()
        {
            EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5f);
                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    _bootupWindowStateProp.enumValueIndex = EditorGUILayout.Popup(
                        _bootupWindowStateProp.enumValueIndex,
                        Enum.GetNames(typeof(TerminalState)),
                        _popupStyleLazy.Value);
                }
                EditorGUILayout.Space(2f);
                _anchorProp.enumValueIndex = EditorGUILayout.Popup(
                    _anchorProp.enumValueIndex,
                    Enum.GetNames(typeof(TerminalAnchor)),
                    _popupStyleLazy.Value);
                EditorGUILayout.Space(2f);
                _windowStyleProp.enumValueIndex = EditorGUILayout.Popup(
                    _windowStyleProp.enumValueIndex,
                    Enum.GetNames(typeof(TerminalWindowStyle)),
                    _popupStyleLazy.Value);
                EditorGUILayout.Space(5f);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Param", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(2f);
                EditorGUILayout.Slider(_durationProp, 0f, 3f);
                EditorGUILayout.Space(1f);
                EditorGUILayout.Slider(_compactScaleProp, 0.1f, 1f);
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.Space(6f);
        }

        private void RenderInputCategory()
        {
            var keyboardTypeIndex = _inputKeyboardTypeProp.enumValueIndex;
            var keyboardType = (InputKeyboardType)keyboardTypeIndex;
            
            EditorGUILayout.LabelField("Keyboard Type", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(1f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (InputKeyboardType type in Enum.GetValues(typeof(InputKeyboardType)))
                    {
                        var isEditable =
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                            true;
#elif ENABLE_INPUT_SYSTEM
                    type is not InputKeyboardType.Legacy;
#elif ENABLE_LEGACY_INPUT_MANAGER
                    type is not InputKeyboardType.InputSystem;
#else
                    type is InputKeyboardType.None;
#endif
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        using (new EditorGUI.DisabledScope(!isEditable))
                        {
                            var isSelected = GUILayout.Toggle(
                                keyboardType == type,
                                type.ToString(),
                                _typeStyleLazy.Value);
                            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                            if (isSelected && keyboardType != type && isEditable)
                            {
                                _inputKeyboardTypeProp.enumValueIndex = (int)type;
                            }
                        }
                    }
                }
                EditorGUILayout.Space(1f);
            }
            EditorGUILayout.Space(6f);
            
            switch (keyboardType)
            {
                case InputKeyboardType.None:
                    DrawNone();
                    break;
                case InputKeyboardType.InputSystem:
                    DrawInputSystem();
                    break;
                case InputKeyboardType.Legacy:
                    DrawLegacyInput();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RenderSystemCategory()
        {
            EditorGUILayout.LabelField("Buffer", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(_bufferSizeProp);
            }
            
            EditorGUILayout.Space(6f);
            
            EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_promptProp);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(_bootupCommandProp);
            }
            
            EditorGUILayout.Space(6f);
        }
        
        private void DrawNone()
        {
            const string msg = "キーボード入力は設定されていません。\nGUI操作をサポートする場合はDrawButtonを有効にしてください。";
            EditorGUILayout.HelpBox(msg, MessageType.Info);
        }

        [Conditional("ENABLE_INPUT_SYSTEM")]
        private void DrawInputSystem()
        {
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel);
            using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            var openKeyProp = _inputSystemKeyProp.FindPropertyRelative("_openKey");
            var closeKeyProp = _inputSystemKeyProp.FindPropertyRelative("_closeKey");
            var executeKeyProp = _inputSystemKeyProp.FindPropertyRelative("_executeKey");
            var prevHistoryKeyProp = _inputSystemKeyProp.FindPropertyRelative("_prevHistoryKey");
            var nextHistoryKeyProp = _inputSystemKeyProp.FindPropertyRelative("_nextHistoryKey");
            var autocompleteKeyProp = _inputSystemKeyProp.FindPropertyRelative("_autocompleteKey");
            var focusKeyProp = _inputSystemKeyProp.FindPropertyRelative("_focusKey");
            
            EditorGUILayout.PropertyField(openKeyProp, new GUIContent(OpenKeyName));
            EditorGUILayout.PropertyField(closeKeyProp, new GUIContent(CloseKeyName));
            EditorGUILayout.PropertyField(executeKeyProp, new GUIContent(ExecuteKeyName));
            EditorGUILayout.PropertyField(prevHistoryKeyProp, new GUIContent(PrevHistoryKeyName));
            EditorGUILayout.PropertyField(nextHistoryKeyProp, new GUIContent(NextHistoryKeyName));
            EditorGUILayout.PropertyField(autocompleteKeyProp, new GUIContent(AutocompleteKeyName));
            EditorGUILayout.PropertyField(focusKeyProp, new GUIContent(FocusKeyName));
        }

        [Conditional("ENABLE_LEGACY_INPUT_MANAGER")]
        private void DrawLegacyInput()
        {
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel);
            using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            
            var openKeyProp = _legacyInputKeyProp.FindPropertyRelative("_openKeyCode");
            var closeKeyProp = _legacyInputKeyProp.FindPropertyRelative("_closeKeyCode");
            var executeKeyProp = _legacyInputKeyProp.FindPropertyRelative("_executeKeyCode");
            var prevHistoryKeyProp = _legacyInputKeyProp.FindPropertyRelative("_prevHistoryKeyCode");
            var nextHistoryKeyProp = _legacyInputKeyProp.FindPropertyRelative("_nextHistoryKeyCode");
            var autocompleteKeyProp = _legacyInputKeyProp.FindPropertyRelative("_autocompleteKeyCode");

            EditorGUILayout.PropertyField(openKeyProp, new GUIContent(OpenKeyName));
            EditorGUILayout.PropertyField(closeKeyProp, new GUIContent(CloseKeyName));
            EditorGUILayout.PropertyField(executeKeyProp, new GUIContent(ExecuteKeyName));
            EditorGUILayout.PropertyField(prevHistoryKeyProp, new GUIContent(PrevHistoryKeyName));
            EditorGUILayout.PropertyField(nextHistoryKeyProp, new GUIContent(NextHistoryKeyName));
            EditorGUILayout.PropertyField(autocompleteKeyProp, new GUIContent(AutocompleteKeyName));
        }
    }
}
#endif
