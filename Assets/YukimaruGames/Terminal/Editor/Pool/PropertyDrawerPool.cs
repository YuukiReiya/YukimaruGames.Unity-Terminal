using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace YukimaruGames.Terminal.Editor
{
    internal static class PropertyDrawerPool
    {
        private static Dictionary<Type, PropertyDrawer> _pool = new();

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

                    if (field?.GetValue(attribute) is Type filedType)
                    {
                        if (filedType == type)
                        {
                            return cache;
                        }
                        
                        var useForChildrenField = attribute.GetType().GetField("m_UseForChildren", bindingFlags);
                        if (useForChildrenField != null)
                        {
                            var useForChildrenFieldValue = useForChildrenField.GetValue(attribute);
                            if (useForChildrenFieldValue is bool and true)
                            {
                                if (Array.Exists(interfaces, x => x == filedType))
                                {
                                    return cache;
                                }

                                var baseType = type.BaseType;
                                while (baseType != null)
                                {
                                    if (baseType == filedType)
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
