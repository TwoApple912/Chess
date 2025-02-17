#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChessManager))]
public class ChessManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector for all other variables and components
        DrawDefaultInspector();

        // Reference the target script (ChessManager)
        ChessManager manager = (ChessManager)target;

        // Check if the 2D array exists and is populated
        if (manager.chessPieces != null && manager.tiles != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Current Layout", EditorStyles.boldLabel);

            // Iterate through the array to display it
            for (int y = manager.boardSize - 1; y >= 0; y--) // Loop from top to bottom
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < manager.boardSize; x++)
                {
                    ChessPiece piece = manager.chessPieces[x, y];
                    string pieceDisplay = "."; // Default to dot for empty spaces

                    if (piece != null)
                    {
                        // Get the first letter of the piece's name
                        char initialLetter = piece.name[6];
                        // Determine team and adjust case accordingly
                        bool isWhiteTeam = piece.chessPieceTeam == ChessPieceTeam.White;
                        pieceDisplay = isWhiteTeam ? char.ToUpper(initialLetter).ToString() : char.ToLower(initialLetter).ToString();
                    }

                    // Show a compact 20x20 box for each piece's display
                    GUILayout.Label(pieceDisplay, GUILayout.Width(20), GUILayout.Height(20));
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label("\nGo into play mode to see the board layout.\n" +
                            "Or that the chessPieces array is not initialized.");
        }
    }
}
#endif