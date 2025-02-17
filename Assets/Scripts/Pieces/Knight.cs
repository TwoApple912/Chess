using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    public override int value { get; set; } = 3;
    
    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int boardSize)
    {
        List<Vector2Int> availableMoves = new List<Vector2Int>();

        int[] xMoves = { 2, 2, 1, 1, -1, -1, -2, -2 };
        int[] yMoves = { 1, -1, 2, -2, 2, -2, 1, -1 };

        for (int i = 0; i < xMoves.Length; i++)
        {
            int newX = currentX + xMoves[i];
            int newY = currentY + yMoves[i];

            if (IsValidMove(newX, newY, boardSize))
            {
                if (board[newX, newY] == null || board[newX, newY].chessPieceTeam != chessPieceTeam)
                    availableMoves.Add(new Vector2Int(newX, newY));
                else if (board[newX, newY]?.chessPieceTeam == chessPieceTeam &&
                         board[newX, newY].IsAvailableToAttach(false) && IsAvailableToAttach(true) &&
                         board[newX, newY].value != this.value) availableMoves.Add(new Vector2Int(newX, newY));
            }
        }

        return availableMoves;
    }
}
