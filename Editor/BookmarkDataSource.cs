using System;
using UnityEditor;

namespace UnityX.Bookmarks
{
    [Serializable]
    public abstract class BookmarkDataSource
    {
        public abstract string MenuName { get; }
        internal event Action ItemsChanged;

        public void NotifyItemsChanged() { ItemsChanged?.Invoke(); }

        public abstract GlobalObjectId[] ProvideItems();
        public virtual void OnAssetsModifiedWhileBookmarksDisplayed(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) { }
    }
}