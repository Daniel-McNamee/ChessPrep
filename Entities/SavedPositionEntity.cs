using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.Entities
{
    public class SavedPositionEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string FEN { get; set; }

        public string Notes { get; set; }

        public DateTime DateSaved { get; set; }

        public string SourceGame { get; set; }
    }
}
