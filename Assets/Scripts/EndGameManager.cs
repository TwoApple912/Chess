using TMPro;
using UnityEngine;

public class EndGameManager : MonoBehaviour
{
    public static EndGameManager Instance;

    public enum WinType { Checkmate, Resignation, TimeOut }
    public enum DrawType { Stalemate, ThreefoldRepetition, FiftyMoveRule, InsufficientMaterial, MutualAgreement }
    
    [Header("Window Elements")]
    [SerializeField] private GameObject endGameWindow;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        if (!endGameWindow) endGameWindow = GameObject.Find("Canvas/End Game Window");
    }

    private void Start()
    {
        endGameWindow.SetActive(false);
    }

    public void WinEndGame(WinType winType, ChessPieceTeam winningTeam)
    {
        EndGame();
        
        string teamName = winningTeam == ChessPieceTeam.White ? "White" : "Black";
        string winTypeString = winType == WinType.Checkmate ? "Checkmate" :
            winType == WinType.Resignation ? "Resignation" : "Time Out";
        
        title.text = $"{teamName} Won!";
        description.text = $"by {winTypeString}";
        
        Debug.Log($"{teamName} Won! by {winTypeString}");
    }

    public void DrawEndGame(DrawType drawType)
    {
        EndGame();

        string drawTypeString = drawType == DrawType.Stalemate ? "Stalemate" :
            drawType == DrawType.ThreefoldRepetition ? "Threefold Repetition" :
            drawType == DrawType.FiftyMoveRule ? "Fifty Move Rule" :
            drawType == DrawType.InsufficientMaterial ? "Insufficient Material" : "Agreement";
        
        title.text = "Draw!";
        description.text = $"by {drawTypeString}";
        
        Debug.Log($"{title.text} {description.text}");
    }

    void EndGame()
    {
        ChessManager.Instance.gameEnded = true;
        
        // Call the end game screen
        endGameWindow.SetActive(true);
        title = endGameWindow.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        description = endGameWindow.transform.Find("Description").GetComponent<TextMeshProUGUI>();
        
        // Apply camera offset
        CameraManager.Instance.TriggerEndGameCamera();
        
        // Other system stuff
        GameTimerManager.Instance.StopTimer();
    }
}