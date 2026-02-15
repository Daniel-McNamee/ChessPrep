using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChessProject.ViewModels
{
    /// Base class for all ViewModels in the application.
    /// Implements INotifyPropertyChanged so that UI elements bound to ViewModel properties automatically update when values change.
    public abstract class ViewModelBase : INotifyPropertyChanged
    {

        /// Called when a ViewModel property value changes, WPF binding listens to this to refresh the UI.
        public event PropertyChangedEventHandler PropertyChanged;


        /// Notifies the UI that a property has changed.
        /// CallerMemberName automatically supplies the property name.
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
