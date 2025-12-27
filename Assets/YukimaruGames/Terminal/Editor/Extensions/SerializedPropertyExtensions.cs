#if UNITY_2021_3_OR_NEWER
#define SUPPORTS_MANAGED_REFERENCE_VALUE 
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace YukimaruGames.Terminal.Editor
{
    internal static class SerializedPropertyExtensions
    {
        internal static Type GetTypeByManagedReferenceFullTypename(this SerializedProperty property)
        {
            return GetType(property.managedReferenceFullTypename);
        }

        internal static Type GetTypeByManagedReferenceFieldTypename(this SerializedProperty property)
        {
            return GetType(property.managedReferenceFieldTypename);
        }
        
        private static Type GetType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var index = typeName.IndexOf(' ');

            if (index < 0 || typeName.Length - 1 < index)
            {
                return null;
            }

            var assemblyName = string.Empty;
            var className = string.Empty;
            try
            {
                assemblyName = typeName.Substring(0, index);
                className = typeName.Substring(index + 1);
                
                var assembly = Assembly.Load(assemblyName);
                return assembly?.GetType(className);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SerializedPropertyExtensions)}] Failed to load type: {typeName}.{Environment.NewLine}AssemblyName: \"{assemblyName}\", ClassName: \"{className}\".{Environment.NewLine}Exception: {e}");
                return null;
            }
        }

        internal static IEnumerable<SerializedProperty> GetChildProperties(this SerializedProperty self, int depth = 1)
        {
            var parent = self.Copy();
            var parentDepth = parent.depth;
            var e = parent.GetEnumerator();

            try
            {
                while (e.MoveNext())
                {
                    if (e.Current is not SerializedProperty childProp)
                    {
                        continue;
                    }

                    if ((parentDepth + depth) < childProp.depth)
                    {
                        continue;
                    }

                    yield return childProp.Copy();
                }
            }
            finally
            {
                if (e is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        internal static object SetManagedReferenceValue(this SerializedProperty property, Type type)
        {
            object result = null;
            
#if SUPPORTS_MANAGED_REFERENCE_VALUE
            if (type != null && property.managedReferenceValue != null)
            {
                var json = JsonUtility.ToJson(property.managedReferenceValue);
                result = JsonUtility.FromJson(json, type);
            }
#endif

            if (result == null && type!=null)
            {
                var constructor = type.GetConstructor(Type.EmptyTypes);

                if (constructor != null)
                {
                    result = Activator.CreateInstance(type);
                }
                else
                {
                    Debug.LogError($"[{nameof(SerializedPropertyExtensions)}] Failed to create instance: {type.FullName}.The type must have a parameterless constructor (public or non-public).{Environment.NewLine}The property \"{property.displayName}\" has been set to null.");
                }
            }

            property.managedReferenceValue = result;
            return result;
        }
    }
}
