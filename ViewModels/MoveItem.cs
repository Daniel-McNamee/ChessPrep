using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.ViewModels
{
    // This class represents a move in a chess game, including its index, notation and any associated notes.
    public class MoveItem
    {
        public int MoveIndex { get; set; }
        public string MoveNotation { get; set; }
        public string Note { get; set; }
    }
}
