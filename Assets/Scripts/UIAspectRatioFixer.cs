using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class UIAspectRatioFixer : MonoBehaviour
{
    // Attach this to your main Canvas in Scene 0 and Scene 1
    void Start()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // For 21:9, we usually want to match the height (1) 
            // so things don't get cut off vertically, but horizontal space expands.
            scaler.matchWidthOrHeight = 1.0f; 
        }
    }
}
