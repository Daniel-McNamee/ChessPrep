namespace ChessProject.ViewModels
{
    public class MoveViewModel : ViewModelBase
    {
        // Properties:
        public int Index { get; } // Move number in the sequence
        public string Notation { get; }

        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                _isCurrent = value;
                OnPropertyChanged(); // Notify the UI when this move becomes the current move
            }
        }

        // Constructors:
        public MoveViewModel(int index, string notation)
        {
            Index = index;
            Notation = notation;
        }
    }
}
