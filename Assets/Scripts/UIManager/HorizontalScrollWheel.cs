using UnityEngine;
using UnityEngine.UI;

public class HorizontalScrollWheel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    private float scrollSpeed = 60f;

    void Update()
    {
        // Get the mouse scroll wheel input (scroll is positive when scrolling up, negative when down)
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            // Adjust horizontal scroll position based on mouse wheel input
            scrollRect.horizontalNormalizedPosition += scroll * scrollSpeed * Time.deltaTime;
        }
    }
}