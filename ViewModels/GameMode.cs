using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.ViewModels
{
    // Defines the different modes the application can be in, which affects how the board and UI behave.
    public enum GameMode
    {
        Opening,
        Replay,
        Local2Player,
        SavedPosition,
        Puzzle
    }
}
