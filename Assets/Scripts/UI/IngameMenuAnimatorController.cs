using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class IngameMenuAnimatorController : MonoBehaviour, IPointerExitHandler
{
    [SerializeField] private bool isMenuOpen;

    [Header("Parameters")]
    [SerializeField] private float delayStep = 0.025f;
    
    private Vector2 originalPanelPosition;
    private Vector2 targetPanelPosition;

    [Header("References")]
    [SerializeField] private Animator ambience;
    [SerializeField] private RectTransform menuIcon;
    [SerializeField] private Animator menuIconAnimator;
    [SerializeField] private List<Animator> buttonsAnimators;
    [SerializeField] private List<Animator> gameplayButtonAnimators;
    [SerializeField] private Animator pauseButtonAnimator;
    [SerializeField] private Animator resumeButtonAnimator;
    [SerializeField] private Animator pauseGradientAnimator;

    private bool previousGamePausedState;
    
    private void Awake()
    {
        if (!ambience) ambience = GetComponent<Animator>();
        if (!menuIcon) menuIcon = transform.Find("Icon").GetComponent<RectTransform>();
        if (!menuIconAnimator) menuIconAnimator = menuIcon.GetComponent<Animator>();
        buttonsAnimators = new List<Animator>(transform.GetComponentsInChildren<Animator>());
        if (!pauseButtonAnimator) pauseButtonAnimator = transform.Find("Pause Button").GetComponent<Animator>();
        if (!resumeButtonAnimator) resumeButtonAnimator = transform.Find("Resume Button").GetComponent<Animator>();
        if (!pauseGradientAnimator) pauseGradientAnimator = transform.parent.Find("Pause Gradient").GetComponent<Animator>();
        
        if (buttonsAnimators.Contains(menuIconAnimator)) buttonsAnimators.Remove(menuIconAnimator);
        if (buttonsAnimators.Contains(resumeButtonAnimator)) buttonsAnimators.Remove(resumeButtonAnimator);
    }

    private void Update()
    {
        UpdateAnimatorWhenGamePause();
    }

    void OnPointerEnter()
    {
        if (!isMenuOpen && !ChessManager.Instance.gameEnded) OpenMenu();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (isMenuOpen) CloseMenu();
    }

    void OpenMenu()
    {
        isMenuOpen = true;
        StartCoroutine(AnimateMenuElement());
        ambience.SetBool("isMenuOpen", isMenuOpen);
    }

    void CloseMenu()
    {
        isMenuOpen = false;
        StartCoroutine(AnimateMenuElement());
        ambience.SetBool("isMenuOpen", isMenuOpen);
    }

    IEnumerator AnimateMenuElement()
    {
        menuIconAnimator.SetBool("isMenuOpen", isMenuOpen);
        foreach (var animator in buttonsAnimators)
        {
            animator.SetBool("isMenuOpen", isMenuOpen);
            yield return new WaitForSeconds(delayStep);
        }
    }

    void UpdateAnimatorWhenGamePause()
    {
        bool currentGamePausedState = MenuManager.Instance.gamePaused;
        if (currentGamePausedState != previousGamePausedState)
        {
            pauseButtonAnimator.SetBool("Game Paused", currentGamePausedState);
            resumeButtonAnimator.SetBool("Game Paused", currentGamePausedState);
            foreach (var animators in gameplayButtonAnimators) animators.SetBool("Game Paused", currentGamePausedState);
            pauseGradientAnimator.SetBool("Game Paused", currentGamePausedState);
            
            previousGamePausedState = currentGamePausedState;
        }
    }
}