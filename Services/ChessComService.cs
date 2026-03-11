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
                "ChessPrepStudentProject/1.0 (contact: student@example.com)"
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

            foreach (var game in data["games"])
            {
                games.Add(new ChessGame
                {
                    White = game["white"]["username"].ToString(),
                    Black = game["black"]["username"].ToString(),

                    WhiteElo = (int)game["white"]["rating"],
                    BlackElo = (int)game["black"]["rating"],

                    Result = game["white"]["result"].ToString(),
                    Date = game["end_time"].ToString(),

                    TimeControl = game["time_control"].ToString(),

                    Pgn = game["pgn"].ToString()
                });
            }

            return games;
        }
    }
}