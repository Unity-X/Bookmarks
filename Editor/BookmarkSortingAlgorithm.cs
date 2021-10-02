using System.Collections.Generic;

namespace UnityX.Bookmarks
{
    [System.Serializable]
    public abstract class BookmarkSortingAlgorithm : Enableable, IComparer<BookmarksWindowLocalState.Item>
    {
        public virtual string SortMenuDisplayName => GetType().Name;
        public virtual bool DisplayInSortMenu => true;
        public abstract int Compare(BookmarksWindowLocalState.Item x, BookmarksWindowLocalState.Item y);
    }
}
