using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChessProject.Services
{
    public static class PgnParser
    {
        public static List<string> ExtractMoves(string pgn)
        {
            // Remove comments (including clocks)
            string cleaned = Regex.Replace(pgn, @"\{.*?\}", "");

            var lines = cleaned.Split('\n');

            string moveLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("1.")) ?? "";

            // Remove move numbers like "1." and "1..."
            moveLine = Regex.Replace(moveLine, @"\d+\.(\.\.)?", "");

            var tokens = moveLine.Split(' ');

            return tokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => t != "1-0" && t != "0-1" && t != "1/2-1/2")
                .Select(t => t.Replace("+", "").Replace("#", ""))
                .ToList();
        }

        public static List<string> ExtractClocks(string pgn)
        {
            var clocks = new List<string>();

            var matches = Regex.Matches(pgn, @"\[%clk\s+([0-9:\.]+)\]");

            foreach (Match match in matches)
            {
                clocks.Add(match.Groups[1].Value);
            }

            return clocks;
        }
    }
}