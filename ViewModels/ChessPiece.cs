namespace ChessProject.ViewModels
{
    // Enums
    public enum PieceType
    {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }

    public enum PieceColour
    {
        White,
        Black
    }

    public class ChessPiece
    {
        // Properties:
        public PieceType Type { get; }
        public PieceColour Colour { get; }

        // Constructors:
        public ChessPiece(PieceType type, PieceColour colour)
        {
            Type = type;
            Colour = colour;
        }
    }
}