using System.Windows.Controls;
using ChessProject.ViewModels;

namespace ChessProject.Views
{
    public partial class OpeningBrowserView : UserControl
    {
        public OpeningBrowserView()
        {
            InitializeComponent();
            DataContext = new OpeningBrowserViewModel(); // Set the DataContext to an instance of the OpeningBrowserViewModel
        }
    }
}
