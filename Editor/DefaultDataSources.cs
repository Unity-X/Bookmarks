using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityX.Bookmarks
{
    public static class DefaultDataSources
    {
        [Serializable]
        public class FolderView : BookmarkDataSource
        {
            [SerializeField] private string _folder = "Assets/Scenes";
            [SerializeField] private bool _includeSubfolders = true;

            public override string MenuName => "Folder View";
            public override bool ProvidedItemsAreAlreadySorted => false;

            public override GlobalObjectId[] ProvideItems()
            {
                string[] guids = AssetDatabase.FindAssets("", new string[] { _folder });

                List<UnityEngine.Object> objects = new List<UnityEngine.Object>(guids.Length);

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    if (string.IsNullOrEmpty(Path.GetExtension(path)))
                        continue;

                    if (!_includeSubfolders && path.IndexOf('/', _folder.Length + 1) != -1)
                        continue;

                    var obj = AssetDatabase.LoadMainAssetAtPath(path);
                    if (obj != null)
                        objects.Add(obj);
                }

                GlobalObjectId[] result = new GlobalObjectId[objects.Count];
                GlobalObjectId.GetGlobalObjectIdsSlow(objects.ToArray(), result);
                return result;
            }

            public override void OnAssetsModifiedWhileBookmarksDisplayed(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                base.OnAssetsModifiedWhileBookmarksDisplayed(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);

                if (AffectedByChanges(importedAssets) ||
                    AffectedByChanges(deletedAssets) ||
                    AffectedByChanges(movedAssets) ||
                    AffectedByChanges(movedFromAssetPaths))
                {
                    NotifyItemsChanged();
                }

                bool AffectedByChanges(string[] paths)
                {
                    foreach (var item in paths)
                    {
                        if (item.StartsWith(_folder))
                            return true;
                    }
                    return false;
                }
            }

        }


        [Serializable]
        public class RecentSelections : BookmarkDataSource
        {
            [SerializeField] private int _capacity = 10;
            [SerializeField, HideInInspector] private List<string> _savedSelections = new List<string>();

            private List<GlobalObjectId> _selections = new List<GlobalObjectId>();

            public override string MenuName => "Recent Selections";
            public override bool ProvidedItemsAreAlreadySorted => true;

            protected override void OnEnable()
            {
                Selection.selectionChanged += OnSelectionChanged;

                _selections.Clear();
                foreach (var item in _savedSelections)
                {
                    if (GlobalObjectId.TryParse(item, out GlobalObjectId id))
                        _selections.Add(id);
                }
            }

            protected override void OnDisable()
            {
                Selection.selectionChanged -= OnSelectionChanged;
            }

            private void OnSelectionChanged()
            {
                int[] selectedInstanceIds = Selection.instanceIDs;
                if (selectedInstanceIds.Length < 0)
                    return;
                GlobalObjectId[] selectedGlobalIds = new GlobalObjectId[selectedInstanceIds.Length];
                GlobalObjectId.GetGlobalObjectIdsSlow(selectedInstanceIds, selectedGlobalIds);

                foreach (var globalId in selectedGlobalIds)
                {
                    if (globalId.assetGUID.Empty())
                        continue;

                    int index = _selections.IndexOf(globalId);
                    if (index != -1)
                    {
                        _selections.Move(index, 0);
                    }
                    else
                    {
                        _selections.Insert(0, globalId);
                    }
                }

                if (_selections.Count > _capacity)
                {
                    _selections.RemoveRange(_capacity, _selections.Count - _capacity);
                }

                _savedSelections.Clear();
                foreach (var item in _selections)
                {
                    _savedSelections.Add(item.ToString());
                }

                NotifyItemsChanged();
            }

            public override GlobalObjectId[] ProvideItems()
            {
                return _selections.ToArray();
            }
        }
    }
}