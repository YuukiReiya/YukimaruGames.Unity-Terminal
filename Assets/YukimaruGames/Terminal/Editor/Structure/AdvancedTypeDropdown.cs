using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace YukimaruGames.Terminal.Editor
{
    internal sealed class AdvancedTypeDropdown : AdvancedDropdown
    {
        private const int kMaxNamespaceNestCount = 16;

        private static readonly float kHeaderHeight = EditorGUIUtility.singleLineHeight * 2;
        private static readonly ConditionalWeakTable<AdvancedDropdownItem, IReadOnlyDictionary<string, AdvancedDropdownItem>> _nodeMapCache = new();

        private readonly IEnumerable<Type> _types;
        
        private SerializedProperty _serializedProperty;
        
        public event Action<SerializedProperty, AdvancedTypeDropdownItem> OnItemSelected;

        /// <summary>
        /// Initializes the dropdown with the provided types and configures its maximum visible height based on the given line count.
        /// </summary>
        /// <param name="types">The collection of types to display in the dropdown.</param>
        /// <param name="maxLineCount">Maximum number of visible type rows to show (affects dropdown height).</param>
        /// <param name="state">The dropdown state passed to the base AdvancedDropdown constructor.</param>
        internal AdvancedTypeDropdown(IEnumerable<Type> types, int maxLineCount, AdvancedDropdownState state) : base(state)
        {
            _types = types;
            minimumSize = new Vector2(minimumSize.x, EditorGUIUtility.singleLineHeight * maxLineCount + kHeaderHeight);
        }

        /// <summary>
        /// Associates a SerializedProperty to receive the selected type and resets any previously attached selection handlers.
        /// </summary>
        /// <param name="property">The SerializedProperty that will be supplied as the first argument to OnItemSelected when a type is chosen; may be null to clear the association.</param>
        internal void Prepare(SerializedProperty property)
        {
            _serializedProperty = property;
            ClearEvents();
        }

        /// <summary>
        /// Removes all subscribers from the <c>OnItemSelected</c> event.
        /// </summary>
        private void ClearEvents()
        {
            OnItemSelected = null;
        }

        /// <summary>
        /// Constructs the root dropdown item for the menu and populates it with the available type entries.
        /// </summary>
        /// <returns>The root <see cref="AdvancedDropdownItem"/> containing the header and all populated child items.</returns>
        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(TypeMenuUtility.kMenuDropdownHeader);
            AddTo(root, _types);
            return root;
        }

        /// <summary>
        /// Handles a dropdown item selection and raises <c>OnItemSelected</c> when the selected item represents a type.
        /// </summary>
        /// <param name="item">The dropdown item that was selected.</param>
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is AdvancedTypeDropdownItem advancedTypeDropdownItem)
            {
                OnItemSelected?.Invoke(_serializedProperty, advancedTypeDropdownItem);
            }
        }

        /// <summary>
        /// Populate the given root dropdown item with a null/default entry and entries for the supplied types.
        /// </summary>
        /// <param name="root">The root dropdown item to add entries under.</param>
        /// <param name="types">Collection of types to include in the menu; the method will build a hierarchical or flattened structure based on each type's metadata.</param>
        private static void AddTo(AdvancedDropdownItem root, IEnumerable<Type> types)
        {
            var (data, shouldFlattenMenu) = PrepareMenu(types);
            var itemCount = 0;
            AddNullItem(root, ref itemCount);

            foreach (var item in data)
            {
                var parent = BuildTypeNode(
                    root,
                    item.Segments,
                    ref itemCount,
                    shouldFlattenMenu);
                var typeName = ObjectNames.NicifyVariableName(item.Segments[^1]);
                var typeItem = new AdvancedTypeDropdownItem(item.Type, typeName)
                {
                    id = itemCount++
                };

                parent.AddChild(typeItem);
            }
        }

        /// <summary>
        /// Adds a null (default) type entry to the given root dropdown item.
        /// </summary>
        /// <param name="root">The parent dropdown item to which the null entry will be added.</param>
        /// <param name="id">The id value to assign to the new entry; this value is incremented after assignment.</param>
        private static void AddNullItem(AdvancedDropdownItem root, ref int id)
        {
            var nullItem = new AdvancedTypeDropdownItem(null, TypeMenuUtility.kMenuDropdownNullDisplayName)
            {
                id = id++,
            };
            root.AddChild(nullItem);
        }

        /// <summary>
        /// Produces menu entries for the provided types and determines whether the menu can be displayed flattened (no nested namespace nodes).
        /// </summary>
        /// <returns>
        /// A tuple containing:
        /// - <c>MenuData</c>: an array of <c>TypeMenuData</c> for each input type in display order.
        /// - <c>shouldFlattenMenu</c>: <c>true</c> when the menu can be presented without intermediate namespace segments, <c>false</c> when nesting is required.
        /// </returns>
        private static (TypeMenuData[] MenuData, bool shouldFlattenMenu) PrepareMenu(IEnumerable<Type> types)
        {
            var sortedTypes = types
                .OrderByType()
                .Select(item => new TypeMenuData(
                        item,
                        TypeMenuUtility.GetSplitPathSegments(item),
                        TypeMenuUtility.GetAttribute(item)
                    )
                )
                .ToArray();

            var shouldFlattenMenu = true;
            var namespaces = new string[kMaxNamespaceNestCount];
            foreach (var data in sortedTypes)
            {
                if (data.Segments.Length <= 1)
                {
                    continue;
                }

                if (data.Attribute != null)
                {
                    shouldFlattenMenu = false;
                    break;
                }

                for (var k = 0; k < (data.Segments.Length - 1); ++k)
                {
                    var ns = namespaces[k];
                    var segment = data.Segments[k];
                    if (ns == null)
                    {
                        namespaces[k] = segment;
                    }
                    else if (ns != segment)
                    {
                        shouldFlattenMenu = false;
                        break;
                    }
                }

                if (!shouldFlattenMenu)
                {
                    break;
                }
            }

            return (sortedTypes, shouldFlattenMenu);
        }

        /// <summary>
        /// Ensure the hierarchical parent node for the provided path segments and return it, creating intermediate nodes when necessary.
        /// </summary>
        /// <param name="root">The root dropdown item to start from.</param>
        /// <param name="segments">Path segments representing nested nodes; the last segment is excluded from parent creation.</param>
        /// <param name="id">Incrementing identifier used for newly created nodes; it is advanced for each created node.</param>
        /// <param name="shouldFlattenMenu">If true, nesting is disabled and the root is returned without creating nodes.</param>
        /// <returns>The parent AdvancedDropdownItem under which the final segment's item should be added (or the root if no nesting is required).</returns>
        private static AdvancedDropdownItem BuildTypeNode(AdvancedDropdownItem root,
            string[] segments,
            ref int id,
            bool shouldFlattenMenu)
        {
            if (segments.Length == 0 || shouldFlattenMenu)
            {
                return root;
            }

            var parent = root;
            for (var k = 0; k < (segments.Length - 1); ++k)
            {
                var segment = segments[k];
                var map = GetNodeMap(parent);

                if (map.TryGetValue(segment, out var node))
                {
                    parent = node;
                }
                else
                {
                    var newItem = new AdvancedDropdownItem(segment)
                    {
                        id = id++,
                    };

                    parent.AddChild(newItem);
                    parent = newItem;
                }
            }

            return parent;
        }

        /// <summary>
        /// Get or create a cached dictionary that maps each child item's name to its AdvancedDropdownItem for the given parent.
        /// </summary>
        /// <param name="parent">The parent dropdown item whose children will be indexed by name.</param>
        /// <returns>A read-only dictionary mapping child item names to their corresponding <see cref="AdvancedDropdownItem"/> instances.</returns>
        private static IReadOnlyDictionary<string, AdvancedDropdownItem> GetNodeMap(AdvancedDropdownItem parent)
        {
            if (!_nodeMapCache.TryGetValue(parent, out var nodeMap))
            {
                nodeMap = parent.children.ToDictionary(item => item.name, item => item);
                _nodeMapCache.AddOrUpdate(parent, nodeMap);
            }

            return nodeMap;
        }
    }
}