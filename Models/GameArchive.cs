namespace ChessProject.Models
{
    public class GameArchive
    {
        public string Url { get; set; }

        // The display name is derived from the URL, which is in the format "https://api.chess.com/pub/player/{username}/games/{year}/{month}"
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(Url))
                    return "";

                var parts = Url.Split('/');

                string year = parts[parts.Length - 2];
                string month = parts[parts.Length - 1];

                return year + " / " + month;
            }
        }
    }
}