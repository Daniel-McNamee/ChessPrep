using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ChessProject.ViewModels
{
    // ViewModel for the Play tab, handling local game setup and navigation
    public class PlayViewModel : ViewModelBase 
    {
        // Reference to MainViewModel for navigation and starting games
        private readonly MainViewModel _main; 

        // Constructor
        public PlayViewModel(MainViewModel main)
        {
            _main = main; // Stores reference to MainViewModel to allow starting games and switching tabs

            StartLocalModeCommand = new RelayCommand(() =>
            {
                IsSetupVisible = true; // Show the game setup UI when the user clicks "Play Local"
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

        // Time control options
        public List<TimeControlOption> TimeOptions { get; } = new List<TimeControlOption>
        {
            new TimeControlOption { Display = "1 min", Minutes = 1, Increment = 0, Category = "Bullet" },
            new TimeControlOption { Display = "2 min", Minutes = 2, Increment = 0, Category = "Bullet" },
            new TimeControlOption { Display = "2 + 2", Minutes = 2, Increment = 2, Category = "Bullet" },

            new TimeControlOption { Display = "3 min", Minutes = 3, Increment = 0, Category = "Blitz" },
            new TimeControlOption { Display = "3 + 2", Minutes = 3, Increment = 2, Category = "Blitz" },
            new TimeControlOption { Display = "3 + 5", Minutes = 3, Increment = 5, Category = "Blitz" },
            new TimeControlOption { Display = "5 min", Minutes = 5, Increment = 0, Category = "Blitz" },
            new TimeControlOption { Display = "5 + 3", Minutes = 5, Increment = 3, Category = "Blitz" },

            new TimeControlOption { Display = "10 min", Minutes = 10, Increment = 0, Category = "Rapid" },
            new TimeControlOption { Display = "10 + 5", Minutes = 10, Increment = 5, Category = "Rapid" },
            new TimeControlOption { Display = "15 + 10", Minutes = 15, Increment = 10, Category = "Rapid" },
        };

        // Filtered lists for UI binding
        public IEnumerable<TimeControlOption> BulletTimes => TimeOptions.Where(t => t.Category == "Bullet");
        public IEnumerable<TimeControlOption> BlitzTimes => TimeOptions.Where(t => t.Category == "Blitz");
        public IEnumerable<TimeControlOption> RapidTimes => TimeOptions.Where(t => t.Category == "Rapid");


        // Selected time control
        private TimeControlOption _selectedTimeControl;
        public TimeControlOption SelectedTimeControl
        {
            get => _selectedTimeControl;
            set
            {
                _selectedTimeControl = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand StartLocalModeCommand { get; }
        public ICommand StartLocalGameCommand { get; }
        public ICommand SelectTimeControlCommand => new RelayCommand<TimeControlOption>(option =>
        {
            // clear previous
            foreach (var t in BulletTimes.Concat(BlitzTimes).Concat(RapidTimes))
                t.IsSelected = false;

            // set selected
            option.IsSelected = true;

            SelectedTimeControl = option;
        });

        // Start game
        private void StartGame()
        {
            // Use default names if none provided
            var White = string.IsNullOrWhiteSpace(WhitePlayer) ? "White" : WhitePlayer;
            var Black = string.IsNullOrWhiteSpace(BlackPlayer) ? "Black" : BlackPlayer;

            if (SelectedTimeControl == null)
                return;

            // Start the local game with the selected settings
            _main.BoardViewModel.StartNewLocalGame(
                White,
                Black,
                SelectedTimeControl
            );

            // Switch to board tab
            _main.SelectedTabIndex = 0;

            // Reset UI for next time
            IsSetupVisible = false;
        }
    }
}