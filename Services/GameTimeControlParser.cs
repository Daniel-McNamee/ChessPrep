using System.Text.RegularExpressions;

namespace ChessProject.Services
{
    public static class GameTimeControlParser
    {
        // Parses the PGN string to determine the time control category (Bullet, Blitz, Rapid, Classical)
        public static string GetCategory(string pgn)
        {
            var match = Regex.Match(pgn, @"\[TimeControl\s+""([^""]+)""\]"); // Extracts the time control value from the PGN header

            if (!match.Success)
                return "Unknown";

            string timeControl = match.Groups[1].Value; // Time control is typically in the format "300+5" (base time + increment) or just "300" (base time)

            int baseSeconds = 0;

            if (timeControl.Contains("+")) // If there is an increment, we only care about the base time for categorization
            {
                var parts = timeControl.Split('+');
                int.TryParse(parts[0], out baseSeconds);
            }
            else
            {
                int.TryParse(timeControl, out baseSeconds); // If there is no increment, the entire value is the base time
            }

            if (baseSeconds < 180) // Less than 3 minutes
                return "Bullet";

            if (baseSeconds < 600) // Less than 10 minutes
                return "Blitz";

            if (baseSeconds < 1800) // Less than 30 minutes
                return "Rapid";

            return "Classical"; // 30 minutes or more is considered Classical
        }
    }
}