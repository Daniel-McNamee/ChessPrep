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
        public int StartingTimeSeconds { get; set; }

        public string Pgn { get; set; }

        public override string ToString()
        {
            string resultText;

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

            return $"{White} ({WhiteElo}) vs {Black} ({BlackElo}) • {GameType} • {resultText}";
        }
    }
}