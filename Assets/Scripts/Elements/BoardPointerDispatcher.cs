using UnityEngine;

/// <summary>
/// Replaces the legacy OnMouseDown / OnMouseUp messages, which do not work when
/// the project uses the Input System package only. Created automatically after
/// every scene load; it routes presses on 2D colliders to ElementController and
/// SpecialElements (mouse, touch and Device Simulator all go through PointerInput).
/// </summary>
public class BoardPointerDispatcher : MonoBehaviour
{
    private ElementController pressedElement;
    private SpecialElements pressedSpecial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<BoardPointerDispatcher>() != null)
            return;

        var go = new GameObject("BoardPointerDispatcher");
        DontDestroyOnLoad(go);
        go.AddComponent<BoardPointerDispatcher>();
    }

    private void Update()
    {
        if (PointerInput.PressedThisFrame)
            HandlePress();

        if (PointerInput.ReleasedThisFrame)
            HandleRelease();
    }

    private void HandlePress()
    {
        pressedElement = null;
        pressedSpecial = null;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector2 world = cam.ScreenToWorldPoint((Vector3)PointerInput.Position);

        foreach (Collider2D col in Physics2D.OverlapPointAll(world))
        {
            if (col.TryGetComponent(out ElementController element))
            {
                pressedElement = element;
                element.PointerDown();
                return;
            }

            if (col.TryGetComponent(out SpecialElements special))
            {
                pressedSpecial = special;
                special.PointerDown();
                return;
            }
        }
    }

    private void HandleRelease()
    {
        // Like OnMouseUp: goes to the object that was pressed, wherever the pointer is now.
        if (pressedElement != null)
            pressedElement.PointerUp();

        pressedElement = null;
        pressedSpecial = null;
    }
}
