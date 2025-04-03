using UnityEngine;

public class MapCamera : MonoBehaviour
{
    private void Awake()
    {
        // Set up camera for UI rendering
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Only render UI layer
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.depth = 0;
            cam.useOcclusionCulling = false;
        }
    }
}