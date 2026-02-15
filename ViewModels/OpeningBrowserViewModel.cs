using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ChessProject.Models;
using ChessProject.Services;

namespace ChessProject.ViewModels
{
    public class OpeningBrowserViewModel : ViewModelBase
    {
        // Properties:
        private List<Openings> _allOpenings; // Full dataset loaded from JSON

        // Events:
        public event Action<Openings> OpeningSelected; // Raised when user chooses an opening to load

        // Sorting state
        private string _currentSortField = "Opening"; // Current column being sorted
        private bool _isAscending = true; // Current sort direction

        public string CurrentSortField => _currentSortField;
        public bool IsSortingByOpening => _currentSortField == "Opening";
        public bool IsSortingByEco => _currentSortField == "ECO";
        public bool IsSortingByWhiteWin => _currentSortField == "WhiteWin";
        public bool IsSortingByBlackWin => _currentSortField == "BlackWin";
        public bool IsSortAscending => _isAscending;

        // Collections:
        // Openings displayed in the UI after filtering/sorting
        public ObservableCollection<Openings> FilteredOpenings { get; }

        // Search / Filter Properties
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters(); // Re-filter list whenever search text changes
            }
        }

        private string _selectedColour;
        public string SelectedColour
        {
            get => _selectedColour;
            set
            {
                _selectedColour = value;
                OnPropertyChanged();
                ApplyFilters(); // Re-filter list whenever colour filter changes
            }
        }

        // Selection:
        private Openings _selectedOpening;
        public Openings SelectedOpening
        {
            get => _selectedOpening;
            set
            {
                _selectedOpening = value;
                OnPropertyChanged();
                (LoadOpeningCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Enable/disable Load button
            }
        }

        // Commands:
        public ICommand LoadOpeningCommand { get; }

        // Constructors:
        public OpeningBrowserViewModel()
        {
            _allOpenings = new List<Openings>();
            FilteredOpenings = new ObservableCollection<Openings>();

            LoadOpeningCommand = new RelayCommand(
                LoadSelectedOpening,
                CanLoadOpening);

            SelectedColour = "All";

            LoadOpenings(); // Load dataset on startup
        }

        // Load Openings from JSON:
        private void LoadOpenings()
        {
            try
            {
                _allOpenings = OpeningDataService.LoadOpenings(); // Deserialize JSON into objects
                ApplyFilters(); // Populate UI collection
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                _allOpenings = new List<Openings>(); // Fail safely
            }
        }

        // Filtering / Searching / Sorting (LINQ):
        private void ApplyFilters()
        {
            if (_allOpenings == null)
                return;

            IEnumerable<Openings> query = _allOpenings; // Start from full dataset

            // Search
            if (!string.IsNullOrWhiteSpace(SearchText)) // Checks if string is "" (empty) or "  " (only spaces)
            {
                string search = SearchText.Trim().ToLower(); // Remove leading/trailing spaces and normalize case

                query = query
                    .Where(o =>
                        o.Opening.ToLower().Contains(search) ||
                        o.ECO.ToLower().Contains(search)) // Match opening name or ECO code
                    .OrderBy(o =>
                        o.Opening.ToLower().StartsWith(search) ? 0 : 1); // Prefer results starting with search text
            }

            // Colour filter
            if (!string.IsNullOrWhiteSpace(SelectedColour) &&
                SelectedColour != "All")
            {
                query = query.Where(o =>
                    o.Colour.Equals(SelectedColour, StringComparison.OrdinalIgnoreCase)); // Filter by side
            }

            // Sorting (disabled while searching)
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                query = ApplySorting(query); // Apply user-selected column sort
            }

            // Refresh ObservableCollection
            FilteredOpenings.Clear();
            foreach (var opening in query)
                FilteredOpenings.Add(opening);
        }

        private IEnumerable<Openings> ApplySorting(IEnumerable<Openings> query)
        {
            switch (_currentSortField)
            {
                case "Opening":
                    return _isAscending
                        ? query.OrderBy(o => o.Opening)
                        : query.OrderByDescending(o => o.Opening);

                case "WhiteWin":
                    return _isAscending
                        ? query.OrderBy(o => o.WhiteWinPercent)
                        : query.OrderByDescending(o => o.WhiteWinPercent);

                case "BlackWin":
                    return _isAscending
                        ? query.OrderBy(o => o.BlackWinPercent)
                        : query.OrderByDescending(o => o.BlackWinPercent);

                case "ECO":
                    return _isAscending
                        ? query.OrderBy(o => o.ECO)
                        : query.OrderByDescending(o => o.ECO);

                default:
                    return query;
            }
        }

        private void SetSort(string field)
        {
            if (_currentSortField == field)
            {
                _isAscending = !_isAscending; // Toggle direction if same column
            }
            else
            {
                _currentSortField = field; // Switch column
                _isAscending = true;       // Default to ascending
            }

            // Notify UI that sort state changed
            OnPropertyChanged(nameof(IsSortAscending));
            OnPropertyChanged(nameof(IsSortingByOpening));
            OnPropertyChanged(nameof(IsSortingByEco));
            OnPropertyChanged(nameof(IsSortingByWhiteWin));
            OnPropertyChanged(nameof(IsSortingByBlackWin));

            ApplyFilters(); // Reapply sorting to list
        }

        // Command Logic:
        private void LoadSelectedOpening()
        {
            OpeningSelected?.Invoke(SelectedOpening); // Notify BoardViewModel to load chosen opening
        }

        private bool CanLoadOpening()
        {
            return SelectedOpening != null; // Load button enabled only when an opening is selected
        }

        public ICommand SortByOpeningCommand =>
            new RelayCommand(() => SetSort("Opening"));

        public ICommand SortByWhiteWinCommand =>
            new RelayCommand(() => SetSort("WhiteWin"));

        public ICommand SortByBlackWinCommand =>
            new RelayCommand(() => SetSort("BlackWin"));

        public ICommand SortByEcoCommand =>
            new RelayCommand(() => SetSort("ECO"));
    }
}
