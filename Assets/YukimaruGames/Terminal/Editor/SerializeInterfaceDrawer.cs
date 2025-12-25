using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Editor
{
    [CustomPropertyDrawer(typeof(SerializeInterfaceAttribute))]
    public sealed class SerializeInterfaceDrawer : PropertyDrawer
    {

        private const int kMaxTypeDropdownLineCount = 13;

        private static readonly GUIContent _nullDisplayName = new("None(null)");
        private static readonly GUIContent _isNotManagedReferenceLabel = new("The property type is not manage reference.");
        private readonly Dictionary<string, GUIContent> _typeNameDic = new();
        private readonly Dictionary<string, TypeDropdownCache> _typeDropdowns = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using var _ = new EditorGUI.PropertyScope(position, label, property);
            switch (property.propertyType)
            {
                case SerializedPropertyType.ManagedReference:
                    DrawManagedReference(position, property, label);
                    DrawDropdown(position, property, label);
                    DrawFoldout(position, property);
                    DrawExpanded(position, property, label);
                    break;
                default:
                    DrawNotManagedReference(position, label);
                    break;
            }
        }

        private void DrawManagedReference(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = EditorGUIUtility.singleLineHeight;

#if UNITY_2021_3_OR_NEWER
            var customAttribute = attribute as SerializeInterfaceAttribute;
            if (property.hasMultipleDifferentValues && (customAttribute?.UseToStringAsLabel ?? false))
            {
                var managedReferenceValue = property.managedReferenceValue;
                if (managedReferenceValue != null)
                {
                    label.text = managedReferenceValue.ToString();
                }
            }
#endif
        }

        private void DrawNotManagedReference(in Rect rect, GUIContent label)
        {
            EditorGUI.LabelField(rect, label, _isNotManagedReferenceLabel);
        }

        private void DrawDropdown(in Rect rect, SerializedProperty property, GUIContent label)
        {
            var prefixRect = EditorGUI.PrefixLabel(rect, label);

            // NOTE:高さを調整しないとサブクラスのシリアライズメンバー(プロパティ)のクリック判定を覆ってしまうため１行分の高さに補正.
            prefixRect.height = EditorGUIUtility.singleLineHeight;

            if (EditorGUI.DropdownButton(prefixRect, GetTypeName(property), FocusType.Keyboard))
            {
                var cache = GetTypeDropdown(property);

                // NOTE:
                // managedReferenceFieldTypenameを利用した「同じ型のフィールドであれば、ドロップダウンのインスタンスを使い回す」
                // 実装になっているため同じ型の別プロパティを参照した際に古いプロパティの参照が残ってしまう懸念があるため直前で
                // SerializedPropertyをセットする。(イベントのクリアも行い、多重登録を防止)
                cache.TypeDropdown.Prepare(property);

                cache.TypeDropdown.OnItemSelected += OnItemSelected;
                cache.TypeDropdown.Show(prefixRect);
            }
        }

        private void DrawFoldout(Rect rect, SerializedProperty property)
        {
            if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                return;
            }

            rect.height = EditorGUIUtility.singleLineHeight;
#if UNITY_2022_2_OR_NEWER && !UNITY_6000_0_OR_NEWER
            // NOTE: Position x must be adjusted.
            // FIXME: Is there a more essential solution...?
            // The most promising is UI Toolkit, but it is currently unable to reproduce all of SubclassSelector features. (Complete provision of contextual menu, e.g.)
            // 2021.3: No adjustment
            // 2022.1: No adjustment
            // 2022.2: Adjustment required
            // 2022.3: Adjustment required
            // 2023.1: Adjustment required
            // 2023.2: Adjustment required
            // 6000.0: No adjustment
            rect.x -= 12;
#endif

            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, GUIContent.none, true);
        }

        private void DrawExpanded(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return;
            }

            using var indentLabelScope = new EditorGUI.IndentLevelScope();
            var drawer = GetDrawer(property);
            var offset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            rect.y += offset;

            if (drawer != null)
            {
                rect.height = drawer.GetPropertyHeight(property, label);
                drawer.OnGUI(rect, property, label);
            }
            else
            {
                // 子要素のプロパティを描画します。
                // 注: 次のコードでは、折り畳みレイアウトが正しく機能していないため、子要素のプロパティを自分で反復処理します。
                // EditorGUI.PropertyField(position, property, GUIContent.none, true);

                foreach (var childProp in property.GetChildProperties())
                {
                    var height = EditorGUI.GetPropertyHeight(childProp, new GUIContent(childProp.displayName, childProp.tooltip), true);
                    EditorGUI.PropertyField(rect, childProp, true);

                    rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }

        private GUIContent GetTypeName(SerializedProperty property)
        {
            var managedReferenceFullTypename = property.managedReferenceFullTypename;

            if (string.IsNullOrEmpty(managedReferenceFullTypename))
            {
                return _nullDisplayName;
            }

            if (_typeNameDic.TryGetValue(managedReferenceFullTypename, out var cachedTypeName))
            {
                return cachedTypeName;
            }

            var type = property.GetTypeByManagedReferenceFullTypename();
            string typeName = null;

            var typeMenu = TypeMenuUtility.GetAttribute(type);
            if (typeMenu != null)
            {
                typeName = typeMenu.GetMenuNameWithoutPath();
                if (!string.IsNullOrWhiteSpace(typeName))
                {
                    typeName = ObjectNames.NicifyVariableName(typeName);
                }
            }

            if (string.IsNullOrEmpty(typeName))
            {
                typeName = ObjectNames.NicifyVariableName(type.Name);
            }

            var content = new GUIContent(typeName);
            _typeNameDic[managedReferenceFullTypename] = content;
            return content;
        }

        private TypeDropdownCache GetTypeDropdown(SerializedProperty property)
        {
            var managedReferenceFieldTypename = property.managedReferenceFieldTypename;

            if (!_typeDropdowns.TryGetValue(managedReferenceFieldTypename, out var result))
            {
                var state = new AdvancedDropdownState();
                var baseType = property.GetTypeByManagedReferenceFieldTypename();
                var dropdown = new AdvancedTypeDropdown(
                    TypeSearch.GetAvailableReferenceTypes(baseType),
                    kMaxTypeDropdownLineCount,
                    state);

                result = new TypeDropdownCache(dropdown, state);
                _typeDropdowns[managedReferenceFieldTypename] = result;
            }

            return result;
        }

        private PropertyDrawer GetDrawer(SerializedProperty property)
        {
            var propertyType = property.GetTypeByManagedReferenceFullTypename();
            if (propertyType != null && PropertyDrawerPool.TryGet(propertyType, out var drawer))
            {
                return drawer;
            }

            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var drawer = GetDrawer(property);
            return drawer switch
            {
                not null => property.isExpanded ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + drawer.GetPropertyHeight(property, label) : EditorGUIUtility.singleLineHeight,
                null => property.isExpanded ? EditorGUI.GetPropertyHeight(property, true) : EditorGUIUtility.singleLineHeight,
            };
        }

        private void OnItemSelected(SerializedProperty property, AdvancedTypeDropdownItem item)
        {
            var cache = GetTypeDropdown(property);
            cache.TypeDropdown.OnItemSelected -= OnItemSelected;

            var type = item.Type;
            foreach (var targetObject in property.serializedObject.targetObjects)
            {
                var individualObject = new SerializedObject(targetObject);
                var individualProperty = individualObject.FindProperty(property.propertyPath);
                var obj = individualProperty.SetManagedReferenceValue(type);
                individualProperty.isExpanded = obj != null;

                individualObject.ApplyModifiedProperties();
                individualObject.Update();
            }
        }
    }
}