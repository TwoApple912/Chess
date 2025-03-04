using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuManager : MonoBehaviour
{
    [SerializeField] private int currentMenuIndex = 0;
    [Space]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Piece Spawning")]
    [SerializeField] private float initialDelay = 3f;
    [SerializeField] private Vector3 pawnPosition = new Vector3(-1.5f, 0, 1.5f);
    [SerializeField] private Vector3 pieceSpawnPosition = new Vector3(0.5f, 0f, -0.5f);
    [Space]
    [SerializeField] private Vector3 carrierOffset = new Vector3(-0.2f, 0f, 0.15f);
    [SerializeField] private Vector3 adjunctOffset = new Vector3(0.25f, 0f, -0.2f);
    [SerializeField] private float adjunctSize = 0.85f;
    [SerializeField] private GameObject[] chessPiecesPrefab;
    
    private ChessPiece pawn;
    
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera startMenuCamera;
    [SerializeField] private CinemachineVirtualCamera newGameCamera;
    [SerializeField] private CinemachineVirtualCamera gameSceneCamera;
    
    private void Awake()
    {
        if (!pawn) pawn = GameObject.Find("Pawn").GetComponent<ChessPiece>();
        
        if (chessPiecesPrefab == null || chessPiecesPrefab.Length == 0)
            chessPiecesPrefab = new GameObject[4]
            {
                Resources.Load<GameObject>("Prefabs/Chess Pieces/Bishop"),
                Resources.Load<GameObject>("Prefabs/Chess Pieces/Knight"),
                Resources.Load<GameObject>("Prefabs/Chess Pieces/Rook"),
                Resources.Load<GameObject>("Prefabs/Chess Pieces/Queen")
            };
        
        if (!startMenuCamera) startMenuCamera = GameObject.Find("Start Menu Camera").GetComponent<CinemachineVirtualCamera>();
        if (!newGameCamera) newGameCamera = GameObject.Find("New Game Camera").GetComponent<CinemachineVirtualCamera>();
        if (!gameSceneCamera) gameSceneCamera = GameObject.Find("Game Scene Camera").GetComponent<CinemachineVirtualCamera>();
    }
    
    void Start()
    {
        StartCoroutine(PieceSequence());
    }

    #region Start Menu Buttons
    
    public void NewGame()
    {
        if (currentMenuIndex != 0) return;
        
        currentMenuIndex++;
        
        newGameCamera.Priority = 1;
    }

    public void OpenRules()
    {
        if (currentMenuIndex != 0) return;
        
        MenuManager.OpenRules();
    }
    
    public void QuitGame()
    {
        if (currentMenuIndex != 0) return;
        
        MenuManager.QuitGame();
    }
    
    #endregion
    
    #region New Game Buttons

    public void NGClassicMode()
    {
        if (currentMenuIndex != 1) return;
        
        StartCoroutine(LoadGameScene());
    }

    public void NGWildMode()
    {
        if (currentMenuIndex != 1) return;
        
        StartCoroutine(LoadGameScene(true));
    }

    public void NGBack()
    {
        if (currentMenuIndex != 1) return;
        
        currentMenuIndex--;
        
        newGameCamera.Priority = -1;
    }

    #endregion
    
    IEnumerator LoadGameScene(bool isWildMode = false)
    {
        gameSceneCamera.Priority = 2;

        yield return new WaitForSeconds(2.01f);
        SceneManager.LoadScene(gameSceneName);
    }
    
    #region Piece Movement Logic

    IEnumerator PieceSequence()
    {
        ChessPiece cp =
            Instantiate(chessPiecesPrefab[Random.Range(0, chessPiecesPrefab.Length)], pieceSpawnPosition,
                Quaternion.identity).GetComponent<ChessPiece>();

        yield return new WaitForSeconds(initialDelay);
        
        cp.AnimatedMovePieceTo(pawnPosition + carrierOffset, 1.75f);
        pawn.AnimatedMovePieceTo(pawnPosition + adjunctOffset, 1.5f);
    }
    
    #endregion
}