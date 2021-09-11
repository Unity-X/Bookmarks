using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityX.Bookmarks
{
    public partial class BookmarksWindow
    {
        public class AdjustableGroupLayout : VisualElement
        {
            public enum Orientation
            {
                Horizontal = 0,
                Vertical = 1
            }

            private class ElementResizer : MouseManipulator
            {
                Vector2 _start;
                protected bool _active;
                AdjustableGroupLayout _group;
                int _direction;
                Orientation _orientation;

                public ElementResizer(AdjustableGroupLayout group, int dir, Orientation orientation)
                {
                    _orientation = orientation;
                    _group = group;
                    _direction = dir;
                    activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                    _active = false;
                }

                protected override void RegisterCallbacksOnTarget()
                {
                    target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                    target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                    target.RegisterCallback<MouseUpEvent>(OnMouseUp);
                }

                protected override void UnregisterCallbacksFromTarget()
                {
                    target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                    target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                    target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                }

                public void ApplyDelta(float delta)
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        target.style.left = target.style.left.value.value + delta;
                    }
                    else
                    {
                        target.style.top = target.style.top.value.value + delta;
                    }
                    _group.SetPositionsFromSeparators(delta > 0);
                }

                protected void OnMouseDown(MouseDownEvent e)
                {
                    if (_active)
                    {
                        e.StopImmediatePropagation();
                        return;
                    }

                    if (CanStartManipulation(e))
                    {
                        _start = e.localMousePosition;

                        _active = true;
                        target.CaptureMouse();
                        e.StopPropagation();
                    }
                }

                protected void OnMouseMove(MouseMoveEvent e)
                {
                    if (!_active || !target.HasMouseCapture())
                        return;

                    Vector2 diff = e.localMousePosition - _start;
                    float mouseDiff = diff.x;
                    if (_orientation == Orientation.Vertical)
                        mouseDiff = diff.y;

                    float delta = _direction * mouseDiff;

                    ApplyDelta(delta);

                    e.StopPropagation();
                }

                protected void OnMouseUp(MouseUpEvent e)
                {
                    if (!_active || !target.HasMouseCapture() || !CanStopManipulation(e))
                        return;

                    _active = false;
                    target.ReleaseMouse();
                    e.StopPropagation();
                }
            }

            private const float ELEMENT_MIN_SIZE = 50f;
            private List<Separator> _separators = new List<Separator>();
            private VisualElement _content;
            private Orientation _orientation;
            private int _lastContentChildCount;
            private List<float> _floatBuffer = new List<float>();

            public override VisualElement contentContainer => _content;

            public AdjustableGroupLayout(Orientation orientation)
            {
                _orientation = orientation;
                AddToClassList("group");
                AddToClassList(orientation == Orientation.Vertical ? "group--vertical" : "group--horizontal");
                _content = new VisualElement();
                _content.name = "content";
                _content.style.flexGrow = 1;
                _content.AddToClassList(orientation == Orientation.Vertical ? "group-content--vertical" : "group-content--horizontal");
                hierarchy.Add(_content);

                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            private void OnGeometryChanged(GeometryChangedEvent evt)
            {
                if (_lastContentChildCount != _content.childCount)
                {
                    _lastContentChildCount = _content.childCount;
                    OnChildCountChanged();
                    SetSeparatorCount(_content.childCount - 1);
                }

                SetPositionsFromChildren();
            }

            private void OnChildCountChanged()
            {
                foreach (var item in _content.Children())
                {
                    item.style.flexGrow = 0;
                    item.style.flexShrink = 0;
                    item.style.flexBasis = 0;
                    item.style.position = Position.Absolute;

                    if (_orientation == Orientation.Vertical)
                    {
                        item.style.left = 0;
                        item.style.right = 0;
                    }
                    else
                    {
                        item.style.top = 0;
                        item.style.bottom = 0;
                    }
                }
            }

            public void SetPositionsFromChildren()
            {
                _floatBuffer.Clear();

                foreach (var child in _content.Children())
                    _floatBuffer.Add(GetChildWeight(child));

                float sum = 0;
                foreach (var item in _floatBuffer)
                {
                    sum += item;
                }

                float multiplier = (_orientation == Orientation.Vertical ? _content.contentRect.height : _content.contentRect.width) / sum;
                for (int i = 0; i < _floatBuffer.Count; i++)
                {
                    _floatBuffer[i] *= multiplier;
                }

                SetPositions(sizes: _floatBuffer, -1);
            }

            public void SetPositionsFromSeparators(bool fromStart)
            {
                _floatBuffer.Clear();
                int dir = fromStart ? 1 : -1;

                float p2;
                float p1 = fromStart
                    ? 0
                    : _orientation == Orientation.Vertical
                        ? _content.contentRect.height
                        : _content.contentRect.width;

                int begin = fromStart ? 0 : _separators.Count - 1;
                int end = fromStart ? _separators.Count : -1;
                int i = begin - dir;

                while ((i += dir) != end)
                {
                    p2 = _orientation == Orientation.Vertical
                        ? _separators[i].style.top.value.value
                        : _separators[i].style.left.value.value;

                    float size = (p2 - p1) * dir;
                    if (size < ELEMENT_MIN_SIZE)
                    {
                        size = ELEMENT_MIN_SIZE;
                        p2 = p1 + (size * dir);
                    }

                    _floatBuffer.Add(size);
                    p1 = p2;
                }

                p2 = fromStart
                    ? _orientation == Orientation.Vertical
                        ? _content.contentRect.height
                        : _content.contentRect.width
                    : 0;
                _floatBuffer.Add((p2 - p1) * dir);

                if (!fromStart)
                    _floatBuffer.Reverse();

                SetPositions(sizes: _floatBuffer, dir);
            }

            private void SetPositions(List<float> sizes, int sizeValidationDirection)
            {
                if (sizes.Count != _content.childCount)
                {
                    Debug.LogError($"SetPositions's argument {nameof(sizes)} should have a size equal to number of children in the content");
                    return;
                }

                if (sizes.Count == 0)
                    return;

                // Ensure no NaN
                for (int i = 0; i < sizes.Count; i++)
                {
                    if (float.IsNaN(sizes[i]))
                        sizes[i] = 50f;
                }

                if (sizeValidationDirection == 1)
                    EnsureMinimumSize(-1, sizes.Count - 1, 0);
                else
                    EnsureMinimumSize(1, 0, sizes.Count - 1);

                void EnsureMinimumSize(int direction, int first, int last)
                {
                    float remainingSpace = _orientation == Orientation.Vertical ? _content.contentRect.height : _content.contentRect.width;

                    int i = first;
                    for (; i != last; i += direction)
                    {
                        float missingSize = ELEMENT_MIN_SIZE - sizes[i];
                        if (missingSize > 0)
                        {
                            sizes[i] += missingSize;
                            if (i + direction != last)
                                sizes[i + direction] -= missingSize;
                        }

                        remainingSpace -= sizes[i];
                    }

                    sizes[i] = Mathf.Max(remainingSpace, ELEMENT_MIN_SIZE);
                }

                float p = 0;
                for (int i = 0; i < sizes.Count; i++)
                {
                    var contentChild = _content.ElementAt(i);
                    if (_orientation == Orientation.Vertical)
                    {
                        contentChild.style.top = p;
                        contentChild.style.height = sizes[i];
                    }
                    else
                    {
                        contentChild.style.left = p;
                        contentChild.style.width = sizes[i];
                    }

                    SetChildWeight(contentChild, sizes[i]);

                    p += sizes[i];

                    if (i < _separators.Count)
                    {
                        if (_orientation == Orientation.Vertical)
                        {
                            _separators[i].style.top = p;
                        }
                        else
                        {
                            _separators[i].style.left = p;
                        }
                    }
                }
            }

            private void SetSeparatorCount(int count)
            {
                while (_separators.Count < count)
                {
                    var newSeparator = new Separator(horizontalDrag: _orientation == Orientation.Horizontal);
                    newSeparator.AddManipulator(new ElementResizer(this, 1, _orientation));

                    if (_orientation == Orientation.Vertical)
                    {
                        newSeparator.style.left = 0;
                        newSeparator.style.right = 0;
                    }
                    else
                    {
                        newSeparator.style.top = 0;
                        newSeparator.style.bottom = 0;
                    }
                    hierarchy.Add(newSeparator);
                    _separators.Add(newSeparator);
                }

                while (_separators.Count > count)
                {
                    hierarchy.Remove(_separators[_separators.Count - 1]);
                    _separators.RemoveAt(_separators.Count - 1);
                }
            }

            private float GetChildWeight(VisualElement child)
            {
                if (child is ILayoutWeighted weighted)
                    return weighted.LayoutWeight;
                else
                    return child.style.flexBasis.value.value;
            }

            private void SetChildWeight(VisualElement child, float weight)
            {
                if (child is ILayoutWeighted weighted)
                    weighted.LayoutWeight = weight;
                else
                    child.style.flexBasis = weight;
            }
        }
    }
}