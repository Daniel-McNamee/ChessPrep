namespace ChessProject.ViewModels
{
    // Indicates how the last move squares should be visually highlighted on the board.
    // Used by SquareViewModel to determine highlight colour after a move is applied.
    // Set during move application in BoardViewModel (normal move vs capture).
    public enum LastMoveHighlightType
    {
        None,    // No highlight
        Normal,  // Standard move highlight (non-capture)
        Capture  // Capture move highlight (different colour)
    }
}
