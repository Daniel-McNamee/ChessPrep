namespace ChessProject.Models
{
    public class GameArchive
    {
        public string Url { get; set; }

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