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
        private BookmarksWindowLocalState _editedObject;
        private BookmarksWindow.CellView _cellView;
        private bool _closeInNextUpdate = false;
        private string _cellPath;
        private SerializedObject _serializedObject;
        private ScrollView _container;
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
            if (_closeInNextUpdate)
                Close();
        }

        private void Init(BookmarksWindow.CellView cellView)
        {
            _editedObject = BookmarksWindowLocalState.instance;

            _cellView = cellView;
            _cellPath = GetCellPropertyPath(cellView);
            _serializedObject = new SerializedObject(_editedObject);

            rootVisualElement.Clear();
            _container = new ScrollView(ScrollViewMode.Vertical);
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
            RegisterProperty("custom-color", nameof(BookmarksWindowLocalState.Cell.UseCustomColor), out _, out _useCustomColorProperty);
            RegisterProperty("custom-color-value", nameof(BookmarksWindowLocalState.Cell.Color), out _customColorView, out SerializedProperty _);

            _container.Bind(_serializedObject);
            _container.RegisterCallback<SerializedPropertyChangeEvent>((evt) => OnPropertyChange());
            OnPropertyChange();

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

        private void OnPropertyChange()
        {
            _customColorView.style.display = new StyleEnum<DisplayStyle>(_useCustomColorProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None);
            _sortingAlgoView.style.display = new StyleEnum<DisplayStyle>(_cellView.CellData.DataSource?.ProvidedItemsAreAlreadySorted != true ? DisplayStyle.Flex : DisplayStyle.None);
            _cellView.OnSettingsModified();
        }

        private string GetCellPropertyPath(BookmarksWindow.CellView cellView)
        {
            var cellData = cellView.CellData;
            var groupIndex = _editedObject.CellGroups.FindIndex((g) => g.Cells.Contains(cellData));
            if (groupIndex == -1)
            {
                return string.Empty;
            }

            var cellIndex = _editedObject.CellGroups[groupIndex].Cells.IndexOf(cellData);
            return $"{nameof(BookmarksWindowLocalState.CellGroups)}.Array.data[{groupIndex}].{nameof(BookmarksWindowLocalState.CellGroup.Cells)}.Array.data[{cellIndex}]";
        }
    }
}