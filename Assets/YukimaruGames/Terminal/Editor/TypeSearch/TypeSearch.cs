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

        [InitializeOnLoadMethod]
        private static void Clear()
        {
            // NOTE:
            // ドメインリロードされてもキャッシュが残ってしまうので明示的にクリアする.
            _typeCacheDic?.Clear();
        }
        
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
        private static IEnumerable<Type> GetSubtypesFast(Type baseType)
        {
            return TypeCache.GetTypesDerivedFrom(baseType)
                .Append(baseType)
                .Where(IsSupportedSerializableType);
        }

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
