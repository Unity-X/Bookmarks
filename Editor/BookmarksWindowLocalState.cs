using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityX.Bookmarks
{
    [FilePath("UserSettings/Bookmarks/BookmarksWindowLocalState.txt", FilePathAttribute.Location.ProjectFolder)]
    public class BookmarksWindowLocalState : ScriptableSingleton<BookmarksWindowLocalState>
    {
        [Serializable]
        public class CellGroup
        {
            public float LayoutWeight;
            public float ScrollPosition;
            public List<Cell> Cells;

            public static CellGroup CreateDefault()
            {
                return new CellGroup()
                {
                    LayoutWeight = 300f,
                    Cells = new List<Cell>() { Cell.CreateDefault() }
                };
            }
        }

        [Serializable]
        public class Cell
        {
            public bool NameHasBeenSet;
            public string Name;
            [SerializeReference, SubclassSelector] public BookmarkDataSource DataSource = null;
            [SerializeReference, SubclassSelector] public BookmarkSortingAlgorithm SortingAlgorithm = null;
            public List<Item> Items;
            public bool FoldoutOpened;
            public int SortByType; // 1 ascendant, -1 descendant, 0 none
            public int SortByName; // 1 ascendant, -1 descendant, 0 none
            public bool UseCustomColor;
            public Color Color = new Color(0, 0.5f, 1f, 0.2f);

            public int IndexOfItem(GlobalObjectId itemId)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i].GlobalObjectId.Equals(itemId))
                        return i;
                }
                return -1;
            }

            public bool HasItem(GlobalObjectId itemId)
            {
                return IndexOfItem(itemId) != -1;
            }

            public static Cell CreateDefault()
            {
                return new Cell()
                {
                    NameHasBeenSet = false,
                    Name = "Group Name",
                    Items = new List<Item>() { },
                    FoldoutOpened = true,
                };
            }
        }

        [Serializable]
        public class Item : ISerializationCallbackReceiver
        {
            public enum ObjectType
            {
                Null,
                ImportedAsset,
                SceneObject,
                SourceAsset
            }

            [SerializeField] private string _globalIdStr;
            [SerializeField] private string _latestObjectName;
            [SerializeField] private string _latestSceneName;

            public GlobalObjectId GlobalObjectId
            {
                get => _globalObjectId;
                set
                {
                    _globalObjectId = value;
                }
            }

            public void OnAfterDeserialize()
            {
                GlobalObjectId.TryParse(_globalIdStr, out _globalObjectId);
            }

            public void OnBeforeSerialize()
            {
                _globalIdStr = GlobalObjectId.ToString();
            }

            #region Caching
            [NonSerialized] private GlobalObjectId _globalObjectId;
            [NonSerialized] private UnityEngine.Object _cachedObjectReference;
            [NonSerialized] private SceneAsset _cachedSceneAssetReference;
            [NonSerialized] private Texture _cachedAssetIcon;
            [NonSerialized] private string _cachedAssetPath;
            [NonSerialized] private bool _cachedSceneAssetReferenceSet;
            [NonSerialized] private bool _cachedObjectReferenceSet;
            [NonSerialized] private bool _cachedAssetIconSet;
            [NonSerialized] private bool _cachedAssetPathSet;

            public string LatestObjectName => _latestObjectName;
            public string LatestSceneName => _latestSceneName;
            public ObjectType Type
            {
                get
                {
                    switch (_globalObjectId.identifierType)
                    {
                        case 0:
                        default:
                            return ObjectType.Null;

                        case 1:
                            return ObjectType.ImportedAsset;

                        case 2:
                            return ObjectType.SceneObject;

                        case 3:
                            return ObjectType.SourceAsset;
                    }
                }
            }
            public SceneAsset CachedSceneAssetReference
            {
                get => _cachedSceneAssetReference;
                private set
                {
                    _cachedSceneAssetReference = value;

                    if (value)
                    {
                        _latestSceneName = value.name;
                    }

                    _cachedSceneAssetReferenceSet = true;
                }
            }
            public UnityEngine.Object CachedObjectReference
            {
                get => _cachedObjectReference;
                private set
                {
                    _cachedObjectReference = value;

                    if (value)
                    {
                        _latestObjectName = value.name;
                    }

                    _cachedObjectReferenceSet = true;
                }
            }
            public Texture CachedAssetIcon
            {
                get => _cachedAssetIcon;
                private set
                {
                    _cachedAssetIcon = value;
                    _cachedAssetIconSet = true;
                }
            }
            public string CachedAssetPath
            {
                get => _cachedAssetPath;
                private set
                {
                    _cachedAssetPath = value;
                    _cachedAssetPathSet = true;
                }
            }

            public static void UpdateCache_All(Item item, bool force = false)
            {
                UpdateCache_AssetPath(item, force);
                UpdateCache_Scene(item, force);
                UpdateCache_Object(item, force);
                UpdateCache_Icon(item, force);
            }

            public static void UpdateCache_Object(Item item, bool force)
            {
                if ((!item._cachedObjectReferenceSet || force)
                    && (item.Type != ObjectType.SceneObject || item.CachedSceneAssetReference != null))
                {
                    item.CachedObjectReference = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(item.GlobalObjectId);
                }
            }

            public static void UpdateCache_AssetPath(Item item, bool force)
            {
                if (!item._cachedAssetPathSet || force)
                {
                    item.CachedAssetPath = AssetDatabase.GUIDToAssetPath(item.GlobalObjectId.assetGUID.ToString());
                }
            }

            public static void UpdateCache_Scene(Item item, bool force)
            {
                if ((!item._cachedSceneAssetReferenceSet || force)
                                && item.Type == ObjectType.SceneObject)
                {
                    SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(item.CachedAssetPath);
                    item.CachedSceneAssetReference = sceneAsset;
                }
            }

            public static void UpdateCache_Icon(Item item, bool force)
            {
                if (!item._cachedAssetIconSet || force)
                {
                    item.CachedAssetIcon = AssetDatabase.GetCachedIcon(item.CachedAssetPath);
                }
            }

            public static void UpdateCaches_All(List<Item> items, bool force = false)
            {
                // Asset paths
                UpdateCaches_AssetPaths(items, force);

                // Scenes
                UpdateCaches_Scenes(items, force);

                // Objects
                {
                    UpdateCaches_Objects(items, force);
                }

                // Icons
                UpdateCaches_Icons(items, force);
            }

            public static void UpdateCaches_Icons(List<Item> items, bool force)
            {
                foreach (var item in items)
                {
                    UpdateCache_Icon(item, force);
                }
            }

            public static void UpdateCaches_Objects(List<Item> items, bool force)
            {
                List<Item> itemObjectsToFetch = new List<Item>();
                foreach (var item in items)
                {
                    if ((!item._cachedObjectReferenceSet || force)
                        && (item.Type != ObjectType.SceneObject || item.CachedSceneAssetReference != null))
                    {
                        itemObjectsToFetch.Add(item);
                    }
                }

                GlobalObjectId[] idsToGet = new GlobalObjectId[itemObjectsToFetch.Count];
                for (int i = 0; i < itemObjectsToFetch.Count; i++)
                {
                    idsToGet[i] = itemObjectsToFetch[i].GlobalObjectId;
                }

                UnityEngine.Object[] result = new UnityEngine.Object[itemObjectsToFetch.Count];
                GlobalObjectId.GlobalObjectIdentifiersToObjectsSlow(idsToGet, result);

                for (int i = 0; i < itemObjectsToFetch.Count; i++)
                {
                    itemObjectsToFetch[i].CachedObjectReference = result[i];
                }
            }

            public static void UpdateCaches_Scenes(List<Item> items, bool force)
            {
                foreach (var item in items)
                {
                    UpdateCache_Scene(item, force);
                }
            }

            public static void UpdateCaches_AssetPaths(List<Item> items, bool force)
            {
                foreach (var item in items)
                {
                    UpdateCache_AssetPath(item, force);
                }
            }
            #endregion
        }

        public List<CellGroup> CellGroups = new List<CellGroup>();

        private void OnEnable()
        {
            foreach (var group in CellGroups)
            {
                foreach (var cell in group.Cells)
                {
                    cell.DataSource?.Enable();
                    cell.SortingAlgorithm?.Enable();
                }
            }
        }

        public void BeginUndoableChange()
        {
            Undo.RecordObject(this, "Shelf Window Change");
        }

        public void EndUndoableChange()
        {
            CellGroups.RemoveAll((g) => g.Cells.Count == 0);
            if (CellGroups.Count == 0)
            {
                CellGroups.Add(CellGroup.CreateDefault());
            }

            Save(true);
        }

        private void OnDisable()
        {
            Save(true);

            foreach (var group in CellGroups)
            {
                foreach (var cell in group.Cells)
                {
                    cell.DataSource?.Disable();
                    cell.SortingAlgorithm?.Disable();
                }
            }
        }
    }
}
