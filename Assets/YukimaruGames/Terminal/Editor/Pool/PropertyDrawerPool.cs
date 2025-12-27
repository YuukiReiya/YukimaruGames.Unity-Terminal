using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace YukimaruGames.Terminal.Editor
{
    internal static class PropertyDrawerPool
    {
        private static readonly Dictionary<Type, PropertyDrawer> _pool = new();

        /// <summary>
        /// Retrieves a PropertyDrawer for the specified type, creating and caching one if none is cached.
        /// </summary>
        /// <param name="type">The target type for which to resolve a PropertyDrawer.</param>
        /// <param name="drawer">When this method returns, contains the resolved PropertyDrawer instance, or null if none was found.</param>
        /// <returns>`true` if a non-null PropertyDrawer was obtained and returned in <paramref name="drawer"/>, `false` otherwise.</returns>
        internal static bool TryGet(Type type, out PropertyDrawer drawer)
        {
            if (!_pool.TryGetValue(type,out drawer))
            {
                var drawerType = GetDrawerType(type);
                if (drawerType != null)
                {
                    drawer = Activator.CreateInstance(drawerType) as PropertyDrawer;
                    if (drawer == null)
                    {
                        UnityEngine.Debug.LogError($"Failed to instantiate PropertyDrawer of type {drawerType}");
                    }
                }
                _pool[type] = drawer;
            }

            return drawer != null;
        }

        /// <summary>
        /// Finds the PropertyDrawer type associated with the provided target type by inspecting CustomPropertyDrawer attributes and honoring the attribute's "use for children" behavior.
        /// </summary>
        /// <param name="type">The target type to locate a corresponding PropertyDrawer for.</param>
        /// <returns>The matching PropertyDrawer <see cref="Type"/> if found; otherwise <c>null</c>.</returns>
        private static Type GetDrawerType(Type type)
        {
            var interfaces = type.GetInterfaces();
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            
            // 属性を持つTypeを取得.
            var caches = TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>();
            foreach (var cache in caches)
            {
                // 
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
                            if (useForChildrenFieldValue is bool and true)
                            {
                                if (Array.Exists(interfaces, x => x == fieldType))
                                {
                                    return cache;
                                }

                                var baseType = type.BaseType;
                                while (baseType != null)
                                {
                                    if (baseType == fieldType)
                                    {
                                        return cache;
                                    }

                                    baseType = baseType.BaseType;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}