using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace YukimaruGames.Terminal.Editor
{
    internal static class PropertyDrawerPool
    {
        private static readonly Dictionary<Type, PropertyDrawer> _pool = new();

        internal static bool TryGet(Type type, out PropertyDrawer drawer)
        {
            if (!_pool.TryGetValue(type, out drawer))
            {
                var drawerType = GetDrawerType(type);
                if (drawerType != null)
                {
                    var constructor = drawerType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null);

                    if (constructor != null)
                    {
                        drawer = Activator.CreateInstance(drawerType, true) as PropertyDrawer;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(
                            $"[{nameof(PropertyDrawerPool)}] Failed to instantiate '{drawerType.FullName}'.{Environment.NewLine}The class must have a parameterless constructor.");
                    }
                }

                _pool[type] = drawer;
            }

            return drawer != null;
        }

        private static Type GetDrawerType(Type type)
        {
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            // 属性を持つTypeを取得.
            var caches = TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>();
            foreach (var cache in caches)
            {
                var attributes = cache.GetCustomAttributes(typeof(CustomPropertyDrawer), true);
                foreach (CustomPropertyDrawer attribute in attributes)
                {
                    var field = attribute.GetType().GetField("m_Type", bindingFlags);

                    if (field?.GetValue(attribute) is Type fieldType)
                    {
                        if (fieldType == type)
                        {
                            return cache;
                        }

                        var useForChildrenField = attribute.GetType().GetField("m_UseForChildren", bindingFlags);
                        if (useForChildrenField != null)
                        {
                            var useForChildrenFieldValue = useForChildrenField.GetValue(attribute);
                            if (useForChildrenFieldValue is bool and true &&
                                fieldType.IsAssignableFrom(type))
                            {
                                return cache;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
