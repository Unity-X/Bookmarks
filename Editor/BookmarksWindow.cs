using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;


namespace UnityX.Bookmarks
{
    public partial class BookmarksWindow : EditorWindow
    {
        internal class Resources
        {
            public VisualTreeAsset CellAsset;
            public VisualTreeAsset ItemAsset;
            public BookmarksWindow Window;
            public ItemView DraggedItem;
        }

        private class AssetPostProcessor : UnityEditor.AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (s_activeInstance == null)
                    return;

                foreach (var cellGroupView in s_activeInstance._cellGroupViews)
                {
                    foreach (var cell in cellGroupView.CellViews)
                    {
                        cell.OnAssetsChanged(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
                    }
                }
            }
        }

        private static readonly string s_bookmarksCellUXML = "5df08b9646f23814a91bc4ac78ca6b4c";
        private static readonly string s_bookmarksItemUXML = "14f7108de5c51c943836dc0d9827373a";
        private static readonly string s_bookmarksWindowUXML = "47003525bf18bb841b272af5cc4acd01";

        private Resources _resources = new Resources();
        private List<CellGroupView> _cellGroupViews = new List<CellGroupView>();

        private static BookmarksWindow s_activeInstance;

        [MenuItem("Window/Bookmarks")]
        public static void ShowExample()
        {
            BookmarksWindow wnd = GetWindow<BookmarksWindow>();
            wnd.titleContent = new GUIContent("Bookmarks");
        }

        public void OnEnable()
        {
            s_activeInstance = this;
            _resources.Window = this;
            ReloadWindow();

            rootVisualElement.RegisterCallback<DragExitedEvent>(OnDragExit);
        }

        private void OnDisable()
        {
            if (s_activeInstance == this)
                s_activeInstance = null;
        }

        private void OnGUI()
        {
            // This is a hack to remove the drag 'ghost' visuals we put on the dragged item. I could not find a proper way
            // to do this with normal events because 'dropping' outside of the window fires no event. So there's apparently no simple and global way
            // to know when the drag and drop completes.
            if (mouseOverWindow != this || DragAndDrop.visualMode == DragAndDropVisualMode.None)
                ClearDragVisuals();
        }

        private void OnDragExit(DragExitedEvent evt)
        {
            ClearDragVisuals();
        }

        private void ClearDragVisuals()
        {
            _resources.DraggedItem?.ShowDraggedVisuals(false);
            _resources.DraggedItem = null;
        }

        private void ReloadWindow()
        {
            BookmarkGroupInspectorWindow.Hide();

            VisualElement root = rootVisualElement;

            root.Clear();
            _cellGroupViews.Clear();

            _resources.CellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(s_bookmarksCellUXML));
            _resources.ItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(s_bookmarksItemUXML));

            // Import Assets
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(s_bookmarksWindowUXML));
            var treeRoot = visualTree.Instantiate();
            treeRoot.style.flexGrow = 1;
            root.Add(treeRoot);

            var groupGroup = new AdjustableGroupLayout(AdjustableGroupLayout.Orientation.Horizontal);
            groupGroup.style.flexGrow = 1;

            foreach (BookmarksWindowLocalState.CellGroup cellGroupData in BookmarksWindowLocalState.instance.CellGroups)
            {
                var cellGroupView = new CellGroupView(cellGroupData, _resources);
                _cellGroupViews.Add(cellGroupView);
                groupGroup.Add(cellGroupView);
            }

            root.Q("group-container").Add(groupGroup);
            root.Q<Button>("reloadButton").clicked += ReloadWindow;
            root.Q<Button>("add-group-button").clicked += AddGroupToData;
        }

        private void AddGroupToData()
        {
            BookmarksWindowLocalState.instance.BeginUndoableChange();
            BookmarksWindowLocalState.instance.CellGroups.Add(BookmarksWindowLocalState.CellGroup.CreateDefault());
            BookmarksWindowLocalState.instance.EndUndoableChange();
            ReloadWindow();
        }

        private static Button CreateAddButton()
        {
            Button element = new Button();
            element.text = "+";
            element.AddToClassList("add-button");
            return element;
        }

        private interface ILayoutWeighted
        {
            float LayoutWeight { get; set; }
        }
    }
}