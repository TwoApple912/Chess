using System;
using System.Collections;
using Cinemachine;
using UnityEngine;

public class ChangeCameraProperty : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float iPadFOV = 12;
    [Space]
    [SerializeField] private bool changeFOVAfterEndGame;
    [SerializeField] private float ipadFOVAfterEndGame = 38;

    [Header("References")]
    private CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    void Start()
    {
        if (EndGameManager.Instance != null) EndGameManager.Instance.onEndGame += ChangeCameraFOVOnEndGame;

        if (Application.platform == RuntimePlatform.IPhonePlayer) virtualCamera.m_Lens.FieldOfView = iPadFOV;
    }

    void ChangeCameraFOVOnEndGame()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer && changeFOVAfterEndGame)
            StartCoroutine(ChangeFOVOverTime(ipadFOVAfterEndGame));
    }
    
    IEnumerator ChangeFOVOverTime(float newValue)
    {
        float startValue = virtualCamera.m_Lens.FieldOfView;
        float elapsedTime = 0f;

        while (elapsedTime < 0.12f)
        {
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(startValue, newValue, elapsedTime / 0.12f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        virtualCamera.m_Lens.FieldOfView = newValue;
    }
}