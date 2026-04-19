using System.Collections.Generic;
using System.Linq;

namespace ChessProject.Models
{
    public class Openings
    {
        public string Opening { get; set; }
        public string Colour { get; set; }
        public string ECO { get; set; }

        public double WhiteWinPercent { get; set; }
        public double DrawPercent { get; set; }
        public double BlackWinPercent { get; set; }

        public string Moves { get; set; }

        // Computed property to get a list of moves from the Moves string
        public List<string> MovesList =>
        Moves?
            .Split(' ')
            .Select(m => m.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(CleanMove)
            .Where(m => !string.IsNullOrEmpty(m))
            .ToList()
        ?? new List<string>();

        // Helper method to clean move strings
        private string CleanMove(string move)
        {
            if (string.IsNullOrWhiteSpace(move))
                return null;

            // Remove move numbers like "1.e4"
            move = System.Text.RegularExpressions.Regex.Replace(move, @"^\d+\.*", "");

            // Remove annotations
            move = move.Replace("+", "")
                       .Replace("#", "")
                       .Replace("!", "")
                       .Replace("?", "");

            return move.Trim();
        }

    }
}
