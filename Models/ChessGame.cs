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

        public string TimeControl { get; set; }

        public string Pgn { get; set; }

        public override string ToString()
        {
            return $"{White} vs {Black} ({Result})";
        }
    }
}