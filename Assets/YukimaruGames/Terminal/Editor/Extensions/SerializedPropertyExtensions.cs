#if UNITY_2021_3_OR_NEWER
#define SUPPORTS_MANAGED_REFERENCE_VALUE 
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;


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
                ResetTypeMismatchedFields(result, type);
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

        /// <summary>
        /// 型切り替え時の値引き継ぎによって<see cref="ResetOnTypeMismatchAttribute"/>付きの
        /// フィールドの型が食い違った場合に、新しい型の既定値へ差し替える.
        /// </summary>
        /// <remarks>
        /// <see cref="SetManagedReferenceValue"/>はJSON経由で切り替え前の値を引き継ぐが、
        /// 「所有者の型ごとに既定の実装型が異なるフィールド」では古い型のまま引き継がれてしまう。
        /// <para>
        /// <b>型が一致する場合は何もしない。</b>引き継ぎ自体は共通フィールドの設定を維持するための
        /// 意図的な挙動であり、既定型が同じ所有者どうしの切り替えでは従来の値を保つ必要があるため
        /// (属性が付いていないフィールドはそもそも判定対象にならず、一切影響を受けない).
        /// </para>
        /// </remarks>
        private static void ResetTypeMismatchedFields(object target, Type type)
        {
            if (target == null)
            {
                return;
            }

            // 既定値の取得にはパラメータレスコンストラクタが要る。持たない型は
            // そもそもSetManagedReferenceValueが生成できないため、ここでは静かに諦める.
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                return;
            }

            object defaults = null;

            for (var current = type; current != null; current = current.BaseType)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                foreach (var field in current.GetFields(flags))
                {
                    if (field.GetCustomAttribute<ResetOnTypeMismatchAttribute>() == null)
                    {
                        continue;
                    }

                    // 既定値は属性付きフィールドが1つでもあったときだけ生成する
                    // (対象が無いのに毎回インスタンスを作らないため).
                    defaults ??= Activator.CreateInstance(type);

                    var expected = field.GetValue(defaults);
                    if (expected == null)
                    {
                        continue;
                    }

                    var inherited = field.GetValue(target);
                    if (inherited != null && inherited.GetType() == expected.GetType())
                    {
                        continue;
                    }

                    field.SetValue(target, expected);
                }
            }
        }
    }
}
