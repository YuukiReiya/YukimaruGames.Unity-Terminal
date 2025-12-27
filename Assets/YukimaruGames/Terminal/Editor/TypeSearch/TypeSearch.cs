#if UNITY_2023_2_OR_NEWER
#define SUPPORTS_SERIALIZED_GENERIC_INSTANCE
#endif
using System;
using System.Collections.Generic;
using System.Linq;

#if SUPPORTS_SERIALIZED_GENERIC_INSTANCE
using System.Reflection;
#endif

using UnityEditor;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Editor
{
    internal static class TypeSearch
    {
#if SUPPORTS_SERIALIZED_GENERIC_INSTANCE
        private static readonly Dictionary<Type, IList<Type>> _typeCacheDic = new();
#endif

        /// <summary>
        /// Finds available reference types that are compatible with the provided base type.
        /// </summary>
        /// <param name="baseType">The base type to find compatible reference types for.</param>
        /// <returns>
        /// A sequence of types that can be used as references for <paramref name="baseType"/>:
        /// for generic base types, compatible closed generic implementations; otherwise, derived non-generic types (including the base type itself).
        /// </returns>
        internal static IEnumerable<Type> GetAvailableReferenceTypes(Type baseType)
        {
#if SUPPORTS_SERIALIZED_GENERIC_INSTANCE
            if (baseType.IsGenericType)
            {
                return GetCompatibleGenericTypes(baseType);
            }
#endif
            return GetSubtypesFast(baseType);
        }
        
#if SUPPORTS_SERIALIZED_GENERIC_INSTANCE

        /// <summary>
        /// Clears the internal generic-type compatibility cache when the editor domain is loaded.
        /// </summary>
        /// <remarks>
        /// Invoked at editor load to remove cached entries stored in <c>_typeCacheDic</c> so stale type information does not persist across domain reloads.
        /// </remarks>
        [InitializeOnLoadMethod]
        private static void Clear()
        {
            // NOTE:
            // ドメインリロードされてもキャッシュが残ってしまうので明示的にクリアする.
            _typeCacheDic?.Clear();
        }
        
        /// <summary>
        /// Retrieves concrete types that are compatible with the specified generic base type, using an internal cache to avoid repeated scans.
        /// </summary>
        /// <param name="baseType">The generic base type (or generic type definition) for which to find compatible concrete types.</param>
        /// <returns>A list of concrete types compatible with <paramref name="baseType"/>; empty if none are found.</returns>
        private static IList<Type> GetCompatibleGenericTypes(Type baseType)
        {
            if (_typeCacheDic.TryGetValue(baseType, out var types))
            {
                return types;
            }

            var scannedTypes = ScanCompatibleGenericTypeList(baseType);
            _typeCacheDic[baseType] = scannedTypes;
            return scannedTypes;
        }

        /// <summary>
        /// Finds types in loaded assemblies that implement the specified generic type definition and whose generic arguments are compatible with the provided base type's generic arguments.
        /// </summary>
        /// <param name="baseType">The (possibly constructed) generic type used to determine the target generic type definition and its desired type arguments.</param>
        /// <returns>A list of concrete, supported serializable types that implement the target generic interface with type arguments compatible with <paramref name="baseType"/>.</returns>
        private static IList<Type> ScanCompatibleGenericTypeList(Type baseType)
        {
            var results = new List<Type>();
            
            var genericTypeDefinition = baseType;
            var targetTypeArguments = Type.EmptyTypes;
            var genericTypeParameters = Type.EmptyTypes;

            if (baseType.IsGenericType)
            {
                genericTypeDefinition = baseType.GetGenericTypeDefinition();
                targetTypeArguments = baseType.GetGenericArguments();
                genericTypeParameters = genericTypeDefinition.GetGenericArguments();
            }

            var eType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(IsSupportedSerializableType);

            foreach (var type in eType)
            {
                var interfaces = type.GetInterfaces();
                foreach (var interfaceType in interfaces)
                {
                    if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != genericTypeDefinition)
                    {
                        continue;
                    }

                    var sourceTypeArguments = interfaceType.GetGenericArguments();
                    var isAllArgumentsCompatible = true;

                    for (var i = 0; i < genericTypeParameters.Length; i++)
                    {
                        var variance = genericTypeParameters[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask;

                        var sourceTypeArgument = sourceTypeArguments[i];
                        var targetTypeArgument = targetTypeArguments[i];

                        isAllArgumentsCompatible = IsArgumentCompatible(sourceTypeArgument, targetTypeArgument, variance);
                        if (!isAllArgumentsCompatible)
                        {
                            break;
                        }
                    }

                    if (isAllArgumentsCompatible)
                    {
                        results.Add(type);
                        break;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Determines whether a source generic type argument is compatible with a target generic type argument for the specified variance.
        /// </summary>
        /// <param name="source">The source type argument.</param>
        /// <param name="target">The target type argument to compare against.</param>
        /// <param name="variance">The generic parameter variance (Invariant, Covariant, or Contravariant) to apply to the compatibility check.</param>
        /// <returns>`true` if the arguments are compatible under the given variance, `false` otherwise.</returns>
        private static bool IsArgumentCompatible(Type source, Type target, GenericParameterAttributes variance)
        {
            return variance switch
            {
                GenericParameterAttributes.Contravariant => source.IsAssignableFrom(target),
                GenericParameterAttributes.Covariant => target.IsAssignableFrom(source),
                _ => source == target,
            };
        }
#endif
        /// <summary>
        /// Enumerates types derived from the specified base type, including the base type, filtered to supported serializable types.
        /// </summary>
        /// <returns>An enumerable of types that are the base type or derive from it and satisfy the supported-serializable predicate.</returns>
        private static IEnumerable<Type> GetSubtypesFast(Type baseType)
        {
            return TypeCache.GetTypesDerivedFrom(baseType)
                .Append(baseType)
                .Where(IsSupportedSerializableType);
        }

        /// <summary>
        /// Determines whether a type is eligible to appear as a serializable, concrete, non-Unity selectable type in type menus.
        /// </summary>
        /// <returns>`true` if the type is marked with <see cref="SerializableAttribute"/>, is not assignable from <c>UnityEngine.Object</c>, is not marked with <see cref="HideInTypeMenuAttribute"/>, is not generic, is not abstract, and is public or a nested type; `false` otherwise.</returns>
        private static bool IsSupportedSerializableType(Type type)
        {
            return
                !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
                Attribute.IsDefined(type, typeof(SerializableAttribute)) &&
                !Attribute.IsDefined(type,typeof(HideInTypeMenuAttribute)) &&
                !type.IsGenericType &&
                !type.IsAbstract &&
                (type.IsPublic || type.IsNestedPublic || type.IsNestedPrivate);

        }
    }
}