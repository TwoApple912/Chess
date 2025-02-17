using System.Collections;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    
    [Header("Parameters")]
    [SerializeField] private float rotationDuration = 0.5f;
    
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera whiteCamera;
    [SerializeField] private Animator whiteCameraAnimator;
    [SerializeField] private CinemachineVirtualCamera blackCamera;
    [SerializeField] private Animator blackCameraAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        if (!whiteCamera) whiteCamera = GameObject.Find("White Camera").GetComponent<CinemachineVirtualCamera>();
        if (!whiteCameraAnimator) whiteCameraAnimator = whiteCamera.GetComponent<Animator>();
        if (!blackCamera) blackCamera = GameObject.Find("Black Camera").GetComponent<CinemachineVirtualCamera>();
        if (!blackCameraAnimator) blackCameraAnimator = blackCamera.GetComponent<Animator>();
    }

    public void SwitchCamera()
    {
        if (whiteCamera.Priority > blackCamera.Priority)
        {
            blackCamera.Priority = whiteCamera.Priority;
            whiteCamera.Priority -= 1;
        }
        else
        {
            whiteCamera.Priority = blackCamera.Priority;
            blackCamera.Priority -= 1;
        }
    }

    public void TriggerEndGameCamera()
    {
        whiteCameraAnimator.SetTrigger("Game Ended");
        blackCameraAnimator.SetTrigger("Game Ended");
    }
}