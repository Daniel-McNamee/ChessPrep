using ChessProject.Data;
using ChessProject.Entities;
using ChessProject.Models;
using ChessProject.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessProject.ViewModels
{
    public class GameBrowserViewModel : ViewModelBase
    {
        // Service for fetching data from Chess.com API
        private readonly ChessComService _service;

        // Observable collections for data binding to the UI
        public ObservableCollection<GameArchive> Archives { get; }
        public ObservableCollection<ChessGame> OnlineGames { get; set; }
        public ObservableCollection<LocalGameEntity> LocalGames { get; set; }
        public ObservableCollection<ChessGame> SavedGames { get; set; }
        public ObservableCollection<ChessGame> RecentGames { get; set; }
        public ObservableCollection<string> FavouritePlayers { get; set; }
        public ObservableCollection<ChessGame> DisplayGames { get; set; }

        // Commands for UI interactions
        public ICommand SearchPlayerCommand { get; }
        public ICommand LoadArchiveCommand { get; }
        public ICommand ShowOnlineCommand { get; }
        public ICommand ShowSavedCommand { get; }
        public ICommand DeleteSavedGameCommand { get; }
        public ICommand ShowRecentCommand { get; }
        public ICommand SaveFavouritePlayerCommand { get; }
        public ICommand DeleteFavouritePlayerCommand { get; }
        public ICommand ShowFavouritePlayersCommand { get; }
        public ICommand ShowLocalGamesCommand { get; }

        // Event to notify when a game is selected for viewing
        public event Action<ChessGame> GameSelected;



        // The username of the player whose games are being browsed
        private string _username;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        // The currently selected game archive (specific month of games)
        private GameArchive _selectedArchive;
        
        public GameArchive SelectedArchive
        {
            get => _selectedArchive;
            set
            {
                _selectedArchive = value;
                OnPropertyChanged();
            }
        }

        // The currently selected game from the list of online/saved/recent games
        private ChessGame _selectedGame;
        
        public ChessGame SelectedGame
        {
            get => _selectedGame;
            set
            {
                _selectedGame = value;
                OnPropertyChanged();

                LoadSelectedGame();
            }
        }

        // Methods to load and display games based on user interaction
        private void ShowOnlineGames()
        {
            IsSavedMode = false;

            DisplayGames.Clear();
            foreach (var game in OnlineGames)
                DisplayGames.Add(game);
        }

        private bool _isSavedMode;
        public bool IsSavedMode
        {
            get => _isSavedMode;
            set
            {
                _isSavedMode = value;
                OnPropertyChanged();
            }
        }

        private void ShowSavedGames()
        {
            IsSavedMode = true;

            LoadSavedGames();

            DisplayGames.Clear();
            foreach (var game in SavedGames)
                DisplayGames.Add(game);
        }

        private void ShowRecentGames()
        {
            IsSavedMode = false;

            LoadRecentGames();

            DisplayGames.Clear();
            foreach (var game in RecentGames)
                DisplayGames.Add(game);
        }

        // Loads the selected game and raises the GameSelected event to notify the UI to display it
        private void LoadSelectedGame()
        {
            if (SelectedGame == null)
                return;

            GameSelected?.Invoke(SelectedGame);
        }

        // Loads saved games from the local database and populates the SavedGames collection
        private void LoadSavedGames()
        {
            using (var db = new ChessDbContext())
            {
                SavedGames.Clear();

                var games = db.Games.ToList();

                foreach (var game in games)
                    SavedGames.Add(GameMapper.ToModel(game));
            }
        }

        // Loads recently viewed games from the local database and populates the RecentGames collection
        private void LoadRecentGames()
        {
            using (var db = new ChessDbContext())
            {
                RecentGames.Clear();

                var games = db.RecentGames
                    .OrderByDescending(g => g.DateViewed)
                    .ToList();

                foreach (var game in games)
                    RecentGames.Add(GameMapper.ToModel(game));
            }
        }

        // Flag to determine whether the UI is currently showing favourite players or game archives
        private bool _isFavouritePlayersMode;
        public bool IsArchiveModeVisible => !_isFavouritePlayersMode;
        public bool IsFavouritePlayersVisible => _isFavouritePlayersMode;

        // Status message and color for user feedback when adding favourite players
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
        }

        // Saves the current username as a favourite player in the local database, with validation and user feedback
        private async void SaveFavouritePlayer()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                StatusColor = Brushes.Red;
                StatusMessage = "Enter a username first.";
                await ClearStatusMessage();
                return;
            }

            var username = Username.Trim();

            using (var db = new ChessDbContext())
            {
                var exists = db.FavouritePlayers
                    .FirstOrDefault(p => p.Username == username);

                if (exists != null)
                {
                    StatusColor = Brushes.Orange;
                    StatusMessage = $"{username} is already in favourites.";
                    await ClearStatusMessage();
                    return;
                }

                db.FavouritePlayers.Add(new FavouritePlayerEntity
                {
                    Username = username,
                    DateAdded = DateTime.Now
                });

                db.SaveChanges();
            }

            StatusColor = Brushes.LightGreen;
            StatusMessage = $"{username} added to favourites.";
            await ClearStatusMessage();
        }

        private void DeleteFavouritePlayer(string player)
        {
            if (string.IsNullOrEmpty(player))
                return;

            using (var db = new ChessDbContext())
            {
                var entity = db.FavouritePlayers.FirstOrDefault(p => p.Username == player);

                if (entity != null)
                {
                    db.FavouritePlayers.Remove(entity);
                    db.SaveChanges();
                }
            }

            FavouritePlayers.Remove(player);
        }

        // Loads the list of favourite players from the local database and populates the FavouritePlayers collection
        private void LoadFavouritePlayers()
        {
            FavouritePlayers.Clear();

            using (var db = new ChessDbContext())
            {
                var players = db.FavouritePlayers
                                .OrderBy(p => p.Username)
                                .ToList();

                foreach (var player in players)
                    FavouritePlayers.Add(player.Username);
            }
        }

        // Switches the UI to show the list of favourite players instead of game archives
        private void ShowFavouritePlayers()
        {
            LoadFavouritePlayers();
            _isFavouritePlayersMode = true;

            OnPropertyChanged(nameof(IsArchiveModeVisible));
            OnPropertyChanged(nameof(IsFavouritePlayersVisible));
        }

        // When a favourite player is selected from the list, this property is set, which triggers loading that player's games
        private string _selectedFavouritePlayer;
        
        public string SelectedFavouritePlayer
        {
            get => _selectedFavouritePlayer;
            set
            {
                _selectedFavouritePlayer = value;
                OnPropertyChanged();

                if (!string.IsNullOrEmpty(value))
                {
                    Username = value;

                    _isFavouritePlayersMode = false;
                    OnPropertyChanged(nameof(IsArchiveModeVisible));
                    OnPropertyChanged(nameof(IsFavouritePlayersVisible));

                    SearchPlayerCommand.Execute(null);
                }
            }
        }

        // Constructor initializes the service, collections and commands for the ViewModel
        public GameBrowserViewModel()
        {
            _service = new ChessComService();

            Archives = new ObservableCollection<GameArchive>();

            OnlineGames = new ObservableCollection<ChessGame>();
            SavedGames = new ObservableCollection<ChessGame>();
            RecentGames = new ObservableCollection<ChessGame>();
            DisplayGames = new ObservableCollection<ChessGame>();
            FavouritePlayers = new ObservableCollection<string>();

            SearchPlayerCommand = new RelayCommand(async () => await SearchPlayer());
            LoadArchiveCommand = new RelayCommand(async () => await LoadArchive());

            ShowOnlineCommand = new RelayCommand(ShowOnlineGames);
            ShowSavedCommand = new RelayCommand(ShowSavedGames);
            DeleteSavedGameCommand = new RelayCommand<ChessGame>(DeleteSavedGame);
            ShowRecentCommand = new RelayCommand(ShowRecentGames);
            SaveFavouritePlayerCommand = new RelayCommand(SaveFavouritePlayer);
            DeleteFavouritePlayerCommand = new RelayCommand<string>(DeleteFavouritePlayer);
            ShowFavouritePlayersCommand = new RelayCommand(ShowFavouritePlayers);
            ShowLocalGamesCommand = new RelayCommand(ShowLocalGames);
        }

        // Fetches the game archives for the specified username from the Chess.com API and populates the Archives collection
        private async Task SearchPlayer()
        {
            _isFavouritePlayersMode = false;
            OnPropertyChanged(nameof(IsArchiveModeVisible));
            OnPropertyChanged(nameof(IsFavouritePlayersVisible));
            Archives.Clear();

            if (string.IsNullOrWhiteSpace(Username))
                return;

            try
            {
                var archives = await _service.GetGameArchives(Username.Trim());

                archives.Reverse();

                foreach (var archive in archives)
                {
                    Archives.Add(new GameArchive
                    {
                        Url = archive
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        // Loads the games from the selected archive (month) and populates the OnlineGames and DisplayGames collections
        private async Task LoadArchive()
        {
            OnlineGames.Clear();
            DisplayGames.Clear();

            var games = await _service.GetGamesFromArchive(SelectedArchive.Url, _username);

            foreach (var game in games)
            {
                OnlineGames.Add(game);
                DisplayGames.Add(game);
            }
        }


        private void DeleteSavedGame(ChessGame game)
        {
            if (game == null)
                return;

            using (var db = new ChessDbContext())
            {
                var entity = db.Games.FirstOrDefault(g => g.PGN == game.Pgn);

                if (entity != null)
                {
                    db.Games.Remove(entity);
                    db.SaveChanges();
                }
            }

            DisplayGames.Remove(game);
        }

        private void ShowLocalGames()
        {
            IsSavedMode = false;

            LoadLocalGames();
        }

        private void LoadLocalGames()
        {
            using (var db = new ChessDbContext())
            {
                LocalGames.Clear();
                DisplayGames.Clear();

                var games = db.LocalGames
                    .OrderByDescending(g => g.DatePlayed)
                    .ToList();

                foreach (var game in games)
                {
                    LocalGames.Add(game);

                    DisplayGames.Add(GameMapper.ToModel(game));
                }
            }
        }

    }
}