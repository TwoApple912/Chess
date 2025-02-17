using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CapturedPiecePlacer : MonoBehaviour
{
    [SerializeField] private float minDistance = 0.7f;
    [SerializeField] private float moveDurationMultiplier = 0.9f;
    [Space]
    [SerializeField] private BoxCollider whitePlacementArea;
    [SerializeField] private BoxCollider blackPlacementArea;
    
    private List<Vector3> placedWhitePieces = new List<Vector3>();
    private List<Vector3> placedBlackPieces = new List<Vector3>();

    private void Awake()
    {
        if (!whitePlacementArea)
            whitePlacementArea = GameObject.Find("Captured White Pieces Area").GetComponent<BoxCollider>();
        if (!blackPlacementArea)
            blackPlacementArea = GameObject.Find("Captured Black Pieces Area").GetComponent<BoxCollider>();
    }

    public void PlaceCapturedPiece(ChessPiece piece, ChessPieceTeam team)
    {
        Vector3 randomPosition;
        bool positionValid;
        BoxCollider placementArea = (team == 0) ? whitePlacementArea : blackPlacementArea;
        List<Vector3> placedPositions = (team == 0) ? placedWhitePieces : placedBlackPieces;

        do
        {
            randomPosition = GetRandomPositionWithinBounds(placementArea);
            positionValid = CheckMinimumDistance(randomPosition, placedPositions);
        } while (!positionValid);

        piece.SuddenMoveTo(randomPosition, piece.AnimatedMoveDuration);
        placedPositions.Add(randomPosition);
    }

    private Vector3 GetRandomPositionWithinBounds(BoxCollider boxCollider)
    {
        Vector3 center = boxCollider.center + boxCollider.transform.position;
        Vector3 size = boxCollider.size;

        float randomX = Random.Range(center.x - size.x / 2, center.x + size.x / 2);
        float randomZ = Random.Range(center.z - size.z / 2, center.z + size.z / 2);

        return new Vector3(randomX, center.y, randomZ);
    }

    private bool CheckMinimumDistance(Vector3 position, List<Vector3> placedPositions)
    {
        foreach (Vector3 placedPosition in placedPositions)
        {
            if (Vector3.Distance(position, placedPosition) < minDistance)
            {
                return false;
            }
        }

        return true;
    }
}
