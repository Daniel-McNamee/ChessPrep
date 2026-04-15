using ChessProject.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessProject.Views
{
    public partial class BoardView : UserControl
    {
        // Constructor
        public BoardView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            Loaded += (s, e) =>
            {
                this.Focus();
                Keyboard.Focus(this);
            };
        }

        // Subscribe to ViewModel property changes when DataContext is set
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is INotifyPropertyChanged vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        // Scroll move list to current move when it changes
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentMoveIndex")
            {
                Dispatcher.Invoke(() =>
                {
                    if (MoveList.Items.Count > 0)
                    {
                        var vm = DataContext as BoardViewModel;

                        if (vm != null && vm.CurrentMoveIndex < MoveList.Items.Count)
                        {
                            MoveList.ScrollIntoView(MoveList.Items[vm.CurrentMoveIndex]);
                        }
                    }
                });
            }
        }

        // Move navigation using arrow keys
        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            var vm = DataContext as BoardViewModel;
            if (vm == null)
                return;

            switch (e.Key)
            {
                // Prev move
                case Key.Up:
                    vm.PreviousCommand.Execute(null);
                    e.Handled = true;
                    break;

                // Next move
                case Key.Down:
                    vm.NextCommand.Execute(null);
                    e.Handled = true;
                    break;

                // Jump to beginning
                case Key.Left:
                    vm.ResetBoard();
                    e.Handled = true;
                    break;

                // Jump to end
                case Key.Right:
                    vm.JumpToEnd();
                    e.Handled = true;
                    break;
            }
        }

    }
}
