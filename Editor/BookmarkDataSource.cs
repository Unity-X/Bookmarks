using System;
using UnityEditor;

namespace UnityX.Bookmarks
{
    [Serializable]
    public abstract class BookmarkDataSource : Enableable
    {
        public abstract string MenuName { get; }
        public abstract bool ProvidedItemsAreAlreadySorted { get; }
        internal event Action ItemsChanged;

        public void NotifyItemsChanged() { ItemsChanged?.Invoke(); }

        public abstract GlobalObjectId[] ProvideItems();
        public virtual void OnAssetsModifiedWhileBookmarksDisplayed(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) { }
    }
}