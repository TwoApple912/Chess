using System;
using Cinemachine;
using UnityEngine;

public class RulesCanvas : MonoBehaviour
{
    public static RulesCanvas Instance;

    [SerializeField] private bool isOpen;
    public bool IsOpen => isOpen;
    
    [Header("Parameter")]
    [SerializeField] private float fadeDuration = 1f;
    
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CinemachineVirtualCamera rulesCamera;

    private Coroutine currentCoroutine;
    public event Action onRulesOpened;
    public event Action onRulesClosed;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!rulesCamera) rulesCamera = GetComponentInChildren<CinemachineVirtualCamera>();
    }

    void Start()
    {
        canvasGroup.alpha = 0;
        rulesCamera.Priority = -1;
    }

    public void ShowRules()
    {
        onRulesOpened?.Invoke();
        
        isOpen = true;
        rulesCamera.Priority = 10;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(MenuManager.AlphaFadeCanvasGroup(canvasGroup, 1, fadeDuration));
    }
    
    public void CloseRules()
    {
        Debug.Log("CloseRules called");
        if (onRulesClosed != null)
        {
            Debug.Log("assigned");
            onRulesClosed.Invoke();
        }
        else Debug.Log("unassigned");
        
        isOpen = false;
        rulesCamera.Priority = -1;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(MenuManager.AlphaFadeCanvasGroup(canvasGroup, 0, fadeDuration));
    }
}