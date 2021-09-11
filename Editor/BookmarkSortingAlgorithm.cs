using System.Collections.Generic;

namespace UnityX.Bookmarks
{
    public abstract class BookmarkSortingAlgorithm : IComparer<BookmarksWindowLocalState.Item>
    {
        public abstract string MenuName { get; }
        public abstract int Compare(BookmarksWindowLocalState.Item x, BookmarksWindowLocalState.Item y);
    }
}
