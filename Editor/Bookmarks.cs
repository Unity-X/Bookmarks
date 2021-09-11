using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityX.Bookmarks
{
    public static class Bookmarks
    {
        public static bool Initialized { get; private set; }

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            if (Initialized)
                return;

            TypeCache.TypeCollection sortingAlgoTypes = TypeCache.GetTypesDerivedFrom<BookmarkSortingAlgorithm>();

            foreach (Type type in sortingAlgoTypes)
            {
                if (!type.IsAbstract)
                {
                    try
                    {
                        BookmarkSortingAlgorithm algoInstance = Activator.CreateInstance(type) as BookmarkSortingAlgorithm;
                        if (algoInstance != null)
                        {
                            SortingAlgorithms.Add(algoInstance);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to instantiate bookmark algorithm {type.Name}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }

            SortingAlgorithms.Sort((a, b) => a.MenuName.CompareTo(b.MenuName));

            Initialized = true;
        }

        public readonly static List<BookmarkSortingAlgorithm> SortingAlgorithms = new List<BookmarkSortingAlgorithm>();
    }
}