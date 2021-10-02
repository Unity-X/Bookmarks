using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityX.Bookmarks
{
#if !UNITY_2021_1_OR_NEWER
    internal class DropdownField : BaseField<int>
    {
        private Button _button;
        private readonly List<string> _choices;

        public DropdownField(string label, List<string> choices, int currentIndex) : base(label, new Button())
        {
            _button = this.Q<Button>();
            _button.clickable.clickedWithEventInfo += OnButtonClicked;
            _button.RegisterCallback<ContextualMenuPopulateEvent>((evt) =>
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    int index = i;
                    evt.menu.AppendAction(choices[i], (x) =>
                    {
                        value = index;
                    });
                }
            });
            
            _choices = choices;
            SetValueWithoutNotify(currentIndex);
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateButtonText();
        }

        private void UpdateButtonText()
        {
            _button.text = index >= 0 && index < _choices.Count ? _choices[index] : "";
        }

        private void OnButtonClicked(EventBase clickEvent)
        {
            _button.panel.contextualMenuManager.DisplayMenu(clickEvent, _button);
        }

        public int index => value;
    }
#endif

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class SubclassSelectorAttribute : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    internal class SubclassSelectorDrawer : PropertyDrawer
    {
        private class DataCache
        {
            public List<string> AvailableTypeNames;
            public string[] AvailableTypeNamesArray;
            public List<Type> AvailableTypes;

            public object GetNewManagedReference(int newIndex)
            {
                var type = AvailableTypes[newIndex];
                return type != null ? Activator.CreateInstance(AvailableTypes[newIndex]) : null;
            }

            public int IndexOfCurrentInstance(SerializedProperty property)
            {
                Type instanceType = UnityTypeNameToSystemType(property.managedReferenceFullTypename);
                return AvailableTypes.IndexOf(instanceType);
            }

            public DataCache(string managedReferenceFieldTypename)
            {
                AvailableTypeNames = new List<string>();
                AvailableTypes = new List<Type>();

                AvailableTypeNames.Add("None");
                AvailableTypes.Add(null);

                Type fieldType = UnityTypeNameToSystemType(managedReferenceFieldTypename);
                foreach (var subType in TypeCache.GetTypesDerivedFrom(fieldType))
                {
                    if (subType.IsAbstract || subType.IsGenericType)
                        continue;

                    if (typeof(UnityEngine.Object).IsAssignableFrom(subType))
                        continue;

                    AvailableTypeNames.Add(subType.Name);
                    AvailableTypes.Add(subType);
                }

                AvailableTypeNamesArray = AvailableTypeNames.ToArray();
            }

            static Type UnityTypeNameToSystemType(string type)
            {
                string[] fieldTypename = type.Split(' ');

                try
                {
                    var assembly = Assembly.Load(fieldTypename[0]);
                    return assembly.GetType(fieldTypename[1]);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The key being the 'managedReferenceFieldTypename'
        /// </summary>
        private static Dictionary<string, DataCache> s_dataCaches = new Dictionary<string, DataCache>();

        private static DataCache GetDataCache(SerializedProperty property)
        {
            if (!s_dataCaches.TryGetValue(property.managedReferenceFieldTypename, out DataCache cache))
            {
                cache = new DataCache(property.managedReferenceFieldTypename);
                s_dataCaches[property.managedReferenceFieldTypename] = cache;
            }

            return cache;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new UIToolkitElement(property, attribute as SubclassSelectorAttribute);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference) return;

            var dataCache = GetDataCache(property);

            int currentIndex = dataCache.IndexOfCurrentInstance(property);
            int newIndex = EditorGUI.Popup(GetPopupPosition(position), currentIndex, dataCache.AvailableTypeNamesArray);
            if (newIndex != currentIndex)
            {
                property.managedReferenceValue = dataCache.GetNewManagedReference(newIndex);
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }

        class UIToolkitElement : BindableElement
        {
            private readonly SerializedProperty _property;
            private readonly DropdownField _typeField;
            private readonly VisualElement _childPropertyContainer;

            public UIToolkitElement(SerializedProperty property, SubclassSelectorAttribute attribute)
            {
                _property = property;
                _childPropertyContainer = new VisualElement() { name = "child-prop-container" };
                _childPropertyContainer.style.paddingLeft = 15f; // same as EditorGUI.cs kIndentPerLevel

                DataCache dataCache = GetDataCache(property);
                int selectedIndex = dataCache.IndexOfCurrentInstance(property);

                _typeField = new DropdownField(_property.displayName, dataCache.AvailableTypeNames, selectedIndex);
                _typeField.RegisterValueChangedCallback((evt) =>
                {
                    _property.managedReferenceValue = dataCache.GetNewManagedReference(_typeField.index);
                    _property.serializedObject.ApplyModifiedProperties();
                    _property.serializedObject.Update();

                    RebuildChildProperty();
                });

                Add(_typeField);
                Add(_childPropertyContainer);

                RebuildChildProperty();
            }

            private void RebuildChildProperty()
            {
                _childPropertyContainer.Clear();

                foreach (var childProperty in new ShallowChildEnumerator(_property.Copy()))
                {
                    _childPropertyContainer.Add(new PropertyField(childProperty));
                }

                // update binding
                _childPropertyContainer.Bind(_property.serializedObject);
            }

            struct ShallowChildEnumerator
            {
                bool _enterChildren;
                string _parentPath;
                public ShallowChildEnumerator(SerializedProperty property)
                {
                    Current = property;
                    _enterChildren = property.hasChildren;
                    _parentPath = property.propertyPath;
                }

                // Enumerator interface
                public ShallowChildEnumerator GetEnumerator() => this;
                public SerializedProperty Current { get; }
                public bool MoveNext()
                {
                    bool result = Current.Next(_enterChildren);

                    _enterChildren = false;

                    return result && Current.propertyPath.Contains(_parentPath);
                }
            }
        }

        private static Rect GetPopupPosition(Rect currentPosition)
        {
            Rect popupPosition = new Rect(currentPosition);
            popupPosition.width -= EditorGUIUtility.labelWidth;
            popupPosition.x += EditorGUIUtility.labelWidth;
            popupPosition.height = EditorGUIUtility.singleLineHeight;
            return popupPosition;
        }
    }
}
