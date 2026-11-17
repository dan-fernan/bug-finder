using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BugDefense
{
    /// <summary>Keys the game cares about, independent of which input backend is active.</summary>
    public enum KeyCodeLite { Alpha1, Alpha2, Alpha3, Alpha4, Escape, R, Space, F, H, P, C, M, N, L }

    /// <summary>
    /// Single wrapper around all input so the game compiles and runs under either the
    /// new Input System package or the legacy Input Manager (Project Settings > Player > Active Input Handling).
    /// </summary>
    public static class GameInput
    {
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool LeftClickDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        public static bool RightClickDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(1);
#endif
            }
        }

        public static bool KeyDown(KeyCodeLite k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case KeyCodeLite.Alpha1: return kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
                case KeyCodeLite.Alpha2: return kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;
                case KeyCodeLite.Alpha3: return kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame;
                case KeyCodeLite.Alpha4: return kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame;
                case KeyCodeLite.Escape: return kb.escapeKey.wasPressedThisFrame;
                case KeyCodeLite.R: return kb.rKey.wasPressedThisFrame;
                case KeyCodeLite.Space: return kb.spaceKey.wasPressedThisFrame;
                case KeyCodeLite.F: return kb.fKey.wasPressedThisFrame;
                case KeyCodeLite.H: return kb.hKey.wasPressedThisFrame;
                case KeyCodeLite.P: return kb.pKey.wasPressedThisFrame;
                case KeyCodeLite.C: return kb.cKey.wasPressedThisFrame;
                case KeyCodeLite.M: return kb.mKey.wasPressedThisFrame;
                case KeyCodeLite.N: return kb.nKey.wasPressedThisFrame;
                case KeyCodeLite.L: return kb.lKey.wasPressedThisFrame;
            }
            return false;
#else
            switch (k)
            {
                case KeyCodeLite.Alpha1: return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                case KeyCodeLite.Alpha2: return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
                case KeyCodeLite.Alpha3: return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
                case KeyCodeLite.Alpha4: return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
                case KeyCodeLite.Escape: return Input.GetKeyDown(KeyCode.Escape);
                case KeyCodeLite.R: return Input.GetKeyDown(KeyCode.R);
                case KeyCodeLite.Space: return Input.GetKeyDown(KeyCode.Space);
                case KeyCodeLite.F: return Input.GetKeyDown(KeyCode.F);
                case KeyCodeLite.H: return Input.GetKeyDown(KeyCode.H);
                case KeyCodeLite.P: return Input.GetKeyDown(KeyCode.P);
                case KeyCodeLite.C: return Input.GetKeyDown(KeyCode.C);
                case KeyCodeLite.M: return Input.GetKeyDown(KeyCode.M);
                case KeyCodeLite.N: return Input.GetKeyDown(KeyCode.N);
                case KeyCodeLite.L: return Input.GetKeyDown(KeyCode.L);
            }
            return false;
#endif
        }
            /// <summary>Camera pan from WASD / arrow keys (held). x = right, y = forward.</summary>
        public static Vector2 PanAxis
        {
            get
            {
                Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb == null) return v;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
#else
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
#endif
                return v;
            }
        }

        /// <summary>Mouse wheel this frame, clamped to -1..1 (positive = scroll up).</summary>
        public static float ScrollDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current == null) return 0f;
                float y = Mouse.current.scroll.ReadValue().y;
                return Mathf.Clamp(y, -1f, 1f); // raw units vary by platform (often 120/notch), so just clamp
#else
                return Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f);
#endif
            }
        }
    }
}
