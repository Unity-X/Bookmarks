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

            GetSortingAlgorithms();

            Initialized = true;
        }

        private static void GetSortingAlgorithms()
        {
            SortMenuAlgorithms.Clear();

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
                        if (!algoInstance.DisplayInSortMenu)
                            continue;

                        if (SortMenuAlgorithms.Any((x) => x.SortMenuDisplayName == algoInstance.SortMenuDisplayName))
                        {
                            Debug.LogError($"Failed to add bookmark sorting algorithm {type.Name}: an algorithm with the {nameof(BookmarkSortingAlgorithm.SortMenuDisplayName)} \"{algoInstance.SortMenuDisplayName}\" already exists.");
                            continue;
                        }

                        SortMenuAlgorithms.Add(algoInstance);
                    }
                }
            }

            SortMenuAlgorithms.Sort((a, b) => a.SortMenuDisplayName.CompareTo(b.SortMenuDisplayName));
        }

        public readonly static List<BookmarkSortingAlgorithm> SortMenuAlgorithms = new List<BookmarkSortingAlgorithm>();
    }
}