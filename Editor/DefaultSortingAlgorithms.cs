namespace UnityX.Bookmarks
{
    public static class DefaultSortingAlgorithms
    {
        [System.Serializable]
        public class Alphabetic : BookmarkSortingAlgorithm
        {
            public override int Compare(BookmarksWindowLocalState.Item x, BookmarksWindowLocalState.Item y)
            {
                return BookmarksWindow.ItemView.GetDisplayName(x).CompareTo(BookmarksWindow.ItemView.GetDisplayName(y));
            }
        }

        [System.Serializable]
        public class TypeThenAlphabetic : BookmarkSortingAlgorithm
        {
            private Alphabetic _alphabetic = new Alphabetic();

            public override int Compare(BookmarksWindowLocalState.Item x, BookmarksWindowLocalState.Item y)
            {
                string typeX = getSortableItemType(x);
                string typeY = getSortableItemType(y);

                int result = string.Compare(typeX, typeY);

                if (result != 0)
                    return result;

                return _alphabetic.Compare(x, y);

                string getSortableItemType(BookmarksWindowLocalState.Item item)
                {
                    switch (item.Type)
                    {
                        default:
                        case BookmarksWindowLocalState.Item.ObjectType.Null:
                            return "0";

                        case BookmarksWindowLocalState.Item.ObjectType.SourceAsset:
                        case BookmarksWindowLocalState.Item.ObjectType.ImportedAsset:
                            return item.CachedObjectReference != null ? $"1_{item.CachedObjectReference.GetType()}" : "0";

                        case BookmarksWindowLocalState.Item.ObjectType.SceneObject:
                            return "2";
                    }
                }
            }
        }
    }
}
