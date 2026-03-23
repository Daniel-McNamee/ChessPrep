using ChessProject.Data;
using ChessProject.Models;
using ChessProject.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChessProject.ViewModels
{
    public class GameBrowserViewModel : ViewModelBase
    {
        private readonly ChessComService _service;

        public ObservableCollection<GameArchive> Archives { get; }
        public ObservableCollection<ChessGame> OnlineGames { get; set; }
        public ObservableCollection<ChessGame> SavedGames { get; set; }
        public ObservableCollection<ChessGame> RecentGames { get; set; }

        public ObservableCollection<ChessGame> DisplayGames { get; set; }

        public ICommand SearchPlayerCommand { get; }

        public ICommand LoadArchiveCommand { get; }
        public ICommand ShowOnlineCommand { get; }
        public ICommand ShowSavedCommand { get; }
        public ICommand ShowRecentCommand { get; }

        public event Action<ChessGame> GameSelected;

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

        private void ShowOnlineGames()
        {
            DisplayGames.Clear();
            foreach (var game in OnlineGames)
                DisplayGames.Add(game);
        }

        private void ShowSavedGames()
        {
            LoadSavedGames();

            DisplayGames.Clear();
            foreach (var game in SavedGames)
                DisplayGames.Add(game);
        }

        private void ShowRecentGames()
        {
            LoadRecentGames();

            DisplayGames.Clear();
            foreach (var game in RecentGames)
                DisplayGames.Add(game);
        }

        private void LoadSelectedGame()
        {
            if (SelectedGame == null)
                return;

            GameSelected?.Invoke(SelectedGame);
        }

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

        public GameBrowserViewModel()
        {
            _service = new ChessComService();

            Archives = new ObservableCollection<GameArchive>();

            OnlineGames = new ObservableCollection<ChessGame>();
            SavedGames = new ObservableCollection<ChessGame>();
            RecentGames = new ObservableCollection<ChessGame>();
            DisplayGames = new ObservableCollection<ChessGame>();

            SearchPlayerCommand = new RelayCommand(async () => await SearchPlayer());
            LoadArchiveCommand = new RelayCommand(async () => await LoadArchive());

            ShowOnlineCommand = new RelayCommand(ShowOnlineGames);
            ShowSavedCommand = new RelayCommand(ShowSavedGames);
            ShowRecentCommand = new RelayCommand(ShowRecentGames);
        }

        private async Task SearchPlayer()
        {
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

        private async Task LoadArchive()
        {
            OnlineGames.Clear();
            DisplayGames.Clear();

            var games = await _service.GetGamesFromArchive(SelectedArchive.Url);

            foreach (var game in games)
            {
                OnlineGames.Add(game);
                DisplayGames.Add(game);
            }
        }
    }
}