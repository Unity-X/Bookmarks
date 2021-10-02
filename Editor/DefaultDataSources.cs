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

            public override GlobalObjectId[] ProvideItems()
            {
                string[] guids = AssetDatabase.FindAssets("", new string[] { _folder });

                List<UnityEngine.Object> objects = new List<UnityEngine.Object>(guids.Length);

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    if(string.IsNullOrEmpty(Path.GetExtension(path)))
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
    }
}