using System;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UnityX.Bookmarks
{
    public partial class BookmarksWindow
    {
        internal class CellView : VisualElement
        {
            private readonly BookmarksWindowLocalState.Cell _cellData;
            private readonly Resources _resources;
            private readonly Button _optionsButton;
            private readonly Foldout _foldout;
            private readonly VisualElement _itemsContainer;
            private readonly TextField _nameInputField;
            private readonly VisualElement _dropGhostLine;
            private BookmarkDataSource _hookedDataSource;
            private BookmarkSortingAlgorithm _hookedSortingAlgo;
            private bool _initComplete;

            internal BookmarksWindowLocalState.Cell CellData => _cellData;

            public CellView(BookmarksWindowLocalState.Cell cellData, Resources resources)
            {
                AddToClassList("cell");
                resources.CellAsset.CloneTree(this);

                // Fetch & create objects
                _cellData = cellData;
                _resources = resources;
                _optionsButton = this.Q<Button>("options-button");
                _foldout = this.Q<Foldout>("foldout");
                _itemsContainer = this.Q<VisualElement>("items-container");
                _nameInputField = this.Q<TextField>("name-input");
                _dropGhostLine = this.Q<VisualElement>("drop-ghost-line");

                // Adjust view
                _foldout.value = cellData.FoldoutOpened;
                _foldout.text = _cellData.Name;
                _nameInputField.value = _cellData.Name;
                _dropGhostLine.visible = false;

                // Register events
                _nameInputField.RegisterCallback<FocusOutEvent>(OnNameInputFieldLoseFocus);
                _nameInputField.RegisterValueChangedCallback(OnNameInputValueChanged);
                _optionsButton.clickable.clickedWithEventInfo += OnOptionsButtonClicked;
                _foldout.AddManipulator(new ContextualMenuManipulator(PopulateOptionsMenu));
                _foldout.RegisterValueChangedCallback(OnFoldoutCollapseChange);
                if (_cellData.UseCustomColor)
                    _foldout.style.backgroundColor = new StyleColor(_cellData.Color);
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

                RegisterCallback<DragEnterEvent>(OnDragEnter);
                RegisterCallback<DragLeaveEvent>(OnDragLeave);
                RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                RegisterCallback<DragPerformEvent>(OnDragPerformed);

                OnAutomatedDataSourceChanged();
                OnAutomatedSortingChanged();

                _initComplete = true;

                UpdateItemViewList();
            }

            private void OnAutomatedSortingChanged(bool forceUpdate = false)
            {
                var newSource = _cellData.SortingAlgorithm;
                bool sourceChange = _hookedSortingAlgo != newSource;

                if (sourceChange)
                {
                    _hookedSortingAlgo?.Disable();
                    _hookedSortingAlgo = newSource;
                    _hookedSortingAlgo?.Enable();
                }

                if ((sourceChange || forceUpdate) && _hookedSortingAlgo != null)
                {
                    if (_cellData.DataSource == null || !_cellData.DataSource.ProvidedItemsAreAlreadySorted) // skip sorting if DataSource already sorts
                    {
                        _cellData.Items.Sort(_hookedSortingAlgo);
                        UpdateItemViewList();
                    }
                }
            }

            private void OnAutomatedDataSourceChanged(bool forceUpdate = false)
            {
                var newSource = _cellData.DataSource;
                bool sourceChange = _hookedDataSource != newSource;

                if (sourceChange)
                {
                    if (_hookedDataSource != null)
                    {
                        _hookedDataSource.ItemsChanged -= OnAutomatedDataSourceItemsChanged;
                        _hookedDataSource.Disable();
                    }

                    _hookedDataSource = newSource;

                    if (_hookedDataSource != null)
                    {
                        _hookedDataSource.Enable();
                        _hookedDataSource.ItemsChanged += OnAutomatedDataSourceItemsChanged;
                    }
                }

                if ((sourceChange || forceUpdate) && _hookedDataSource != null)
                {
                    OnAutomatedDataSourceItemsChanged();
                }
            }

            private void OnAutomatedDataSourceItemsChanged()
            {
                GlobalObjectId[] newItems = _hookedDataSource.ProvideItems();

                if (_hookedDataSource.ProvidedItemsAreAlreadySorted)
                {
                    int i = 0;
                    for (; i < newItems.Length; i++)
                    {
                        int existingEntryIndex = _cellData.IndexOfItem(newItems[i]);

                        if (existingEntryIndex != -1)
                        {
                            _cellData.Items.Swap(i, existingEntryIndex);
                        }
                        else
                        {
                            _cellData.Items.Insert(i, new BookmarksWindowLocalState.Item()
                            {
                                GlobalObjectId = newItems[i]
                            });
                        }
                    }

                    if(newItems.Length < _cellData.Items.Count)
                    {
                        _cellData.Items.RemoveRange(newItems.Length, _cellData.Items.Count - newItems.Length);
                    }
                }
                else
                {
                    // remove old items
                    for (int i = _cellData.Items.Count - 1; i >= 0; i--)
                    {
                        if (Array.IndexOf(newItems, _cellData.Items[i].GlobalObjectId) == -1)
                        {
                            _cellData.Items.RemoveAt(i);
                        }
                    }

                    // add new items
                    foreach (var item in newItems)
                    {
                        if (!_cellData.HasItem(item))
                        {
                            _cellData.Items.Add(new BookmarksWindowLocalState.Item()
                            {
                                GlobalObjectId = item
                            });
                        }
                    }
                }

                if (_foldout.value)
                {
                    BookmarksWindowLocalState.Item.UpdateCaches_All(_cellData.Items, force: true);

                    // Sort if needed
                    var sortingAlgo = _cellData.SortingAlgorithm;
                    if (sortingAlgo != null && !_hookedDataSource.ProvidedItemsAreAlreadySorted)
                    {
                        _cellData.Items.Sort(sortingAlgo);
                    }

                    UpdateItemViewList();
                }
            }

            public void OnAssetsChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (!_foldout.value)
                    return;

                bool affectedByChange = false;

                if (_cellData.DataSource != null)
                {
                    _cellData.DataSource.OnAssetsModifiedWhileBookmarksDisplayed(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
                }
                else
                {
                    ProcessAssetChanges(importedAssets, ref affectedByChange);
                    ProcessAssetChanges(deletedAssets, ref affectedByChange);
                    ProcessAssetChanges(movedFromAssetPaths, ref affectedByChange);
                    void ProcessAssetChanges(string[] paths, ref bool affectedByChange)
                    {
                        foreach (var assetPath in paths)
                        {
                            for (int i = 0; i < _itemsContainer.childCount; i++)
                            {
                                ItemView itemView = (ItemView)_itemsContainer.ElementAt(i);
                                if (string.Equals(itemView.GetItem().CachedAssetPath, assetPath))
                                {
                                    affectedByChange = true;
                                    BookmarksWindowLocalState.Item.UpdateCache_All(itemView.GetItem(), force: true);
                                    itemView.UpdateViewFromItemData();
                                }
                            }
                        }
                    }

                    if (affectedByChange)
                    {
                        var sortingAlgo = _cellData.SortingAlgorithm;
                        if (sortingAlgo != null && _cellData.DataSource?.ProvidedItemsAreAlreadySorted != true)
                        {
                            _cellData.Items.Sort(sortingAlgo);
                            UpdateItemViewList();
                        }
                    }
                }
            }

            private void OnDragEnter(DragEnterEvent evt)
            {
                if (UpdateDragAndDropVisualMode())
                {
                    // Show ghost
                    _dropGhostLine.visible = true;
                }

                evt.StopPropagation();
            }

            private void OnDragUpdated(DragUpdatedEvent evt)
            {
                if (UpdateDragAndDropVisualMode())
                {
                    int insertIndex = FindInsertIndexFromLocalMousePosition(evt.localMousePosition);
                    _dropGhostLine.style.top = FindInsertGhostHeightFromItemIndex(insertIndex);
                }

                evt.StopPropagation();
            }

            private void OnDragLeave(DragLeaveEvent evt)
            {
                // Hide ghost
                _dropGhostLine.visible = false;
                evt.StopPropagation();
            }

            private void OnDragPerformed(DragPerformEvent evt)
            {
                int insertIndex = FindInsertIndexFromLocalMousePosition(evt.localMousePosition);

                var objectReferences = DragAndDrop.objectReferences;
                if (DragAndDrop.GetGenericData("shelf-item") is ItemView draggedItemView)
                {
                    if (CanMoveCellItemHere(draggedItemView))
                    {
                        DragAndDrop.AcceptDrag();

                        _foldout.value = true; // make sure group is opened

                        MoveCellItemHere(draggedItemView, insertIndex);
                    }
                }
                else if (CanAddAnyToCell(objectReferences))
                {
                    DragAndDrop.AcceptDrag();

                    _foldout.value = true; // make sure group is opened

                    AddObjectsToCellData(DragAndDrop.objectReferences, insertIndex);
                }

                _dropGhostLine.visible = false;
                evt.StopPropagation();
            }

            private bool UpdateDragAndDropVisualMode()
            {
                if (DragAndDrop.GetGenericData("shelf-item") is ItemView draggedItemView)
                {
                    if (CanMoveCellItemHere(draggedItemView))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        return true;
                    }
                }
                else if (CanAddAnyToCell(DragAndDrop.objectReferences))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    return true;
                }

                return false;
            }

            private void OnFoldoutCollapseChange(ChangeEvent<bool> evt)
            {
                _cellData.FoldoutOpened = evt.newValue;

                if (evt.newValue)
                {
                    BookmarksWindowLocalState.Item.UpdateCaches_All(_cellData.Items, force: true);
                    UpdateItemViewList();
                }
            }

            private void OnGeometryChanged(GeometryChangedEvent evt)
            {
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                OnAfterGeometryFirstBuilt();
            }

            private void OnAfterGeometryFirstBuilt()
            {
                if (!_cellData.NameHasBeenSet)
                {
                    StartRename();
                }
                else
                {
                    ShowNameEditable(false);
                }
            }

            private void OnNameInputFieldLoseFocus(FocusOutEvent evt)
            {
                EndRename();
            }

            private void OnNameInputValueChanged(ChangeEvent<string> evt)
            {
                _foldout.text = evt.newValue;
            }

            private void PopulateOptionsMenu(ContextualMenuPopulateEvent evt)
            {
                if (_cellData.SortingAlgorithm == null && _cellData.DataSource?.ProvidedItemsAreAlreadySorted != true)
                {
                    foreach (var sortAlgo in Bookmarks.SortMenuAlgorithms)
                    {
                        evt.menu.AppendAction($"Sort/{sortAlgo.SortMenuDisplayName}", (x) =>
                        {
                            BookmarksWindowLocalState.Item.UpdateCaches_All(_cellData.Items);

                            BookmarksWindowLocalState.instance.BeginUndoableChange();
                            _cellData.Items.Sort(sortAlgo);
                            BookmarksWindowLocalState.instance.EndUndoableChange();

                            UpdateItemViewList();
                        });
                    }
                }
                else
                {
                    evt.menu.AppendAction($"Sort (Already Automated)", null, DropdownMenuAction.Status.Disabled);
                }

                evt.menu.AppendAction("Add Current Selection", (x) =>
                {
                    AddObjectsToCellData(Selection.objects, insertIndex: _cellData.Items.Count);
                }, status: CanAddAnyToCell(Selection.objects) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendAction("Delete", (x) =>
                {
                    if (EditorUtility.DisplayDialog("Delete Group", $"You are about to delete the '{_cellData.Name}' group.", "Delete Group", "Cancel"))
                    {
                        BookmarksWindowLocalState.instance.BeginUndoableChange();
                        foreach (var item in BookmarksWindowLocalState.instance.CellGroups)
                        {
                            item.Cells.Remove(_cellData);
                        }
                        BookmarksWindowLocalState.instance.EndUndoableChange();
                        _resources.Window.ReloadWindow();
                    }
                });
            }

            private void MoveCellItemHere(ItemView draggedItemView, int insertIndex)
            {
                if (!CanMoveCellItemHere(draggedItemView))
                    return;

                var draggedCellSource = draggedItemView.FirstParentOfType<CellView>();
                BookmarksWindowLocalState.instance.BeginUndoableChange();

                if (draggedCellSource == this)
                {
                    _cellData.Items.Move(draggedItemView.GetItem(), insertIndex);
                }
                else
                {
                    // remove from other
                    draggedCellSource._cellData.Items.Remove(draggedItemView.GetItem());

                    // add to ours
                    int indexHere = _cellData.IndexOfItem(draggedItemView.GetItem().GlobalObjectId);
                    if (indexHere != -1) // if we already have that item, just move it
                    {
                        _cellData.Items.Move(indexHere, insertIndex);
                    }
                    else
                    {
                        _cellData.Items.Insert(insertIndex, draggedItemView.GetItem());
                    }
                }

                BookmarksWindowLocalState.instance.EndUndoableChange();

                if (draggedCellSource != this)
                    draggedCellSource.UpdateItemViewList();
                UpdateItemViewList();
            }

            private bool CanMoveCellItemHere(ItemView draggedItemView)
            {
                var draggedCellSource = draggedItemView.FirstParentOfType<CellView>();
                if (draggedCellSource == null)
                    return false;

                // If we're dragging a child and we have an automated sorting, refuse
                if (draggedCellSource == this && (_cellData.SortingAlgorithm != null || _cellData.DataSource?.ProvidedItemsAreAlreadySorted == true))
                    return false;

                // If we're dragging from another source and we have an automated source, refuse
                if (draggedCellSource != this && _cellData.DataSource != null || draggedCellSource.CellData.DataSource != null)
                    return false;

                return true;
            }

            private bool CanAddAnyToCell(UnityEngine.Object[] selection)
            {
                if (_cellData.DataSource != null)
                    return false;

                GlobalObjectId[] ids = new GlobalObjectId[selection.Length];
                GlobalObjectId.GetGlobalObjectIdsSlow(selection, ids);

                for (int i = 0; i < ids.Length; i++)
                {
                    if (!ids[i].assetGUID.Empty())
                        return true;
                }
                return false;
            }

            private void AddObjectsToCellData(UnityEngine.Object[] selection, int insertIndex)
            {
                BookmarksWindowLocalState.instance.BeginUndoableChange();
                GlobalObjectId[] ids = new GlobalObjectId[selection.Length];
                GlobalObjectId.GetGlobalObjectIdsSlow(selection, ids);

                for (int i = 0; i < ids.Length; i++)
                {
                    if (!_cellData.HasItem(ids[i]))
                    {
                        _cellData.Items.Insert(insertIndex, new BookmarksWindowLocalState.Item()
                        {
                            GlobalObjectId = ids[i],
                        });
                    }
                }

                BookmarksWindowLocalState.instance.EndUndoableChange();

                UpdateItemViewList();
            }

            private void UpdateItemViewList()
            {
                if (!_foldout.value || !_initComplete)
                    return;

                BookmarksWindowLocalState.Item.UpdateCaches_All(_cellData.Items);

                int i = 0;
                for (; i < _cellData.Items.Count; i++)
                {
                    if (i >= _itemsContainer.childCount)
                    {
                        var newItemView = new ItemView(_resources);
                        newItemView.RemoveRequested += OnItemRemoveRequested;
                        _itemsContainer.Add(newItemView);
                    }

                    ItemView itemView = (ItemView)_itemsContainer.ElementAt(i);
                    itemView.SetItem(_cellData.Items[i]);
                }

                for (int r = _itemsContainer.childCount - 1; r >= i; r--)
                {
                    _itemsContainer.RemoveAt(r);
                }
            }

            private void OnItemRemoveRequested(BookmarksWindowLocalState.Item itemData)
            {
                BookmarksWindowLocalState.instance.BeginUndoableChange();
                _cellData.Items.Remove(itemData);
                BookmarksWindowLocalState.instance.EndUndoableChange();
                UpdateItemViewList();
            }

            private void StartRename()
            {
                ShowNameEditable(true);
                _nameInputField.Focus();
            }

            private void EndRename()
            {
                BookmarksWindowLocalState.instance.BeginUndoableChange();
                _cellData.Name = _nameInputField.value;
                _cellData.NameHasBeenSet = true;
                BookmarksWindowLocalState.instance.EndUndoableChange();

                ShowNameEditable(false);
            }

            private void ShowNameEditable(bool editable)
            {
                _nameInputField.style.display = editable ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void OnOptionsButtonClicked(EventBase evt)
            {
                BookmarkGroupInspectorWindow.Show(this, _resources.Window.position);
                //_optionsButton.panel.contextualMenuManager.DisplayMenu(evt, _optionsButton);
            }

            private int FindInsertIndexFromLocalMousePosition(Vector2 localMousePos)
            {
                int i = 0;
                for (; i < _itemsContainer.childCount; i++)
                {
                    if (_itemsContainer.ElementAt(i).GetRectRelativeTo(this).center.y > localMousePos.y)
                        break;
                }
                return i;
            }

            private float FindInsertGhostHeightFromItemIndex(int index)
            {
                float yPos;
                if (_itemsContainer.childCount == 0 || !_foldout.value)
                    yPos = contentRect.yMax - 3;
                else if (index >= _itemsContainer.childCount)
                    yPos = _itemsContainer.ElementAt(index - 1).GetRectRelativeTo(this).yMax;
                else
                    yPos = _itemsContainer.ElementAt(index).GetRectRelativeTo(this).yMin;

                float lineThickness = _dropGhostLine.resolvedStyle.height;
                return Mathf.Clamp(yPos, 0, contentRect.yMax - lineThickness);
            }

            public void OnSettingsModified()
            {
                _foldout.text = _cellData.Name;
                _nameInputField.value = _cellData.Name;

                OnAutomatedDataSourceChanged(forceUpdate: true);
                OnAutomatedSortingChanged(forceUpdate: true);

                if (_cellData.UseCustomColor)
                    _foldout.style.backgroundColor = new StyleColor(_cellData.Color);
                else
                    _foldout.style.backgroundColor = new StyleColor(StyleKeyword.Initial);
            }
        }
    }
}