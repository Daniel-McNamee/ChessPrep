using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ChessProject.Models;
using ChessProject.Services;

namespace ChessProject.ViewModels
{
    public class BoardViewModel : ViewModelBase
    {
        #region Properties
        // Properties :

        // All 64 board squares (data source for chessboard UI)
        // Bound to ItemsControl in BoardView.xaml
        public ObservableCollection<SquareViewModel> Squares { get; }

        // All openings loaded from JSON dataset
        private List<Openings> _openings;

        // Currently selected opening (loaded from OpeningBrowser)
        private Openings _currentOpening;

        // Moves displayed in the side panel move list
        // Bound to move list UI and highlights current move
        public ObservableCollection<MoveViewModel> DisplayMoves { get; }

        // Commands:
        // Bound to board control buttons in UI
        public ICommand ResetCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand FlipBoardCommand { get; }


        // Opening Information (displayed in side panel)
        // Values come from currently loaded opening
        public string OpeningName
        {
            get => _currentOpening?.Opening ?? "No opening loaded";
        }

        public string ECOCode
        {
            get => _currentOpening?.ECO ?? string.Empty;
        }

        public double WhiteWinPercentage
        {
            get => _currentOpening?.WhiteWinPercent ?? 0;
        }

        public double DrawPercentage
        {
            get => _currentOpening?.DrawPercent ?? 0;
        }

        public double BlackWinPercentage
        {
            get => _currentOpening?.BlackWinPercent ?? 0;
        }

        // Move sequence for current opening (SAN notation only)
        // Used by move application engine to rebuild board state
        private List<string> _moves = new List<string>();

        public string WhitePlayer { get; private set; }
        public int WhiteRating { get; private set; }

        public string BlackPlayer { get; private set; }
        public int BlackRating { get; private set; }

        public string GameResult { get; private set; }
        public string GameDate { get; private set; }

        public string GameTimeControl { get; private set; }


        private bool _isOpeningMode;
        public bool IsOpeningMode
        {
            get => _isOpeningMode;
            set
            {
                _isOpeningMode = value;
                OnPropertyChanged();
            }
        }

        private bool _isGameMode;
        public bool IsGameMode
        {
            get => _isGameMode;
            set
            {
                _isGameMode = value;
                OnPropertyChanged();
            }
        }

        public MoveViewModel CurrentMove
        {
            get
            {
                if (CurrentMoveIndex < 0 || CurrentMoveIndex >= DisplayMoves.Count)
                    return null;

                return DisplayMoves[CurrentMoveIndex];
            }
        }

        #endregion


        #region Constructors
        // Constructors :
        public BoardViewModel()
        {
            IsOpeningMode = true;
            IsGameMode = false;

            // Create board square collection and populate with 64 squares
            Squares = new ObservableCollection<SquareViewModel>();
            CreateBoard();

            // Initialize board control commands (bound to UI buttons)
            ResetCommand = new RelayCommand(ResetBoard);
            NextCommand = new RelayCommand(NextMove, () => CurrentMoveIndex < _moves.Count);
            PreviousCommand = new RelayCommand(PreviousMove, () => CurrentMoveIndex > 0);
            FlipBoardCommand = new RelayCommand(FlipBoard);

            // Initialize move list displayed in side panel
            DisplayMoves = new ObservableCollection<MoveViewModel>();

            try
            {
                // Load openings dataset from JSON file
                _openings = OpeningDataService.LoadOpenings();

                // Select first opening as default
                _currentOpening = _openings.FirstOrDefault();

                // Extract SAN moves from opening (remove move numbers)
                _moves = _currentOpening.MovesList
                    .Where(m => !string.IsNullOrWhiteSpace(m) && !char.IsDigit(m[0]))
                    .ToList();
            }
            catch (Exception ex)
            {
                // Fail safely so the window still opens if JSON load fails
                System.Diagnostics.Debug.WriteLine(ex);
                _moves = new List<string>();
            }
        }
        #endregion


        // Methods :
        #region Board Setup
        // Generate Board
        private void CreateBoard()
        {
            // Clear any existing squares
            Squares.Clear();

            // Create 8×8 grid of square view models
            // Each square stores its board coordinates and colour
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Chessboard pattern: alternating light/dark squares
                    bool isLightSquare = (row + col) % 2 == 0;

                    Squares.Add(new SquareViewModel(row, col, isLightSquare));
                }
            }

            // After creating empty board, place pieces in starting layout
            SetStartingPosition();
        }

        // Get Square
        private SquareViewModel GetSquare(int row, int column)
        {
            // Find square by its board coordinates
            return Squares.First(s => s.Row == row && s.Column == column);
        }

        // Set starting position
        private void SetStartingPosition()
        {
            // Clear all squares first
            foreach (var square in Squares)
            {
                square.ClearPiece();
            }

            // Pawns
            for (int col = 0; col < 8; col++)
            {
                GetSquare(1, col).SetPiece(new ChessPiece(PieceType.Pawn, PieceColour.Black));
                GetSquare(6, col).SetPiece(new ChessPiece(PieceType.Pawn, PieceColour.White));
            }

            // Rooks
            GetSquare(0, 0).SetPiece(new ChessPiece(PieceType.Rook, PieceColour.Black));
            GetSquare(0, 7).SetPiece(new ChessPiece(PieceType.Rook, PieceColour.Black));
            GetSquare(7, 0).SetPiece(new ChessPiece(PieceType.Rook, PieceColour.White));
            GetSquare(7, 7).SetPiece(new ChessPiece(PieceType.Rook, PieceColour.White));

            // Knights
            GetSquare(0, 1).SetPiece(new ChessPiece(PieceType.Knight, PieceColour.Black));
            GetSquare(0, 6).SetPiece(new ChessPiece(PieceType.Knight, PieceColour.Black));
            GetSquare(7, 1).SetPiece(new ChessPiece(PieceType.Knight, PieceColour.White));
            GetSquare(7, 6).SetPiece(new ChessPiece(PieceType.Knight, PieceColour.White));

            // Bishops
            GetSquare(0, 2).SetPiece(new ChessPiece(PieceType.Bishop, PieceColour.Black));
            GetSquare(0, 5).SetPiece(new ChessPiece(PieceType.Bishop, PieceColour.Black));
            GetSquare(7, 2).SetPiece(new ChessPiece(PieceType.Bishop, PieceColour.White));
            GetSquare(7, 5).SetPiece(new ChessPiece(PieceType.Bishop, PieceColour.White));

            // Queens
            GetSquare(0, 3).SetPiece(new ChessPiece(PieceType.Queen, PieceColour.Black));
            GetSquare(7, 3).SetPiece(new ChessPiece(PieceType.Queen, PieceColour.White));

            // Kings
            GetSquare(0, 4).SetPiece(new ChessPiece(PieceType.King, PieceColour.Black));
            GetSquare(7, 4).SetPiece(new ChessPiece(PieceType.King, PieceColour.White));
        }

        #endregion

        #region Opening Loading
        // Load selected opening
        public void LoadOpening(Openings opening)
        {
            IsOpeningMode = true;
            IsGameMode = false;

            // Ignore null selections
            if (opening == null)
                return;

            // Store selected opening so metadata properties update
            _currentOpening = opening;

            // Set board orientation based on the opening colour
            if (opening.Colour.Equals("black", StringComparison.OrdinalIgnoreCase))
            {
                Orientation = BoardOrientation.BlackBottom;
            }
            else
            {
                Orientation = BoardOrientation.WhiteBottom;
            }

            // Extract SAN move list from opening
            _moves = opening.MovesList
                .Where(m => !string.IsNullOrWhiteSpace(m) && !char.IsDigit(m[0])) // Remove move numbers and empty entries from JSON list
                .ToList();

            // Rebuild move display list for UI move panel
            DisplayMoves.Clear();

            for (int i = 0; i < _moves.Count; i++)
            {
                DisplayMoves.Add(new MoveViewModel(i, _moves[i]));
            }

            // Reset playback to start of opening
            CurrentMoveIndex = 0;

            // Reset board to starting chess position
            SetStartingPosition();

            // Ensure move highlight state is correct at start
            UpdateCurrentMoveHighlight();

            // Update navigation button enabled state
            (NextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviousCommand as RelayCommand)?.RaiseCanExecuteChanged();

            // Notify UI that opening metadata changed
            OnPropertyChanged(nameof(OpeningName));
            OnPropertyChanged(nameof(ECOCode));
            OnPropertyChanged(nameof(WhiteWinPercentage));
            OnPropertyChanged(nameof(DrawPercentage));
            OnPropertyChanged(nameof(BlackWinPercentage));

            // Notify UI that board orientation may have changed
            OnPropertyChanged(nameof(VisualSquares));
        }
        #endregion

        #region Game Loading
        public void LoadGame(ChessGame game)
        {
            IsOpeningMode = false;
            IsGameMode = true;

            var moves = PgnParser.ExtractMoves(game.Pgn);

            _moves = moves;

            DisplayMoves.Clear();

            for (int i = 0; i < _moves.Count; i++)
            {
                DisplayMoves.Add(new MoveViewModel(i, _moves[i]));
            }

            WhitePlayer = game.White;
            WhiteRating = game.WhiteElo;

            BlackPlayer = game.Black;
            BlackRating = game.BlackElo;

            GameResult = game.Result;
            GameDate = game.Date;

            GameTimeControl = game.TimeControl;

            OnPropertyChanged(nameof(WhitePlayer));
            OnPropertyChanged(nameof(WhiteRating));
            OnPropertyChanged(nameof(BlackPlayer));
            OnPropertyChanged(nameof(BlackRating));
            OnPropertyChanged(nameof(GameResult));
            OnPropertyChanged(nameof(GameDate));
            OnPropertyChanged(nameof(GameTimeControl));

            CurrentMoveIndex = 0;
            SetStartingPosition();
        }

        public void LoadMovesFromPgn(string pgn)
        {
            var moves = Services.PgnParser.ExtractMoves(pgn);

            _moves = moves;

            DisplayMoves.Clear();

            for (int i = 0; i < moves.Count; i++)
            {
                DisplayMoves.Add(new MoveViewModel(i, moves[i]));
            }

            CurrentMoveIndex = 0;

            SetStartingPosition();
        }

        #endregion


        #region Flip Board
        // Board Orientation Logic

        // Current board orientation state
        // Determines which side of the board appears at the bottom of the UI
        private BoardOrientation _orientation = BoardOrientation.WhiteBottom;

        public BoardOrientation Orientation
        {
            get => _orientation;
            set
            {
                _orientation = value;

                // Notify UI that orientation changed
                OnPropertyChanged();

                // VisualSquares order depends on orientation. Must refresh board view
                OnPropertyChanged(nameof(VisualSquares));
            }
        }

        // Squares in the order they should be rendered in the UI
        // Returns normal order for WhiteBottom
        // Returns reversed board order for BlackBottom
        public IEnumerable<SquareViewModel> VisualSquares
        {
            get
            {
                if (Orientation == BoardOrientation.WhiteBottom)
                    return Squares;

                // Flip vertically and horizontally to show board from black perspective
                return Squares
                    .OrderByDescending(s => s.Row)
                    .ThenByDescending(s => s.Column);
            }
        }

        // Toggle board orientation (called by FlipBoardCommand)
        private void FlipBoard()
        {
            Orientation = Orientation == BoardOrientation.WhiteBottom
                ? BoardOrientation.BlackBottom
                : BoardOrientation.WhiteBottom;
        }

        #endregion


        #region Move Navigation

        // Highlight current move in the move list UI
        // Marks the move at CurrentMoveIndex - 1 as active
        // (because CurrentMoveIndex represents how many moves have been applied)
        private void UpdateCurrentMoveHighlight()
        {
            for (int i = 0; i < DisplayMoves.Count; i++)
            {
                DisplayMoves[i].IsCurrent = (i == CurrentMoveIndex - 1);
            }
        }

        // Current position in the move sequence
        // Represents how many moves have been applied to the board
        private int _currentMoveIndex;

        public int CurrentMoveIndex
        {
            get => _currentMoveIndex;
            private set
            {
                _currentMoveIndex = value;

                // Notify UI bindings (move counter, highlights, etc.)
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentMoveDisplay));

                // Navigation buttons depend on move position
                (PreviousCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (NextCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // Text shown in UI indicating current move number
        public string CurrentMoveDisplay
        {
            get => $"Move: {CurrentMoveIndex}";
        }

        // Clear last-move square highlights from all board squares
        // Called before applying a new move or rebuilding position
        private void ClearLastMoveHighlights()
        {
            foreach (var square in Squares)
                square.ClearHighlight();
        }

        // Reset board back to starting position
        private void ResetBoard()
        {
            CurrentMoveIndex = 0;
            RebuildBoardToCurrentMove();

            UpdateCurrentMoveHighlight();

            OnPropertyChanged(nameof(CurrentMove));
        }

        // Apply next move in the opening sequence
        private void NextMove()
        {
            if (CurrentMoveIndex >= _moves.Count)
                return;

            CurrentMoveIndex++;
            RebuildBoardToCurrentMove();

            UpdateCurrentMoveHighlight();

            OnPropertyChanged(nameof(CurrentMove));
        }

        // Undo last move
        private void PreviousMove()
        {
            if (CurrentMoveIndex <= 0)
                return;

            CurrentMoveIndex--;
            RebuildBoardToCurrentMove();

            UpdateCurrentMoveHighlight();

            OnPropertyChanged(nameof(CurrentMove));
        }

        // Rebuild board state from scratch up to CurrentMoveIndex
        private void RebuildBoardToCurrentMove()
        {
            SetStartingPosition();
            ClearLastMoveHighlights();

            for (int i = 0; i < CurrentMoveIndex; i++)
            {
                ApplyMove(_moves[i], i);
            }
        }

        // Parse SAN move string and route to correct piece logic
        private void ApplyMove(string move, int moveIndex)
        {
            bool isWhiteMove = moveIndex % 2 == 0;
            bool isCapture = move.Contains("x");

            // Castling
            if (move == "O-O" || move == "O-O+" || move == "O-O#")
            {
                ApplyCastle(isWhiteMove, true); // King side
                return;
            }

            if (move == "O-O-O" || move == "O-O-O+" || move == "O-O-O#")
            {
                ApplyCastle(isWhiteMove, false); // Queen side
                return;
            }

            // Pawn capture
            if (move.Length == 4 && move.Contains("x") && char.IsLower(move[0]))
            {
                ApplyPawnCapture(move, isWhiteMove);
                return;
            }

            // Knight capture
            if (move.StartsWith("N") && move.Contains("x"))
            {
                ApplyKnightCapture(move, isWhiteMove);
                return;
            }

            // Bishop capture
            if (move.StartsWith("B") && move.Contains("x"))
            {
                ApplyBishopCapture(move, isWhiteMove);
                return;
            }

            // Rook capture
            if (move.StartsWith("R") && move.Contains("x"))
            {
                ApplyRookCapture(move, isWhiteMove);
                return;
            }

            // Queen capture
            if (move.StartsWith("Q") && move.Contains("x"))
            {
                ApplyQueenCapture(move, isWhiteMove);
                return;
            }

            // King capture
            if (move.StartsWith("K") && move.Contains("x"))
            {
                ApplyKingCapture(move, isWhiteMove);
                return;
            }

            // Pawn move (e4, d5)
            if (char.IsLower(move[0]))
            {
                ApplyPawnMove(move, isWhiteMove);
                return;
            }

            // Knight move (Nf3, Nc6)
            if (move.StartsWith("N"))
            {
                ApplyKnightMove(move, isWhiteMove);
                return;
            }

            // Bishop move (Bb5, Be3)
            if (move.StartsWith("B"))
            {
                ApplyBishopMove(move, isWhiteMove);
                return;
            }

            // Rook move (Ra1, Rd8)
            if (move.StartsWith("R"))
            {
                ApplyRookMove(move, isWhiteMove);
                return;
            }

            // Queen move (Qd3, Qh5)
            if (move.StartsWith("Q"))
            {
                ApplyQueenMove(move, isWhiteMove);
                return;
            }

            // King move (Ke2, Kf1)
            if (move.StartsWith("K"))
            {
                ApplyKingMove(move, isWhiteMove);
                return;
            }
        }

        #endregion


        #region Piece Logic
        // === Chess coordinate conversion ===
        // SAN chess notation uses file + rank (e.g e4, Nf3).
        // Our board uses 0 based array indices:
        //
        // Files (columns): a–h = 0–7
        //   column = file - 'a'
        //   Example: 'e' - 'a' = 4 → column 4
        //
        // Ranks (rows): 8–1 = 0–7 (top to bottom in array)
        //   row = 8 - rank
        //   Example: rank '1' = 8 - 1 = 7 → bottom row
        //
        // So square e1 is (row 7, column 4)


        // Pawn move logic (supports one and two square advances)
        private void ApplyPawnMove(string move, bool isWhite)
        {
            // Convert file letter to column index (a=0 … h=7)
            int targetColumn = move[0] - 'a';

            // Convert rank number to row index (8-0 … 1-7)
            int targetRow = 8 - int.Parse(move[1].ToString());

            // White moves up the board (row decreases), black moves down (row increases)
            int direction = isWhite ? -1 : 1;

            // Possible source rows:
            int oneStepRow = targetRow - direction;
            int twoStepRow = targetRow - (2 * direction);

            SquareViewModel fromSquare = null;

            // Try one-square move first
            if (IsValidPawnSource(oneStepRow, targetColumn, isWhite))
            {
                fromSquare = GetSquare(oneStepRow, targetColumn);
            }
            // Otherwise check two-square initial move
            else if (IsValidPawnSource(twoStepRow, targetColumn, isWhite))
            {
                fromSquare = GetSquare(twoStepRow, targetColumn);
            }

            // No valid pawn found = invalid move or unsupported SAN
            if (fromSquare == null)
                return;

            SquareViewModel toSquare = GetSquare(targetRow, targetColumn);

            // Clear previous move highlights before applying new move
            ClearLastMoveHighlights();

            // Move pawn
            toSquare.SetPiece(fromSquare.Piece);
            fromSquare.ClearPiece();

            // Mark origin and destination as last move squares
            fromSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
            toSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
        }


        // Validate pawn source square
        // Checks:
        // - square is on board
        // - square contains pawn
        // - pawn colour matches move side
        private bool IsValidPawnSource(int row, int column, bool isWhite)
        {
            if (row < 0 || row > 7)
                return false;

            SquareViewModel square = GetSquare(row, column);

            return square.HasPiece &&
                   square.Piece.Type == PieceType.Pawn &&
                   square.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black);
        }


        // Pawn capture logic
        private void ApplyPawnCapture(string move, bool isWhite)
        {
            // move[0] : source file
            // move[2] : target file
            // move[3] : target rank
            int sourceColumn = move[0] - 'a';
            int targetColumn = move[2] - 'a';
            int targetRow = 8 - int.Parse(move[3].ToString());

            // Pawn capture always comes from one row behind target
            int direction = isWhite ? -1 : 1;
            int sourceRow = targetRow - direction;

            SquareViewModel fromSquare = GetSquare(sourceRow, sourceColumn);
            SquareViewModel toSquare = GetSquare(targetRow, targetColumn);

            // No pawn at expected source = invalid move
            if (!fromSquare.HasPiece)
                return;

            ClearLastMoveHighlights();

            // Capture piece
            toSquare.SetPiece(fromSquare.Piece);
            fromSquare.ClearPiece();

            // Mark capture squares differently for UI styling
            fromSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            toSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
        }



        // Knight move logic
        private void ApplyKnightMove(string move, bool isWhite)
        {
            // move[1] : target file
            // move[2] : target rank

            int targetColumn = move[1] - 'a';
            int targetRow = 8 - int.Parse(move[2].ToString());

            // Find the knight of the correct colour that can legally reach the target square
            SquareViewModel knightSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Knight &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsKnightMove(s.Row, s.Column, targetRow, targetColumn));

            if (knightSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            ClearLastMoveHighlights();

            targetSquare.SetPiece(knightSquare.Piece);
            knightSquare.ClearPiece();

            knightSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
        }


        // Knight move validation
        private bool IsKnightMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            int rowDiff = Math.Abs(fromRow - toRow);
            int colDiff = Math.Abs(fromCol - toCol);

            // Knight moves in L shape: 2×1 or 1×2
            return (rowDiff == 2 && colDiff == 1) ||
                   (rowDiff == 1 && colDiff == 2);
        }


        // Knight capture logic
        private void ApplyKnightCapture(string move, bool isWhite)
        {
            // move[2] : target file
            // move[3] : target rank

            int targetColumn = move[2] - 'a';
            int targetRow = 8 - int.Parse(move[3].ToString());

            // Find the knight of the correct colour that can legally capture on the target square
            SquareViewModel knightSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Knight &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsKnightMove(s.Row, s.Column, targetRow, targetColumn));

            if (knightSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            ClearLastMoveHighlights();

            targetSquare.SetPiece(knightSquare.Piece);
            knightSquare.ClearPiece();

            knightSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
        }



        // Bishop move logic
        // 0 = piece ('B')
        // 1 = target file (column)
        // 2 = target rank (row)
        private void ApplyBishopMove(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[1] - 'a'; // file to column index
            int targetRow = 8 - int.Parse(move[2].ToString()); // rank to row index

            // Find the bishop of the correct colour that can legally reach target square
            SquareViewModel bishopSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Bishop &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsDiagonalMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearDiagonal(s.Row, s.Column, targetRow, targetColumn));

            if (bishopSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Move piece
            targetSquare.SetPiece(bishopSquare.Piece);
            bishopSquare.ClearPiece();

            // Highlight move squares
            bishopSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
        }

        // Checks whether a move is diagonal 
        private bool IsDiagonalMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            return Math.Abs(fromRow - toRow) == Math.Abs(fromCol - toCol);
        }

        // Ensures no pieces block the bishop's diagonal path
        private bool IsPathClearDiagonal(int fromRow, int fromCol, int toRow, int toCol)
        {
            int rowStep = toRow > fromRow ? 1 : -1;
            int colStep = toCol > fromCol ? 1 : -1;

            int currentRow = fromRow + rowStep;
            int currentCol = fromCol + colStep;

            // Walk along diagonal until target square
            while (currentRow != toRow && currentCol != toCol)
            {
                if (GetSquare(currentRow, currentCol).HasPiece)
                    return false;

                currentRow += rowStep;
                currentCol += colStep;
            }

            return true;
        }

        // Bishop capture logic
        // 0 = piece ('B')
        // 1 = capture marker ('x')
        // 2 = target file
        // 3 = target rank
        private void ApplyBishopCapture(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[2] - 'a';
            int targetRow = 8 - int.Parse(move[3].ToString());

            // Find bishop that can capture on target square
            SquareViewModel bishopSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Bishop &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsDiagonalMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearDiagonal(s.Row, s.Column, targetRow, targetColumn));

            if (bishopSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Capture piece
            targetSquare.SetPiece(bishopSquare.Piece);
            bishopSquare.ClearPiece();

            // Highlight capture squares
            bishopSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
        }



        // Rook move logic
        // 0 = piece ('R')
        // 1 = target file (column)
        // 2 = target rank (row)
        private void ApplyRookMove(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[1] - 'a'; // file to column index
            int targetRow = 8 - int.Parse(move[2].ToString()); // rank to row index

            // Find the rook of the correct colour that can legally reach target square
            SquareViewModel rookSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Rook &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsStraightMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearStraight(s.Row, s.Column, targetRow, targetColumn));

            if (rookSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Move piece
            targetSquare.SetPiece(rookSquare.Piece);
            rookSquare.ClearPiece();

            // Highlight move squares
            rookSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
        }

        // Checks whether a move is horizontal or vertical
        private bool IsStraightMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            return fromRow == toRow || fromCol == toCol;
        }

        // Ensures no pieces block the rook's straight path
        private bool IsPathClearStraight(int fromRow, int fromCol, int toRow, int toCol)
        {
            // Determine step direction along rank/file
            int rowStep = fromRow == toRow ? 0 : (toRow > fromRow ? 1 : -1);
            int colStep = fromCol == toCol ? 0 : (toCol > fromCol ? 1 : -1);

            int currentRow = fromRow + rowStep;
            int currentCol = fromCol + colStep;

            // Walk along rank/file until target square
            while (currentRow != toRow || currentCol != toCol)
            {
                if (GetSquare(currentRow, currentCol).HasPiece)
                    return false;

                currentRow += rowStep;
                currentCol += colStep;
            }

            return true;
        }

        // Rook capture logic
        // 0 = piece ('R')
        // 1 = capture marker ('x')
        // 2 = target file
        // 3 = target rank
        private void ApplyRookCapture(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[2] - 'a';
            int targetRow = 8 - int.Parse(move[3].ToString());

            // Find rook that can capture on target square
            SquareViewModel rookSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Rook &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsStraightMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearStraight(s.Row, s.Column, targetRow, targetColumn));

            if (rookSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Capture piece
            targetSquare.SetPiece(rookSquare.Piece);
            rookSquare.ClearPiece();

            // Highlight capture squares
            rookSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
        }



        // Queen move logic 
        // 0 = piece ('Q')
        // 1 = target file (column)
        // 2 = target rank (row)
        private void ApplyQueenMove(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[1] - 'a'; // file to column index
            int targetRow = 8 - int.Parse(move[2].ToString()); // rank to row index

            // Find the queen of the correct colour that can legally reach target square
            SquareViewModel queenSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Queen &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                (
                    // Combines rook and bishop movement
                    (IsDiagonalMove(s.Row, s.Column, targetRow, targetColumn) &&
                     IsPathClearDiagonal(s.Row, s.Column, targetRow, targetColumn))
                    ||
                    (IsStraightMove(s.Row, s.Column, targetRow, targetColumn) &&
                     IsPathClearStraight(s.Row, s.Column, targetRow, targetColumn))
                )
            );

            if (queenSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Move piece
            targetSquare.SetPiece(queenSquare.Piece);
            queenSquare.ClearPiece();

            // Highlight move squares
            queenSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
        }

        // Queen capture logic
        // 0 = piece ('Q')
        // 1 = capture marker ('x')
        // 2 = target file
        // 3 = target rank
        private void ApplyQueenCapture(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[2] - 'a';
            int targetRow = 8 - int.Parse(move[3].ToString());

            // Find the queen that can capture on target square
            SquareViewModel queenSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Queen &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                (
                    (IsDiagonalMove(s.Row, s.Column, targetRow, targetColumn) &&
                     IsPathClearDiagonal(s.Row, s.Column, targetRow, targetColumn))
                    ||
                    (IsStraightMove(s.Row, s.Column, targetRow, targetColumn) &&
                     IsPathClearStraight(s.Row, s.Column, targetRow, targetColumn))
                )
            );

            if (queenSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Capture piece
            targetSquare.SetPiece(queenSquare.Piece);
            queenSquare.ClearPiece();

            // Highlight capture squares
            queenSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
        }



        // King move logic 
        // 0 = piece ('K')
        // 1 = target file (column)
        // 2 = target rank (row)
        private void ApplyKingMove(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[1] - 'a'; // file to column index
            int targetRow = 8 - int.Parse(move[2].ToString()); // rank to row index

            // Find the king of the correct colour that can legally reach target square
            SquareViewModel kingSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.King &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsKingMove(s.Row, s.Column, targetRow, targetColumn));

            if (kingSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Move piece
            targetSquare.SetPiece(kingSquare.Piece);
            kingSquare.ClearPiece();

            // Highlight move squares
            kingSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
        }

        // Checks whether a move is a valid king move (one square any direction)
        // King can move exactly 1 square horizontally, vertically, or diagonally
        private bool IsKingMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            int rowDiff = Math.Abs(fromRow - toRow);
            int colDiff = Math.Abs(fromCol - toCol);

            // Within one square and not the same square
            return rowDiff <= 1 && colDiff <= 1 && (rowDiff + colDiff) > 0;
        }

        // King capture logic
        // 0 = piece ('K')
        // 1 = capture marker ('x')
        // 2 = target file
        // 3 = target rank
        private void ApplyKingCapture(string move, bool isWhite)
        {
            // Convert SAN target square to board coordinates
            int targetColumn = move[2] - 'a';
            int targetRow = 8 - int.Parse(move[3].ToString());

            // Find the king that can capture on target square
            SquareViewModel kingSquare = Squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.King &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsKingMove(s.Row, s.Column, targetRow, targetColumn));

            if (kingSquare == null)
                return;

            SquareViewModel targetSquare = GetSquare(targetRow, targetColumn);

            // Remove previous move highlight
            ClearLastMoveHighlights();

            // Capture piece
            targetSquare.SetPiece(kingSquare.Piece);
            kingSquare.ClearPiece();

            // Highlight capture squares
            kingSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            targetSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
        }

        private void ApplyCastle(bool isWhite, bool kingSide)
        {
            int row = isWhite ? 7 : 0;

            SquareViewModel kingFrom = GetSquare(row, 4);
            SquareViewModel rookFrom;
            SquareViewModel kingTo;
            SquareViewModel rookTo;

            if (kingSide)
            {
                // King side castle
                rookFrom = GetSquare(row, 7);
                kingTo = GetSquare(row, 6);
                rookTo = GetSquare(row, 5);
            }
            else
            {
                // Queen side castle
                rookFrom = GetSquare(row, 0);
                kingTo = GetSquare(row, 2);
                rookTo = GetSquare(row, 3);
            }

            if (!kingFrom.HasPiece || !rookFrom.HasPiece)
                return;

            ClearLastMoveHighlights();

            // Move king
            kingTo.SetPiece(kingFrom.Piece);
            kingFrom.ClearPiece();

            // Move rook
            rookTo.SetPiece(rookFrom.Piece);
            rookFrom.ClearPiece();

            // Highlight king move
            kingFrom.LastMoveHighlight = LastMoveHighlightType.Normal;
            kingTo.LastMoveHighlight = LastMoveHighlightType.Normal;
        }


        #endregion
    }
}
