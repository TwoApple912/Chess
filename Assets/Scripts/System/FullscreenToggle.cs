using UnityEngine;

public class FullscreenToggle : MonoBehaviour
{
    private Vector2Int previousResolution;
    
    void Start()
    {
        previousResolution = new Vector2Int(Screen.width, Screen.height);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11)) ToggleFullscreen();
    }
    
    void ToggleFullscreen()
    {
        if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
        {
            Screen.SetResolution(previousResolution.x, previousResolution.y, FullScreenMode.Windowed);
        }
        else
        {
            previousResolution = new Vector2Int(Screen.width, Screen.height);
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        }
    }
}
