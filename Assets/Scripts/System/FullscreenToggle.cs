using UnityEngine;

public class FullscreenToggle : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ToggleFullscreen();
        }
    }
    
    void ToggleFullscreen()
    {
        if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow) Screen.fullScreenMode = FullScreenMode.Windowed;
        else Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }
}
