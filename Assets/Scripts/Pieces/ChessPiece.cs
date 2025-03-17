using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum ChessPieceTeam
{
    White = 0,
    Black = 1,
}

public enum ChessPieceType
{
    Pawn = 0,
    Bishop = 1,
    Knight = 2,
    Rook = 3,
    Queen = 4,
    King = 5
}

public class ChessPiece : MonoBehaviour
{
    [Header("Essentials")]
    public ChessPieceTeam chessPieceTeam;
    public ChessPieceType type;
    public virtual int value { get; set; } = 0;
    [Space]
    public int currentX;
    public int currentY;
    public bool isAlive = true;
    public bool hasMoved;
    [Space]
    public bool isAttached = false;
    public ChessPiece attachedPiece;
    
    private Transform model;
    private bool hasMoveState;

    [Header("Materials")]
    [SerializeField] private Material[] teamMaterial = new Material[2];

    [Header("Attach Parameter")]
    [Tooltip("Apply to white pieces. Multiply with -1 for black pieces.")]
    [SerializeField] private Vector3 carrierOffset = new(-0.2f, 0f, 0.15f);
    [Tooltip("Apply to white pieces. Multiply with -1 for black pieces.")]
    [SerializeField] private Vector3 adjunctOffset = new(0.25f, 0f, -0.2f);
    [SerializeField] private float adjunctSize = 0.85f;
    
    [Header("Movement Parameter")]
    [SerializeField] private Vector3 offsetPosition = Vector3.zero;
        public Vector3 OffsetPosition => offsetPosition;
    [SerializeField] private float smoothMoveDuration = 0.21f;
    [Space]
    [SerializeField] private float animatedMoveDuration = 0.37f;
        public float AnimatedMoveDuration => animatedMoveDuration;
    [FormerlySerializedAs("adjustSizeDuration")] [SerializeField] private float sizeAdjustDuration = 0.2f;
    [SerializeField] private float pickUpHeight = 0.9f;
    [Space]
    [SerializeField] private float longDistanceThreshold = 3f;
    [Space]
    [SerializeField] private float rotationDuration = 0.12f;
    
    [Header("Spawn Placement Motion")]
    [SerializeField] private float spawnHeight = 0.25f;
    [SerializeField] private float spawnDuration = 0.15f;

    [Header("References")]
    [SerializeField] private Animator modelAnimator;
    [SerializeField] private Collider collider;

    private void Awake()
    {
        teamMaterial[0] = Resources.Load<Material>("Materials/WhiteTeamPlastic");
        teamMaterial[1] = Resources.Load<Material>("Materials/BlackTeamPlastic");

        model = transform.Find("Model");
        modelAnimator = model.GetComponent<Animator>();
        collider = GetComponent<Collider>();
    }
    
    private void Start()
    {
        SetName();
        SetMaterial();
        AddRotationRandomness();
        
        PlacePiece();
    }

    void SetName()
    {
        gameObject.name = $"{chessPieceTeam} {type} ({currentX}, {currentY})";
    }

    public void SetMaterial()
    {
        model.GetComponent<Renderer>().material = teamMaterial[(int)chessPieceTeam];
    }
    
    void AddRotationRandomness()
    {
        model.Rotate(Vector3.up, Random.Range(-45, 45));
    }

    public void UpdateCoordinate(int x, int y)
    {
        currentX = x;
        currentY = y;
    }

    public virtual List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int boardSize)
    {
        if (isAlive) return new List<Vector2Int>();
        return null;
    }

    // Check if the move is within the board
    internal bool IsValidMove(int x, int y, int boardSize)
    {
        return x >= 0 && y >= 0 && x < boardSize && y < boardSize;
    }
    
    public Vector2Int GetCurrentCoordinate()
    {
        return new Vector2Int(currentX, currentY);
    }

    public bool? GetAttachedStatus() // True for carrier, false for adjunct, null for no attachment
    {
        if (isAttached) return value > attachedPiece.value;
        return null;
    }
    
    public void KillPiece(bool isItCaptured = true)
    {
        hasMoveState = hasMoved;
        
        isAlive = false;
        currentX = -1;
        currentY = -1;
        
        collider.enabled = false;

        if (chessPieceTeam == ChessPieceTeam.White)
        {
            ChessManager.Instance.remainingWhitePieces.Remove(this);
            if (isItCaptured) ChessManager.Instance.capturedWhitePieces.Add(this);
        }
        else
        {
            ChessManager.Instance.remainingBlackPieces.Remove(this);
            if (isItCaptured) ChessManager.Instance.capturedBlackPieces.Add(this);
        }
    }

    public void RevivePiece(int x, int y)
    {
        isAlive = true;
        currentX = x;
        currentY = y;
        
        collider.enabled = true;
        
        if (chessPieceTeam == ChessPieceTeam.White)
        {
            ChessManager.Instance.remainingWhitePieces.Add(this);
            ChessManager.Instance.capturedWhitePieces.Remove(this);
        }
        else
        {
            ChessManager.Instance.remainingBlackPieces.Add(this);
            ChessManager.Instance.capturedBlackPieces.Remove(this);
        }
    }

    public void PlacePieceInCurrentCoordinate() // In case the piece needed to be re-place
    {
        transform.position = ChessManager.Instance.tiles[currentX, currentY].transform.position + offsetPosition;
    }
    
    #region Attach

    public void CarrierAttach(ChessPiece otherPiece, bool allowVisualMovement = true)
    {
        hasMoved = true;
        isAttached = true;
        attachedPiece = otherPiece;
        
        offsetPosition = chessPieceTeam == ChessPieceTeam.White ? carrierOffset : -carrierOffset;
        if (allowVisualMovement)
            SmoothMovePieceTo(ChessManager.Instance.tiles[currentX, currentY].transform.position, 1.5f);
    }
    
    public void AdjunctAttach(ChessPiece otherPiece, bool allowVisualMovement = true)
    {
        hasMoved = true;
        isAttached = true;
        attachedPiece = otherPiece;
        
        offsetPosition = chessPieceTeam == ChessPieceTeam.White ? adjunctOffset : -adjunctOffset;
        ResizeModel(adjunctSize);
        if (allowVisualMovement)
            AnimatedMovePieceTo(ChessManager.Instance.tiles[otherPiece.currentX, otherPiece.currentY].transform.position);
    }
    
    public void Detach(bool isMovingAway, bool enableMoveAnimation = true)
    {
        isAttached = false;
        attachedPiece = null;
        
        offsetPosition = Vector3.zero;
        ResizeModel(1);

        if (enableMoveAnimation && !isMovingAway)
            SmoothMovePieceTo(ChessManager.Instance.tiles[currentX, currentY].transform.position);
    }

    public ChessPiece GetCarrierPiece()
    {
        if (!isAttached) return null;
        
        return value > attachedPiece.value ? this : attachedPiece;
    }

    public virtual bool IsAvailableToAttach(bool initiatingPiece)
    {
        /*if (ChessManager.Instance.isSelectedForDetaching || !isAttached) return true;
        return false;*/
        
        if (!isAttached) return true;
        if (ChessManager.Instance.isSelectedForDetaching && initiatingPiece) return true;

        return false;
    }

    #endregion

    #region Move To Code/Animation
    
    public void SmoothMovePieceTo(Vector3 target, float durationMultiplier = 1f)
    {
        Vector3 targetPosition = target + offsetPosition;
        StartCoroutine(SmoothMovePieceCoroutine(targetPosition, durationMultiplier));
        Debug.Log(this + " SmoothMovePieceTo");
    }
    IEnumerator SmoothMovePieceCoroutine(Vector3 target, float durationMultiplier = 1f)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, target);
        float totalDuration = smoothMoveDuration * durationMultiplier;
        
        if (distance > longDistanceThreshold) totalDuration *= 2f;
        
        float elapsedTime = 0f;
        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / totalDuration;

            Vector3 newPosition = Vector3.Lerp(startPosition, target, fractionOfJourney);
            transform.position = newPosition;
            yield return null;
        }
        transform.position = target;
        
        ChessVisualization.Instance?.SpawnParticleEffect(ChessVisualization.ParticleEffectType.PlacePiece, target);
    }

    public void AnimatedMovePieceTo(Vector3 target, float durationMultiplier = 1f, float initialDelay = 0)
    {
        Vector3 targetPosition = target + offsetPosition;
        
        Coroutine coroutine = StartCoroutine(AnimatedMovePieceCoroutine(targetPosition, durationMultiplier, initialDelay));
        StartCoroutine(RotateToCoroutine(targetPosition, initialDelay));

        ChessManager.Instance?.activeMoveCoroutine.Add(coroutine);
        
        Debug.Log(this + " AnimatedMovePieceTo");
    }
    private IEnumerator AnimatedMovePieceCoroutine(Vector3 target, float durationMultiplier = 1f, float initialDelay = 0)
    {
        yield return new WaitForSeconds(initialDelay);
        
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, target);
        float totalDuration = animatedMoveDuration * durationMultiplier;
        float totalPickUpHeight = pickUpHeight;

        if (distance > longDistanceThreshold)
        {
            //totalDuration *= 2f;
            totalPickUpHeight *= 2f;
        }

        float elapsedTime = 0f;
        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / totalDuration;

            Vector3 newPosition = Vector3.Lerp(startPosition, target, fractionOfJourney);

            float yOffset = Mathf.Sin(Mathf.PI * fractionOfJourney) * totalPickUpHeight;
            newPosition.y += yOffset;

            transform.position = newPosition;
            yield return null;
        }

        transform.position = target;

        ChessVisualization.Instance?.SpawnParticleEffect(ChessVisualization.ParticleEffectType.PlacePiece, target);
    }

    private IEnumerator RotateToCoroutine(Vector3 target, float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);
        
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        Vector3 direction = (target - startPosition).normalized;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float angleDifference = Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle);
        float randomRotation = Random.Range(-Mathf.Abs(angleDifference), Mathf.Abs(angleDifference));
        float finalAngle = transform.eulerAngles.y + randomRotation;

        Quaternion startRotation = model.rotation;
        Quaternion endRotation = Quaternion.Euler(0, finalAngle, 0);

        while (elapsedTime < animatedMoveDuration * 0.25f)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / (animatedMoveDuration * 0.25f);

            model.rotation = Quaternion.Slerp(startRotation, endRotation, fractionOfJourney);
            yield return null;
        }
        model.rotation = endRotation;
    }

    public void SuddenMoveTo(Vector3 targetPosition, float initialDelay = 0f)
    {
        Vector3 target = targetPosition + offsetPosition;
        
        Coroutine coroutine = StartCoroutine(SuddenMoveToCoroutine(target, initialDelay));
        
        ChessManager.Instance.activeMoveCoroutine.Add(coroutine);
    }
    IEnumerator SuddenMoveToCoroutine(Vector3 target, float initialDelay = 0f)
    {
        yield return new WaitForSeconds(initialDelay);
        transform.position = target;
        
        ChessVisualization.Instance.SpawnParticleEffect(ChessVisualization.ParticleEffectType.Capture, target);
        
        yield return new WaitForSeconds(0.25f);
    }
    
    #endregion

    #region Animator Controller

    private void OnMouseEnter()
    {
        if (isAlive)
        {
            if (ChessManager.Instance?.CurrentTurn != chessPieceTeam) return;

            modelAnimator.SetBool("hover", true);
        }
    }

    private void OnMouseExit()
    {
        if (isAlive)
        {
            modelAnimator.SetBool("hover", false);
        }
    }

    private void ResizeModel(float size, float durationMultiplier = 1)
    {
        StartCoroutine(ResizeModelCoroutine(size, durationMultiplier));
    }

    private IEnumerator ResizeModelCoroutine(float size, float durationMultiplier = 1)
    {
        float duration = sizeAdjustDuration * durationMultiplier;
        Vector3 originalScale = model.localScale;
        Vector3 targetScale = Vector3.one * size;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            model.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        model.localScale = targetScale;
    }

    #endregion

    #region Place Piece On Enabled

    private void PlacePiece()
    {
        Vector3 originalPosition = model.localPosition;
        Vector3 elevatedPosition = originalPosition + Vector3.up * spawnHeight;

        StartCoroutine(PlacePieceCoroutine(elevatedPosition, originalPosition));
    }

    private IEnumerator PlacePieceCoroutine(Vector3 elevatedPosition, Vector3 originalPosition)
    {
        model.localPosition = elevatedPosition;
        
        float elapsedTime = 0f;
        while (elapsedTime < spawnDuration)
        {
            model.localPosition = Vector3.Lerp(elevatedPosition, originalPosition, elapsedTime / spawnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        model.localPosition = originalPosition;
    }

    #endregion
}