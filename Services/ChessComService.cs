using ChessProject.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChessProject.Services
{
    public class ChessComService
    {
        // HttpClient instance for making API requests
        private readonly HttpClient _client;

        // Constructor initializes HttpClient with a User-Agent header to avoid being blocked by the Chess.com API
        public ChessComService()
        {
            _client = new HttpClient();

            _client.DefaultRequestHeaders.Add(
                "User-Agent",
                "ChessPrepStudentProject"
            );
        }

        // Fetches the list of game archive URLs for a given username
        public async Task<List<string>> GetGameArchives(string username)
        {
            var response = await _client.GetAsync(
                $"https://api.chess.com/pub/player/{username}/games/archives"
            );

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            string json = await response.Content.ReadAsStringAsync();

            JObject data = JObject.Parse(json);

            var archives = new List<string>();

            foreach (var archive in data["archives"])
            {
                archives.Add(archive.ToString());
            }

            return archives;
        }

        // Fetches games from a given archive URL and returns a list of ChessGame objects
        public async Task<List<ChessGame>> GetGamesFromArchive(string archiveUrl, string username)
        {
            var response = await _client.GetAsync(archiveUrl);

            if (!response.IsSuccessStatusCode)
                return new List<ChessGame>();

            // Read the response content as a string
            string json = await response.Content.ReadAsStringAsync();

            // Parse the JSON response into a JObject for easier access to its properties
            JObject data = JObject.Parse(json);

            var games = new List<ChessGame>();

            var gameArray = data["games"];
            if (gameArray == null)
                return games;

            // Iterate through each game in the games array and extract relevant information to create ChessGame objects
            foreach (var game in gameArray)
            {
                var pgn = game["pgn"]?.ToString() ?? "";

                int whiteRating = (int?)game["white"]?["rating"] ?? 0;
                int blackRating = (int?)game["black"]?["rating"] ?? 0;

                string whiteName = game["white"]?["username"]?.ToString() ?? "Unknown";
                string blackName = game["black"]?["username"]?.ToString() ?? "Unknown";

                string result = game["white"]?["result"]?.ToString() ?? "";
                string date = game["end_time"]?.ToString() ?? "";

                int startingTime = 0;
                var timeControlString = game["time_control"]?.ToString() ?? "0";

                if (timeControlString.Contains("+"))
                    timeControlString = timeControlString.Split('+')[0];

                int.TryParse(timeControlString, out startingTime);

                // Create a new ChessGame object with the extracted information and add it to the list of games
                games.Add(new ChessGame
                {
                    White = whiteName,
                    Black = blackName,

                    WhiteElo = whiteRating,
                    BlackElo = blackRating,

                    PerspectivePlayer = username,

                    Result = result,
                    Date = date,

                    Pgn = pgn,

                    GameType = game["time_class"]?.ToString() ?? "unknown",

                    StartingTimeSeconds = startingTime
                });
            }

            return games;
        }
    }
}