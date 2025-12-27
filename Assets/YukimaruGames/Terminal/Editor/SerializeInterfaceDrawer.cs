using System;
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

        /// <summary>
        /// Draws the inspector UI for the given SerializedProperty, rendering managed-reference controls when applicable and a notice otherwise.
        /// </summary>
        /// <param name="position">Area on the screen to draw the property.</param>
        /// <param name="property">The SerializedProperty to render.</param>
        /// <param name="label">The label content for the property field.</param>
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

        /// <summary>
        /// Renders the header line for a managed-reference property and, when applicable, replaces the label text with the managed reference's ToString() value for multi-object editing.
        /// </summary>
        /// <param name="rect">Layout rectangle for the header; height is set to a single line.</param>
        /// <param name="property">The serialized managed-reference property being drawn.</param>
        /// <param name="label">The field label content to render; may be modified to show the managed-reference's ToString() when configured.</param>
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

        /// <summary>
        /// Draws a label indicating that the given property is not a managed reference.
        /// </summary>
        /// <param name="rect">Area in which to draw the label.</param>
        /// <param name="label">The property label to display before the notice.</param>
        private void DrawNotManagedReference(in Rect rect, GUIContent label)
        {
            EditorGUI.LabelField(rect, label, _isNotManagedReferenceLabel);
        }

        /// <summary>
        /// Draws the type-selection dropdown next to the property's prefix label.
        /// </summary>
        /// <remarks>
        /// Adjusts the prefix area to a single line to avoid covering child-property click targets.
        /// When the dropdown button is pressed, the cached type dropdown for the property's field is prepared,
        /// the drawer subscribes to item selection, and the dropdown is shown at the prefix area.
        /// </remarks>
        /// <param name="rect">The full rect available for the property row.</param>
        /// <param name="property">The serialized property representing the managed reference field.</param>
        /// <param name="label">The label to render as the property's prefix.</param>
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

        /// <summary>
        /// Renders a foldout control that toggles the serialized property's expanded state for managed-reference values.
        /// </summary>
        /// <param name="rect">The rectangle on the inspector to draw the foldout within.</param>
        /// <param name="property">The serialized property whose <see cref="SerializedProperty.isExpanded"/> will be toggled; no action is taken if the property has no managed reference type.</param>
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

        /// <summary>
        /// Renders the expanded view of a managed-reference property: indents and draws either a specialized drawer for the concrete type or the property's child fields when expanded.
        /// </summary>
        /// <param name="rect">The layout rectangle to use for drawing the expanded contents; drawing begins after the property's single-line header within this rect.</param>
        /// <param name="property">The serialized property whose expanded contents should be drawn; must be the managed-reference property shown by this drawer.</param>
        /// <param name="label">The original label for the property, forwarded to any delegated drawer.</param>
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

        /// <summary>
        /// Retrieves a cached display name GUIContent for the property's managed reference type, or a null placeholder if none.
        /// </summary>
        /// <param name="property">The SerializedProperty representing the managed reference whose type name to obtain.</param>
        /// <returns>A GUIContent containing the nicified display name for the property's managed reference type, or a placeholder GUIContent when no type is assigned.</returns>
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

        /// <summary>
        /// Retrieve or create a cached TypeDropdownCache for the managed-reference field represented by the given property.
        /// </summary>
        /// <param name="property">The SerializedProperty whose managedReferenceFieldTypename identifies the dropdown cache; expected to represent a managed reference field.</param>
        /// <returns>The cached or newly created TypeDropdownCache associated with the property's managed reference field typename.</returns>
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

        /// <summary>
        /// Retrieve the PropertyDrawer registered for the managed-reference runtime type represented by the given serialized property.
        /// </summary>
        /// <param name="property">SerializedProperty whose managed-reference full type name will be resolved to find a matching PropertyDrawer.</param>
        /// <returns>The matching PropertyDrawer if one is registered for the resolved type, otherwise <c>null</c>.</returns>
        private PropertyDrawer GetDrawer(SerializedProperty property)
        {
            var propertyType = property.GetTypeByManagedReferenceFullTypename();
            if (propertyType != null && PropertyDrawerPool.TryGet(propertyType, out var drawer))
            {
                return drawer;
            }

            return null;
        }

        /// <summary>
        /// Calculates the pixel height required to render the property in the inspector, accounting for a one-line header and any expanded contents provided by a type-specific drawer.
        /// </summary>
        /// <returns>The height in pixels required to draw the property. If the property is collapsed this is a single line height; if expanded, includes the header plus the nested property's required height (from a drawer if available, otherwise the default property height).</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var drawer = GetDrawer(property);
            return drawer switch
            {
                not null => property.isExpanded ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + drawer.GetPropertyHeight(property, label) : EditorGUIUtility.singleLineHeight,
                null => property.isExpanded ? EditorGUI.GetPropertyHeight(property, true) : EditorGUIUtility.singleLineHeight,
            };
        }

        /// <summary>
        /// Applies the selected concrete type from the type dropdown to the serialized managed-reference property for all target objects and updates their serialized state.
        /// </summary>
        /// <param name="property">The SerializedProperty that holds the managed reference to update.</param>
        /// <param name="item">The selected dropdown item that specifies the concrete type to assign (may be null to clear the reference).</param>
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
                if (type != null && obj == null)
                {
                    Debug.LogError($"[{GetType().Name}] Type {(type?.FullName ?? "null")} does not have a parameterless public constructor.{Environment.NewLine}Please ensure the class is not abstract and has a default constructor.");
                }
                individualProperty.isExpanded = obj != null;

                individualObject.ApplyModifiedProperties();
                individualObject.Update();
            }
        }
    }
}