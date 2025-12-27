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
        /// <summary>
        /// Resolves the System.Type represented by the property's managedReferenceFullTypename.
        /// </summary>
        /// <param name="property">The SerializedProperty whose managedReferenceFullTypename will be resolved.</param>
        /// <returns>The Type represented by managedReferenceFullTypename, or null if the typename is null, empty, or the type cannot be found.</returns>
        internal static Type GetTypeByManagedReferenceFullTypename(this SerializedProperty property)
        {
            return GetType(property.managedReferenceFullTypename);
        }

        /// <summary>
        /// Resolves the Type referenced by the property's managedReferenceFieldTypename.
        /// </summary>
        /// <returns>The resolved Type, or null if the typename is null, empty, or cannot be resolved.</returns>
        internal static Type GetTypeByManagedReferenceFieldTypename(this SerializedProperty property)
        {
            return GetType(property.managedReferenceFieldTypename);
        }
        
        /// <summary>
        /// Resolves a <see cref="Type"/> from a descriptor string that contains an assembly name and a fully-qualified type name separated by a space.
        /// </summary>
        /// <param name="typeName">A string formatted as "<assemblyName> <fullTypeName>" (assembly name, a single space, then the fully-qualified type name).</param>
        /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if <paramref name="typeName"/> is null, empty, or the type cannot be resolved.</returns>
        private static Type GetType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var index = typeName.IndexOf(' ');
            var assembly = Assembly.Load(typeName.Substring(0, index));
            return assembly.GetType(typeName.Substring(index + 1));
        }

        /// <summary>
        /// Enumerates child SerializedProperty entries of the given property up to the specified descendant depth.
        /// </summary>
        /// <param name="depth">Maximum number of descendant levels to include; 1 includes only direct children.</param>
        /// <returns>An enumerable of copies of child SerializedProperty objects contained within the specified depth.</returns>
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

        /// <summary>
        /// Assigns or creates the managed reference value for the given SerializedProperty using the specified type.
        /// </summary>
        /// <param name="property">The SerializedProperty whose <c>managedReferenceValue</c> will be set.</param>
        /// <param name="type">The target type for the managed reference. If <c>null</c>, the managed reference is cleared.</param>
        /// <returns>The object assigned to <c>property.managedReferenceValue</c>, or <c>null</c> if none was created.</returns>
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
            }

            property.managedReferenceValue = result;
            return result;
        }
    }
}