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
        public string PerspectivePlayer { get; set; }

        public int WhiteElo { get; set; }
        public int BlackElo { get; set; }

        public string Result { get; set; }

        public string PGN { get; set; }

        public DateTime DateSaved { get; set; }

        public bool HasNotes { get; set; }

        public string TimeControl { get; set; }

        public bool IsFavourite { get; set; }

        public bool IsLocalGame { get; set; }

        public virtual ICollection<MoveNoteEntity> MoveNotes { get; set; }
    }
}
