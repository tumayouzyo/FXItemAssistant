using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TY.ImageAssistant
{
    internal sealed class PreviewCameraManipulator : PointerManipulator
    {
        private readonly Action<int, Vector2> drag;
        private readonly Action<float> zoom;
        private int pointer = -1;
        private int button;
        private Vector2 last;

        public PreviewCameraManipulator(Action<int, Vector2> drag, Action<float> zoom) { this.drag = drag; this.zoom = zoom; }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(Down);
            target.RegisterCallback<PointerMoveEvent>(Move);
            target.RegisterCallback<PointerUpEvent>(Up);
            target.RegisterCallback<PointerCaptureOutEvent>(Lost);
            target.RegisterCallback<WheelEvent>(Wheel);
            target.RegisterCallback<ContextClickEvent>(Context);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(Down);
            target.UnregisterCallback<PointerMoveEvent>(Move);
            target.UnregisterCallback<PointerUpEvent>(Up);
            target.UnregisterCallback<PointerCaptureOutEvent>(Lost);
            target.UnregisterCallback<WheelEvent>(Wheel);
            target.UnregisterCallback<ContextClickEvent>(Context);
        }

        private void Down(PointerDownEvent evt)
        {
            if (pointer != -1 || evt.button < 0 || evt.button > 2) return;
            pointer = evt.pointerId;
            button = evt.button;
            last = evt.position;
            target.CapturePointer(pointer);
            evt.StopPropagation();
        }

        private void Move(PointerMoveEvent evt)
        {
            if (evt.pointerId != pointer || !target.HasPointerCapture(pointer)) return;
            var position = (Vector2)evt.position;
            drag(button, position - last);
            last = position;
            evt.StopPropagation();
        }

        private void Up(PointerUpEvent evt)
        {
            if (evt.pointerId != pointer || evt.button != button) return;
            target.ReleasePointer(pointer);
            pointer = -1;
            evt.StopPropagation();
        }

        private void Lost(PointerCaptureOutEvent evt) => pointer = -1;
        private void Wheel(WheelEvent evt) { zoom(evt.delta.y); evt.StopPropagation(); evt.PreventDefault(); }
        private void Context(ContextClickEvent evt) => evt.StopPropagation();
    }
}
