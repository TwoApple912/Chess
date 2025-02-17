using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessVisualization : MonoBehaviour
{
    public static ChessVisualization Instance;

    [Header("Tile Highlight")]
    [SerializeField] private Material tileDefaultMaterial;
    [SerializeField] private Material possibleMoveMaterial;
    [SerializeField] private Material possibleCaptureMaterial;

    [Header("Piece Highlight")]
    [SerializeField] private Color selectedColor = new(135f, 206f, 235f);
    [SerializeField] private Color attachedUnitSelectedColor = new(135f, 235f, 138f);
    [SerializeField] private Color captureHighlightColor = new(255f, 69f, 0f);
    [SerializeField] private Color attachHighlightColor = new(0f, 255f, 0f);
    [SerializeField] private float highlightedAlpha = 1f;
    [SerializeField] private float subHighlightAlpha = 0.5f;
    [SerializeField] private float fadeInDuration = 0.21f;
    [SerializeField] private float fadeOutDuration = 0.125f;

    [Header("Particle Effects")]
    [SerializeField] private GameObject placePieceParticles;
    [SerializeField] private GameObject promotionParticles;
    [SerializeField] private GameObject captureParticles;

    private List<ChessPiece> piecesToHighlightCapture = new();
    private List<Coroutine> highlightedCapturableCoroutines = new();
    private List<ChessPiece> piecesToHighlightPairUp = new();
    private List<Coroutine> highlightedPairUpCoroutines = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        if (!tileDefaultMaterial) tileDefaultMaterial = Resources.Load<Material>("Materials/TileDefault");
        if (!possibleMoveMaterial) possibleMoveMaterial = Resources.Load<Material>("Materials/TileAvailableMove");
        if (!possibleCaptureMaterial)
            possibleCaptureMaterial = Resources.Load<Material>("Materials/TileAvailableCapture");
        if (!placePieceParticles)
            placePieceParticles = Resources.Load<GameObject>("Prefabs/Particle Effect/Place Piece Particle");
        if (!promotionParticles)
            promotionParticles = Resources.Load<GameObject>("Prefabs/Particle Effect/Promotion Particle");
        if (!captureParticles)
            captureParticles = Resources.Load<GameObject>("Prefabs/Particle Effect/Capture Particle");
    }

    public void HighlightTiles(List<Vector2Int> tilesToHighlight)
    {
        DehighlightTiles();

        foreach (Vector2Int moveTile in tilesToHighlight)
        {
            GameObject tile = ChessManager.Instance.tiles[moveTile.x, moveTile.y];
            if (tile != null)
            {
                Renderer tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    if (ChessManager.Instance.SelectedPiece is Pawn && ChessManager.Instance.enPassantMove.HasValue &&
                        ChessManager.Instance.enPassantMove.Value == moveTile) // En Passant check
                    {
                        Vector2Int pawnPosition = new Vector2Int(moveTile.x,
                            moveTile.y - ((int)ChessManager.Instance.SelectedPiece.chessPieceTeam == 0 ? 1 : -1));
                        ChessPiece enPassantPawn = ChessManager.Instance.chessPieces[pawnPosition.x, pawnPosition.y];
                        if (enPassantPawn != null) piecesToHighlightCapture.Add(enPassantPawn);
                    }

                    if (ChessManager.Instance.chessPieces[moveTile.x, moveTile.y] != null)
                    {
                        //tileRenderer.material = possibleCaptureMaterial;
                        
                        // Check if the team of the piece on the tile is different from the current turn's team
                        if (ChessManager.Instance.CurrentTurn !=
                            ChessManager.Instance.chessPieces[moveTile.x, moveTile.y].chessPieceTeam)
                        {
                            piecesToHighlightCapture.Add(ChessManager.Instance.chessPieces[moveTile.x, moveTile.y]);

                            if (ChessManager.Instance.chessPieces[moveTile.x, moveTile.y].isAttached)
                                piecesToHighlightCapture.Add(ChessManager.Instance.chessPieces[moveTile.x, moveTile.y]
                                    .attachedPiece);
                        }
                        else piecesToHighlightPairUp.Add(ChessManager.Instance.chessPieces[moveTile.x, moveTile.y]);
                    }
                    else tileRenderer.material = possibleMoveMaterial;
                }
            }
        }

        foreach (ChessPiece piece in piecesToHighlightCapture)
            highlightedCapturableCoroutines.Add(
                StartCoroutine(HighlightPieceCoroutine(piece, captureHighlightColor, highlightedAlpha,
                    fadeInDuration)));

        foreach (ChessPiece piece in piecesToHighlightPairUp)
            highlightedPairUpCoroutines.Add(
                StartCoroutine(HighlightPieceCoroutine(piece, attachHighlightColor, highlightedAlpha, fadeInDuration)));
    }

    public void DehighlightTiles()
    {
        foreach (GameObject tile in ChessManager.Instance.tiles)
        {
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer != null && tileRenderer.material != tileDefaultMaterial)
            {
                tileRenderer.material = tileDefaultMaterial;
            }
        }
        
        foreach (var coroutine in highlightedCapturableCoroutines) StopCoroutine(coroutine);
        foreach (ChessPiece piece in piecesToHighlightCapture)
            StartCoroutine(HighlightPieceCoroutine(piece, captureHighlightColor, 0, fadeOutDuration));
        piecesToHighlightCapture.Clear();
        
        foreach (var coroutine in highlightedPairUpCoroutines) StopCoroutine(coroutine);
        foreach (ChessPiece piece in piecesToHighlightPairUp)
            StartCoroutine(HighlightPieceCoroutine(piece, attachHighlightColor, 0, fadeOutDuration));
        piecesToHighlightPairUp.Clear();
    }

    public void HighlightSelectedPiece(ChessPiece piece, bool subHighlight = false)
    {
        StartCoroutine(HighlightPieceCoroutine(piece, piece.isAttached ? attachedUnitSelectedColor : selectedColor,
            subHighlight ? subHighlightAlpha : highlightedAlpha, fadeInDuration));
    }
    
    public void DehighlightSelectedPiece(ChessPiece piece)
    {
        if (piece != null)
            StartCoroutine(HighlightPieceCoroutine(piece, piece.isAttached ? attachedUnitSelectedColor : selectedColor,
                0,
                fadeOutDuration));
    }

    IEnumerator HighlightPieceCoroutine(ChessPiece piece, Color color, float targetAlpha, float duration)
    {
        if (piece == null) yield break;

        piece.GetComponentInChildren<OutlineFx.OutlineFx>()._color = new Color(color.r, color.g, color.b,
            piece.GetComponentInChildren<OutlineFx.OutlineFx>()._color.a);
        
        float elapsed = 0f;
        float initialAlpha = piece.GetComponentInChildren<OutlineFx.OutlineFx>()._color.a;

        while (elapsed < duration)
        {
            if (piece == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float newAlpha = Mathf.Lerp(initialAlpha, targetAlpha, t);
            
            Color newColor = piece.GetComponentInChildren<OutlineFx.OutlineFx>()._color;
            newColor.a = newAlpha;
            piece.GetComponentInChildren<OutlineFx.OutlineFx>()._color = newColor;

            yield return null;
        }

        Color finalColor = piece.GetComponentInChildren<OutlineFx.OutlineFx>()._color;
        finalColor.a = targetAlpha;
        piece.GetComponentInChildren<OutlineFx.OutlineFx>()._color = finalColor;
    }

    #region Particle Effects

    public enum ParticleEffectType
    {
        PlacePiece, Promotion, Capture, PairUp
    }

    public void SpawnParticleEffect(ParticleEffectType type, Vector3 position)
    {
        switch (type)
        {
            case ParticleEffectType.PlacePiece:
                Instantiate(placePieceParticles, position, Quaternion.identity);
                break;
            case ParticleEffectType.Promotion:
                Instantiate(promotionParticles, position, Quaternion.identity);
                break;
            case ParticleEffectType.Capture:
                Instantiate(captureParticles, position, Quaternion.identity);
                break;
        }
    }
    
    public void SpawnParticleWithDelay(ParticleEffectType type, Vector3 position, float delay)
    {
        StartCoroutine(SpawnParticleWithDelayCoroutine(type, position, delay));
    }
    IEnumerator SpawnParticleWithDelayCoroutine(ParticleEffectType type, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnParticleEffect(type, position);
    }

    #endregion
}