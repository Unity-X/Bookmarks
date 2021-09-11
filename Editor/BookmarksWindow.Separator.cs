using UnityEngine.UIElements;
namespace UnityX.Bookmarks
{
    public partial class BookmarksWindow
    {
        public class Separator : VisualElement
        {
            private VisualElement _dragLine;

            static readonly string s_handleDragLineClassName = "unity-two-pane-split-view__dragline";
            static readonly string s_handleDragLineVerticalClassName = s_handleDragLineClassName + "--vertical";
            static readonly string s_handleDragLineHorizontalClassName = s_handleDragLineClassName + "--horizontal";
            static readonly string s_handleDragLineAnchorClassName = "unity-two-pane-split-view__dragline-anchor";
            static readonly string s_handleDragLineAnchorVerticalClassName = s_handleDragLineAnchorClassName + "--vertical";
            static readonly string s_handleDragLineAnchorHorizontalClassName = s_handleDragLineAnchorClassName + "--horizontal";

            public Separator(bool horizontalDrag)
            {
                // Create drag anchor line.
                name = "unity-dragline-anchor";
                AddToClassList(s_handleDragLineAnchorClassName);

                // Create drag
                _dragLine = new VisualElement();
                _dragLine.name = "unity-dragline";
                _dragLine.AddToClassList(s_handleDragLineClassName);
                Add(_dragLine);

                _dragLine.AddToClassList(horizontalDrag ? s_handleDragLineHorizontalClassName : s_handleDragLineVerticalClassName);
                AddToClassList(horizontalDrag ? s_handleDragLineAnchorHorizontalClassName : s_handleDragLineAnchorVerticalClassName);

                style.position = new StyleEnum<Position>(Position.Absolute);
            }
        }
    }
}