using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.Entities
{
    public class RecentGameEntity
    {
        public int Id { get; set; }

        public string WhitePlayer { get; set; }
        public string BlackPlayer { get; set; }

        public int WhiteElo { get; set; }
        public int BlackElo { get; set; }

        public string Result { get; set; }

        public string PGN { get; set; }

        public DateTime DateViewed { get; set; }

        public string TimeControl { get; set; }
    }
}
