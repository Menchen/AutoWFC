﻿using System;
using UnityEditor;

namespace AutoWfc.Editor.UIToolKit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class DragAndDropManipulator : PointerManipulator
    {
        public event Action OnStartDrop; 
        public event Action OnDrag; 
        public event Action OnEndDrop;
        public event Action<VisualElement> OnDropped;

        public event Action<VisualElement> OnStartSlotHover; 
        public event Action<VisualElement> OnEndSlotHover; 
        // Write a constructor to set target and store a reference to the 
        // root of the visual tree.
        public DragAndDropManipulator(VisualElement target, ScrollView root, VisualElement ghost)
        {
            this.target = target;
            this.root = root;
            this.ghost = ghost;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            // Register the four callbacks on target.
            target.RegisterCallback<PointerDownEvent>(PointerDownHandler);
            target.RegisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.RegisterCallback<PointerUpEvent>(PointerUpHandler);
            target.RegisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            // Un-register the four callbacks from target.
            target.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
            target.UnregisterCallback<PointerMoveEvent>(PointerMoveHandler);
            target.UnregisterCallback<PointerUpEvent>(PointerUpHandler);
            target.UnregisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
        }

        private bool enabled { get; set; }

        private ScrollView root { get; }

        private VisualElement ghost { get; }

        private VisualElement activeSlot { get; set; }

        // This method stores the starting position of target and the pointer, 
        // makes target capture the pointer, and denotes that a drag is now in progress.
        private void PointerDownHandler(PointerDownEvent evt)
        {
            // targetStartPosition = target.worldBound.position + target.worldBound.size/2;
            // pointerStartPosition = evt.position;
            target.CapturePointer(evt.pointerId);
            enabled = true;
            ghost.style.visibility = new StyleEnum<Visibility>(Visibility.Visible);
            ghost.style.backgroundImage = target.style.backgroundImage;

            // var pos = (Vector2)evt.position - (pointerStartPosition - targetStartPosition) + root.scrollOffset;
            var pos = (Vector2)evt.position + root.scrollOffset;
            pos -= ghost.worldBound.size / 2;
            pos = ghost.parent.WorldToLocal(pos);
            ghost.transform.position = pos;
            OnStartDrop?.Invoke();
        }

        // This method checks whether a drag is in progress and whether target has captured the pointer. 
        // If both are true, calculates a new position for target within the bounds of the window.
        private void PointerMoveHandler(PointerMoveEvent evt)
        {
            if (enabled && target.HasPointerCapture(evt.pointerId))
            {
                // var pos = (Vector2)evt.position - (pointerStartPosition - targetStartPosition) + root.scrollOffset;
                var pos = (Vector2)evt.position + root.scrollOffset;
                pos -= ghost.worldBound.size / 2;
                pos = ghost.parent.WorldToLocal(pos);
                ghost.transform.position = pos;
                OnDrag?.Invoke();
                
                
                
                VisualElement slotsContainer = root.Q<VisualElement>(className: "slots");
                UQueryBuilder<VisualElement> allSlots =
                    slotsContainer.Query<VisualElement>(className: "slot");
                UQueryBuilder<VisualElement> overlappingSlots =
                    allSlots.Where(OverlapsTarget);
                VisualElement closestOverlappingSlot =
                    FindClosestSlot(overlappingSlots);
                if (closestOverlappingSlot != activeSlot)
                {
                    if (activeSlot != null)
                    {
                        OnEndSlotHover?.Invoke(activeSlot);
                    }

                    if (closestOverlappingSlot != null)
                    {
                        OnStartSlotHover?.Invoke(closestOverlappingSlot);
                    }

                    activeSlot = closestOverlappingSlot;
                }
            }
        }

        // This method checks whether a drag is in progress and whether target has captured the pointer. 
        // If both are true, makes target release the pointer.
        private void PointerUpHandler(PointerUpEvent evt)
        {
            if (enabled && target.HasPointerCapture(evt.pointerId))
            {
                target.ReleasePointer(evt.pointerId);
                ghost.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden);
                OnEndDrop?.Invoke();
            }
        }

        // This method checks whether a drag is in progress. If true, queries the root 
        // of the visual tree to find all slots, decides which slot is the closest one 
        // that overlaps target, and sets the position of target so that it rests on top 
        // of that slot. Sets the position of target back to its original position 
        // if there is no overlapping slot.
        private void PointerCaptureOutHandler(PointerCaptureOutEvent evt)
        {
            if (enabled)
            {
                VisualElement slotsContainer = root.Q<VisualElement>(className: "slots");
                UQueryBuilder<VisualElement> allSlots =
                    slotsContainer.Query<VisualElement>(className: "slot");
                UQueryBuilder<VisualElement> overlappingSlots =
                    allSlots.Where(OverlapsTarget);
                VisualElement closestOverlappingSlot =
                    FindClosestSlot(overlappingSlots);
                if (closestOverlappingSlot != null)
                {
                    OnDropped?.Invoke(closestOverlappingSlot);
                }

                activeSlot = null;
                enabled = false;
            }
        }

        private bool OverlapsTarget(VisualElement slot)
        {
            return slot.worldBound.Overlaps(new Rect(ghost.worldBound.position+ghost.worldBound.size/2,Vector2.one));
        }

        private VisualElement FindClosestSlot(UQueryBuilder<VisualElement> slots)
        {
            List<VisualElement> slotsList = slots.ToList();
            float bestDistanceSq = float.MaxValue;
            VisualElement closest = null;
            foreach (VisualElement slot in slotsList)
            {
                Vector3 displacement =
                    RootSpaceOfSlot(slot) - ghost.transform.position;
                float distanceSq = displacement.sqrMagnitude;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    closest = slot;
                }
            }

            return closest;
        }

        private Vector3 RootSpaceOfSlot(VisualElement slot)
        {
            Vector2 slotWorldSpace = slot.parent.LocalToWorld(slot.layout.position);
            return root.WorldToLocal(slotWorldSpace);
        }
    }
}