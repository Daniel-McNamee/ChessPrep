using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.ViewModels
{
    // Represents a time control option for the chess game
    public class TimeControlOption : ViewModelBase
    {
        public string Display { get; set; } // "3+5"
        public int Minutes { get; set; }
        public int Increment { get; set; } 
        public string Category { get; set; } // Bullet, Blitz, Rapid

        // For UI selection

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
