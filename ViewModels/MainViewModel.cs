using ChessProject.Models;
using System.Collections.Generic;

namespace ChessProject.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Properties:
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged(); // Notify the UI when active tab changes
            }
        }

        // Child ViewModels:
        public BoardViewModel BoardViewModel { get; } // ViewModel for the chessboard tab
        public OpeningBrowserViewModel OpeningBrowserViewModel { get; } // ViewModel for the opening browser tab
        public GameBrowserViewModel GameBrowserViewModel { get; }

        // Constructors:
        public MainViewModel()
        {
            SelectedTabIndex = 1; // Browser tab shown by default

            BoardViewModel = new BoardViewModel();
            OpeningBrowserViewModel = new OpeningBrowserViewModel();
            GameBrowserViewModel = new GameBrowserViewModel();

            // Subscribe to opening selection event from browser
            OpeningBrowserViewModel.OpeningSelected += OnOpeningSelected;
            GameBrowserViewModel.GameSelected += OnGameSelected;
        }


        // Event Handlers:
        // Called when OpeningBrowserViewModel raises OpeningSelected
        private void OnOpeningSelected(Openings opening)
        {
            if (opening == null)
                return;

            // Load selected opening into the board
            BoardViewModel.LoadOpening(opening);

            // Switch UI to the board tab and display the loaded opening
            SelectedTabIndex = 0;
        }

        private void OnGameSelected(ChessGame game)
        {
            BoardViewModel.LoadGame(game);
            SelectedTabIndex = 0;
        }

    }
}
