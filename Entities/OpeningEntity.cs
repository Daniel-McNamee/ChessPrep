using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessProject.Entities
{
    public class OpeningEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ECO { get; set; }

        public string Moves { get; set; }

        public bool IsFavourite { get; set; }
    }
}
