using System;

namespace ChessProject.ViewModels
{
    public class MoveViewModel : ViewModelBase
    {
        // Reference to the parent ViewModel for event handling
        public ViewModelBase CurrentView { get; set; }

        // Properties
        public int Index { get; }
        public string Notation { get; }

        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                _isCurrent = value;
                OnPropertyChanged();
            }
        }

        // Note management
        private string _note;
        public string Note
        {
            get => _note;
            set
            {
                if (_note != value)
                {
                    _note = value;
                    OnPropertyChanged();

                    NoteChanged?.Invoke(this); // Trigger save
                    OnPropertyChanged(nameof(HasNote));
                }
            }
        }

        // Indicates if a note exists for this move
        public bool HasNote => !string.IsNullOrWhiteSpace(Note);

        public event Action<MoveViewModel> NoteChanged;

        // Constructor
        public MoveViewModel(int index, string notation)
        {
            Index = index;
            Notation = notation;
            _note = "";
        }
    }
}
