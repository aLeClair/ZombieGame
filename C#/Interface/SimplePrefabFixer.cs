#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Simple utility to fix the NodeButton prefab
/// </summary>
public class SimplePrefabFixer : MonoBehaviour
{
    [Header("Drag your NodeButton prefab here")]
    public GameObject nodeButtonPrefab;
    
    public void FixPrefab()
    {
        if (nodeButtonPrefab == null)
        {
            Debug.LogError("Please assign the NodeButton prefab!");
            return;
        }
        
        string prefabPath = AssetDatabase.GetAssetPath(nodeButtonPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("This doesn't seem to be a prefab asset. Please assign the prefab from your Project panel.");
            return;
        }
        
        // Create a temporary instance to modify
        GameObject instance = PrefabUtility.InstantiatePrefab(nodeButtonPrefab) as GameObject;
        
        // Fix the Symbol text component
        Transform symbolTrans = instance.transform.Find("Symbol");
        if (symbolTrans != null)
        {
            TextMeshProUGUI symbolText = symbolTrans.GetComponent<TextMeshProUGUI>();
            if (symbolText != null)
            {
                // Clear the text and increase font size
                symbolText.text = "⚔"; // Default symbol
                symbolText.fontSize = 24;
                symbolText.alignment = TextAlignmentOptions.Center;
                Debug.Log("Fixed Symbol text component");
            }
            else
            {
                Debug.LogWarning("Symbol object doesn't have TextMeshProUGUI component");
            }
        }
        
        // Fix the Highlight GameObject
        Transform highlightTrans = instance.transform.Find("Highlight");
        if (highlightTrans != null)
        {
            // Make sure it's not active by default
            highlightTrans.gameObject.SetActive(false);
            
            // Fix the Image component
            Image highlightImage = highlightTrans.GetComponent<Image>();
            if (highlightImage != null)
            {
                highlightImage.color = Color.yellow;
            }
            
            Debug.Log("Fixed Highlight component");
        }
        
        // Save changes back to the prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);
        
        Debug.Log("Prefab saved successfully!");
    }
}
#endif