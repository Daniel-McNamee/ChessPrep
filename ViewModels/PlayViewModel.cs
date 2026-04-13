using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ChessProject.ViewModels
{
    public class PlayViewModel : ViewModelBase
    {
        // Reference to MainViewModel for navigation and starting games
        private readonly MainViewModel _main; 

        // Constructor
        public PlayViewModel(MainViewModel main)
        {
            _main = main;

            StartLocalModeCommand = new RelayCommand(() =>
            {
                IsSetupVisible = true;
            });

            StartLocalGameCommand = new RelayCommand(StartGame);
        }

        // UI State
        private bool _isSetupVisible;
        public bool IsSetupVisible
        {
            get => _isSetupVisible;
            set
            {
                _isSetupVisible = value;
                OnPropertyChanged();
            }
        }

        // Player setup
        public string WhitePlayer { get; set; } = "";
        public string BlackPlayer { get; set; } = "";

        public List<string> TimeControls { get; } = new List<string>
        {
            "2 min (Bullet)",
            "2 + 2 (Bullet)",
            "3 + 5 (Blitz)",
            "5 min (Blitz)",
            "10 min (Rapid)"
        };

        public string SelectedTimeControl { get; set; } = "10 min (Rapid)";

        // Commands
        public ICommand StartLocalModeCommand { get; }
        public ICommand StartLocalGameCommand { get; }

        // Start game
        private void StartGame()
        {
            _main.BoardViewModel.StartNewLocalGame(
                WhitePlayer,
                BlackPlayer,
                SelectedTimeControl
            );

            // Switch to board tab
            _main.SelectedTabIndex = 0;

            // Reset UI for next time
            IsSetupVisible = false;
        }
    }
}