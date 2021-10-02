using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityX.Bookmarks
{
    public partial class BookmarksWindow
    {
        private class CellGroupView : VisualElement, ILayoutWeighted
        {
            private readonly BookmarksWindowLocalState.CellGroup _cellGroupData;
            private readonly Resources _resources;
            private readonly List<CellView> _cellViews = new List<CellView>();

            public List<CellView> CellViews => _cellViews;

            float ILayoutWeighted.LayoutWeight { get => _cellGroupData.LayoutWeight; set => _cellGroupData.LayoutWeight = value; }

            public CellGroupView(BookmarksWindowLocalState.CellGroup cellGroupData, Resources resources)
            {
                _cellGroupData = cellGroupData;
                _resources = resources;
                style.flexGrow = 1;

                var scrollView = new ScrollView(ScrollViewMode.Vertical);
                scrollView.style.flexGrow = 1;

                var addCellButton = CreateAddButton();
                addCellButton.clicked += AddCellToData;

                Add(scrollView);
                Add(addCellButton);

                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;

                foreach (var item in cellGroupData.Cells)
                {
                    var cellView = new CellView(item, resources);
                    _cellViews.Add(cellView);
                    scrollView.Add(cellView);
                }
            }

            private void AddCellToData()
            {
                BookmarksWindowLocalState.instance.BeginUndoableChange();
                _cellGroupData.Cells.Add(BookmarksWindowLocalState.Cell.CreateDefault());
                BookmarksWindowLocalState.instance.EndUndoableChange();

                _resources.Window.ReloadWindow();
            }
        }
    }
}