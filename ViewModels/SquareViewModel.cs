using System.Windows.Media;

namespace ChessProject.ViewModels
{
    // Represents a single square on the chess board
    public class SquareViewModel : ViewModelBase
    {
        // Properties:
        // Board position
        public int Row { get; }
        public int Column { get; }

        // Background colour of the square (light or dark)
        public Brush SquareColor { get; }

        // The chess piece currently on this square (null if empty)
        private ChessPiece _piece;

        // Exposes the piece on the square and notifies the UI when it changes
        public ChessPiece Piece
        {
            get => _piece;
            private set
            {
                _piece = value;

                // Notify bindings that the piece state has changed
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPiece));
                OnPropertyChanged(nameof(PieceImagePath));
            }
        }

        // Highlights Last Move Square
        private LastMoveHighlightType _lastMoveHighlight;
        public LastMoveHighlightType LastMoveHighlight
        {
            get => _lastMoveHighlight;
            set
            {
                _lastMoveHighlight = value;
                OnPropertyChanged();
            }
        }

        public void ClearHighlight()
        {
            LastMoveHighlight = LastMoveHighlightType.None;
        }


        // Indicates whether the square currently contains a piece
        public bool HasPiece => Piece != null;

        // Returns the image path for the piece on this square (or null if empty)
        public string PieceImagePath
        {
            get
            {
                if (Piece == null)
                    return null;

                return $"/Assets/Pieces/{Piece.Colour.ToString().ToLower()}_{Piece.Type.ToString().ToLower()}.png";
            }
        }

        // Constructors:
        // initialises board position and square colour
        public SquareViewModel(int row, int column, bool isLightSquare)
        {
            Row = row;
            Column = column;

            // Assign light or dark square colour
            SquareColor = isLightSquare
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDD0")) // Light square
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#69923E")); // Dark square
        }

        // Methods:
        // Places a chess piece on this square
        public void SetPiece(ChessPiece piece)
        {
            Piece = piece;
        }

        // Removes any chess piece from this square
        public void ClearPiece()
        {
            Piece = null;
        }
    }
}
