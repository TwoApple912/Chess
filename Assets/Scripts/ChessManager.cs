using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ChessManager : MonoBehaviour
{
    public static ChessManager Instance;
    public event Action OnTurnEnd;
    
    [Header("Game Configurations")]
    [SerializeField] private bool wildMode;
        public bool WildMode => wildMode;
    [SerializeField] private bool isChess960Mode;
        public bool IsChess960Mode => isChess960Mode;

    [Header("Game Tracker")]
    public ChessPiece[,] chessPieces;
    [SerializeField] private ChessPieceTeam currentTurn = ChessPieceTeam.White;
        public ChessPieceTeam CurrentTurn => currentTurn;
    [SerializeField] private bool currentTeamIsChecked;
        public bool CurrentTeamIsChecked => currentTeamIsChecked;
    [SerializeField] private bool otherTeamIsChecked;
        public bool OtherTeamIsChecked => otherTeamIsChecked;
    [Space]
    private List<Vector2Int> possibleMoves = new List<Vector2Int>();
    public Vector2Int? enPassantMove;
    [Space]
    [SerializeField] private bool whiteAgreeToDraw;
    [SerializeField] private bool blackAgreeToDraw;
    
    public bool gameEnded = false;

    [Header("Lists")]
    public List<ChessPiece> remainingWhitePieces;
    public List<ChessPiece> remainingBlackPieces;
    [Space]
    public List<ChessPiece> capturedWhitePieces;
    public List<ChessPiece> capturedBlackPieces;
    
    private ChessPiece whiteKing;
    private ChessPiece blackKing;
    
    [Header("Moves")]
    private List<Move> moveHistory = new List<Move>();

    private float whiteTimerAtTheStartOfTheTurn;
    private float blackTimerAtTheStartOfTheTurn;
    
    [Header("Other Trackers")]
    [SerializeField] private int haveMoveCounter = 0;
    [SerializeField] private Dictionary<string, int> boardStateCounter = new Dictionary<string, int>();
    
    [Space][Space]

    [Header("Tile Spawning")] [SerializeField]
    public int boardSize = 8;

    [SerializeField] private float tileSize = 1f;
    [SerializeField] private GameObject tilePrefab;
    public GameObject[,] tiles; // Tiles prefabs 2D array
    [Space]
    [SerializeField] private GameObject chessboardObject;
    [SerializeField] private float tileYOffset = 0.005f;

    [Header("Pieces Spawning")]
    [SerializeField] private float spawnBaseDelay = 0.1f;

    [SerializeField] private AnimationCurve delayCurve;
    [SerializeField] private bool randomSpawnOrder;

    private bool isSpawningPieces = false;

    [Header("Player Actions")]
    [SerializeField] private GameObject hoveredObject;
    [SerializeField] private ChessPiece selectedPiece;
        public ChessPiece SelectedPiece { get { return selectedPiece; } }
    [Space]
    public bool isSelectingAttachedUnit;
    public bool isSelectedForDetaching;
    [SerializeField] private ChessPiece detachedPieceThatAreMovedAwayFrom;
    [Space]
    [SerializeField] private LayerMask selectableLayer;
    
    [Header("Drag and Drop Parameters")]
    [SerializeField] private bool isDragging = false;
    [SerializeField] private float pressTime = 0f;
    [SerializeField] private float pressThreshold = 0.2f;
    [Space]
    [SerializeField] private float pieceYOffset = 1f;

    [Header("Turn Parameters")]
    [SerializeField] private float switchPOVDelay = 0.3f;
    public List<Coroutine> activeMoveCoroutine = new List<Coroutine>();

    [Header("Promotion Parameters")]
    [SerializeField] private GameObject promotionPrefab;
    [SerializeField] private float promotionSpawningOffset = 3.25f;
    [SerializeField] private float promotionDelay = 0.5f;
    [Space]
    [SerializeField] private bool isPromoting;
        public bool IsPromoting => isPromoting;
    
    [Header("References")]
    [SerializeField] private CapturedPiecePlacer capturedPiecePlacerScript;
    [SerializeField] private GameObject[] chessPiecesPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (!tilePrefab) tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");
        if (!promotionPrefab) promotionPrefab = Resources.Load<GameObject>("Prefabs/Promotion Pack");

        if (!chessboardObject) chessboardObject = GameObject.Find("Chessboard");

        chessPieces = new ChessPiece[boardSize, boardSize];

        selectableLayer = LayerMask.GetMask("Interactive");
        
        capturedPiecePlacerScript = GetComponent<CapturedPiecePlacer>();
    }

    void Start()
    {
        ApplyConfigurations();
        
        GenerateTiles();
        if (!isChess960Mode) StartCoroutine(SpawnPiecesInTheDefaultLayout());
        else StartCoroutine(SpawnPiecesUsingTheChess960Rules());
    }

    private void Update()
    {
        if (gameEnded) return;
        
        Hover();
        HandleSelection();

        //if (isDragging && selectedPiece) DragPiece();
    }

    private Vector2Int LookUpPositionIndex(GameObject _object)
    {
        if (_object.CompareTag("Tile"))
        {
            for (int x = 0; x < boardSize; x++)
            for (int y = 0; y < boardSize; y++)
                if (_object == tiles[x, y])
                    return new Vector2Int(x, y);
        }
        else if (_object.GetComponent<ChessPiece>())
        {
            return new Vector2Int(_object.GetComponent<ChessPiece>().currentX,
                _object.GetComponent<ChessPiece>().currentY);
        }

        return -Vector2Int.one; // Return (-1, -1). Written for the sake of preventing errors.
    }
    
    public void SetPieceIntoBoard(ChessPiece piece, int x, int y)
    {
        chessPieces[x, y] = piece;
    }
    
    #region Start Game Codes

    void ApplyConfigurations()
    {
        wildMode = GameConfigurations.isWildMode;
        isChess960Mode = GameConfigurations.isChess960;
        
        Debug.Log($"Wild mode: {GameConfigurations.isWildMode}");
        Debug.Log($"Chess960: {GameConfigurations.isChess960}");
        Debug.Log($"{GameConfigurations.PlayerTimerMinute}|{GameConfigurations.PlayerIncrementSecond}");
    }
    
    #endregion

    #region Board Generation / Initial Game Setup

    void GenerateTiles()
    {
        Vector3 offset = new Vector3((boardSize * tileSize) / 2.0f - (tileSize / 2.0f), 0,
            (boardSize * tileSize) / 2.0f - (tileSize / 2.0f));

        tiles = new GameObject[boardSize, boardSize];
        for (int x = 0; x < boardSize; x++)
        for (int y = 0; y < boardSize; y++)
        {
            Vector3 position = new Vector3(x * tileSize, tileYOffset, y * tileSize) - offset;
            GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, chessboardObject.transform);
            tile.name = $"X:{x} Y:{y}";

            tiles[x, y] = tile;
        }
    }

    IEnumerator SpawnPiecesInTheDefaultLayout()
    {
        isSpawningPieces = true;

        // White pieces
        List<(ChessPieceType type, ChessPieceTeam team, int x, int y)> whitePiecesToSpawn =
            new List<(ChessPieceType, ChessPieceTeam, int, int)>
            {
                (ChessPieceType.King, ChessPieceTeam.White, 4, 0),
                (ChessPieceType.Queen, ChessPieceTeam.White, 3, 0),
                (ChessPieceType.Bishop, ChessPieceTeam.White, 5, 0),
                (ChessPieceType.Bishop, ChessPieceTeam.White, 2, 0),
                (ChessPieceType.Knight, ChessPieceTeam.White, 6, 0),
                (ChessPieceType.Knight, ChessPieceTeam.White, 1, 0),
                (ChessPieceType.Rook, ChessPieceTeam.White, 7, 0),
                (ChessPieceType.Rook, ChessPieceTeam.White, 0, 0),
            };

        int r = Random.Range(1, 2);
        if (r == 1)
        {
            whitePiecesToSpawn.AddRange(new List<(ChessPieceType, ChessPieceTeam, int, int)>
            {
                (ChessPieceType.Pawn, ChessPieceTeam.White, 0, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 1, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 2, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 3, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 4, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 5, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 6, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 7, 1)
            });
        }
        else
        {
            whitePiecesToSpawn.AddRange(new List<(ChessPieceType, ChessPieceTeam, int, int)>
            {
                (ChessPieceType.Pawn, ChessPieceTeam.White, 7, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 6, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 5, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 4, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 3, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 2, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 1, 1),
                (ChessPieceType.Pawn, ChessPieceTeam.White, 0, 1)
            });
        }

        // Black pieces
        List<(ChessPieceType type, ChessPieceTeam team, int x, int y)> blackPiecesToSpawn =
            new List<(ChessPieceType, ChessPieceTeam, int, int)>
            {
                (ChessPieceType.Queen, ChessPieceTeam.Black, 3, 7),
                (ChessPieceType.King, ChessPieceTeam.Black, 4, 7),
                (ChessPieceType.Bishop, ChessPieceTeam.Black, 2, 7),
                (ChessPieceType.Bishop, ChessPieceTeam.Black, 5, 7),
                (ChessPieceType.Knight, ChessPieceTeam.Black, 1, 7),
                (ChessPieceType.Knight, ChessPieceTeam.Black, 6, 7),
                (ChessPieceType.Rook, ChessPieceTeam.Black, 0, 7),
                (ChessPieceType.Rook, ChessPieceTeam.Black, 7, 7),
            };

        r = Random.Range(1, 2);
        if (r == 1)
        {
            blackPiecesToSpawn.AddRange(new List<(ChessPieceType, ChessPieceTeam, int, int)>
            {
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 0, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 1, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 2, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 3, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 4, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 5, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 6, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 7, 6)
            });
        }
        else
        {
            blackPiecesToSpawn.AddRange(new List<(ChessPieceType, ChessPieceTeam, int, int)>
            {
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 7, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 6, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 5, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 4, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 3, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 2, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 1, 6),
                (ChessPieceType.Pawn, ChessPieceTeam.Black, 0, 6)
            });
        }

        // Shuffle the list
        if (randomSpawnOrder)
        {
            for (int i = 0; i < whitePiecesToSpawn.Count; i++)
            {
                var temp = whitePiecesToSpawn[i];
                int randomIndex = Random.Range(i, whitePiecesToSpawn.Count);
                whitePiecesToSpawn[i] = whitePiecesToSpawn[randomIndex];
                whitePiecesToSpawn[randomIndex] = temp;
            }

            for (int i = 0; i < blackPiecesToSpawn.Count; i++)
            {
                var temp = blackPiecesToSpawn[i];
                int randomIndex = Random.Range(i, blackPiecesToSpawn.Count);
                blackPiecesToSpawn[i] = blackPiecesToSpawn[randomIndex];
                blackPiecesToSpawn[randomIndex] = temp;
            }
        }

        StartCoroutine(SpawnPieces(whitePiecesToSpawn));
        yield return StartCoroutine(SpawnPieces(blackPiecesToSpawn));
        EndInitialGameSetup();
    }

    IEnumerator SpawnPiecesUsingTheChess960Rules()
    {
        isSpawningPieces = true;
        List<(ChessPieceType type, ChessPieceTeam team, int x, int y)> whitePiecesToSpawn =
            new List<(ChessPieceType, ChessPieceTeam, int, int)>();
        List<(ChessPieceType type, ChessPieceTeam team, int x, int y)> blackPiecesToSpawn =
            new List<(ChessPieceType, ChessPieceTeam, int, int)>();
        
        // Generate back rank layout for Chess960
        ChessPieceType[] backRank = GenerateChess960BackRankLayout();

        for (int x = 0; x < 8; x++) whitePiecesToSpawn.Add((backRank[x], ChessPieceTeam.White, x, 0));
        for (int x = 0; x < 8; x++) blackPiecesToSpawn.Add((backRank[x], ChessPieceTeam.Black, x, 7));
        ListRandomizer.Shuffle(whitePiecesToSpawn);
        ListRandomizer.Shuffle(blackPiecesToSpawn);
        
        for (int x = 0; x < 8; x++) whitePiecesToSpawn.Add((ChessPieceType.Pawn, ChessPieceTeam.White, x, 1));
        for (int x = 0; x < 8; x++) blackPiecesToSpawn.Add((ChessPieceType.Pawn, ChessPieceTeam.Black, x, 6));
        
        StartCoroutine(SpawnPieces(whitePiecesToSpawn));
        yield return StartCoroutine(SpawnPieces(blackPiecesToSpawn));
        EndInitialGameSetup();
    }
    
    ChessPieceType[] GenerateChess960BackRankLayout()
    {
        // Array to store layout of 8 positions
        ChessPieceType[] layout = new ChessPieceType[8];
        List<int> positions = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

        System.Random rnd = new System.Random();
        
        // 1. Place bishops on opposite colors:
        // odd positions (assumed white squares: 1,3,5,7)
        List<int> oddIndices = new List<int> { 1, 3, 5, 7 };
        int bishopOddIndex = oddIndices[rnd.Next(oddIndices.Count)];
        layout[bishopOddIndex] = ChessPieceType.Bishop;
        positions.Remove(bishopOddIndex);
        
        // even positions (assumed dark squares: 0,2,4,6)
        List<int> evenIndices = new List<int> { 0, 2, 4, 6 };
        // Filter evenIndices to only available ones
        List<int> availableEven = evenIndices.FindAll(pos => positions.Contains(pos));
        int bishopEvenIndex = availableEven[rnd.Next(availableEven.Count)];
        layout[bishopEvenIndex] = ChessPieceType.Bishop;
        positions.Remove(bishopEvenIndex);
        
        // 2. Place queen in one of the 6 remaining positions:
        int queenPos = positions[rnd.Next(positions.Count)];
        layout[queenPos] = ChessPieceType.Queen;
        positions.Remove(queenPos);
        
        // 3. Place two knights randomly in the remaining 5 positions:
        for (int i = 0; i < 2; i++)
        {
            int knightPos = positions[rnd.Next(positions.Count)];
            layout[knightPos] = ChessPieceType.Knight;
            positions.Remove(knightPos);
        }
        
        // 4. Remaining 3 positions: sort them then assign Rook - King - Rook:
        positions.Sort();
        if (positions.Count == 3)
        {
            layout[positions[0]] = ChessPieceType.Rook;
            layout[positions[1]] = ChessPieceType.King;
            layout[positions[2]] = ChessPieceType.Rook;
        }
        
        return layout;
    }

    IEnumerator SpawnPieces(List<(ChessPieceType type, ChessPieceTeam team, int x, int y)> pieceToSpawn)
    {
        int totalPieces = pieceToSpawn.Count;

        for (int i = 0; i < totalPieces; i++)
        {
            float normalizedPosition = (float)i / (totalPieces - 1);
            float delayFactor = delayCurve.Evaluate(normalizedPosition); // Use the AnimationCurve
            float delay = spawnBaseDelay * delayFactor;

            SpawnSinglePiece(pieceToSpawn[i].type, pieceToSpawn[i].team, pieceToSpawn[i].x, pieceToSpawn[i].y);
            yield return new WaitForSeconds(delay);
        }
    }

    void SpawnSinglePiece(ChessPieceType type, ChessPieceTeam team, int xPosition, int yPosition)
    {
        ChessPiece cp = Instantiate(chessPiecesPrefab[(int)type]).GetComponent<ChessPiece>();

        cp.type = type;
        cp.chessPieceTeam = team;
        cp.SetMaterial();

        // This is partially the same as PerformMove()
        chessPieces[xPosition, yPosition] = cp;

        cp.transform.position = tiles[xPosition, yPosition].transform.position;
        cp.UpdateCoordinate(xPosition, yPosition);

        cp.transform.rotation = Quaternion.Euler(0, team == ChessPieceTeam.White ? 0 : 180, 0);
        
        if (team == ChessPieceTeam.White) remainingWhitePieces.Add(cp);
        else remainingBlackPieces.Add(cp);
    }

    void AssignKingVariable()
    {
        foreach (ChessPiece piece in chessPieces)
        {
            if (piece && piece.type == ChessPieceType.King)
            {
                if (piece.chessPieceTeam == ChessPieceTeam.White) whiteKing = piece;
                else blackKing = piece;
            }
        }
    }

    void EndInitialGameSetup()
    {
        isSpawningPieces = false;
        
        AssignKingVariable();
        
        NewTurnFunctions();
        GameTimerManager.Instance.StartTimer();
    }

    #endregion

    #region Turn System
    
    IEnumerator PreEndturnCheck()
    {
        // Promotion check
        List<ChessPiece> pawnsToPromote = new List<ChessPiece>();
        foreach (ChessPiece piece in chessPieces)
        {
            if (piece && (!piece.isAttached || wildMode) && piece is Pawn && piece.chessPieceTeam == currentTurn &&
                ((piece.chessPieceTeam == ChessPieceTeam.White && piece.currentY == boardSize - 1) ||
                 (piece.chessPieceTeam == ChessPieceTeam.Black && piece.currentY == 0)))
            {
                Debug.Log(piece);
                pawnsToPromote.Add(piece);
            }

            if (piece && piece.isAttached && wildMode && piece.attachedPiece is Pawn &&
                piece.attachedPiece.chessPieceTeam == currentTurn &&
                ((piece.attachedPiece.chessPieceTeam == ChessPieceTeam.White &&
                  piece.attachedPiece.currentY == boardSize - 1) ||
                 (piece.attachedPiece.chessPieceTeam == ChessPieceTeam.Black && piece.attachedPiece.currentY == 0)))
            {
                Debug.Log(piece.attachedPiece);
                pawnsToPromote.Add(piece.attachedPiece);
            }
        }
        
        foreach (ChessPiece pawn in pawnsToPromote)
        {
            if (!moveHistory.Last().isPromotion) moveHistory.Last().isPromotion = true;
            else moveHistory.Last().isPromotion1 = true;
            yield return StartCoroutine(PerformPromotion(pawn));
        }

        EndTurn();
    }

    void EndTurn()
    {
        otherTeamIsChecked = IsKingInDanger(chessPieces,
            currentTurn == ChessPieceTeam.White
                ? blackKing.GetCurrentCoordinate()
                : whiteKing.GetCurrentCoordinate(),
            currentTurn == ChessPieceTeam.White ? ChessPieceTeam.Black : ChessPieceTeam.White);
        
        // Check for end game condition
        if (IsKingInCheckmate(chessPieces, currentTurn == ChessPieceTeam.White ? blackKing : whiteKing))
        {
            EndGameManager.Instance.WinEndGame(EndGameManager.WinType.Checkmate, currentTurn);
        }
        else if (IsStalemate(chessPieces, currentTurn))
        {
            EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.Stalemate);
        }
        else if (IsInsufficientMaterial())
        {
            EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.InsufficientMaterial);
        }
        else if (IsMutualAgreement())
        {
            EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.MutualAgreement);
        }
        else if (haveMoveCounter >= 100)
        {
            EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.FiftyMoveRule);
        }
        else if (IsThreefoldRepetition())
        {
            EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.ThreefoldRepetition);
        }
        // If not, carry onto next turn
        else
        {
            OnTurnEnd?.Invoke();
            
            currentTurn = currentTurn == ChessPieceTeam.White ? ChessPieceTeam.Black : ChessPieceTeam.White;
            currentTeamIsChecked = IsKingInDanger(chessPieces,
                currentTurn == ChessPieceTeam.White
                    ? whiteKing.GetCurrentCoordinate()
                    : blackKing.GetCurrentCoordinate(), currentTurn);
            
            StartCoroutine(SwitchPOVDelay());
        }
    }

    IEnumerator SwitchPOVDelay(bool enableDelay = true)
    {
        foreach (var coroutine in activeMoveCoroutine) yield return coroutine;
        yield return new WaitForSeconds(enableDelay ? switchPOVDelay : 0);
        
        CameraManager.Instance.SwitchCamera();

        activeMoveCoroutine.Clear();
        
        NewTurnFunctions();
    }

    void NewTurnFunctions()
    {
        if (GameConfigurations.PlayerTimerMinute != 0)
        {
            whiteTimerAtTheStartOfTheTurn = GameTimerManager.Instance.whiteTimer;
            blackTimerAtTheStartOfTheTurn = GameTimerManager.Instance.blackTimer;
        } 
    }

    #endregion

    #region End Game Logic

    /*void WinEndGame(EndGameManager.WinType winType)
    {
        gameEnded = true;
        EndGameManager.Instance.WinEndGame(winType, currentTurn);
    }
    
    void DrawEndGame(EndGameManager.DrawType drawType)
    {
        gameEnded = true;
        EndGameManager.Instance.DrawEndGame(drawType);
    }
    
    void CheckmateWinEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.WinEndGame(EndGameManager.WinType.Checkmate, currentTurn);
        
    }
    
    void TimeoutWinEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.WinEndGame(EndGameManager.WinType.TimeOut,
            currentTurn == ChessPieceTeam.White ? ChessPieceTeam.Black : ChessPieceTeam.White);
        Debug.Log("Timeout win!");
    }
    
    public void ResignationWinEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.WinEndGame(EndGameManager.WinType.Resignation,
            currentTurn == ChessPieceTeam.White ? ChessPieceTeam.Black : ChessPieceTeam.White);
        Debug.Log("Resignation win!");
    }
    
    void StalemateDrawEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.Stalemate);
        Debug.Log("Draw by stalemate!");
    }

    void InsufficientMaterialDrawEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.InsufficientMaterial);
        Debug.Log("Draw by insufficient material!");
    }

    void MutualAgreementDrawEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.MutualAgreement);
        Debug.Log("Draw by mutual agreement!");
    }
    
    void FiftyMoveRuleDrawEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.FiftyMoveRule);
        Debug.Log("Draw by fifty move rule!");
    }
    
    void ThreefoldRepetitionDrawEndGame()
    {
        gameEnded = true;
        EndGameManager.Instance.DrawEndGame(EndGameManager.DrawType.ThreefoldRepetition);
        Debug.Log("Draw by threefold repetition!");
    }*/

    bool IsKingInCheckmate(ChessPiece[,] board, ChessPiece king)
    {
        if (!IsKingInDanger(board, new Vector2Int(king.currentX, king.currentY), king.chessPieceTeam))
            return false;

        List<ChessPiece> teamPieces = new List<ChessPiece>();
        for (int x = 0; x < boardSize; x++)
            for (int y = 0; y < boardSize; y++)
                if (board[x, y] != null && board[x, y].chessPieceTeam == king.chessPieceTeam)
                {
                    teamPieces.Add(board[x, y]);
                    if (board[x, y].isAttached)
                    {
                        teamPieces.Add(board[x, y].attachedPiece);
                    }
                }

        foreach (var piece in teamPieces)
        {
            List<Vector2Int> moves = piece.GetAvailableMoves(board, boardSize);
            foreach (Vector2Int move in moves)
            {
                ChessPiece[,] simulatedBoard = (ChessPiece[,])board.Clone();
                Vector2Int kingPosition = new Vector2Int(king.currentX, king.currentY);
                
                simulatedBoard[piece.currentX, piece.currentY] = null;
                simulatedBoard[move.x, move.y] = piece;

                if (piece.isAttached) simulatedBoard[piece.currentX, piece.currentY] = piece.attachedPiece;
                
                if (piece is King)
                {
                    kingPosition = new Vector2Int(move.x, move.y);
                }

                if (piece is Pawn && move == enPassantMove)
                {
                    simulatedBoard[move.x, move.y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)] = null;
                }

                if (piece is King && Mathf.Abs(move.x - piece.currentX) == 2)
                {
                    int rookX = (move.x == 2) ? 0 : boardSize - 1;
                    int newKingX = piece.currentX + (move.x == 2 ? -2 : 2);
                    int newRookX = piece.currentX + (move.x == 2 ? -1 : 1);

                    simulatedBoard[piece.currentX, piece.currentY] = null;
                    simulatedBoard[newKingX, piece.currentY] = piece;
                    simulatedBoard[rookX, piece.currentY] = null;
                    simulatedBoard[newRookX, piece.currentY] = chessPieces[rookX, piece.currentY];
                }

                if (piece is Pawn && Mathf.Abs(move.x - piece.currentX) == 1 && Mathf.Abs(move.y - piece.currentY) == 1)
                {
                    simulatedBoard[move.x, move.y] = piece;
                    simulatedBoard[move.x, move.y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)] = null;
                }

                if (!IsKingInDanger(simulatedBoard, kingPosition, king.chessPieceTeam)) return false;
            }
        }

        return true;
    }

    bool IsStalemate(ChessPiece[,] board, ChessPieceTeam team)
    {
        ChessPieceTeam opponentTeam = (team == ChessPieceTeam.White) ? ChessPieceTeam.Black : ChessPieceTeam.White;
        Vector2Int kingPosition = opponentTeam == ChessPieceTeam.White
            ? whiteKing.GetCurrentCoordinate()
            : blackKing.GetCurrentCoordinate();

        List<ChessPiece> opponentPieces = new List<ChessPiece>();
        for (int x = 0; x < boardSize; x++)
        for (int y = 0; y < boardSize; y++)
        {
            if (board[x, y] != null && board[x, y].chessPieceTeam == opponentTeam)
            {
                opponentPieces.Add(board[x, y]);
                if (board[x, y].isAttached) opponentPieces.Add(board[x, y].attachedPiece);
            }
        }

        foreach (ChessPiece piece in opponentPieces)
        {
            List<Vector2Int> moves = piece.GetAvailableMoves(board, boardSize);

            foreach (var move in moves)
            {
                ChessPiece[,] simulatedBoard = (ChessPiece[,])chessPieces.Clone();

                simulatedBoard[piece.currentX, piece.currentY] = piece.isAttached ? piece.attachedPiece : null;
                simulatedBoard[move.x, move.y] = piece;

                if (piece is King)
                {
                    kingPosition = new Vector2Int(move.x, move.y);
                }

                if (piece is Pawn && move == enPassantMove) // En Passant
                {
                    simulatedBoard[move.x, move.y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)] = null;
                }

                if (piece is King && Mathf.Abs(move.x - piece.currentX) == 2) // Castling
                {
                    int rookX = (move.x == 2) ? 0 : boardSize - 1;
                    int newKingX = piece.currentX + (move.x == 2 ? -2 : 2);
                    int newRookX = piece.currentX + (move.x == 2 ? -1 : 1);

                    simulatedBoard[piece.currentX, piece.currentY] = null;
                    simulatedBoard[newKingX, piece.currentY] = piece;
                    simulatedBoard[rookX, piece.currentY] = null;
                    simulatedBoard[newRookX, piece.currentY] = chessPieces[rookX, piece.currentY];
                }

                if (piece is Pawn && Mathf.Abs(move.x - piece.currentX) == 1 &&
                    Mathf.Abs(move.y - piece.currentY) == 1) // Pawn Capture
                {
                    simulatedBoard[move.x, move.y] = piece;
                    simulatedBoard[move.x, move.y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)] = null;
                }

                if (!IsKingInDanger(simulatedBoard, kingPosition, opponentTeam))
                {
                    //Debug.Log("Not Stalemate " + piece + " " + move);
                    return false;
                }
            }
        }

        return true;
    }

    bool IsInsufficientMaterial()
    {
        // King vs King
        if (remainingWhitePieces.Count == 1 && remainingBlackPieces.Count == 1)
            return true;
        
        // King vs King + Bishop
        if (remainingWhitePieces.Count == 2 && remainingBlackPieces.Count == 1 &&
            remainingWhitePieces.Exists(p => p.type == ChessPieceType.Bishop))
            return true;
        if (remainingBlackPieces.Count == 2 && remainingWhitePieces.Count == 1 &&
            remainingBlackPieces.Exists(p => p.type == ChessPieceType.Bishop))
            return true;
        
        // King vs King + Knight
        if (remainingWhitePieces.Count == 2 && remainingBlackPieces.Count == 1 &&
            remainingWhitePieces.Exists(p => p.type == ChessPieceType.Knight))
            return true;
        if (remainingBlackPieces.Count == 2 && remainingWhitePieces.Count == 1 &&
            remainingBlackPieces.Exists(p => p.type == ChessPieceType.Knight))
            return true;
        
        // King + Bishop vs King + Bishop (Bishops on the same color)
        if (remainingWhitePieces.Count == 2 && remainingBlackPieces.Count == 2 &&
            remainingWhitePieces.Exists(p => p.type == ChessPieceType.Bishop) &&
            remainingBlackPieces.Exists(p => p.type == ChessPieceType.Bishop))
        {
            ChessPiece whiteBishop = remainingWhitePieces.Find(p => p.type == ChessPieceType.Bishop);
            ChessPiece blackBishop = remainingBlackPieces.Find(p => p.type == ChessPieceType.Bishop);

            if ((whiteBishop.currentX + whiteBishop.currentY) % 2 == (blackBishop.currentX + blackBishop.currentY) % 2)
                return true;
        }

        return false;
    }

    bool IsMutualAgreement()
    {
        if (whiteAgreeToDraw && blackAgreeToDraw) return true;
        return false;
    }

    bool IsThreefoldRepetition()
    {
        string boardState = GetBoardState();
        return boardStateCounter.ContainsKey(boardState) && boardStateCounter[boardState] >= 3;
    }
    string GetBoardState()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null)
                {
                    sb.Append(piece.chessPieceTeam == ChessPieceTeam.White ? "W" : "B");
                    sb.Append((int)piece.type);
                }
                else
                {
                    sb.Append("00");
                }
            }
        }
        return sb.ToString();
    }

    #endregion

    #region Player Actions

    void Hover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
        {
            hoveredObject = hit.transform.gameObject;
        }
        else hoveredObject = null;
    }

    void HandleSelection()
    {
        if (isSpawningPieces || isPromoting) return;
        
        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            if (hoveredObject) // Click logic (same as drag logic but slightly different)
            {
                if (!selectedPiece) // If haven't select any piece
                {
                    ChessPiece piece = hoveredObject.GetComponent<ChessPiece>();

                    if (piece && piece.chessPieceTeam == currentTurn) SelectPiece(piece);
                }
                else if (isSelectingAttachedUnit && hoveredObject.GetComponent<ChessPiece>() &&
                         selectedPiece.GetCarrierPiece() ==
                         hoveredObject.GetComponent<ChessPiece>()
                             .GetCarrierPiece()) // If selecting an attached piece within a selected unit
                {
                    isSelectedForDetaching = true;
                    ChessPiece piece = hoveredObject.GetComponent<ChessPiece>();
                    SelectPiece(piece);
                }
                else // If already selecting a piece
                {
                    if (hoveredObject.CompareTag("Tile") && possibleMoves.Contains(LookUpPositionIndex(hoveredObject)))
                    {
                        PerformMove(selectedPiece, LookUpPositionIndex(hoveredObject).x,
                            LookUpPositionIndex(hoveredObject).y);
                    }
                    else if (hoveredObject.GetComponent<ChessPiece>())
                    {
                        ChessPiece pressedPiece = hoveredObject.GetComponent<ChessPiece>();

                        if (pressedPiece.chessPieceTeam == selectedPiece.chessPieceTeam &&
                            !possibleMoves.Contains(pressedPiece.GetCurrentCoordinate()))
                        {
                            if (pressedPiece == selectedPiece) DeselectPiece();
                            else
                            {
                                DeselectPiece();
                                SelectPiece(pressedPiece);
                            }
                        }
                        else
                        {
                            PerformMove(selectedPiece, LookUpPositionIndex(pressedPiece.gameObject).x,
                                LookUpPositionIndex(pressedPiece.gameObject).y);
                        }
                    }
                    else DeselectPiece();
                }
            }
            else DeselectPiece();
        }

        if (Input.GetMouseButtonUp(1)) DeselectPiece();
    }

    void SelectPiece(ChessPiece piece)
    {
        if (!piece.isAttached || isSelectingAttachedUnit)
        {
            ChessVisualization.Instance.DehighlightSelectedPiece(selectedPiece);
            
            selectedPiece = piece;
            possibleMoves.Clear();
            possibleMoves = piece.GetAvailableMoves(chessPieces, boardSize);
            if (!piece.isAttached) isSelectingAttachedUnit = false;

            SimulatePieceToCheckForCheckmate(selectedPiece, ref possibleMoves,
                currentTurn == ChessPieceTeam.White ? whiteKing : blackKing);
            
            ChessVisualization.Instance.HighlightTiles(possibleMoves);
            ChessVisualization.Instance.HighlightSelectedPiece(piece);
            if (isSelectedForDetaching) ChessVisualization.Instance.HighlightSelectedPiece(piece.attachedPiece, true);
        }
        else // If it's an attached unit
        {
            ChessVisualization.Instance.DehighlightSelectedPiece(selectedPiece);

            ChessPiece carrier = piece.GetCarrierPiece();
            selectedPiece = carrier;
            possibleMoves.Clear();
            possibleMoves = carrier.GetAvailableMoves(chessPieces, boardSize);
            if (wildMode)
            {
                HashSet<Vector2Int> uniqueMoves = new HashSet<Vector2Int>(possibleMoves);
                uniqueMoves.UnionWith(carrier.attachedPiece.GetAvailableMoves(chessPieces, boardSize));
                possibleMoves = uniqueMoves.ToList();
            }
            isSelectingAttachedUnit = true;

            SimulatePieceToCheckForCheckmate(selectedPiece, ref possibleMoves,
                currentTurn == ChessPieceTeam.White ? whiteKing : blackKing);

            ChessVisualization.Instance.HighlightTiles(possibleMoves);
            ChessVisualization.Instance.HighlightSelectedPiece(carrier);
            ChessVisualization.Instance.HighlightSelectedPiece(carrier.attachedPiece);
        }
    }

    void DeselectPiece()
    {
        ChessVisualization.Instance.DehighlightSelectedPiece(selectedPiece);
        ChessVisualization.Instance.DehighlightSelectedPiece(selectedPiece?.attachedPiece);
        ChessVisualization.Instance.DehighlightSelectedPiece(detachedPieceThatAreMovedAwayFrom);
        ChessVisualization.Instance.DehighlightTiles();

        selectedPiece = null;
        possibleMoves.Clear();

        isSelectedForDetaching = false;
        isSelectingAttachedUnit = false;
        detachedPieceThatAreMovedAwayFrom = null;
    }
    
    void PerformMove(ChessPiece piece, int x, int y)
    {
        if (!possibleMoves.Contains(new Vector2Int(x, y))) return;
        
        // Register move to history
        ChessPiece capturedPiece = chessPieces[x, y];
        moveHistory.Add(new Move(piece, new Vector2Int(piece.currentX, piece.currentY), new Vector2Int(x, y),
            capturedPiece));
        moveHistory.Last().initiateAttachPiece = piece.initiateAttachPiece;
        if (GameConfigurations.PlayerTimerMinute != 0)
            moveHistory.Last().AssignTimer(whiteTimerAtTheStartOfTheTurn, blackTimerAtTheStartOfTheTurn);
        
        // Record move for 50 move rule
        if (piece is Pawn || capturedPiece != null) haveMoveCounter = 0;
        else haveMoveCounter++;

        // Perform En Passant
        if (piece is Pawn && new Vector2Int(x, y) == enPassantMove)
        {
            PerformEnPassant(piece, x, y);
            moveHistory.Last().isEnPassant = true;
        }

        // Perform Castling
        else if (piece is King && Mathf.Abs(x - piece.currentX) == 2 && piece.currentY == y)
        {
            PerformCastling(piece, (x - piece.currentX) / 2);
            moveHistory.Last().isCastling = true;
        }

        // Check for valid pawn capture (this is to prevent a bug where Pawn can capture moving forward)
        /*else if (piece is Pawn && chessPieces[x, y] != null && Mathf.Abs(x - piece.currentX) == 1 &&
                 Mathf.Abs(y - piece.currentY) == 1)
        {
            MoveTo(piece, x, y);
        }*/

        // Move piece like normal if no special move is performed
        else
        {
            if (isSelectedForDetaching)
            {
                detachedPieceThatAreMovedAwayFrom = piece.attachedPiece;
                
                moveHistory.Last().detachFrom = detachedPieceThatAreMovedAwayFrom;
                
                piece.attachedPiece.Detach(false);
                piece.Detach(true);
            }

            EnPassantCheck(piece, x, y);
            MoveTo(piece, x, y);
        }

        DeselectPiece();

        // Promotion check
        /*if (piece is Pawn && ((piece.chessPieceTeam == ChessPieceTeam.Black && y == 0) ||
                              (piece.chessPieceTeam == ChessPieceTeam.White && y == boardSize - 1)))
        {
            StartCoroutine(PerformPromotion(piece));
            moveHistory.Last().isPromotion = true;
            return;
        }*/
        StartCoroutine(PreEndturnCheck()); // EndTurn() is included in here

        // Update boardstate for 3fold repetition
        string boardState = GetBoardState();
        if (boardStateCounter.ContainsKey(boardState)) boardStateCounter[boardState]++;
        else boardStateCounter[boardState] = 1;
    }

    void MoveTo(ChessPiece piece, int x, int y, bool forceSmoothMoveTo = false)
    {
        if (piece.isAttached && !isSelectedForDetaching &&
            piece.GetAttachedStatus() == true) // Move attached piece when selecting both unit
        {
            MoveTo(piece.attachedPiece, x, y);
            moveHistory.Last().attachedPiece = piece.attachedPiece;
        }

        chessPieces[piece.currentX, piece.currentY] = null;
        
        if (chessPieces[x, y] != null)
        {
            if (chessPieces[x, y].chessPieceTeam != currentTurn)
            {
                Capture(chessPieces[x, y]);
                ChessVisualization.Instance.SpawnParticleWithDelay(ChessVisualization.ParticleEffectType.Capture,
                    tiles[x, y].transform.position, piece.AnimatedMoveDuration);
            }
            else if (chessPieces[x, y].chessPieceTeam == currentTurn && piece.IsAvailableToAttach(true))
            {
                piece.UpdateCoordinate(x, y);
                
                moveHistory.Last().AssignAttachTo(chessPieces[x, y]);

                Attach(selectedPiece, chessPieces[x, y]);
                ChessVisualization.Instance.SpawnParticleWithDelay(ChessVisualization.ParticleEffectType.PairUp,
                    tiles[x, y].transform.position, piece.AnimatedMoveDuration);
            }
        }
        chessPieces[x, y] = piece.isAttached ? piece.GetCarrierPiece() : piece;
        
        if (isDragging || forceSmoothMoveTo) piece.SmoothMovePieceTo(tiles[x, y].transform.position);
        else
            piece.AnimatedMovePieceTo(tiles[x, y].transform.position, 1f,
                piece.GetAttachedStatus() == false ? 0.1f : 0f);
        piece.UpdateCoordinate(x, y);
        piece.hasMoved = true;

        if (isSelectedForDetaching)
            chessPieces[detachedPieceThatAreMovedAwayFrom.currentX, detachedPieceThatAreMovedAwayFrom.currentY] =
                detachedPieceThatAreMovedAwayFrom;
    }

    void Capture(ChessPiece pieceToCapture)
    {
        if (pieceToCapture)
        {
            chessPieces[pieceToCapture.currentX, pieceToCapture.currentY] = null;
            pieceToCapture.KillPiece();
            
            capturedPiecePlacerScript.PlaceCapturedPiece(pieceToCapture, pieceToCapture.chessPieceTeam);

            if (pieceToCapture.isAttached)
            {
                pieceToCapture.attachedPiece.KillPiece();

                moveHistory.Last().capturedAttachedPiece = pieceToCapture.attachedPiece;

                capturedPiecePlacerScript.PlaceCapturedPiece(pieceToCapture.attachedPiece,
                    pieceToCapture.chessPieceTeam);
            }
        }
    }
    
    void Attach(ChessPiece targetPiece, ChessPiece pieceToAttach, bool allowVisualMovement = true)
    {
        if (pieceToAttach)
        {
            if (pieceToAttach.isAttached)
            {
                pieceToAttach.attachedPiece.Detach(false, allowVisualMovement);
                pieceToAttach.Detach(true, allowVisualMovement);
            }

            (ChessPiece higherPiece, ChessPiece lowerPiece) = targetPiece.value > pieceToAttach.value
                ? (targetPiece, pieceToAttach)
                : targetPiece.value < pieceToAttach.value
                    ? (pieceToAttach, targetPiece)
                    : (targetPiece, pieceToAttach);

            higherPiece.CarrierAttach(lowerPiece, higherPiece == selectedPiece ? false : allowVisualMovement);
            lowerPiece.AdjunctAttach(higherPiece, allowVisualMovement);
        }
    }

    public void UndoMove()
    {
        if (moveHistory.Count == 0) return;

        if (GameConfigurations.PlayerTimerMinute != 0)
            GameTimerManager.Instance.ChangeTimer(moveHistory.Last().whiteTimer, moveHistory.Last().blackTimer);
        
        Move lastMove = moveHistory[moveHistory.Count - 1];
        moveHistory.RemoveAt(moveHistory.Count - 1);
        
        // Clear chessPieces[,]
        chessPieces[lastMove.piece.currentX, lastMove.piece.currentY] = null;
        chessPieces[lastMove.startPosition.x, lastMove.startPosition.y] = lastMove.piece;
        
        // Reposition piece
        lastMove.piece.UpdateCoordinate(lastMove.startPosition.x, lastMove.startPosition.y);
        lastMove.piece.PlacePieceInCurrentCoordinate();
        lastMove.piece.hasMoved = lastMove.pieceHasMovedState;
        
        // Redo attach mechanics
        if (lastMove.attachedTo)
        {
            lastMove.piece.Detach(true);
            lastMove.piece.PlacePieceInCurrentCoordinate();
            lastMove.attachedTo.Detach(false, false);
        }
        if (lastMove.detachFrom)
        {
            Attach(lastMove.piece, lastMove.detachFrom, false);
            lastMove.piece.PlacePieceInCurrentCoordinate();
            lastMove.detachFrom.PlacePieceInCurrentCoordinate();
            lastMove.piece.initiateAttachPiece = lastMove.initiateAttachPiece;
            lastMove.detachFrom.initiateAttachPiece = lastMove.initiateAttachPiece;
        }
        if (lastMove.attachedPiece && !lastMove.attachedTo && !lastMove.detachFrom)
        {
            lastMove.attachedPiece.UpdateCoordinate(lastMove.startPosition.x, lastMove.startPosition.y);
            lastMove.attachedPiece.PlacePieceInCurrentCoordinate();
        }
        
        // Special moves
        if (lastMove.isEnPassant)
        {
            ChessPiece capturedPawn = chessPieces[lastMove.endPosition.x, lastMove.startPosition.y];
            capturedPawn.RevivePiece(lastMove.endPosition.x, lastMove.startPosition.y);
            chessPieces[lastMove.endPosition.x, lastMove.startPosition.y] = capturedPawn;
        }
        else if (lastMove.isCastling)
        {
            int rookStartX = lastMove.endPosition.x == 6 ? 7 : 0;
            int rookEndX = lastMove.endPosition.x == 6 ? 5 : 3;
            ChessPiece rook = chessPieces[rookEndX, lastMove.endPosition.y];
            chessPieces[rookEndX, lastMove.endPosition.y] = null;
            chessPieces[rookStartX, lastMove.endPosition.y] = rook;
            rook.UpdateCoordinate(rookStartX, lastMove.endPosition.y);
            rook.transform.position = tiles[rookStartX, lastMove.endPosition.y].transform.position;
            rook.hasMoved = false;
        }
        else if (lastMove.isPromotion)
        {
            if (lastMove.attachedPiece && !lastMove.attachedTo && !lastMove.detachFrom)
            {
                ChessPiece nonInitiateAttachPiece;
                if (lastMove.initiateAttachPiece == lastMove.piece) nonInitiateAttachPiece = lastMove.attachedPiece;
                else nonInitiateAttachPiece = lastMove.piece;

                Debug.Log(lastMove.initiateAttachPiece);
                lastMove.initiateAttachPiece.CarrierAttach(nonInitiateAttachPiece);
                nonInitiateAttachPiece.AdjunctAttach(lastMove.initiateAttachPiece);
                
                chessPieces[lastMove.endPosition.x, lastMove.endPosition.y] = null;
            }
            else
            {
                chessPieces[lastMove.endPosition.x, lastMove.endPosition.y] = lastMove.promotedPiece;
            }
            
            lastMove.promotedPiece.KillPiece(false);
            Destroy(lastMove.promotedPiece.gameObject);
            lastMove.piece.RevivePiece(lastMove.startPosition.x, lastMove.startPosition.y);

            if (lastMove.isPromotion1)
            {
                if (lastMove.attachedPiece && !lastMove.attachedTo && !lastMove.detachFrom)
                {
                    ChessPiece nonInitiateAttachPiece;
                    if (lastMove.initiateAttachPiece == lastMove.piece) nonInitiateAttachPiece = lastMove.attachedPiece;
                    else nonInitiateAttachPiece = lastMove.piece;

                    Debug.Log(lastMove.initiateAttachPiece);
                    lastMove.initiateAttachPiece.CarrierAttach(nonInitiateAttachPiece);
                    nonInitiateAttachPiece.AdjunctAttach(lastMove.initiateAttachPiece);
                
                    chessPieces[lastMove.endPosition.x, lastMove.endPosition.y] = null;
                }
                else
                {
                    chessPieces[lastMove.endPosition.x, lastMove.endPosition.y] = lastMove.promotedPiece1;
                }
            
                lastMove.promotedPiece1.KillPiece(false);
                Destroy(lastMove.promotedPiece1.gameObject);
                lastMove.piece.RevivePiece(lastMove.startPosition.x, lastMove.startPosition.y);
            }
        }

        // Redo capture piece
        if (lastMove.capturedPiece != null)
        {
            if (lastMove.capturedAttachedPiece)
            {
                lastMove.capturedAttachedPiece.transform.position =
                    tiles[lastMove.endPosition.x, lastMove.endPosition.y].transform.position;
                lastMove.capturedAttachedPiece.RevivePiece(lastMove.endPosition.x, lastMove.endPosition.y);
                
                Attach(lastMove.capturedPiece, lastMove.capturedAttachedPiece, false);
                lastMove.capturedAttachedPiece.PlacePieceInCurrentCoordinate();
            }
            
            chessPieces[lastMove.endPosition.x, lastMove.endPosition.y] = lastMove.capturedPiece;
            
            lastMove.capturedPiece.RevivePiece(lastMove.endPosition.x, lastMove.endPosition.y);
            lastMove.capturedPiece.PlacePieceInCurrentCoordinate();
            lastMove.capturedPiece.hasMoved = lastMove.capturedPieceHasMovedState;
        }
        
        // Update turn & change camera
        currentTurn = currentTurn == ChessPieceTeam.White ? ChessPieceTeam.Black : ChessPieceTeam.White;
        StartCoroutine(SwitchPOVDelay());
    }

    void DragPiece()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
        {
            Vector3 pointingPosition = hit.point;
            selectedPiece.transform.position = pointingPosition + Vector3.up * pieceYOffset;
        }
    }

    void ReturnDraggedPieceToOriginalPosition(ChessPiece piece)
    {
        piece.SmoothMovePieceTo(tiles[piece.currentX, piece.currentY].transform.position, 0.5f);
    }

    #endregion

    #region Special Moves

    void EnPassantCheck(ChessPiece piece, int x, int y)
    {
        enPassantMove = null;

        if (piece is Pawn && Mathf.Abs(piece.currentY - y) == 2)
        {
            enPassantMove = new Vector2Int(x, (piece.currentY + y) / 2);
            //Debug.Log("Possible En Passant at" + enPassantMove);
        }
    }

    void PerformEnPassant(ChessPiece piece, int x, int y)
    {
        MoveTo(piece, x, y);
        Capture(chessPieces[x, y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)]);
        ChessVisualization.Instance.SpawnParticleWithDelay(ChessVisualization.ParticleEffectType.Capture,
            tiles[x, y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)].transform.position, piece.AnimatedMoveDuration);
        Debug.Log("En Croissant at " + new Vector2Int(x, y));
        enPassantMove = null;
    }

    void PerformCastling(ChessPiece king, int direction)
    {
        int rookX = (direction == 1) ? (boardSize - 1) : 0;
        int newKingX = king.currentX + direction * 2;
        int newRookX = king.currentX + direction;

        MoveTo(king, newKingX, king.currentY);
        MoveTo(chessPieces[rookX, king.currentY], newRookX, king.currentY, true);
        Debug.Log("Pussy King at " + new Vector2Int(newKingX, king.currentY));
    }

    IEnumerator PerformPromotion(ChessPiece pawn)
    {
        isPromoting = true;
        GameTimerManager.Instance.StopTimer();

        yield return new WaitForSeconds(pawn.AnimatedMoveDuration + 0.25f);

        ChessVisualization.Instance.SpawnParticleEffect(ChessVisualization.ParticleEffectType.Promotion,
            pawn.transform.position);

        yield return new WaitForSeconds(promotionDelay);

        GameObject promotion = Instantiate(promotionPrefab,
            pawn.transform.position + new Vector3(0, 0,
                promotionSpawningOffset * (pawn.chessPieceTeam == ChessPieceTeam.White ? 1 : -1)),
            Quaternion.Euler(0, (pawn.chessPieceTeam == ChessPieceTeam.White ? 0 : 180), 0));

        PromotionBehavior promotionBehavior = promotion.GetComponent<PromotionBehavior>();
        Transform selectedPiece = null;

        yield return StartCoroutine(
            promotionBehavior.InitiatePromotionSequence(pawn.chessPieceTeam, piece => selectedPiece = piece));

        if (selectedPiece != null)
        {
            chessPieces[pawn.currentX, pawn.currentY] = selectedPiece.GetComponent<ChessPiece>();

            ChessPiece pieceScript = selectedPiece.GetComponent<ChessPiece>();
            
            pieceScript.UpdateCoordinate(pawn.currentX, pawn.currentY);
            pieceScript.hasMoved = true;
            selectedPiece.gameObject.layer = LayerMask.NameToLayer("Interactive");
            selectedPiece.transform.parent = null;
            
            if (!pawn.isAttached)
                pieceScript.AnimatedMovePieceTo(tiles[pawn.currentX, pawn.currentY].transform.position);
            else
            {
                ChessPiece attachedPiece = pawn.attachedPiece;
                
                pawn.attachedPiece.attachedPiece = pieceScript;
                pieceScript.attachedPiece = pawn.attachedPiece;
                pawn.attachedPiece.initiateAttachPiece =
                    pawn.attachedPiece.value > pieceScript.value ? pawn.attachedPiece : pieceScript;
                pieceScript.initiateAttachPiece =
                    pawn.attachedPiece.value > pieceScript.value ? pawn.attachedPiece : pieceScript;
                
                if (pieceScript.value > attachedPiece.value)
                {
                    pieceScript.CarrierAttach(attachedPiece, false);
                    pieceScript.AnimatedMovePieceTo(
                        tiles[pieceScript.currentX, pieceScript.currentY].transform.position, 1.5f);
                    
                    attachedPiece.Detach(false, false);
                    attachedPiece.AdjunctAttach(pieceScript, true);
                }
                else
                { 
                    pieceScript.AdjunctAttach(attachedPiece, false);
                    pieceScript.AnimatedMovePieceTo(
                        tiles[pieceScript.currentX, pieceScript.currentY].transform.position, 1.5f);
                    
                    attachedPiece.Detach(false, false);
                    attachedPiece.CarrierAttach(pieceScript, true);
                }
            }

            if (pawn.chessPieceTeam == ChessPieceTeam.White)
            {
                remainingWhitePieces.Remove(pawn);
                remainingWhitePieces.Add(pieceScript);
            }
            else
            {
                remainingBlackPieces.Remove(pawn);
                remainingBlackPieces.Add(pieceScript);
            }

            pieceScript.isPromotionPiece = false;

            pawn.transform.position -= new Vector3(0, 5, 0); // Make the pawn go bye bye
            if (!moveHistory.Last().isPromotion1) moveHistory.Last().promotedPiece = pieceScript;
            else moveHistory.Last().promotedPiece1 = pieceScript;
        }

        yield return StartCoroutine(promotionBehavior.ExitPromotionSequence());
        promotionBehavior.SetInactiveCamera();
        yield return new WaitForSeconds(1.5f);

        isPromoting = false;
        GameTimerManager.Instance.StartTimer();
        Destroy(promotionBehavior.gameObject);
    }
    
    #endregion
    
    #region Checkmate Logic

    void SimulatePieceToCheckForCheckmate(ChessPiece piece, ref List<Vector2Int> moves, ChessPiece king)
    {
        Vector2Int kingPosition = new Vector2Int(king.currentX, king.currentY);
        List<Vector2Int> safeMove = new List<Vector2Int>();

        foreach (Vector2Int move in moves)
        {
            ChessPiece[,] simulatedBoard = (ChessPiece[,])chessPieces.Clone();
            
            simulatedBoard[piece.currentX, piece.currentY] = null;
            simulatedBoard[move.x, move.y] = piece;

            if (piece is King || (piece.isAttached && piece.attachedPiece is King && !isSelectedForDetaching))
            {
                kingPosition = new Vector2Int(move.x, move.y);
            }

            // En Passant
            if (piece is Pawn && move == enPassantMove)
            {
                simulatedBoard[move.x, move.y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)] = null;
            }

            // Castling
            if (piece is King && Mathf.Abs(move.x - piece.currentX) == 2)
            {
                int rookX = (move.x == 2) ? 0 : boardSize - 1;
                int newKingX = piece.currentX + (move.x == 2 ? -2 : 2);
                int newRookX = piece.currentX + (move.x == 2 ? -1 : 1);

                simulatedBoard[piece.currentX, piece.currentY] = null;
                simulatedBoard[newKingX, piece.currentY] = piece;
                simulatedBoard[rookX, piece.currentY] = null;
                simulatedBoard[newRookX, piece.currentY] = chessPieces[rookX, piece.currentY];
            }

            // Pawn capture
            if (piece is Pawn && Mathf.Abs(move.x - piece.currentX) == 1 &&
                Mathf.Abs(move.y - piece.currentY) == 1 &&
                chessPieces[move.x, move.y] != null &&
                chessPieces[move.x, move.y].chessPieceTeam != piece.chessPieceTeam)
            {
                simulatedBoard[move.x, move.y] = piece;
                Debug.Log(piece + " " + new Vector2Int(move.x, move.y));
                Debug.Log(piece + " " + new Vector2Int(move.x, move.y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)));
                simulatedBoard[move.x, move.y - ((int)piece.chessPieceTeam == 0 ? 1 : -1)] = null;
            }

            if (isSelectingAttachedUnit)
            {
                if (!isSelectedForDetaching)
                {
                    simulatedBoard[piece.attachedPiece.currentX, piece.attachedPiece.currentY] = null;
                }
                else
                {
                    simulatedBoard[piece.currentX, piece.currentY] = piece.attachedPiece;
                }
            }

            if (!IsKingInDanger(simulatedBoard, kingPosition, currentTurn)) safeMove.Add(move);
        }

        moves = safeMove;
    }
 
    public bool IsKingInDanger(ChessPiece[,] board, Vector2Int kingPosition, ChessPieceTeam team)
    {
        foreach (var piece in board)
        {
            if (piece != null && piece.chessPieceTeam != team)
            {
                List<Vector2Int> opponentMoves = piece.GetAvailableMoves(board, boardSize);
                if (piece.isAttached)
                {
                    opponentMoves.AddRange(piece.attachedPiece.GetAvailableMoves(board, boardSize));
                }
                
                if (opponentMoves.Contains(new Vector2Int(kingPosition.x, kingPosition.y)))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < boardSize && y >= 0 && y < boardSize;
    }
    
    #endregion
    
    #region Draw Offer Methods

    public void DrawOfferAccepted()
    {
        if (currentTurn == ChessPieceTeam.White) whiteAgreeToDraw = true;
        else blackAgreeToDraw = true;
        
        EndTurn();
    }
    
    public void DrawOfferRejected()
    {
        whiteAgreeToDraw = false;
        blackAgreeToDraw = false;
        
        EndTurn();
    }

    #endregion
}

public class Move
{
    public float whiteTimer;
    public float blackTimer;
    
    public ChessPiece piece;
    public Vector2Int startPosition;
    public Vector2Int endPosition;
    public ChessPiece capturedPiece;
    public ChessPiece capturedAttachedPiece;

    public bool pieceHasMovedState;
    public bool capturedPieceHasMovedState;
    
    public bool isEnPassant;
    public bool isCastling;
    public bool isPromotion;
    public ChessPiece promotedPiece;
    public bool isPromotion1;
    public ChessPiece promotedPiece1;

    public ChessPiece attachedTo;
    public ChessPiece detachFrom;
    public ChessPiece attachedPiece;

    public bool attachedToHasMovedState;

    public ChessPiece initiateAttachPiece;
        
    public Move(ChessPiece piece, Vector2Int startPosition, Vector2Int endPosition, ChessPiece capturedPiece)
    {
        this.piece = piece;
        this.startPosition = startPosition;
        this.endPosition = endPosition;
        this.capturedPiece = capturedPiece;
        
        this.pieceHasMovedState = piece.hasMoved;
        this.capturedPieceHasMovedState = capturedPiece != null ? capturedPiece.hasMoved : false;

        this.attachedPiece = piece.attachedPiece;
    }

    public void AssignAttachTo(ChessPiece piece)
    {
        attachedTo = piece;
        attachedToHasMovedState = piece.hasMoved;
    }

    public void AssignTimer(float whiteTime, float blackTime)
    {
        whiteTimer = whiteTime;
        blackTimer = blackTime;
    }
}