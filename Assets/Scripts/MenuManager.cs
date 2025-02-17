using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    public bool gamePaused = false;
    
    [Header("References")]
    [SerializeField] private GameObject drawOffer;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        if (!drawOffer) drawOffer = GameObject.Find("Canvas/Draw Offer");
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        gamePaused = true;
    }
    
    public void ResumeGame()
    {
        Time.timeScale = 1;
        gamePaused = false;
    }
    
    public void NewGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void UndoMove()
    {
        ChessManager.Instance.UndoMove();
        ChessManager.Instance.UndoMove();
    }

    public void ReturnToMainMenu()
    {
        
    }

    public void QuitGame()
    {
        
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
        
    }
}
