namespace ChessProject.ViewModels
{
    // Represents which side of the chessboard is shown at the bottom of the UI.
    // Used by BoardViewModel to determine VisualSquares ordering (flipped or normal).
    // Set automatically when loading an opening (white/black) or via FlipBoardCommand.
    public enum BoardOrientation
    {
        WhiteBottom, // White pieces at bottom (standard orientation)
        BlackBottom  // Black pieces at bottom (board flipped)
    }
}
