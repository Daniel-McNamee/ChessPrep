using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChessProject.Services
{
    public static class PgnParser
    {
        public static List<string> ExtractMoves(string pgn)
        {
            // Remove metadata lines like [Event "..."]
            var cleaned = Regex.Replace(pgn, @"\[[^\]]*\]", "");

            // Remove clock annotations {[%clk ...]}
            cleaned = Regex.Replace(cleaned, @"\{[^}]*\}", "");

            // Normalize whitespace
            cleaned = cleaned.Replace("\n", " ").Replace("\r", " ");

            var tokens = cleaned.Split(' ');

            return tokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => !t.Contains(".")) // remove move numbers
                .Where(t => t != "1-0" && t != "0-1" && t != "1/2-1/2")
                .Select(t => t.Replace("+", "").Replace("#", ""))
                .ToList();
        }
    }
}