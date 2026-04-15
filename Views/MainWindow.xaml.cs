using ChessProject.ViewModels;
using System.Windows;

namespace ChessProject.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(); // Set the DataContext to an instance of MainViewModel
        }
    }
}
