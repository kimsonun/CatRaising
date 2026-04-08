using UnityEngine;

public class UIShineSweep : MonoBehaviour
{
    public float speed = 400f;
    public float resetPosition = -150f;
    public float endPosition = 150f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Move the shine across the icon
        rectTransform.anchoredPosition += new Vector2(speed * Time.deltaTime, 0);

        // Reset to start when it goes off-screen
        if (rectTransform.anchoredPosition.x > endPosition)
        {
            rectTransform.anchoredPosition = new Vector2(resetPosition, 0);
        }
    }
}