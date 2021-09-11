using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                    BookmarkSortingAlgorithm algoInstance = null;
                    try
                    {
                        algoInstance = Activator.CreateInstance(type) as BookmarkSortingAlgorithm;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to instantiate bookmark sorting algorithm {type.Name}: {e.Message}\n{e.StackTrace}");
                        continue;
                    }

                    if (algoInstance != null)
                    {
                        if (SortingAlgorithms.Any((x) => x.MenuName == algoInstance.MenuName))
                        {
                            Debug.LogError($"Failed to add bookmark sorting algorithm {type.Name}: an algorithm with the {nameof(BookmarkSortingAlgorithm.MenuName)} \"{algoInstance.MenuName}\" already exists.");
                            continue;
                        }

                        SortingAlgorithms.Add(algoInstance);
                    }
                }
            }

            SortingAlgorithms.Sort((a, b) => a.MenuName.CompareTo(b.MenuName));

            Initialized = true;
        }

        public readonly static List<BookmarkSortingAlgorithm> SortingAlgorithms = new List<BookmarkSortingAlgorithm>();
    }
}