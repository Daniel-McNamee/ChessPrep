using ChessProject.Models;
using ChessProject.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;

namespace ChessProject.ViewModels
{
    public class GameBrowserViewModel : ViewModelBase
    {
        private readonly ChessComService _service;

        public ObservableCollection<GameArchive> Archives { get; }

        public ObservableCollection<ChessGame> Games { get; }

        public ICommand SearchPlayerCommand { get; }

        public ICommand LoadArchiveCommand { get; }

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

        private void LoadSelectedGame()
        {
            if (SelectedGame == null)
                return;

            GameSelected?.Invoke(SelectedGame);
        }

        public GameBrowserViewModel()
        {
            _service = new ChessComService();

            Archives = new ObservableCollection<GameArchive>();
            Games = new ObservableCollection<ChessGame>();

            SearchPlayerCommand = new RelayCommand(async () => await SearchPlayer());

            LoadArchiveCommand = new RelayCommand(async () => await LoadArchive());
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
            Games.Clear();

            var games = await _service.GetGamesFromArchive(SelectedArchive.Url);

            foreach (var game in games)
            {
                Games.Add(game);
            }
        }
    }
}