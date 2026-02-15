using System.Collections.Generic;

namespace ChessProject.Models
{
    // Represents a single chess opening loaded from JSON
    public class Openings
    {
        public string Opening { get; set; }
        public string Colour { get; set; }
        public string ECO { get; set; }

        public double WhiteWinPercent { get; set; }
        public double DrawPercent { get; set; }
        public double BlackWinPercent { get; set; }

        public string Moves { get; set; }

        // List of SAN moves (e4, c5, Nf3, etc.)
        public List<string> MovesList { get; set; }
    }
}
