using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override int value { get; set; } = 0;
    
    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int boardSize)
    {
        List<Vector2Int> availableMoves = new List<Vector2Int>();

        int[] xDirections = { 1, -1, 1, -1, 0, 0, 1, -1 };
        int[] yDirections = { 1, 1, -1, -1, 1, -1, 0, 0 };
        
        // Move 1 space in any direction
        for (int i = 0; i < xDirections.Length; i++)
        {
            int newX = currentX + xDirections[i];
            int newY = currentY + yDirections[i];

            if (IsValidMove(newX, newY, boardSize))
            {
                if (board[newX, newY] == null || board[newX, newY]?.chessPieceTeam != chessPieceTeam)
                    availableMoves.Add(new Vector2Int(newX, newY));
                if (board[newX, newY]?.chessPieceTeam == chessPieceTeam &&
                    board[newX, newY].IsAvailableToAttach(false) &&
                    IsAvailableToAttach(true)) availableMoves.Add(new Vector2Int(newX, newY));
            }
        }
        
        // Castling
        if (!hasMoved)
        {
            if (IsValidCastling(board, boardSize, 1))
            {
                availableMoves.Add(new Vector2Int(currentX + 2, currentY));
            }
            if (IsValidCastling(board, boardSize, -1))
            {
                availableMoves.Add(new Vector2Int(currentX - 2, currentY));
            }
        }
        
        return availableMoves;
    }

    bool IsValidCastling(ChessPiece[,] chessPieces, int boardSize, int direction)
    {
        int rookX = (direction == 1) ? boardSize - 1 : 0;

        ChessPiece rook = chessPieces[rookX, currentY];
        
        if (rook && rook is Rook && !rook.hasMoved && !hasMoved && rook.chessPieceTeam == chessPieceTeam)
        {
            int step = (direction == 1) ? 1 : -1;
            for (int i = currentX + step; i != rookX; i += step)
            {
                if (chessPieces[i, currentY] != null) return false;
            }
        }
        else return false;
        
        return true;
    }
}