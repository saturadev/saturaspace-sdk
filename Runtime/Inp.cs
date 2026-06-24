#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SaturaSpace
{
    /// <summary>
    /// Frame-coherent input facade over the New Input System.
    ///
    /// Snapshots Keyboard/Mouse <c>isPressed</c> state once per frame (just before
    /// MonoBehaviour.Update) and derives press/release edges by diffing against the previous
    /// frame. Edges are derived from state rather than the devices'
    /// <c>wasPressedThisFrame</c>/<c>wasReleasedThisFrame</c> so that real and injected input
    /// behave identically and stay deterministic and testable.
    ///
    /// Limitation: a press+release entirely within one frame (a sub-frame tap) is not seen.
    /// </summary>
    public static class Inp
    {
        // Size the key array from the enum's largest value (no Key.Count constant exists across
        // Input System versions), so an index is never out of range.
        static readonly int KeyArrayLen = MaxKeyValue() + 1;

        static readonly bool[] keyCur = new bool[KeyArrayLen];
        static readonly bool[] keyPrev = new bool[KeyArrayLen];

        static int MaxKeyValue()
        {
            int max = 0;
            foreach (Key k in (Key[])Enum.GetValues(typeof(Key)))
                if ((int)k > max) max = (int)k;
            return max;
        }

        // 0 = left, 1 = right, 2 = middle
        static readonly bool[] btnCur = new bool[3];
        static readonly bool[] btnPrev = new bool[3];

        public static bool KeyboardPresent { get; private set; }
        public static bool MousePresent { get; private set; }
        public static Vector2 MousePosition { get; private set; }
        public static Vector2 MouseDelta { get; private set; }
        public static Vector2 Scroll { get; private set; }
        public static float ScrollY => Scroll.y;

        // ---------- keyboard ----------
        public static bool IsPressed(Key k)   { int i = (int)k; return (uint)i < (uint)keyCur.Length && keyCur[i]; }
        public static bool WasPressed(Key k)  { int i = (int)k; return (uint)i < (uint)keyCur.Length && keyCur[i] && !keyPrev[i]; }
        public static bool WasReleased(Key k) { int i = (int)k; return (uint)i < (uint)keyCur.Length && !keyCur[i] && keyPrev[i]; }

        public static bool ShiftHeld => IsPressed(Key.LeftShift) || IsPressed(Key.RightShift);
        public static bool CtrlHeld  => IsPressed(Key.LeftCtrl) || IsPressed(Key.RightCtrl);

        // ---------- mouse ----------
        public static bool LeftHeld   => btnCur[0];
        public static bool RightHeld  => btnCur[1];
        public static bool MiddleHeld => btnCur[2];
        public static bool LeftPressedThisFrame  => btnCur[0] && !btnPrev[0];
        public static bool LeftReleasedThisFrame => !btnCur[0] && btnPrev[0];
        public static bool RightPressedThisFrame => btnCur[1] && !btnPrev[1];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            Array.Clear(keyCur, 0, keyCur.Length);
            Array.Clear(keyPrev, 0, keyPrev.Length);
            btnCur[0] = btnCur[1] = btnCur[2] = false;
            btnPrev[0] = btnPrev[1] = btnPrev[2] = false;
            MousePosition = MouseDelta = Scroll = Vector2.zero;
            KeyboardPresent = MousePresent = false;

            // Tear down any InpPump left over from a previous Play session. InpPump is
            // HideAndDontSave and survives Play Mode exit when Domain Reload is disabled; left
            // behind, multiple pumps would call Snapshot() several times per frame and clobber the
            // previous-frame state that WasPressed/WasReleased depend on.
            var leftovers = Resources.FindObjectsOfTypeAll<InpPump>();
            foreach (var p in leftovers) UnityEngine.Object.DestroyImmediate(p.gameObject);

            // One snapshot per frame from a high-priority Update: after the Input System has been
            // pumped for the frame and before any game script reads input, which keeps edges exact.
            var go = new GameObject("SaturaSpace.InpPump");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<InpPump>();
            Debug.Log($"[Inp] installed (per-frame snapshot over {KeyArrayLen} keys); cleared {leftovers.Length} leftover pump(s)");
        }

        /// <summary>One frame's state capture. Public so Edit Mode tests can pump it manually.</summary>
        public static void Snapshot()
        {
            Array.Copy(keyCur, keyPrev, keyCur.Length);
            btnPrev[0] = btnCur[0]; btnPrev[1] = btnCur[1]; btnPrev[2] = btnCur[2];

            var kb = Keyboard.current;
            KeyboardPresent = kb != null;
            Array.Clear(keyCur, 0, keyCur.Length);
            if (kb != null)
            {
                // Fill from the device's authoritative control list (no sentinel/None gaps).
                var keys = kb.allKeys;
                for (int i = 0; i < keys.Count; i++)
                {
                    var ctrl = keys[i];
                    int idx = (int)ctrl.keyCode;
                    if ((uint)idx < (uint)keyCur.Length) keyCur[idx] = ctrl.isPressed;
                }
            }

            var mouse = Mouse.current;
            MousePresent = mouse != null;
            if (mouse != null)
            {
                btnCur[0] = mouse.leftButton.isPressed;
                btnCur[1] = mouse.rightButton.isPressed;
                btnCur[2] = mouse.middleButton.isPressed;
                MousePosition = mouse.position.ReadValue();
                MouseDelta = mouse.delta.ReadValue();
                Scroll = mouse.scroll.ReadValue();
            }
            else
            {
                btnCur[0] = btnCur[1] = btnCur[2] = false;
                MouseDelta = Vector2.zero;
                Scroll = Vector2.zero;
            }
        }
    }

    // Drives Inp.Snapshot once per frame, after the input pump and before any game script,
    // so press edges are coherent for the frame's readers.
    [DefaultExecutionOrder(int.MinValue + 1)]
    internal sealed class InpPump : MonoBehaviour
    {
        void Update() => Inp.Snapshot();
    }
}
#endif
