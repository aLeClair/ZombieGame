using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

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
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                tooltipObject.transform.SetParent(canvas.transform);
            }
            else
            {
                Debug.LogError("No Canvas found for NodeTooltip!");
                return;
            }
            
            // Add background image
            var image = tooltipObject.AddComponent<Image>();
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
            rectTransform.sizeDelta = new Vector2(200, 80);
            
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
            // Find the TextMeshProUGUI component in the tooltip's children
            TextMeshProUGUI tooltipTextComponent = tooltipObject.GetComponentInChildren<TextMeshProUGUI>();
            if (tooltipTextComponent != null)
            {
                tooltipTextComponent.text = tooltipText;
                
                // Adjust tooltip size based on text content
                RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
                float width = Mathf.Max(200, tooltipTextComponent.preferredWidth + 20);
                float height = Mathf.Max(80, tooltipTextComponent.preferredHeight + 20);
                tooltipRect.sizeDelta = new Vector2(width, height);
            }
            
            // Set position near the mouse but ensure it stays on screen
            Vector2 position = eventData.position + new Vector2(20, 20);
            RectTransform canvasRect = tooltipObject.transform.parent.GetComponent<RectTransform>();
            
            if (position.x + tooltipObject.GetComponent<RectTransform>().sizeDelta.x > canvasRect.sizeDelta.x)
            {
                position.x = eventData.position.x - tooltipObject.GetComponent<RectTransform>().sizeDelta.x - 20;
            }
            
            if (position.y + tooltipObject.GetComponent<RectTransform>().sizeDelta.y > canvasRect.sizeDelta.y)
            {
                position.y = eventData.position.y - tooltipObject.GetComponent<RectTransform>().sizeDelta.y - 20;
            }
            
            tooltipObject.transform.position = position;
            
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