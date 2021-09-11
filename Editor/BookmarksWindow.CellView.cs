using System;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
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
            private bool _hasUpdatedItemViewListAtLeastOnce;

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
                _optionsButton.RegisterCallback<ContextualMenuPopulateEvent>(PopulateOptionsMenu);
                _foldout.RegisterValueChangedCallback(OnFoldoutCollapseChange);
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

                RegisterCallback<DragEnterEvent>(OnDragEnter);
                RegisterCallback<DragLeaveEvent>(OnDragLeave);
                RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                RegisterCallback<DragPerformEvent>(OnDragPerformed);

                UpdateItemViewList();
            }

            public void OnAssetChanged(string assetPath)
            {
                if (_foldout.value)
                {
                    for (int i = 0; i < _itemsContainer.childCount; i++)
                    {
                        ItemView itemView = (ItemView)_itemsContainer.ElementAt(i);
                        if (string.Equals(itemView.GetItem().CachedAssetPath, assetPath))
                        {
                            BookmarksWindowLocalState.Item.UpdateCache_All(itemView.GetItem(), force: true);
                            itemView.UpdateViewFromItemData();
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
                    DragAndDrop.AcceptDrag();

                    _foldout.value = true; // make sure group is opened

                    MoveItemsFromOtherCellToHere(draggedItemView, insertIndex);
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
                if (DragAndDrop.GetGenericData("shelf-item") is ItemView)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    return true;
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

                if (!_hasUpdatedItemViewListAtLeastOnce)
                    UpdateItemViewList();
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
                evt.menu.AppendAction("Automation/Remove Missing References", (x) =>
                {
                    BookmarksWindowLocalState.instance.BeginImportantChange();
                    _cellData.RemoveMissingReferences = !_cellData.RemoveMissingReferences;
                    BookmarksWindowLocalState.instance.EndImportantChange();
                },
                status: _cellData.RemoveMissingReferences ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

                evt.menu.AppendAction("Automation/Sort/Alphabetic", (x) =>
                {
                    EditorUtility.DisplayDialog("Todo", "Todo", "Ok");
                });

                evt.menu.AppendAction("Automation/Sort/Type Then Alphabetic", (x) =>
                {
                    EditorUtility.DisplayDialog("Todo", "Todo", "Ok");
                });

                evt.menu.AppendAction("Color/Default", (x) => { });
                evt.menu.AppendAction("Color/Red", (x) => { });
                evt.menu.AppendAction("Color/Blue", (x) => { });
                evt.menu.AppendAction("Color/Yellow", (x) => { });
                evt.menu.AppendAction("Color/Green", (x) => { });
                evt.menu.AppendAction("Color/Purple", (x) => { });
                evt.menu.AppendAction("Color/Orange", (x) => { });
                evt.menu.AppendAction("Color/Custom", (x) => { });

                evt.menu.AppendSeparator();

                foreach (var sortAlgo in Bookmarks.SortingAlgorithms)
                {
                    evt.menu.AppendAction($"Sort/{sortAlgo.MenuName}", (x) => SortItems(sortAlgo));
                }

                evt.menu.AppendAction("Rename", (x) =>
                {
                    StartRename();
                });

                evt.menu.AppendAction("Add Current Selection To Group", (x) =>
                {
                    AddObjectsToCellData(Selection.objects, insertIndex: _cellData.Items.Count);
                }, status: CanAddAnyToCell(Selection.objects) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendAction("Delete Group", (x) =>
                {
                    if (EditorUtility.DisplayDialog("Delete Group", $"You are about to delete the '{_cellData.Name}' group.", "Delete Group", "Cancel"))
                    {
                        BookmarksWindowLocalState.instance.BeginImportantChange();
                        foreach (var item in BookmarksWindowLocalState.instance.CellGroups)
                        {
                            item.Cells.Remove(_cellData);
                        }
                        BookmarksWindowLocalState.instance.EndImportantChange();
                        _resources.Window.ReloadWindow();
                    }
                });
            }

            private void SortItems(BookmarkSortingAlgorithm itemComparison)
            {
                BookmarksWindowLocalState.Item.UpdateCaches_All(_cellData.Items);

                BookmarksWindowLocalState.instance.BeginImportantChange();
                _cellData.Items.Sort(itemComparison);
                BookmarksWindowLocalState.instance.EndImportantChange();
                UpdateItemViewList();
            }

            private void MoveItemsFromOtherCellToHere(ItemView draggedItemView, int insertIndex)
            {
                var draggedCellSource = draggedItemView.FirstParentOfType<CellView>();
                if (draggedCellSource != null)
                {
                    BookmarksWindowLocalState.instance.BeginImportantChange();

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

                    BookmarksWindowLocalState.instance.EndImportantChange();

                    if (draggedCellSource != this)
                        draggedCellSource.UpdateItemViewList();
                    UpdateItemViewList();
                }
            }

            private bool CanAddAnyToCell(UnityEngine.Object[] selection)
            {
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
                BookmarksWindowLocalState.instance.BeginImportantChange();
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

                BookmarksWindowLocalState.instance.EndImportantChange();

                UpdateItemViewList();
            }

            private void UpdateItemViewList()
            {
                if (!_foldout.value)
                    return;

                _hasUpdatedItemViewListAtLeastOnce = true;

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
                BookmarksWindowLocalState.instance.BeginImportantChange();
                _cellData.Items.Remove(itemData);
                BookmarksWindowLocalState.instance.EndImportantChange();
                UpdateItemViewList();
            }

            private void StartRename()
            {
                ShowNameEditable(true);
                _nameInputField.Focus();
            }

            private void EndRename()
            {
                BookmarksWindowLocalState.instance.BeginImportantChange();
                _cellData.Name = _nameInputField.value;
                _cellData.NameHasBeenSet = true;
                BookmarksWindowLocalState.instance.EndImportantChange();

                ShowNameEditable(false);
            }

            private void ShowNameEditable(bool editable)
            {
                _nameInputField.style.display = editable ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void OnOptionsButtonClicked(EventBase evt)
            {
                _optionsButton.panel.contextualMenuManager.DisplayMenu(evt, _optionsButton);
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
        }
    }
}