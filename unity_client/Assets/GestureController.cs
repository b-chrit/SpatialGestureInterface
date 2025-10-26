using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // ✅ new input system

public class GestureController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector2 startPos;
    private float lastSwipeUpTime = 0f; // track for double-swipe up
    public UIManager uiManager;

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Subtle tilt feedback while swiping
        Vector2 delta = eventData.position - startPos;
        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
            uiManager.titleText.transform.localRotation = Quaternion.Euler(delta.y * 0.05f, 0, 0);
        else
            uiManager.titleText.transform.localRotation = Quaternion.Euler(0, -delta.x * 0.05f, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 endPos = eventData.position;
        Vector2 delta = endPos - startPos;
        uiManager.titleText.transform.localRotation = Quaternion.identity; // Reset tilt

        // Vertical swipe
        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
        {
            if (delta.y > 100f)
            {
                // Detect double-swipe up (within 0.5 seconds)
                if (Time.time - lastSwipeUpTime < 0.5f)
                {
                    uiManager.ShowQuickActions();
                    lastSwipeUpTime = 0f;
                }
                else
                {
                    uiManager.ShowMessages();
                    lastSwipeUpTime = Time.time;
                }
            }
            else if (delta.y < -100f)
                uiManager.ShowHome();
        }
        // Horizontal swipe
        else
        {
            if (delta.x < -100f)
                uiManager.ShowSettings();
            else if (delta.x > 100f)
                uiManager.ShowNotifications();
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // ✅ Simulate pinch gesture (ConfirmSelection)
        if (Keyboard.current.pKey.wasPressedThisFrame)
            uiManager.ConfirmSelection();

        // ✅ Simulate rotation gesture (AdjustControl)
        if (Keyboard.current.rKey.isPressed)
        {
            float amount = Mathf.PingPong(Time.time * 0.2f, 1f);
            uiManager.AdjustControl(amount);
        }

        // ✅ Double-tap toggle (press D twice fast)
        if (Keyboard.current.dKey.wasPressedThisFrame)
            uiManager.HandleDoubleTap();
    }
}
