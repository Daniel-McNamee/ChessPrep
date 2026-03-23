using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.Entities
{
    public class MoveNoteEntity
    {
        public int Id { get; set; }

        public int GameId { get; set; }

        public int MoveIndex { get; set; }

        public string Note { get; set; }

        public virtual GameEntity Game { get; set; }
    }
}
