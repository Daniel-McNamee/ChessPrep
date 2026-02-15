using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ChessProject.Models;

namespace ChessProject.Services
{
    // Responsible for loading openings from JSON
    public static class OpeningDataService
    {
        private const string FilePath = "Data/openings.json";

        public static List<Openings> LoadOpenings()
        {
            if (!File.Exists(FilePath))
                return new List<Openings>();

            string json = File.ReadAllText(FilePath);

            return JsonConvert.DeserializeObject<List<Openings>>(json);
        }
    }
}
