using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class NodeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string tooltipText;
    private static GameObject tooltipObject;
    
    private void Awake()
    {
        // Create tooltip object if it doesn't exist
        if (tooltipObject == null)
        {
            tooltipObject = new GameObject("NodeTooltip");
            tooltipObject.transform.SetParent(GameObject.Find("Canvas").transform);
            
            // Add background image
            var image = tooltipObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // Add text
            var textObj = new GameObject("TooltipText");
            textObj.transform.SetParent(tooltipObject.transform);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 16;
            text.color = Color.white;
            
            // Size and position
            var rectTransform = tooltipObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 40);
            
            var textRectTransform = textObj.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = new Vector2(5, 5);
            textRectTransform.offsetMax = new Vector2(-5, -5);
            
            // Hide initially
            tooltipObject.SetActive(false);
        }
    }
    
    public void SetTooltipText(string text)
    {
        tooltipText = text;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipObject != null)
        {
            // Set position near the mouse
            tooltipObject.transform.position = eventData.position + new Vector2(20, 20);
            
            // Set text
            var text = tooltipObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = tooltipText;
            
            // Show tooltip
            tooltipObject.SetActive(true);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }
}