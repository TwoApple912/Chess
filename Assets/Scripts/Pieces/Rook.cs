using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    public override int value { get; set; } = 5;
    
    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int boardSize)
    {
        List<Vector2Int> availableMoves = new List<Vector2Int>();

        int[] xDirections = { 1, -1, 0, 0 };
        int[] yDirections = { 0, 0, 1, -1 };

        for (int i = 0; i < xDirections.Length; i++)
        {
            int x = currentX;
            int y = currentY;

            while (true)
            {
                x += xDirections[i];
                y += yDirections[i];

                if (!IsValidMove(x, y, boardSize)) break;

                if (board[x, y] == null)
                {
                    availableMoves.Add(new Vector2Int(x, y));
                }
                else
                {
                    if (board[x, y]?.chessPieceTeam != chessPieceTeam ||
                        (board[x, y]?.chessPieceTeam == chessPieceTeam && board[x, y].IsAvailableToAttach(false) &&
                         IsAvailableToAttach(true) &&
                         (board[x, y].value != this.value || ChessManager.Instance.WildMode)))
                        availableMoves.Add(new Vector2Int(x, y));
                    break;
                }
            }
        }
        
        return availableMoves;
    }
}
