﻿using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.SceneManagement;

namespace UnityX.Bookmarks
{
    public partial class BookmarksWindow
    {
        internal class ItemView : VisualElement
        {
            // This is a hacky way to correctly release the pointer when we begin drag.
            // I did not find a better way (panel.ReleasePointer leaves the VE in active state)
            private class CancellableClickable : Clickable
            {
                public CancellableClickable(Action<EventBase> handler) : base(handler) { }
                public CancellableClickable(Action handler) : base(handler) { }
                public CancellableClickable(Action handler, long delay, long interval) : base(handler, delay, interval) { }

                public void Cancel() { ProcessUpEvent(PointerUpEvent.GetPooled(), Vector2.one * 9999999f, 0);}
            }

            private BookmarksWindowLocalState.Item _itemData;
            private readonly Resources _resources;
            private Label _label;
            private Button _button;
            private VisualElement _icon;
            private bool _canStartDrag;
            private VisualElement _dragGhost;
            private Vector2 _dragStart;

            public event Action<BookmarksWindowLocalState.Item> RemoveRequested;

            public ItemView(Resources resources)
            {
                resources.ItemAsset.CloneTree(this);
                _resources = resources;

                _label = this.Q<Label>("label");
                _icon = this.Q<VisualElement>("icon");
                _button = this.Q<Button>("button");
                _dragGhost = this.Q("drag-ghost");

                _button.clickable = new CancellableClickable(default(Action));

                ShowDraggedVisuals(false);

                RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
                _button.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
                _button.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);

                RegisterCallback<MouseEnterEvent>(OnMouseEnterEvent);
                _button.clicked += OnClick;
                _button.RegisterCallback<MouseDownEvent>((x) =>
                {
                    if (x.clickCount == 2)
                    {
                        OnDoubleClick();
                        x.StopPropagation();
                    }
                }, TrickleDown.TrickleDown);

                this.AddManipulator(new ContextualMenuManipulator(PopulateContextualMenu));
            }

            private void OnPointerUpEvent(PointerUpEvent evt)
            {
                _canStartDrag = false;
                _dragStart = evt.position;
            }

            private void OnPointerDownEvent(PointerDownEvent evt)
            {
                _canStartDrag = true;
            }

            public void ShowDraggedVisuals(bool shown)
            {
                _dragGhost.visible = shown;
            }

            private void OnPointerMoveEvent(PointerMoveEvent evt)
            {
                if (!_canStartDrag)
                    return;

                if (_canStartDrag && Vector2.Distance(evt.position, _dragStart) > 5)
                {
                    // Drag can only be started by mouse events or else it will throw an error, so we leave early.
                    if (Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseDrag)
                        return;

                    BookmarksWindowLocalState.Item.UpdateCache_All(_itemData, force: true);

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.paths = new string[] { }; // apparently, PrepartStartDrag() doesn't clear this. So we do it manually here

                    if (_itemData.CachedObjectReference != null)
                    {
                        Selection.SetActiveObjectWithContext(_itemData.CachedObjectReference, null);
                        DragAndDrop.objectReferences = new UnityEngine.Object[] { _itemData.CachedObjectReference };
                    }
                    else
                    {
                        DragAndDrop.objectReferences = new UnityEngine.Object[] { };
                    }

                    DragAndDrop.SetGenericData("shelf-item", this);
                    DragAndDrop.StartDrag("Shelf Item Drag");

                    // cancel click, this will release pointer and remove the 'active' state from the button
                    ((CancellableClickable)_button.clickable).Cancel();

                    ShowDraggedVisuals(true);
                    _resources.DraggedItem = this;
                    _canStartDrag = false;
                }
            }

            private void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
            {
                if (GetCell().DataSource == null)
                {
                    evt.menu.AppendAction("Remove", (x) =>
                    {
                        RemoveRequested?.Invoke(_itemData);
                    }, status: GetCell().DataSource == null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }
                else
                {
                    evt.menu.AppendAction("Remove (Managed by Data Source)", null, status: DropdownMenuAction.Status.Disabled);
                }
            }

            private void OnMouseEnterEvent(MouseEnterEvent evt)
            {
                BookmarksWindowLocalState.Item.UpdateCache_All(_itemData, force: true);
                UpdateViewFromItemData();
            }

            private void OnDoubleClick()
            {
                if (_itemData.Type == BookmarksWindowLocalState.Item.ObjectType.SceneObject)
                {
                    if (_itemData.CachedObjectReference == null)
                    {
                        if (_itemData.CachedSceneAssetReference == null)
                            return;

                        // open scene
                        AssetDatabase.OpenAsset(_itemData.CachedSceneAssetReference);

                        // refresh gameobject reference
                        BookmarksWindowLocalState.Item.UpdateCache_All(_itemData, force: true);
                    }

                    if (PrefabStageUtility.GetCurrentPrefabStage())
                    {

                    }

                    // select
                    Selection.SetActiveObjectWithContext(_itemData.CachedObjectReference, null);
                    EditorGUIUtility.PingObject(_itemData.CachedObjectReference);
                }
                else
                {
                    AssetDatabase.OpenAsset(_itemData.CachedObjectReference);
                }
            }


            private void OnClick()
            {
                if (_itemData.Type == BookmarksWindowLocalState.Item.ObjectType.SceneObject && _itemData.CachedObjectReference == null)
                {
                    Selection.SetActiveObjectWithContext(_itemData.CachedSceneAssetReference, null);
                    EditorGUIUtility.PingObject(_itemData.CachedSceneAssetReference);
                }
                else
                {
                    Selection.SetActiveObjectWithContext(_itemData.CachedObjectReference, null);
                    EditorGUIUtility.PingObject(_itemData.CachedObjectReference);
                }

            }

            public BookmarksWindowLocalState.Item GetItem() => _itemData;
            public BookmarksWindowLocalState.Cell GetCell() => this.FirstParentOfType<CellView>().CellData;

            public void SetItem(BookmarksWindowLocalState.Item itemData)
            {
                _itemData = itemData;

                UpdateViewFromItemData();
            }

            public void UpdateViewFromItemData()
            {
                _label.text = GetDisplayName(_itemData);
                // set tooltip to text to allow user to view longer names if the overflow from the view
                tooltip = _label.text;

                _icon.style.backgroundImage = new StyleBackground(_itemData.CachedAssetIcon as Texture2D);
            }

            public static string GetDisplayName(BookmarksWindowLocalState.Item itemData)
            {
                if (itemData.Type == BookmarksWindowLocalState.Item.ObjectType.SceneObject)
                {
                    if (itemData.CachedSceneAssetReference == null)
                    {
                        return $"Missing ({itemData.LatestSceneName} > {itemData.LatestObjectName})";
                    }
                    else if (itemData.CachedObjectReference == null)
                    {
                        return $"{itemData.CachedSceneAssetReference.name} > {itemData.LatestObjectName}";
                    }
                    else
                    {
                        return $"{itemData.CachedSceneAssetReference.name} > {itemData.CachedObjectReference.name}";
                    }
                }
                else
                {
                    if (itemData.CachedObjectReference == null)
                    {
                        return $"Missing ({itemData.LatestObjectName})";
                    }
                    else
                    {
                        return itemData.CachedObjectReference.name;
                    }
                }
            }
        }
    }
}