using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.Entities
{
    public class LocalGameEntity
    {
        public int Id { get; set; }

        public string WhitePlayer { get; set; }
        public string BlackPlayer { get; set; }

        public string Result { get; set; }

        public string PGN { get; set; }

        public DateTime DatePlayed { get; set; }
    }
}
