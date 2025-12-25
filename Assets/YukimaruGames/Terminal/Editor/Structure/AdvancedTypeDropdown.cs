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
        public event Action<AdvancedTypeDropdownItem> OnItemSelected;

        internal AdvancedTypeDropdown(IEnumerable<Type> types, int maxLineCount, AdvancedDropdownState state) : base(state)
        {
            _types = types;
            minimumSize = new Vector2(minimumSize.x, EditorGUIUtility.singleLineHeight * maxLineCount + kHeaderHeight);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(TypeMenuUtility.kMenuDropdownHeader);
            AddTo(root, _types);
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is AdvancedTypeDropdownItem advancedTypeDropdownItem)
            {
                OnItemSelected?.Invoke(advancedTypeDropdownItem);
            }
        }

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

        private static void AddNullItem(AdvancedDropdownItem root, ref int id)
        {
            var nullItem = new AdvancedTypeDropdownItem(null, TypeMenuUtility.kMenuDropdownNullDisplayName)
            {
                id = id++,
            };
            root.AddChild(nullItem);
        }

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

        private static IReadOnlyDictionary<string, AdvancedDropdownItem> GetNodeMap(AdvancedDropdownItem parent)
        {
            if (!_nodeMapCache.TryGetValue(parent,out var nodeMap))
            {
                nodeMap = parent.children.ToDictionary(item => item.name, item => item);
                _nodeMapCache.AddOrUpdate(parent, nodeMap);
            }

            return nodeMap;
        }
    }
}