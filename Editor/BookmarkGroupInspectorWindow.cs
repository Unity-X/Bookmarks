using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using System;

namespace UnityX.Bookmarks
{
    internal class BookmarkGroupInspectorWindow : EditorWindow
    {
        internal static void Show(BookmarksWindow.CellView cellView, Rect instigatorWindowRect)
        {
            var window = GetWindow<BookmarkGroupInspectorWindow>(true, "Group Settings", true);

            //Vector2 size = new Vector2(350, 200);
            //Vector2 pos = instigatorWindowRect.min + Vector2.right * (instigatorWindowRect.width - 100f);

            //window.position = new Rect(pos, size);
            window.Init(cellView);
            window.Show();
        }

        internal static void Hide()
        {
            if (s_openedWindow != null)
                s_openedWindow._closeInNextUpdate = true;
        }

        private static readonly string s_windowUXML = "d7f89dbb03f79274394619184b723bc3";
        private static BookmarkGroupInspectorWindow s_openedWindow = null;
        private BookmarksWindow.CellView _cellView;
        private bool _closeInNextUpdate = false;
        private string _cellPath;
        private SerializedObject _serializedObject;
        private PropertyContainer _container;
        private SerializedProperty _useCustomColorProperty;
        private PropertyField _customColorView;
        private PropertyField _sortingAlgoView;

        private void OnEnable()
        {
            s_openedWindow = this;
        }

        private void OnDisable()
        {
            s_openedWindow = null;
        }

        private void OnGUI()
        {
            if (_closeInNextUpdate || focusedWindow != this)
                Close();
        }

        public class PropertyContainer : ScrollView
        {
            public Action PropertyChanged;

            public PropertyContainer(ScrollViewMode scrollViewMode) : base(scrollViewMode)
            {
            }

            public override void HandleEvent(EventBase evt)
            {
                base.HandleEvent(evt);

                if (evt is SerializedPropertyChangeEvent)
                    PropertyChanged?.Invoke();
            }
        }

        private void Init(BookmarksWindow.CellView cellView)
        {
            _cellView = cellView;
            _cellPath = GetCellPropertyPath(cellView);
            _serializedObject = new SerializedObject(BookmarksWindowLocalState.instance);

            rootVisualElement.Clear();
            _container = new PropertyContainer(ScrollViewMode.Vertical);
            _container.style.flexGrow = 1;
            _container.contentContainer.style.flexGrow = 1;
            rootVisualElement.Add(_container);
            rootVisualElement.style.flexGrow = 1;

            // Import Assets
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(s_windowUXML));
            var treeRoot = visualTree.Instantiate();
            visualTree.CloneTree(_container.contentContainer);
            //container.Add(treeRoot);

            RegisterProperty("name", nameof(BookmarksWindowLocalState.Cell.Name));
            RegisterProperty("data-source", nameof(BookmarksWindowLocalState.Cell.DataSource));
            RegisterProperty("sort-algo", nameof(BookmarksWindowLocalState.Cell.SortingAlgorithm), out _sortingAlgoView, out _);
            RegisterProperty("remove-missing-refs", nameof(BookmarksWindowLocalState.Cell.RemoveMissingReferences));
            RegisterProperty("custom-color", nameof(BookmarksWindowLocalState.Cell.UseCustomColor), out _, out _useCustomColorProperty);
            RegisterProperty("custom-color-value", nameof(BookmarksWindowLocalState.Cell.Color), out _customColorView, out SerializedProperty _);

            _container.Bind(_serializedObject);
            _container.RegisterCallback<SerializedPropertyChangeEvent>(OnPropertyChange);
            OnPropertyChange(null);

            foreach (var item in _container.Children())
            {
                item.style.flexShrink = 0f;
            }
        }

        private void RegisterProperty(string viewName, string propName, out PropertyField view, out SerializedProperty property)
        {
            view = _container.Q<PropertyField>(viewName);
            string path = $"{_cellPath}.{propName}";
            view.bindingPath = path;
            property = _serializedObject.FindProperty(path);
        }

        private void RegisterProperty(string viewName, string propName) => RegisterProperty(viewName, propName, out _, out _);

        private void OnPropertyChange(SerializedPropertyChangeEvent evt)
        {
            _customColorView.style.display = new StyleEnum<DisplayStyle>(_useCustomColorProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None);
            _sortingAlgoView.style.display = new StyleEnum<DisplayStyle>(_cellView.CellData.DataSource?.ProvidedItemsAreAlreadySorted != true ? DisplayStyle.Flex : DisplayStyle.None);
            _cellView.OnSettingsModified();
        }

        private string GetCellPropertyPath(BookmarksWindow.CellView cellView)
        {
            var cellData = cellView.CellData;
            var groupIndex = BookmarksWindowLocalState.instance.CellGroups.FindIndex((g) => g.Cells.Contains(cellData));
            if (groupIndex == -1)
            {
                return string.Empty;
            }

            var cellIndex = BookmarksWindowLocalState.instance.CellGroups[groupIndex].Cells.IndexOf(cellData);
            return $"{nameof(BookmarksWindowLocalState.CellGroups)}.Array.data[{groupIndex}].{nameof(BookmarksWindowLocalState.CellGroup.Cells)}.Array.data[{cellIndex}]";
        }
    }
}