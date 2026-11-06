using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Thin wrapper over the Input System package for mouse and touch.
/// Pointer.current is the last used pointer device (mouse, touch or pen),
/// so one code path covers both PC testing and phones.
/// </summary>
public static class PointerInput
{
    public static Vector2 Position =>
        Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

    public static bool PressedThisFrame =>
        Pointer.current != null && Pointer.current.press.wasPressedThisFrame;

    public static bool ReleasedThisFrame =>
        Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;
}
