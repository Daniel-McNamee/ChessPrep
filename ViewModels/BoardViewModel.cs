using ChessProject.Data;
using ChessProject.Entities;
using ChessProject.Models;
using ChessProject.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

        // Current player's turn
        public string CurrentTurnDisplay =>
            CurrentTurn == PieceColour.White ? "White to move" : "Black to move";

        // Flag to indicate if the game has ended (used to disable move navigation)
        private bool _isGameOver; 

        // Moves displayed in the side panel move list
        // Bound to move list UI and highlights current move
        public ObservableCollection<MoveViewModel> DisplayMoves { get; }

        // Commands:
        // Bound to board control buttons in UI
        public ICommand ResetCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand SelectMoveCommand { get; }
        public ICommand FlipBoardCommand { get; }
        public ICommand SaveGameCommand { get; }
        public ICommand SaveFavouriteOpeningCommand { get; }
        public ICommand SaveLocalGameCommand { get; }


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

        // En passant target square (if last move was a double pawn advance)
        private SquareViewModel _enPassantTarget;

        // UI Display Information for loaded game (from PGN metadata)
        public int TotalMoves => _moves?.Count ?? 0;

        public string WhitePlayer { get; private set; }
        public int WhiteRating { get; private set; }

        public string BlackPlayer { get; private set; }
        public int BlackRating { get; private set; }

        public string GameResult { get; private set; }
        public string GameDate { get; private set; }

        public string GameType { get; set; }

        private ChessGame _currentGame;
        public GameMode CurrentGameMode { get; set; }

        // Mode flags to control UI state and behaviour
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

        // Current move being displayed in the UI (null if at starting position)
        public MoveViewModel CurrentMove
        {
            get
            {
                if (CurrentMoveIndex < 0 || CurrentMoveIndex >= DisplayMoves.Count)
                    return null;

                return DisplayMoves[CurrentMoveIndex];
            }
        }

        // Service for generating legal moves (used for move application and validation)
        private readonly MoveGenerationService _moveService = new MoveGenerationService();

        private SquareViewModel _selectedSquare;
        private List<SquareViewModel> _legalMoves = new List<SquareViewModel>();

        public ICommand SquareClickCommand { get; }

        // Castling rights tracking
        private bool _whiteKingMoved = false;
        private bool _blackKingMoved = false;

        private bool _whiteKingsideRookMoved = false;
        private bool _whiteQueensideRookMoved = false;

        private bool _blackKingsideRookMoved = false;
        private bool _blackQueensideRookMoved = false;

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
            SelectMoveCommand = new RelayCommand<MoveViewModel>(SelectMove);
            FlipBoardCommand = new RelayCommand(FlipBoard);
            SaveGameCommand = new RelayCommand(SaveGame);
            SaveFavouriteOpeningCommand = new RelayCommand(SaveFavouriteOpening);
            SaveLocalGameCommand = new RelayCommand(SaveLocalGame);
            SquareClickCommand = new RelayCommand<SquareViewModel>(OnSquareClicked);

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
            // Set mode flags to update UI state and visibility of controls
            CurrentGameMode = GameMode.Opening;

            // Notify UI that mode changed so it can update visibility and bindings
            OnPropertyChanged(nameof(IsGameMode));
            OnPropertyChanged(nameof(IsOpeningMode));
            OnPropertyChanged(nameof(IsLocalGame));

            // Reset state
            IsOpeningMode = true;
            IsGameMode = false;
            ClearModeState();

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
                // Create view model for each move (stores move notation and user notes)
                var moveVm = new MoveViewModel(i, _moves[i]);

                // Subscribe to note changes so we can save them to the database
                moveVm.NoteChanged += (m) =>
                {
                    SaveNote(m);
                };

                DisplayMoves.Add(moveVm);
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
        private List<string> _clocks = new List<string>();

        public string WhiteClock
        {
            get
            {
                if (_clocks == null || _clocks.Count == 0)
                    return FormatClock(_startingTimeSeconds);

                int index = CurrentMoveIndex;

                if (index % 2 == 0)
                    return _clocks[Math.Min(index, _clocks.Count - 1)];

                return _clocks[Math.Max(index - 1, 0)];
            }
        }

        public string BlackClock
        {
            get
            {
                if (_clocks == null || _clocks.Count == 0)
                    return FormatClock(_startingTimeSeconds);

                int index = CurrentMoveIndex;

                if (index % 2 == 1)
                    return _clocks[Math.Min(index, _clocks.Count - 1)];

                return _clocks[Math.Max(index - 1, 0)];
            }
        }

        private string FormatClock(int seconds)
        {
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;

            return $"{minutes}:{remainingSeconds:D2}";
        }

        private int _startingTimeSeconds;

        // Load game from PGN data (used for replaying saved games)
        public void LoadGame(ChessGame game)
        {
            // Set mode flags to update UI state
            CurrentGameMode = GameMode.Replay;

            // Notify UI that mode changed so it can update visibility and bindings
            OnPropertyChanged(nameof(IsGameMode));
            OnPropertyChanged(nameof(IsOpeningMode));
            OnPropertyChanged(nameof(IsLocalGame));

            // Reset state
            IsOpeningMode = false;
            IsGameMode = true;
            _currentGame = game;
            _currentOpening = null;
            _isGameOver = false;
            ClearModeState();

            _startingTimeSeconds = game.StartingTimeSeconds;

            _moves = PgnParser.ExtractMoves(game.Pgn);
            _clocks = PgnParser.ExtractClocks(game.Pgn);
            OnPropertyChanged(nameof(TotalMoves));

            DisplayMoves.Clear();

            for (int i = 0; i < _moves.Count; i++)
            {
                // Create view model for each move (stores move notation and user notes)
                var moveVm = new MoveViewModel(i, _moves[i]);

                // Subscribe to note changes so we can save them to the database
                moveVm.NoteChanged += (m) =>
                {
                    SaveNote(m);
                };

                DisplayMoves.Add(moveVm);
            }

            LoadNotes();

            WhitePlayer = game.White;
            WhiteRating = game.WhiteElo;

            BlackPlayer = game.Black;
            BlackRating = game.BlackElo;

            // Set board orientation based on player's perspective
            if (!string.IsNullOrEmpty(game.PerspectivePlayer))
            {
                bool isWhite = game.PerspectivePlayer.Equals(game.White, StringComparison.OrdinalIgnoreCase);
                bool isBlack = game.PerspectivePlayer.Equals(game.Black, StringComparison.OrdinalIgnoreCase);

                if (isWhite)
                    Orientation = BoardOrientation.WhiteBottom;
                else if (isBlack)
                    Orientation = BoardOrientation.BlackBottom;
            }
            else
            {
                Orientation = BoardOrientation.WhiteBottom; // default if no perspective
            }

            GameResult = game.GetResultForPlayer();
            GameDate = game.Date;

            GameType = game.GameType;

            OnPropertyChanged(nameof(WhitePlayer));
            OnPropertyChanged(nameof(WhiteRating));
            OnPropertyChanged(nameof(BlackPlayer));
            OnPropertyChanged(nameof(BlackRating));
            OnPropertyChanged(nameof(GameResult));
            OnPropertyChanged(nameof(GameDate));
            OnPropertyChanged(nameof(GameType));
            OnPropertyChanged(nameof(WhiteClock));
            OnPropertyChanged(nameof(BlackClock));
            AddToRecentGames(game);

            CurrentMoveIndex = 0;
            CurrentTurn = PieceColour.White;
            StatusMessage = "";
            ClearLastMoveHighlights();
            ResetCastlingRights();
            SetStartingPosition();
        }

        public void LoadMovesFromPgn(string pgn)
        {
            _moves = PgnParser.ExtractMoves(pgn);
            _clocks = PgnParser.ExtractClocks(pgn);

            DisplayMoves.Clear();

            for (int i = 0; i < _moves.Count; i++)
            {
                // Create view model for each move (stores move notation and user notes)
                var moveVm = new MoveViewModel(i, _moves[i]);

                // Subscribe to note changes so we can save them to the database
                moveVm.NoteChanged += (m) =>
                {
                    SaveNote(m);
                };

                DisplayMoves.Add(moveVm);
            }

            OnPropertyChanged(nameof(WhiteClock));
            OnPropertyChanged(nameof(BlackClock));

            CurrentMoveIndex = 0;

            SetStartingPosition();
        }

        private void AddToRecentGames(ChessGame game)
        {
            using (var db = new ChessDbContext())
            {
                var existing = db.RecentGames
                    .FirstOrDefault(g => g.PGN == game.Pgn);

                if (existing != null)
                {
                    existing.DateViewed = DateTime.Now;
                }
                else
                {
                    db.RecentGames.Add(GameMapper.ToRecentEntity(game));
                }

                db.SaveChanges();

                // Limit recent games to 20
                var oldGames = db.RecentGames
                    .OrderByDescending(g => g.DateViewed)
                    .Skip(20)
                    .ToList();

                if (oldGames.Any())
                {
                    db.RecentGames.RemoveRange(oldGames);
                    db.SaveChanges();
                }
            }
        }

        #endregion

        #region Move Notes
        // Note Saving Logic
        private void SaveNote(MoveViewModel move)
        {
            if (_currentGame == null)
                return;

            using (var db = new ChessDbContext())
            {
                // Try to find the game in the db
                var gameEntity = db.Games
                    .FirstOrDefault(g => g.PGN == _currentGame.Pgn);

                // If game doesn't exist create it
                if (gameEntity == null)
                {
                    gameEntity = new GameEntity
                    {
                        WhitePlayer = _currentGame.White,
                        BlackPlayer = _currentGame.Black,
                        PerspectivePlayer = _currentGame.PerspectivePlayer,
                        WhiteElo = _currentGame.WhiteElo,
                        BlackElo = _currentGame.BlackElo,
                        Result = _currentGame.Result,
                        TimeControl = _currentGame.GameType,
                        PGN = _currentGame.Pgn,
                        DateSaved = DateTime.Now,
                        HasNotes = true
                    };

                    db.Games.Add(gameEntity);
                    db.SaveChanges();
                }

                // Find existing note for this move
                var existing = db.MoveNotes
                    .FirstOrDefault(n => n.GameId == gameEntity.Id && n.MoveIndex == move.Index);

                // If empty or null delete note
                if (string.IsNullOrWhiteSpace(move.Note))
                {
                    if (existing != null)
                        db.MoveNotes.Remove(existing);

                    db.SaveChanges();
                    return;
                }

                // Update existing
                if (existing != null)
                {
                    existing.Note = move.Note;
                }
                else
                {
                    // Create new
                    db.MoveNotes.Add(new MoveNoteEntity
                    {
                        GameId = gameEntity.Id,
                        MoveIndex = move.Index,
                        Note = move.Note
                    });
                }

                // Flag for annotated game display
                gameEntity.HasNotes = true;

                db.SaveChanges();
            }
        }

        // Note Loading Logic
        private void LoadNotes()
        {
            if (_currentGame == null)
                return;

            using (var db = new ChessDbContext())
            {
                // Find game in db
                var gameEntity = db.Games
                    .FirstOrDefault(g => g.PGN == _currentGame.Pgn);

                if (gameEntity == null)
                    return;

                // Get all notes for this game
                var notes = db.MoveNotes
                    .Where(n => n.GameId == gameEntity.Id)
                    .ToList();

                // Apply notes to moves
                foreach (var move in DisplayMoves)
                {
                    var note = notes.FirstOrDefault(n => n.MoveIndex == move.Index);
                    move.Note = note?.Note;
                }
            }
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

        #region Save Game
        private async void SaveGame()
        {
            if (_currentGame == null)
            {
                StatusColor = Brushes.Orange;
                StatusMessage = "No game loaded.";
                await ClearStatusMessage();
                return;
            }

            using (var db = new ChessDbContext())
            {
                var existing = db.Games.FirstOrDefault(g => g.PGN == _currentGame.Pgn);

                if (existing != null)
                {
                    StatusColor = Brushes.Orange;
                    StatusMessage = "Game already saved.";
                    await ClearStatusMessage();
                    return;
                }

                var entity = new GameEntity
                {
                    WhitePlayer = _currentGame.White,
                    BlackPlayer = _currentGame.Black,
                    PerspectivePlayer = _currentGame.PerspectivePlayer,
                    WhiteElo = _currentGame.WhiteElo,
                    BlackElo = _currentGame.BlackElo,
                    Result = _currentGame.Result,
                    TimeControl = _currentGame.GameType,
                    PGN = _currentGame.Pgn,
                    DateSaved = DateTime.Now
                };

                db.Games.Add(entity);
                db.SaveChanges();
            }
            StatusColor = Brushes.LightGreen;
            StatusMessage = "Game saved successfully.";
            await ClearStatusMessage();
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        private Brush _statusColor = Brushes.LightGreen;
        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        private async Task ClearStatusMessage()
        {
            await Task.Delay(2500);
            StatusMessage = "";
            StatusColor = Brushes.LightGreen;
        }
        #endregion

        #region Save Opening
        private async void SaveFavouriteOpening()
        {
            if (_currentOpening == null)
            {
                StatusColor = Brushes.Red;
                StatusMessage = "No opening loaded.";
                await ClearStatusMessage();
                return;
            }

            using (var db = new ChessDbContext())
            {
                var exists = db.FavouriteOpenings
                    .FirstOrDefault(o => o.Name == _currentOpening.Opening);

                if (exists != null)
                {
                    StatusColor = Brushes.Orange;
                    StatusMessage = "Opening already saved.";
                    await ClearStatusMessage();
                    return;
                }

                db.FavouriteOpenings.Add(new OpeningEntity
                {
                    Name = _currentOpening.Opening,
                    ECO = _currentOpening.ECO,
                    Moves = string.Join(" ", _moves),
                    DateAdded = DateTime.Now
                });

                db.SaveChanges();
            }

            StatusColor = Brushes.LightGreen;
            StatusMessage = "Opening saved successfully.";
            await ClearStatusMessage();
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
                OnPropertyChanged(nameof(WhiteClock));
                OnPropertyChanged(nameof(BlackClock));

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
        public void ResetBoard()
        {
            _isGameOver = false;
            CurrentMoveIndex = 0;
            CurrentTurn = PieceColour.White;
            ResetCastlingRights();
            RebuildBoardToCurrentMove();

            // Reset clocks if we're in game mode (opening mode doesn't have clock data)
            if (CurrentGameMode == GameMode.Local2Player && _selectedTimeOption != null)
            {
                InitializeClocks(_selectedTimeOption);
            }

            // Clear move data
            _moves.Clear();
            DisplayMoves.Clear();

            UpdateCurrentMoveHighlight();
            UpdateCheckHighlight();

            OnPropertyChanged(nameof(CurrentMove));
            StatusMessage = "";
        }

        // Clears all mode-specific state when switching modes to ensure clean transitions
        private void ClearModeState() 
        {
            // Moves
            _moves.Clear();
            DisplayMoves.Clear();

            // Clocks
            _clockTimer?.Stop();
            WhiteTimeRemaining = TimeSpan.Zero;
            BlackTimeRemaining = TimeSpan.Zero;

            // Local game data
            WhitePlayerName = "";
            BlackPlayerName = "";
            SelectedTimeControl = "";

            // Replay data
            WhitePlayer = "";
            BlackPlayer = "";
            WhiteRating = 0;
            BlackRating = 0;

            StatusMessage = "";
        }

        // Apply next move in the opening sequence
        private void NextMove()
        {
            if (CurrentMoveIndex >= _moves.Count)
                return;

            CurrentTurn = CurrentTurn == PieceColour.White
                ? PieceColour.Black
                : PieceColour.White;

            CurrentMoveIndex++;
            RebuildBoardToCurrentMove();

            UpdateCurrentMoveHighlight();

            UpdateCheckHighlight();

            CheckGameEnd();

            OnPropertyChanged(nameof(CurrentMove));
        }

        // Undo last move
        private void PreviousMove()
        {
            if (CurrentMoveIndex <= 0)
                return;

            CurrentTurn = CurrentTurn == PieceColour.White
                ? PieceColour.Black
                : PieceColour.White;

            CurrentMoveIndex--;
            RebuildBoardToCurrentMove();

            UpdateCurrentMoveHighlight();

            UpdateCheckHighlight();

            OnPropertyChanged(nameof(CurrentMove));
        }

        // Rebuild board state from scratch up to CurrentMoveIndex
        private void RebuildBoardToCurrentMove()
        {
            SetStartingPosition();
            ClearLastMoveHighlights();
            UpdateCheckHighlight();

            for (int i = 0; i < CurrentMoveIndex; i++)
            {
                ApplyMove(_moves[i], i);
            }
        }

        // Handles user selecting a move from the move list.
        // Jumps the board to the chosen move by rebuilding the position
        private void SelectMove(MoveViewModel move)
        {
            if (move == null)
                return;

            // Move index represents position after the move is applied
            CurrentMoveIndex = move.Index + 1;

            // Rebuild board to that position
            RebuildBoardToCurrentMove();

            // Determine whose turn it is based on the number of moves applied.
            // Even index = White to move, Odd index = Black to move
            CurrentTurn = (CurrentMoveIndex % 2 == 0)
                ? PieceColour.White
                : PieceColour.Black;

            UpdateCurrentMoveHighlight();
            UpdateCheckHighlight();

            OnPropertyChanged(nameof(CurrentMove));
        }

        // Jump directly to final position 
        public void JumpToEnd()
        {
            CurrentMoveIndex = _moves.Count;
            RebuildBoardToCurrentMove();

            CurrentTurn = (CurrentMoveIndex % 2 == 0)
                ? PieceColour.White
                : PieceColour.Black;

            UpdateCurrentMoveHighlight();
            UpdateCheckHighlight();

            OnPropertyChanged(nameof(CurrentMove));
        }

        // Parse SAN move string and route to correct piece logic
        private void ApplyMove(string move, int moveIndex)
        {
            move = move.Replace("+", "").Replace("#", "");

            bool isWhiteMove = moveIndex % 2 == 0;
            bool isCapture = move.Contains("x");

            // Pawn promotion (including capture promotions)
            if (move.Contains("="))
            {
                ApplyPawnPromotion(move, isWhiteMove);
                return;
            }

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

            System.Diagnostics.Debug.WriteLine("Applying move: " + move);
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
        private void ApplyPawnPromotion(string move, bool isWhite)
        {
            bool isCapture = move.Contains("x");

            char promotionPiece = move.Last();

            PieceType newPiece;

            switch (promotionPiece)
            {
                case 'Q':
                    newPiece = PieceType.Queen;
                    break;

                case 'R':
                    newPiece = PieceType.Rook;
                    break;

                case 'B':
                    newPiece = PieceType.Bishop;
                    break;

                case 'N':
                    newPiece = PieceType.Knight;
                    break;

                default:
                    newPiece = PieceType.Queen;
                    break;
            }

            int targetColumn;
            int targetRow;

            if (isCapture)
            {
                // Example: bxa8=Q
                targetColumn = move[2] - 'a';
                targetRow = 8 - int.Parse(move[3].ToString());
            }
            else
            {
                // Example: e8=Q
                targetColumn = move[0] - 'a';
                targetRow = 8 - int.Parse(move[1].ToString());
            }

            int direction = isWhite ? -1 : 1;

            int sourceRow = targetRow - direction;

            int sourceColumn;

            if (isCapture)
            {
                // Pawn comes from the file specified before 'x'
                sourceColumn = move[0] - 'a';
            }
            else
            {
                // Pawn comes from same file
                sourceColumn = targetColumn;
            }

            SquareViewModel fromSquare = GetSquare(sourceRow, sourceColumn);

            if (!fromSquare.HasPiece ||
                fromSquare.Piece.Type != PieceType.Pawn ||
                fromSquare.Piece.Colour != (isWhite ? PieceColour.White : PieceColour.Black))
                return;

            SquareViewModel toSquare = GetSquare(targetRow, targetColumn);

            ClearLastMoveHighlights();

            // Replace pawn with promoted piece
            toSquare.SetPiece(new ChessPiece(newPiece, isWhite ? PieceColour.White : PieceColour.Black));
            fromSquare.ClearPiece();

            // Highlight move
            if (isCapture)
            {
                fromSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
                toSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            }
            else
            {
                fromSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
                toSquare.LastMoveHighlight = LastMoveHighlightType.Normal;
            }
        }

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

            // Detect double pawn advance (en passant opportunity)
            if (Math.Abs(targetRow - fromSquare.Row) == 2)
            {
                int enPassantRow = (targetRow + fromSquare.Row) / 2;
                _enPassantTarget = GetSquare(enPassantRow, targetColumn);
            }
            else
            {
                _enPassantTarget = null;
            }

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

            // En Passant
            if (!toSquare.HasPiece && _enPassantTarget != null &&
                _enPassantTarget.Row == targetRow &&
                _enPassantTarget.Column == targetColumn)
            {
                int capturedPawnRow = targetRow - direction;
                SquareViewModel capturedPawn = GetSquare(capturedPawnRow, targetColumn);

                capturedPawn.ClearPiece();
            }

            // Normal capture
            toSquare.SetPiece(fromSquare.Piece);
            fromSquare.ClearPiece();

            // Mark capture squares differently for UI styling
            fromSquare.LastMoveHighlight = LastMoveHighlightType.Capture;
            toSquare.LastMoveHighlight = LastMoveHighlightType.Capture;

            _enPassantTarget = null;
        }



        // Knight move logic
        private void ApplyKnightMove(string move, bool isWhite)
        {
            int targetColumn;
            int targetRow;
            char disambiguation = '\0';

            // Determine target square
            if (move.Length == 3)
            {
                targetColumn = move[1] - 'a';
                targetRow = 8 - int.Parse(move[2].ToString());
            }
            else
            {
                disambiguation = move[1];
                targetColumn = move[2] - 'a';
                targetRow = 8 - int.Parse(move[3].ToString());
            }

            // Find all knights that could reach the square
            var possibleKnights = Squares.Where(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Knight &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsKnightMove(s.Row, s.Column, targetRow, targetColumn)
            ).ToList();

            // Apply disambiguation if present
            if (disambiguation != '\0')
            {
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';
                    possibleKnights = possibleKnights
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }
                else if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());
                    possibleKnights = possibleKnights
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel knightSquare = possibleKnights.FirstOrDefault();

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
            int targetColumn;
            int targetRow;
            char disambiguation = '\0';

            // Standard capture (Nxf6)
            if (move.Length == 4)
            {
                targetColumn = move[2] - 'a';
                targetRow = 8 - int.Parse(move[3].ToString());
            }
            else
            {
                // Disambiguated capture (Nbxf6 or N1xf6)
                disambiguation = move[1];
                targetColumn = move[3] - 'a';
                targetRow = 8 - int.Parse(move[4].ToString());
            }

            // Find all knights that could capture on target square
            var possibleKnights = Squares.Where(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Knight &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsKnightMove(s.Row, s.Column, targetRow, targetColumn)
            ).ToList();

            // Apply disambiguation if present
            if (disambiguation != '\0')
            {
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';
                    possibleKnights = possibleKnights
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }
                else if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());
                    possibleKnights = possibleKnights
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel knightSquare = possibleKnights.FirstOrDefault();

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
            int targetColumn;
            int targetRow;
            char disambiguation = '\0';

            // Convert SAN target square to board coordinates
            // Standard bishop move (Bb5)
            if (move.Length == 3)
            {
                targetColumn = move[1] - 'a'; // file to column index
                targetRow = 8 - int.Parse(move[2].ToString()); // rank to row index
            }
            else
            {
                // Disambiguated bishop move (Bdb5, B1b5)
                // move[1] contains file or rank identifying which bishop moves
                disambiguation = move[1];
                targetColumn = move[2] - 'a';
                targetRow = 8 - int.Parse(move[3].ToString());
            }

            // Find all bishops of the correct colour that can legally reach target square
            var possibleBishops = Squares.Where(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Bishop &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsDiagonalMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearDiagonal(s.Row, s.Column, targetRow, targetColumn)
            ).ToList();

            // Apply disambiguation if present
            // SAN may specify the originating file or rank when two bishops can move to same square
            if (disambiguation != '\0')
            {
                // File disambiguation (e.g. Bdb5)
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';

                    possibleBishops = possibleBishops
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }

                // Rank disambiguation (e.g. B1b5)
                if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());

                    possibleBishops = possibleBishops
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel bishopSquare = possibleBishops.FirstOrDefault();

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

            char disambiguation = '\0';

            // Capture with disambiguation (Bdxe5, B1xe5)
            if (move.Length == 5)
                disambiguation = move[1];

            // Find bishops that can capture on target square
            var possibleBishops = Squares.Where(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Bishop &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsDiagonalMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearDiagonal(s.Row, s.Column, targetRow, targetColumn)
            ).ToList();

            // Apply disambiguation if present
            // Used when two bishops could capture the same square
            if (disambiguation != '\0')
            {
                // File disambiguation (Bdxe5)
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';

                    possibleBishops = possibleBishops
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }

                // Rank disambiguation (B1xe5)
                if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());

                    possibleBishops = possibleBishops
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel bishopSquare = possibleBishops.FirstOrDefault();

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
        // Rook move logic
        // 0 = piece ('R')
        // 1 = target file (column)
        // 2 = target rank (row)
        private void ApplyRookMove(string move, bool isWhite)
        {
            int targetColumn;
            int targetRow;
            char disambiguation = '\0';

            // Standard rook move (Rd1)
            if (move.Length == 3)
            {
                targetColumn = move[1] - 'a'; // file to column index
                targetRow = 8 - int.Parse(move[2].ToString()); // rank to row index
            }
            else
            {
                // Disambiguated rook move (Rad1, R1d1)
                disambiguation = move[1];
                targetColumn = move[2] - 'a';
                targetRow = 8 - int.Parse(move[3].ToString());
            }

            // Find all rooks of the correct colour that can legally reach target square
            var possibleRooks = Squares.Where(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Rook &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsStraightMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearStraight(s.Row, s.Column, targetRow, targetColumn)
            ).ToList();

            // Apply disambiguation filter if present
            if (disambiguation != '\0')
            {
                // File disambiguation (Rad1)
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';
                    possibleRooks = possibleRooks
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }

                // Rank disambiguation (R1d1)
                if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());
                    possibleRooks = possibleRooks
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel rookSquare = possibleRooks.FirstOrDefault();

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
        // 1 = capture marker ('x') OR disambiguation
        // 2 = target file
        // 3 = target rank
        private void ApplyRookCapture(string move, bool isWhite)
        {
            int targetColumn;
            int targetRow;
            char disambiguation = '\0';

            // Standard capture (Rxd1)
            if (move.Length == 4)
            {
                targetColumn = move[2] - 'a';
                targetRow = 8 - int.Parse(move[3].ToString());
            }
            else
            {
                // Disambiguated capture (Raxd1 or R1xd1)
                disambiguation = move[1];
                targetColumn = move[3] - 'a';
                targetRow = 8 - int.Parse(move[4].ToString());
            }

            // Find all rooks that could capture on target square
            var possibleRooks = Squares.Where(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.Rook &&
                s.Piece.Colour == (isWhite ? PieceColour.White : PieceColour.Black) &&
                IsStraightMove(s.Row, s.Column, targetRow, targetColumn) &&
                IsPathClearStraight(s.Row, s.Column, targetRow, targetColumn)
            ).ToList();

            // Apply disambiguation if present
            if (disambiguation != '\0')
            {
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';
                    possibleRooks = possibleRooks
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }
                else if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());
                    possibleRooks = possibleRooks
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel rookSquare = possibleRooks.FirstOrDefault();

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
            int targetColumn;
            int targetRow;
            char disambiguation = '\0';

            // Convert SAN target square to board coordinates
            // Standard queen move (Qd4)
            if (move.Length == 3)
            {
                targetColumn = move[1] - 'a'; // file to column index
                targetRow = 8 - int.Parse(move[2].ToString()); // rank to row index
            }
            else
            {
                // Disambiguated queen move (Qhd4, Q1d4)
                disambiguation = move[1];
                targetColumn = move[2] - 'a';
                targetRow = 8 - int.Parse(move[3].ToString());
            }

            // Find all queens of the correct colour that can legally reach target square
            var possibleQueens = Squares.Where(s =>
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
            ).ToList();

            // Apply disambiguation if present
            // SAN may specify file or rank when two queens could move to the same square
            if (disambiguation != '\0')
            {
                // File disambiguation (Qhd4)
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';

                    possibleQueens = possibleQueens
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }

                // Rank disambiguation (Q1d4)
                if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());

                    possibleQueens = possibleQueens
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel queenSquare = possibleQueens.FirstOrDefault();

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

            char disambiguation = '\0';

            // Capture with disambiguation (Qdxe4, Q1xe4)
            if (move.Length == 5)
                disambiguation = move[1];

            // Find queens that can capture on target square
            var possibleQueens = Squares.Where(s =>
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
            ).ToList();

            // Apply disambiguation if present
            if (disambiguation != '\0')
            {
                // File disambiguation (Qdxe4)
                if (disambiguation >= 'a' && disambiguation <= 'h')
                {
                    int fileColumn = disambiguation - 'a';

                    possibleQueens = possibleQueens
                        .Where(s => s.Column == fileColumn)
                        .ToList();
                }

                // Rank disambiguation (Q1xe4)
                if (disambiguation >= '1' && disambiguation <= '8')
                {
                    int rankRow = 8 - int.Parse(disambiguation.ToString());

                    possibleQueens = possibleQueens
                        .Where(s => s.Row == rankRow)
                        .ToList();
                }
            }

            SquareViewModel queenSquare = possibleQueens.FirstOrDefault();

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

        // Castling logic
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


        // Check if castling move is legal (not moving through check, king/rook not moved)
        public bool IsCastleLegal(SquareViewModel from, SquareViewModel to)
        {
            if (from.Piece.Type != PieceType.King)
                return true;

            if (Math.Abs(to.Column - from.Column) != 2)
                return true;

            bool isWhite = from.Piece.Colour == PieceColour.White;

            // King moved
            if (isWhite && _whiteKingMoved) return false;
            if (!isWhite && _blackKingMoved) return false;

            // Rook moved
            if (to.Column == 6) // kingside
            {
                if (isWhite && _whiteKingsideRookMoved) return false;
                if (!isWhite && _blackKingsideRookMoved) return false;
            }
            else if (to.Column == 2) // queenside
            {
                if (isWhite && _whiteQueensideRookMoved) return false;
                if (!isWhite && _blackQueensideRookMoved) return false;
            }

            int row = from.Row;

            // In check
            if (_moveService.IsKingInCheck(Squares, from.Piece.Colour))
                return false;

            // Determine direction of castling (kingside or queenside)
            int direction = to.Column > from.Column ? 1 : -1;

            // Check squares the king moves through are not under attack
            for (int i = 1; i <= 2; i++)
            {
                int col = from.Column + (i * direction);

                if (_moveService.IsSquareUnderAttack(
                    Squares,
                    row,
                    col,
                    isWhite ? PieceColour.Black : PieceColour.White))
                {
                    return false;
                }
            }

            return true;
        }

        private void ResetCastlingRights()
        {
            _whiteKingMoved = false;
            _blackKingMoved = false;

            _whiteKingsideRookMoved = false;
            _whiteQueensideRookMoved = false;

            _blackKingsideRookMoved = false;
            _blackQueensideRookMoved = false;
        }

        #endregion

        #region Game Logic
        // Handles user clicks on squares for piece selection and movement
        private void OnSquareClicked(SquareViewModel square)
        {            
            if (square == null)
                return;

            if(_isGameOver)
                return;

            // No piece selected yet 
            if (_selectedSquare == null)
            {
                // Only allow selecting a square with a piece
                if (!square.HasPiece)
                    return;

                if (square.Piece.Colour != CurrentTurn)
                    return;

                SelectSquare(square);
                return;
            }

            // Clicking a valid move
            if (_legalMoves.Contains(square))
            {
                MovePiece(_selectedSquare, square);
                ClearSelection();
                return;
            }

            // Selecting a different piece
            if (square.HasPiece &&
                square.Piece.Colour == _selectedSquare.Piece.Colour)
            {
                SelectSquare(square);
                return;
            }

            // Invalid click -> reset
            ClearSelection();
        }

        // Highlights the selected square and legal move squares
        private void SelectSquare(SquareViewModel square)
        {
            ClearSelection();

            _selectedSquare = square;

            _legalMoves = _moveService.GetLegalMoves(square, Squares, _enPassantTarget);

            _legalMoves = _legalMoves.Where(move => IsCastleLegal(square, move)).ToList();

            // Highlight selected square
            square.LastMoveHighlight = LastMoveHighlightType.Normal;

            // Highlight legal moves
            foreach (var move in _legalMoves)
            {
                move.LastMoveHighlight = LastMoveHighlightType.Normal;
            }
        }

        // Moves a piece from one square to another, updating the board state and move highlights
        private void MovePiece(SquareViewModel from, SquareViewModel to)
        {
            ClearLastMoveHighlights();

            var piece = from.Piece;

            // en passant capture logic: if a pawn moves to the en passant target square, remove the captured pawn
            if (piece.Type == PieceType.Pawn &&
                _enPassantTarget != null &&
                to == _enPassantTarget)
            {
                int direction = piece.Colour == PieceColour.White ? 1 : -1;

                var capturedPawn = GetSquare(to.Row + direction, to.Column);
                capturedPawn.ClearPiece();
            }

            // Castling logic: if the king moves 2 squares, also move the rook
            if (piece.Type == PieceType.King && Math.Abs(to.Column - from.Column) == 2)
            {
                int row = from.Row;

                // Kingside
                if (to.Column == 6)
                {
                    var rookFrom = GetSquare(row, 7);
                    var rookTo = GetSquare(row, 5);

                    rookTo.SetPiece(rookFrom.Piece);
                    rookFrom.ClearPiece();
                }
                // Queenside
                else if (to.Column == 2)
                {
                    var rookFrom = GetSquare(row, 0);
                    var rookTo = GetSquare(row, 3);

                    rookTo.SetPiece(rookFrom.Piece);
                    rookFrom.ClearPiece();
                }
            }

            // Generate notation before modifying board (Otherwise from square and isCapture info is lost)
            string moveNotation = GenerateMoveNotation(from, to);

            // Move piece
            to.SetPiece(piece);
            from.ClearPiece();

            // Track king movement
            if (piece.Type == PieceType.King)
            {
                if (piece.Colour == PieceColour.White)
                    _whiteKingMoved = true;
                else
                    _blackKingMoved = true;
            }

            // Track rook movement
            if (piece.Type == PieceType.Rook)
            {
                if (piece.Colour == PieceColour.White)
                {
                    if (from.Column == 0) _whiteQueensideRookMoved = true;
                    if (from.Column == 7) _whiteKingsideRookMoved = true;
                }
                else
                {
                    if (from.Column == 0) _blackQueensideRookMoved = true;
                    if (from.Column == 7) _blackKingsideRookMoved = true;
                }
            }

            // Set en passant target if a pawn moved 2 squares forward
            if (piece.Type == PieceType.Pawn &&
                Math.Abs(to.Row - from.Row) == 2)
            {
                int midRow = (from.Row + to.Row) / 2;
                _enPassantTarget = GetSquare(midRow, from.Column);
            }
            else
            {
                _enPassantTarget = null;
            }

            // Highlight move
            from.LastMoveHighlight = LastMoveHighlightType.Normal;
            to.LastMoveHighlight = LastMoveHighlightType.Normal;

            // Switch turn
            CurrentTurn = CurrentTurn == PieceColour.White
                ? PieceColour.Black
                : PieceColour.White;

            // Apply increment to player who just moved
            if (CurrentTurn == PieceColour.White)
            {
                // Black just moved
                BlackTimeRemaining += TimeSpan.FromSeconds(_incrementSeconds);
            }
            else
            {
                // White just moved
                WhiteTimeRemaining += TimeSpan.FromSeconds(_incrementSeconds);
            }

            if (CurrentGameMode == GameMode.Local2Player)
            {
                var moveVm = new MoveViewModel(DisplayMoves.Count, moveNotation);

                moveVm.NoteChanged += (m) =>
                {
                    SaveNote(m);
                };

                DisplayMoves.Add(moveVm); // Add move to display list for UI
                _moves.Add(moveNotation); // Add move to PGN move list for saving
                CurrentMoveIndex = DisplayMoves.Count - 1; // Update current move index to latest move for scroll to view in move list UI
            }

            UpdateCheckHighlight();
            CheckGameEnd();
        }

        // Clears move highlights from the last move
        private void ClearSelection()
        {
            _selectedSquare = null;
            _legalMoves.Clear();

            ClearLastMoveHighlights();
        }

        // Highlights any king that is currently in check after a move
        private void UpdateCheckHighlight()
        {
            // Clear previous check highlights
            foreach (var square in Squares)
                square.IsInCheck = false;

            var service = new MoveGenerationService();

            // Check both kings
            foreach (var colour in new[] { PieceColour.White, PieceColour.Black })
            {
                if (service.IsKingInCheck(Squares, colour))
                {
                    var king = service.FindKing(Squares, colour);

                    if (king != null)
                        king.IsInCheck = true;
                }
            }
        }

        // Tracks which player's turn it is and notifies bindings when it changes
        private PieceColour _currentTurn = PieceColour.White;
        
        public PieceColour CurrentTurn
        {
            get => _currentTurn;
            set
            {
                _currentTurn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentTurnDisplay));
            }
        }

        // Checks if the given colour has any legal moves available (used for checkmate/stalemate detection)
        private bool HasAnyLegalMoves(PieceColour colour) 
        {
            var service = new MoveGenerationService();

            foreach (var square in Squares)
            {
                if (!square.HasPiece)
                    continue;

                if (square.Piece.Colour != colour)
                    continue;

                var moves = service.GetLegalMoves(square, Squares, _enPassantTarget);

                if (moves.Any())
                    return true;
            }

            return false;
        }

        // Checks for checkmate or stalemate after each move and updates game status accordingly
        private void CheckGameEnd() 
        {
            var service = new MoveGenerationService();

            bool isInCheck = service.IsKingInCheck(Squares, CurrentTurn);
            bool hasMoves = HasAnyLegalMoves(CurrentTurn);

            if (isInCheck && !hasMoves)
            {
                _isGameOver = true;
                StatusMessage = $"Checkmate! {(CurrentTurn == PieceColour.White ? "Black" : "White")} wins.";
                StatusColor = Brushes.Red;
            }
            else if (!isInCheck && !hasMoves)
            {
                _isGameOver = true;
                StatusMessage = "Stalemate!";
                StatusColor = Brushes.Orange;
            }

            if (_isGameOver && CurrentGameMode == GameMode.Local2Player)
            {
                SaveLocalGame();
            }
        }


        #endregion

        #region Local Game
        // Properties

        // Stores moves (PGN)
        private List<string> _currentGameMoves = new List<string>();

        private string _whitePlayerName;
        public string WhitePlayerName
        {
            get => _whitePlayerName;
            set
            {
                _whitePlayerName = value;
                OnPropertyChanged();
            }
        }

        private string _blackPlayerName;
        public string BlackPlayerName
        {
            get => _blackPlayerName;
            set
            {
                _blackPlayerName = value;
                OnPropertyChanged();
            }
        }

        private string _selectedTimeControl;
        public string SelectedTimeControl
        {
            get => _selectedTimeControl;
            set
            {
                _selectedTimeControl = value;
                OnPropertyChanged();
            }
        }

        // Time remaining for each player, updated after each move and displayed in the UI
        private TimeSpan _whiteTimeRemaining;
        public TimeSpan WhiteTimeRemaining
        {
            get => _whiteTimeRemaining;
            set
            {
                _whiteTimeRemaining = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WhiteClockDisplay));
            }
        }

        private TimeSpan _blackTimeRemaining;
        public TimeSpan BlackTimeRemaining
        {
            get => _blackTimeRemaining;
            set
            {
                _blackTimeRemaining = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BlackClockDisplay));
            }
        }

        private int _incrementSeconds;
        private DispatcherTimer _clockTimer;
        private TimeControlOption _selectedTimeOption;

        // Display strings for player names and time control in the UI
        public string WhiteDisplay => $"White: {WhitePlayerName}";
        public string BlackDisplay => $"Black: {BlackPlayerName}";
        public string TimeDisplay => $"Time: {SelectedTimeControl}";

        // Formatted clock display strings for the UI, showing minutes and seconds remaining for each player
        public string WhiteClockDisplay => WhiteTimeRemaining.ToString(@"mm\:ss");
        public string BlackClockDisplay => BlackTimeRemaining.ToString(@"mm\:ss");


        // Visibility flag for local game UI elements (player names, clocks, etc.)
        public bool IsLocalGame => CurrentGameMode == GameMode.Local2Player;


        // Initializes player clocks based on the selected time control
        private void InitializeClocks(TimeControlOption time)
        {
            WhiteTimeRemaining = TimeSpan.FromMinutes(time.Minutes);
            BlackTimeRemaining = TimeSpan.FromMinutes(time.Minutes);

            _incrementSeconds = time.Increment;

            // Start timer
            _clockTimer?.Stop();
            _clockTimer = null; // Ensure old timer is fully cleaned up

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();

            // Notify UI
            OnPropertyChanged(nameof(WhiteClockDisplay));
            OnPropertyChanged(nameof(BlackClockDisplay));

            OnPropertyChanged(nameof(IsWhiteLowTime));
            OnPropertyChanged(nameof(IsWhiteCriticalTime));
            OnPropertyChanged(nameof(IsBlackLowTime));
            OnPropertyChanged(nameof(IsBlackCriticalTime));
        }


        // Handles clock ticks, decrementing the active player's time and checking for time expiration to end the game
        private void OnClockTick(object sender, EventArgs e)
        {
            if (_isGameOver)
                return;

            if (CurrentTurn == PieceColour.White)
            {
                WhiteTimeRemaining -= TimeSpan.FromSeconds(1);

                if (WhiteTimeRemaining <= TimeSpan.Zero)
                {
                    WhiteTimeRemaining = TimeSpan.Zero;
                    EndGameOnTime(PieceColour.White);
                }
            }
            else
            {
                BlackTimeRemaining -= TimeSpan.FromSeconds(1);

                if (BlackTimeRemaining <= TimeSpan.Zero)
                {
                    BlackTimeRemaining = TimeSpan.Zero;
                    EndGameOnTime(PieceColour.Black);
                }
            }

            // Notify UI of clock updates and low time warnings
            OnPropertyChanged(nameof(WhiteClockDisplay));
            OnPropertyChanged(nameof(WhiteSeconds));

            OnPropertyChanged(nameof(BlackClockDisplay));
            OnPropertyChanged(nameof(BlackSeconds));

            OnPropertyChanged(nameof(IsWhiteLowTime));
            OnPropertyChanged(nameof(IsWhiteCriticalTime));

            OnPropertyChanged(nameof(IsBlackLowTime));
            OnPropertyChanged(nameof(IsBlackCriticalTime));
        }


        // Exposes remaining time in seconds for use in UI bindings
        public double WhiteSeconds => WhiteTimeRemaining.TotalSeconds;
        public double BlackSeconds => BlackTimeRemaining.TotalSeconds;


        // Indicates whether the clocks should be active and visible in the UI (only during a local 2-player game that is not over)
        public bool IsClockActive => CurrentGameMode == GameMode.Local2Player && !_isGameOver;


        // Flags for low time warnings in the UI, triggered when a player's remaining time falls below certain thresholds 
        public bool IsWhiteLowTime => IsClockActive && WhiteTimeRemaining.TotalSeconds <= 30;

        public bool IsWhiteCriticalTime => IsClockActive && WhiteTimeRemaining.TotalSeconds <= 15;

        public bool IsBlackLowTime => IsClockActive && BlackTimeRemaining.TotalSeconds <= 30;

        public bool IsBlackCriticalTime => IsClockActive && BlackTimeRemaining.TotalSeconds <= 15;



        // Ends the game when a player's time runs out, updating the status message and saving the game
        private void EndGameOnTime(PieceColour loser)
        {
            _isGameOver = true;
            _clockTimer?.Stop();

            StatusMessage = loser == PieceColour.White
                ? "Black wins on time!"
                : "White wins on time!";

            StatusColor = Brushes.Red;

            if (CurrentGameMode == GameMode.Local2Player)
            {
                SaveLocalGame();
            }
        }


        // Starts a new local 2-player game with the given player names and time control, resetting the board and move history
        public void StartNewLocalGame(string white, string black, TimeControlOption time)
        {
            // Set mode
            CurrentGameMode = GameMode.Local2Player;

            // Notify UI of mode change to show/hide appropriate UI elements
            OnPropertyChanged(nameof(IsGameMode));
            OnPropertyChanged(nameof(IsOpeningMode));
            OnPropertyChanged(nameof(IsLocalGame));

            // Reset state
            IsOpeningMode = false;
            IsGameMode = false;
            _isGameOver = false;
            _selectedTimeOption = time;
            ClearModeState();

            // Defaults
            WhitePlayerName = string.IsNullOrWhiteSpace(white) ? "White" : white;
            BlackPlayerName = string.IsNullOrWhiteSpace(black) ? "Black" : black;

            SelectedTimeControl = time.Display;

            // Reset board state
            ResetBoard();

            // Reset move tracking
            _moves.Clear();
            _currentGameMoves.Clear();

            // Set up clocks based on selected time control
            InitializeClocks(time);            

            UpdateCheckHighlight();

            // Notify UI of changes
            OnPropertyChanged(nameof(WhiteDisplay));
            OnPropertyChanged(nameof(BlackDisplay));
            OnPropertyChanged(nameof(TimeDisplay));
            OnPropertyChanged(nameof(IsLocalGame));
        }


        // Saves the completed local game to the database with player names, result and PGN
        private void SaveLocalGame()
        {
            if (CurrentGameMode != GameMode.Local2Player)
                return;

            // Create ChessGame model from current game data
            var game = new ChessGame
            {
                White = WhitePlayerName,
                Black = BlackPlayerName,
                GameType = SelectedTimeControl,
                Result = GetGameResult(),
                Pgn = GeneratePgn()
            };

            // Save to database
            using (var db = new ChessDbContext())
            {
                db.LocalGames.Add(GameMapper.ToLocalEntity(game));
                db.SaveChanges();
            }

            StatusMessage += " Game saved successfully.";
        }


        // Determines the game result based on the status message, returning standard PGN result strings ("1-0", "0-1", "1/2-1/2") or "*" if the game is not finished
        private string GetGameResult()
        {
            if (!_isGameOver)
                return "*"; // Game not finished

            if (StatusMessage.Contains("White wins"))
                return "1-0";

            if (StatusMessage.Contains("Black wins"))
                return "0-1";

            if (StatusMessage.Contains("Draw") || StatusMessage.Contains("Stalemate"))
                return "1/2-1/2";

            if (StatusMessage.Contains("time"))
                return StatusMessage.Contains("White") ? "1-0" : "0-1";

            return "*";
        }


        // Generates a simple move notation string, which can be displayed in the UI and saved in the move history
        private string GenerateMoveNotation(SquareViewModel from, SquareViewModel to)
        {
            var piece = from.Piece;

            // Check if move is a capture (either normal capture or en passant)
            bool isEnPassant = piece.Type == PieceType.Pawn &&
                   _enPassantTarget != null &&
                   to.Row == _enPassantTarget.Row &&
                   to.Column == _enPassantTarget.Column;

            bool isCapture = to.HasPiece || isEnPassant;

            // Castling
            if (piece.Type == PieceType.King)
            {
                if (from.Column == 4 && to.Column == 6)
                    return "O-O";

                if (from.Column == 4 && to.Column == 2)
                    return "O-O-O";
            }

            string notation = "";

            // Piece letter (pawn = empty)
            notation += GetPieceLetter(piece.Type);

            // Disambiguation: If another piece of the same type can move to the same square, specify the file/rank of the moving piece
            if (piece.Type != PieceType.Pawn && NeedsDisambiguation(from, to))
            {
                notation += (char)('a' + from.Column); // file-based disambiguation
            }

            // Pawn capture needs file (e.g. exd5)
            if (piece.Type == PieceType.Pawn && isCapture)
            {
                notation += (char)('a' + from.Column);
            }

            // Capture marker
            if (isCapture)
                notation += "x";

            // Destination square
            notation += GetSquareName(to);

            // TEMP move to check for check
            var captured = to.Piece;
            to.SetPiece(piece);
            from.ClearPiece();

            var service = new MoveGenerationService();
            var opponent = piece.Colour == PieceColour.White ? PieceColour.Black : PieceColour.White;

            bool isCheck = service.IsKingInCheck(Squares, opponent);

            // Undo move
            from.SetPiece(piece);
            to.SetPiece(captured);

            if (isCheck)
                notation += "+";

            return notation;
        }


        // Converts board coordinates to standard algebraic notation for move notation generation
        private string GetSquareName(SquareViewModel square)
        {
            char file = (char)('a' + square.Column);
            int rank = 8 - square.Row;
            return $"{file}{rank}";
        }

        // Returns the letter for a piece type (K, Q, R, B, N) or empty string for pawns, used in move notation generation
        private string GetPieceLetter(PieceType type)
        {
            switch (type)
            {
                case PieceType.King: return "K";
                case PieceType.Queen: return "Q";
                case PieceType.Rook: return "R";
                case PieceType.Bishop: return "B";
                case PieceType.Knight: return "N";
                default: return "";
            }
        }


        // Checks if a move requires disambiguation in SAN notation (e.g. if two knights can move to the same square, the move must specify which one)
        private bool NeedsDisambiguation(SquareViewModel from, SquareViewModel to)
        {
            var piece = from.Piece;

            var samePieces = Squares.Where(s =>
                s != from &&
                s.HasPiece &&
                s.Piece.Type == piece.Type &&
                s.Piece.Colour == piece.Colour);

            var service = new MoveGenerationService();

            foreach (var other in samePieces)
            {
                var moves = service.GetLegalMoves(other, Squares, _enPassantTarget);

                if (moves.Any(m => m.Row == to.Row && m.Column == to.Column))
                    return true;
            }

            return false;
        }


        // Generates a PGN string for the current game based on the move history and result, which can be saved to the database and displayed in the UI
        private string GeneratePgn()
        {
            if (_moves == null || !_moves.Any())
                return "";

            var result = GetGameResult();

            var pgn = "";

            // Headers 
            pgn += $"[TimeControl \"{SelectedTimeControl}\"]\n";
            pgn += $"[White \"{WhitePlayerName}\"]\n";
            pgn += $"[Black \"{BlackPlayerName}\"]\n";
            pgn += $"[Result \"{result}\"]\n\n";

            // Moves
            for (int i = 0; i < _moves.Count; i++)
            {
                if (i % 2 == 0)
                {
                    int moveNumber = (i / 2) + 1;
                    pgn += $"{moveNumber}. ";
                }

                pgn += _moves[i] + " ";
            }

            pgn += result;

            return pgn.Trim();
        }
        #endregion
    }
}
