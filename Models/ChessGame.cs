using System;
using static System.Net.Mime.MediaTypeNames;

namespace ChessProject.Models
{
    public class ChessGame
    {
        public string White { get; set; }
        public int WhiteElo { get; set; }

        public string Black { get; set; }
        public int BlackElo { get; set; }

        public string Result { get; set; }
        public string Date { get; set; }

        public string GameType { get; set; }
        public string PerspectivePlayer { get; set; }
        public int StartingTimeSeconds { get; set; }
        public bool HasNotes { get; set; }

        public string Pgn { get; set; }

        // Converts game result into player relative result (Win/Loss/Draw)
        public string GetResultForPlayer()
        {
            if (string.IsNullOrEmpty(PerspectivePlayer))
                return Result;

            bool isWhite = PerspectivePlayer.Equals(White, StringComparison.OrdinalIgnoreCase);

            string playerResult = isWhite ? Result : GetOppositeResult(Result);

            if (playerResult == "win")
                return "Win";

            if (IsDraw(playerResult))
                return "Draw";

            return "Loss";
        }

        // Converts from white result to get black result
        private string GetOppositeResult(string result)
        {
            switch (result)
            {
                case "win": return "lose";
                case "lose": return "win";

                case "checkmated":
                case "resigned":
                case "timeout":
                case "abandoned":
                    return "win";

                case "stalemate":
                case "repetition":
                case "agreed":
                case "insufficient":
                case "50move":
                case "timevsinsufficient":
                    return result;

                default:
                    return result;
            }
        }

        private bool IsDraw(string result)
        {
            return result == "stalemate"
                || result == "repetition"
                || result == "agreed"
                || result == "insufficient"
                || result == "50move"
                || result == "timevsinsufficient";
        }

        public override string ToString()
        {
            string resultText;

            if (!string.IsNullOrEmpty(PerspectivePlayer))
            {
                bool isWhite = PerspectivePlayer.Equals(White, StringComparison.OrdinalIgnoreCase);

                string playerResult = isWhite ? Result : GetOppositeResult(Result);

                if (playerResult == "win")
                    resultText = "Win";
                else if (IsDraw(playerResult))
                    resultText = "Draw";
                else
                    resultText = "Loss";
            }
            else
            {
                // fallback (white perspective)
                switch (Result)
                {
                    case "win":
                        resultText = "Win";
                        break;

                    case "checkmated":
                    case "resigned":
                    case "timeout":
                    case "lose":
                        resultText = "Loss";
                        break;

                    case "stalemate":
                    case "agreed":
                    case "repetition":
                    case "insufficient":
                    case "50move":
                        resultText = "Draw";
                        break;

                    default:
                        resultText = Result;
                        break;
                }
            }

            var text = $"{White} ({WhiteElo}) vs {Black} ({BlackElo}) • {GameType} • {resultText}";

            if (HasNotes)
                text += " (Annotated)";

            return text;
        }
    }
}