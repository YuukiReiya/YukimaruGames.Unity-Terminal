using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Editor
{
    [CustomPropertyDrawer(typeof(SerializeInterfaceAttribute))]
    public sealed class SerializeInterfaceDrawer : PropertyDrawer
    {
        private bool _initialized;
        private Type[] _inheritedTypes;
        private string[] _popupNames;
        private string[] _fullNames;
        private int _typeIndex;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return;
            }

            if (!_initialized)
            {
                Initialize(property);
                _initialized = true;
            }
            GetCurrentTypeIndex(property.managedReferenceFullTypename);
            var selectedIndex = EditorGUI.Popup(GetPopupPosition(position), _typeIndex, _popupNames);
            UpdatePropertyToSelectedTypeIndex(property, selectedIndex);
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }

        private void Initialize(SerializedProperty property)
        {
            var attr = attribute as SerializeInterfaceAttribute;
            GetAllInheritedTypes(GetFieldType(property), attr!.IsIncludeMono);
            GetInheritedTypeNameArrays();
        }

        private void GetCurrentTypeIndex(string typeFullName)
        {
            _typeIndex = Array.IndexOf(_fullNames, typeFullName);
        }

        private void GetAllInheritedTypes(Type baseType, bool includeMono)
        {
            var monoType = typeof(MonoBehaviour);
            _inheritedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => baseType.IsAssignableFrom(t) && t.IsClass && (includeMono || !monoType.IsAssignableFrom(t)))
                .Prepend(null)
                .ToArray();
        }
        
        private void GetInheritedTypeNameArrays()
        {
            _popupNames = _inheritedTypes.Select(type => type == null ? "<null>" : type.ToString()).ToArray();
            _fullNames = _inheritedTypes.Select(type => type == null ? "" : $"{type.Assembly.ToString().Split(',')[0]} {type.FullName}").ToArray();
        }

        private static Type GetFieldType(SerializedProperty property)
        {
            var filedTypeNames = property.managedReferenceFieldTypename.Split(' ');
            var assembly = Assembly.Load(filedTypeNames[0]);
            return assembly.GetType(filedTypeNames[1]);
        }

        private void UpdatePropertyToSelectedTypeIndex(SerializedProperty property, int selectedTypeIndex)
        {
            if (_typeIndex == selectedTypeIndex)
            {
                return;
            }

            _typeIndex = selectedTypeIndex;
            var selectedType = _inheritedTypes[selectedTypeIndex];
            property.managedReferenceValue =
                selectedType == null ? null : Activator.CreateInstance(selectedType);
        }

        private Rect GetPopupPosition(Rect position)
        {
            position.width -= EditorGUIUtility.labelWidth;
            position.x += EditorGUIUtility.labelWidth;
            position.height = EditorGUIUtility.singleLineHeight;
            return position;
        }
    }
}
