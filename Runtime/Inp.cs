#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SaturaSpace
{
    public static class Inp
    {
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

        static readonly bool[] btnCur = new bool[3];
        static readonly bool[] btnPrev = new bool[3];

        public static bool KeyboardPresent { get; private set; }
        public static bool MousePresent { get; private set; }
        public static Vector2 MousePosition { get; private set; }
        public static Vector2 MouseDelta { get; private set; }
        public static Vector2 Scroll { get; private set; }
        public static float ScrollY => Scroll.y;

        public static bool IsPressed(Key k)   { int i = (int)k; return (uint)i < (uint)keyCur.Length && keyCur[i]; }
        public static bool WasPressed(Key k)  { int i = (int)k; return (uint)i < (uint)keyCur.Length && keyCur[i] && !keyPrev[i]; }
        public static bool WasReleased(Key k) { int i = (int)k; return (uint)i < (uint)keyCur.Length && !keyCur[i] && keyPrev[i]; }

        public static bool ShiftHeld => IsPressed(Key.LeftShift) || IsPressed(Key.RightShift);
        public static bool CtrlHeld  => IsPressed(Key.LeftCtrl) || IsPressed(Key.RightCtrl);

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

            var leftovers = Resources.FindObjectsOfTypeAll<InpPump>();
            foreach (var p in leftovers) UnityEngine.Object.DestroyImmediate(p.gameObject);

            var go = new GameObject("SaturaSpace.InpPump");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<InpPump>();
        }

        public static void Snapshot()
        {
            Array.Copy(keyCur, keyPrev, keyCur.Length);
            btnPrev[0] = btnCur[0]; btnPrev[1] = btnCur[1]; btnPrev[2] = btnCur[2];

            var kb = Keyboard.current;
            KeyboardPresent = kb != null && kb.added;
            Array.Clear(keyCur, 0, keyCur.Length);
            if (KeyboardPresent)
            {
                var keys = kb.allKeys;
                for (int i = 0; i < keys.Count; i++)
                {
                    var ctrl = keys[i];
                    if (ctrl == null) continue;
                    int idx = (int)ctrl.keyCode;
                    if ((uint)idx < (uint)keyCur.Length) keyCur[idx] = ctrl.isPressed;
                }
            }

            var mouse = Mouse.current;
            MousePresent = mouse != null && mouse.added;
            if (MousePresent)
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

    [DefaultExecutionOrder(int.MinValue + 1)]
    internal sealed class InpPump : MonoBehaviour
    {
        void Update() => Inp.Snapshot();
    }
}
#endif
