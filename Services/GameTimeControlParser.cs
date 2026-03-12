using System.Text.RegularExpressions;

namespace ChessProject.Services
{
    public static class GameTimeControlParser
    {
        public static string GetCategory(string pgn)
        {
            var match = Regex.Match(pgn, @"\[TimeControl\s+""([^""]+)""\]");

            if (!match.Success)
                return "Unknown";

            string timeControl = match.Groups[1].Value;

            int baseSeconds = 0;

            if (timeControl.Contains("+"))
            {
                var parts = timeControl.Split('+');
                int.TryParse(parts[0], out baseSeconds);
            }
            else
            {
                int.TryParse(timeControl, out baseSeconds);
            }

            if (baseSeconds < 180)
                return "Bullet";

            if (baseSeconds < 600)
                return "Blitz";

            if (baseSeconds < 1800)
                return "Rapid";

            return "Classical";
        }
    }
}