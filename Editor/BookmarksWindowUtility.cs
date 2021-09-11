using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityX.Bookmarks
{
    internal static class BookmarksWindowUtility
    {
        public static T FirstParentOfType<T>(this VisualElement element)
        {
            while (element.parent != null)
            {
                if (element.parent is T p)
                    return p;

                element = element.parent;
            }
            return default;
        }

        public static Rect GetRectRelativeTo(this VisualElement element, VisualElement relative)
        {
            Rect aRect = element.contentRect;
            Rect bRect = relative.contentRect;

            var elementMatrix = element.worldTransform;
            var relativeInverseMatrix = relative.worldTransform.inverse;

            Vector2 relativeSize = relativeInverseMatrix.MultiplyVector(elementMatrix.MultiplyVector(aRect.size));
            Vector2 relativeMin = relativeInverseMatrix.MultiplyPoint(elementMatrix.MultiplyPoint(aRect.min));

            return new Rect(relativeMin - bRect.min, relativeSize);
        }

        public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            var item = list[oldIndex];

            list.RemoveAt(oldIndex);

            if (newIndex > oldIndex) newIndex--;
            // the actual index could have shifted due to the removal

            list.Insert(newIndex, item);
        }

        public static void Move<T>(this List<T> list, T item, int newIndex)
        {
            var oldIndex = list.IndexOf(item);
            if (oldIndex > -1)
            {
                list.RemoveAt(oldIndex);

                if (newIndex > oldIndex) newIndex--;
                // the actual index could have shifted due to the removal

                list.Insert(newIndex, item);
            }
        }
    }
}