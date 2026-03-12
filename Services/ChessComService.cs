using ChessProject.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChessProject.Services
{
    public class ChessComService
    {
        private readonly HttpClient _client;

        public ChessComService()
        {
            _client = new HttpClient();

            _client.DefaultRequestHeaders.Add(
                "User-Agent",
                "ChessPrepStudentProject"
            );
        }

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

        public async Task<List<ChessGame>> GetGamesFromArchive(string archiveUrl)
        {
            var response = await _client.GetAsync(archiveUrl);

            if (!response.IsSuccessStatusCode)
                return new List<ChessGame>();

            string json = await response.Content.ReadAsStringAsync();

            JObject data = JObject.Parse(json);

            var games = new List<ChessGame>();

            var gameArray = data["games"];
            if (gameArray == null)
                return games;

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

                games.Add(new ChessGame
                {
                    White = whiteName,
                    Black = blackName,

                    WhiteElo = whiteRating,
                    BlackElo = blackRating,

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