using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    public bool gamePaused = false;
    
    [Header("References")]
    [SerializeField] private string startMenuSceneName = "Start Menu";
    [Space]
    [SerializeField] private CanvasGroup canvasGroup;
    [Space]
    [SerializeField] private GameObject drawOffer;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (!canvasGroup) canvasGroup = GameObject.Find("Canvas").GetComponent<CanvasGroup>();
        
        if (!drawOffer) drawOffer = GameObject.Find("Canvas/Draw Offer");
    }
    
    private void OnDestroy()
    {
        RulesCanvas.Instance.onRulesClosed -= OnRuleClosed;
    }

    void Start()
    {
        RulesCanvas.Instance.onRulesClosed += OnRuleClosed;
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        gamePaused = true;
        
        GameTimerManager.Instance.StopTimer();
    }
    
    public void ResumeGame()
    {
        Time.timeScale = 1;
        gamePaused = false;
        
        GameTimerManager.Instance.StartTimer();
    }
    
    public void NewGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void UndoMove()
    {
        ChessManager.Instance.UndoMove();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(startMenuSceneName);
    }

    public static void QuitGame()
    {
        Application.Quit();
    }

    public void OfferDraw()
    {
        ChessManager.Instance.DrawOfferAccepted();
        drawOffer.SetActive(true);
    }

    public void Resign()
    {
        EndGameManager.Instance.WinEndGame(EndGameManager.WinType.Resignation,
            ChessManager.Instance.CurrentTurn == ChessPieceTeam.White ? ChessPieceTeam.Black : ChessPieceTeam.White);
    }

    public void OpenRules()
    {
        RulesCanvas.Instance.ShowRules();

        StartCoroutine(MenuManager.AlphaFadeCanvasGroup(canvasGroup, 0.01f, 1));
    }

    void OnRuleClosed()
    {
        StartCoroutine(MenuManager.AlphaFadeCanvasGroup(canvasGroup, 1f, 1));
    }
    
    public static IEnumerator AlphaFadeCanvasGroup(CanvasGroup cg ,float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float time = 0;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        cg.alpha = targetAlpha;
    }
}
