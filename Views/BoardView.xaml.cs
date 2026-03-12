using ChessProject.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ChessProject.Views
{
    public partial class BoardView : UserControl
    {
        public BoardView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is INotifyPropertyChanged vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

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
        
    }
}
