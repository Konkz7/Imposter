using System;
using System.Collections;
using System.Collections.Generic;
using PartyGame.UI.Design;
using UnityEngine;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Navigation as a stack of screens inside one canvas. Screens are created on demand and
    /// destroyed when popped, so no screen can leave stale secret information on display.
    /// </summary>
    public class ScreenStack
    {
        private readonly List<ScreenBase> _stack = new List<ScreenBase>();
        private readonly RectTransform _layer;
        private readonly AppController _app;
        private Coroutine _transition;

        public ScreenStack(RectTransform layer, AppController app)
        {
            _layer = layer;
            _app = app;
        }

        public ScreenBase Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public int Count => _stack.Count;

        public bool AnyPrivateInformationVisible
        {
            get
            {
                var current = Current;
                return current != null && current.ShowsPrivateInformation;
            }
        }

        public T Push<T>(Action<T> configure = null) where T : ScreenBase
        {
            var previous = Current;
            var screen = Instantiate<T>(configure);
            _stack.Add(screen);
            BeginTransition(previous, screen, true);
            return screen;
        }

        /// <summary>Replaces the top of the stack. Used when going forward should not add history.</summary>
        public T Replace<T>(Action<T> configure = null) where T : ScreenBase
        {
            var previous = Current;
            if (previous != null) _stack.RemoveAt(_stack.Count - 1);

            var screen = Instantiate<T>(configure);
            _stack.Add(screen);
            BeginTransition(previous, screen, true, destroyPrevious: true);
            return screen;
        }

        /// <summary>Clears everything and starts again from a fresh root screen.</summary>
        public T Reset<T>(Action<T> configure = null) where T : ScreenBase
        {
            foreach (var screen in _stack)
            {
                if (screen == null) continue;
                screen.OnHidden();
                UnityEngine.Object.Destroy(screen.gameObject);
            }
            _stack.Clear();

            var root = Instantiate<T>(configure);
            _stack.Add(root);
            BeginTransition(null, root, true);
            return root;
        }

        public bool Pop()
        {
            if (_stack.Count <= 1) return false;

            var top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            var next = Current;
            BeginTransition(top, next, false, destroyPrevious: true);
            return true;
        }

        /// <summary>Pops until only the root screen remains.</summary>
        public void PopToRoot()
        {
            while (_stack.Count > 1)
            {
                var top = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                if (top == null) continue;
                top.OnHidden();
                UnityEngine.Object.Destroy(top.gameObject);
            }

            var root = Current;
            if (root == null) return;
            root.gameObject.SetActive(true);
            root.Group.alpha = 1f;
            root.Root.anchoredPosition = Vector2.zero;
            root.OnShown();
        }

        private T Instantiate<T>(Action<T> configure) where T : ScreenBase
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(_layer, false);
            var screen = go.AddComponent<T>();
            configure?.Invoke(screen);
            screen.Construct(_app);
            return screen;
        }

        private void BeginTransition(ScreenBase from, ScreenBase to, bool forward, bool destroyPrevious = false)
        {
            if (_transition != null) UiTween.Stop(_transition);

            if (to != null)
            {
                to.gameObject.SetActive(true);
                to.Root.SetAsLastSibling();
            }

            if (!UiFeedback.AnimationsEnabled)
            {
                FinishTransition(from, to, destroyPrevious);
                return;
            }

            _transition = UiTween.Run(Animate(from, to, forward, destroyPrevious));
            if (_transition == null) FinishTransition(from, to, destroyPrevious);
        }

        private IEnumerator Animate(ScreenBase from, ScreenBase to, bool forward, bool destroyPrevious)
        {
            var distance = forward ? 90f : -90f;

            if (to != null)
            {
                to.Group.alpha = 0f;
                to.Group.blocksRaycasts = false;
                to.Root.anchoredPosition = new Vector2(distance, 0f);
            }

            if (from != null)
            {
                from.Group.blocksRaycasts = false;
                UiTween.Run(UiTween.FadeCanvas(from.Group, 1f, 0f, Theme.FastTransition));
            }

            if (to != null)
            {
                UiTween.Run(UiTween.Slide(to.Root, new Vector2(distance, 0f), Vector2.zero, Theme.NormalTransition));
                yield return UiTween.FadeCanvas(to.Group, 0f, 1f, Theme.NormalTransition);
            }
            else
            {
                yield return null;
            }

            FinishTransition(from, to, destroyPrevious);
        }

        private void FinishTransition(ScreenBase from, ScreenBase to, bool destroyPrevious)
        {
            if (from != null)
            {
                from.OnHidden();
                if (destroyPrevious) UnityEngine.Object.Destroy(from.gameObject);
                else from.gameObject.SetActive(false);
            }

            if (to == null) return;
            to.Group.alpha = 1f;
            to.Group.blocksRaycasts = true;
            to.Root.anchoredPosition = Vector2.zero;
            to.OnShown();
            _app.NotifyScreenChanged();
        }
    }
}
