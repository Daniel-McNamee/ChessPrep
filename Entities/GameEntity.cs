using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.Entities
{
    public class GameEntity
    {
        public int Id { get; set; }

        public string WhitePlayer { get; set; }
        public string BlackPlayer { get; set; }

        public int WhiteElo { get; set; }
        public int BlackElo { get; set; }

        public string Result { get; set; }

        public string PGN { get; set; }

        public DateTime Date { get; set; }

        public string TimeControl { get; set; }

        public bool IsFavourite { get; set; }

        public virtual ICollection<MoveNoteEntity> MoveNotes { get; set; }
    }
}
