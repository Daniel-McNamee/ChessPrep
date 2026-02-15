using System;
using System.Windows.Input;

namespace ChessProject.ViewModels
{
    public class RelayCommand : ICommand
    {
        // Properties:
        private readonly Action _execute; // What to do
        private readonly Func<bool> _canExecute; // When allowed

        // Constructors:
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Methods:
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(); // If no condition allow, if condition evaluate it
        }

        public void Execute(object parameter)
        {
            _execute();  // Runs the assigned action when the command is triggered (e.g button click)
        }

        public event EventHandler CanExecuteChanged; // Notifies UI to recheck CanExecute (enable/disable controls)

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty); // Manually tell the UI that command state changed
        }
    }
}
