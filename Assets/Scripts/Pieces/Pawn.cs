using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override int value { get; set; } = 1;
    
    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int boardSize)
    {
        List<Vector2Int> availableMoves = new List<Vector2Int>();

        int direction = (chessPieceTeam == ChessPieceTeam.White) ? 1 : -1;
        
        // Forward movement by 1 tile
        if (IsValidMove(currentX, currentY + direction, boardSize) && board[currentX, currentY + direction] == null)
        {
            availableMoves.Add(new Vector2Int(currentX, currentY + direction));

            // Double movement if first move
            if (!hasMoved && board[currentX, currentY + 2 * direction] == null)
            {
                availableMoves.Add(new Vector2Int(currentX, currentY + 2 * direction));
            }
            else if (!hasMoved && board[currentX, currentY + 2 * direction].chessPieceTeam == chessPieceTeam &&
                     board[currentX, currentY + 2 * direction].IsAvailableToAttach(false) &&
                     IsAvailableToAttach(true) &&
                     board[currentX, currentY + 2 * direction].value != this.value)
            {
                availableMoves.Add(new Vector2Int(currentX, currentY + 2 * direction));
            }
        }
        else if (IsValidMove(currentX, currentY + direction, boardSize) &&
                 board[currentX, currentY + direction].chessPieceTeam == chessPieceTeam &&
                 board[currentX, currentY + direction].IsAvailableToAttach(false) &&
                 IsAvailableToAttach(true) &&
                 board[currentX, currentY + direction].value != this.value) // Add attachment for moving forward
            availableMoves.Add(new Vector2Int(currentX, currentY + direction));

        // Capture diagonal left
        if (IsValidMove(currentX - 1, currentY + direction, boardSize) &&
            board[currentX - 1, currentY + direction] != null &&
            board[currentX - 1, currentY + direction].GetComponent<ChessPiece>().chessPieceTeam != chessPieceTeam)
        {
            availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
        }
        
        // Capture diagonal right
        if (IsValidMove(currentX + 1, currentY + direction, boardSize) &&
            board[currentX + 1, currentY + direction] != null &&
            board[currentX + 1, currentY + direction].GetComponent<ChessPiece>().chessPieceTeam != chessPieceTeam)
        {
            availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
        }
        
        // En Passant
        if (ChessManager.Instance.enPassantMove.HasValue)
        {
            Vector2Int enPassantPosition = ChessManager.Instance.enPassantMove.Value;

            if (enPassantPosition.x == currentX - 1 && enPassantPosition.y == currentY + direction &&
                board[currentX - 1, currentY] != null &&
                board[currentX - 1, currentY].GetComponent<ChessPiece>().chessPieceTeam != chessPieceTeam)
            {
                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
            }

            if (enPassantPosition.x == currentX + 1 && enPassantPosition.y == currentY + direction &&
                board[currentX + 1, currentY] != null &&
                board[currentX + 1, currentY].GetComponent<ChessPiece>().chessPieceTeam != chessPieceTeam)
            {
                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
            }
        }

        return availableMoves;
    }
}
